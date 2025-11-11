using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;

public class BodyMotionTracker2 : MonoBehaviour
{
    [Header("Target Footprint")]
    [SerializeField] private RectTransform targetFootprintImage; // The footprint UI Image to track
    [SerializeField] private Canvas targetCanvas; // The canvas being projected

    [Header("Camera Setup")]
    [SerializeField] private int physicalCameraIndex = 0; // Physical camera looking down at floor
    [SerializeField] private Camera projectorCamera; // Unity camera that renders to projector

    [Header("Detection Settings")]
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(150f, 150f); // Size in screen pixels
    [SerializeField] private float detectionCooldown = 1f; // Prevent rapid re-detection

    [Header("Performance Settings")]
    [SerializeField] private int frameSkip = 2;
    [SerializeField] private int cameraWidth = 640;
    [SerializeField] private int cameraHeight = 480;
    [SerializeField] private bool drawSkeleton = true;

    [Header("Model Files")]
    [SerializeField] private string protoFile = "pose_deploy_linevec.prototxt";
    [SerializeField] private string weightsFile = "pose_iter_440000.caffemodel";

    [Header("Debug Display (Optional)")]
    [SerializeField] private UnityEngine.UI.RawImage debugDisplay;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showDebugWindow = false;

    private int frameCounter = 0;
    private OpenCvSharp.Point[] lastDetectedPoints;
    private float lastDetectionTime = -999f;

    private VideoCapture cap;
    private Net net;
    private Mat currentFrame;
    private Texture2D displayTexture;
    private Vector4 detectionAreaNorm; // Normalized detection area

    // COCO body parts indices
    private const int LEFT_ANKLE = 13;
    private const int RIGHT_ANKLE = 10;

    private readonly string[] BODY_PARTS = {
        "Nose", "Neck", "RShoulder", "RElbow", "RWrist",
        "LShoulder", "LElbow", "LWrist", "RHip", "RKnee",
        "RAnkle", "LHip", "LKnee", "LAnkle", "REye",
        "LEye", "REar", "LEar"
    };

    private readonly (int, int)[] POSE_PAIRS = {
        (1,2), (1,5), (2,3), (3,4), (5,6), (6,7),
        (1,8), (8,9), (9,10), (1,11), (11,12), (12,13),
        (1,0), (0,14), (14,16), (0,15), (15,17)
    };

    // Event for foot detection - this is what triggers the footprint to move
    public event Action OnFootDetectedInArea;

    void Start()
    {
        if (projectorCamera == null)
        {
            projectorCamera = Camera.main;
        }

        if (targetFootprintImage == null)
        {
            Debug.LogError("Target footprint image not assigned!");
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = targetFootprintImage.GetComponentInParent<Canvas>();
        }

        InitializeCamera();
        InitializePoseDetection();
    }

    private void InitializeCamera()
    {
        cap = new VideoCapture(physicalCameraIndex);

        if (cap == null || !cap.IsOpened())
        {
            Debug.LogError($"Error: Could not open camera at index {physicalCameraIndex}");
            return;
        }

        cap.Set(VideoCaptureProperties.FrameWidth, cameraWidth);
        cap.Set(VideoCaptureProperties.FrameHeight, cameraHeight);
        cap.Set(VideoCaptureProperties.Fps, 30);

        Debug.Log("Camera initialized successfully");

        currentFrame = new Mat();
        cap.Read(currentFrame);
        if (!currentFrame.Empty())
        {
            Debug.Log($"Camera resolution: {currentFrame.Width}x{currentFrame.Height}");

            if (debugDisplay != null)
            {
                displayTexture = new Texture2D(currentFrame.Width, currentFrame.Height, TextureFormat.RGB24, false);
                debugDisplay.texture = displayTexture;
            }
        }
    }

    private void InitializePoseDetection()
    {
        try
        {
            string protoPath = System.IO.Path.Combine(Application.streamingAssetsPath, protoFile);
            string weightsPath = System.IO.Path.Combine(Application.streamingAssetsPath, weightsFile);

            net = CvDnn.ReadNetFromCaffe(protoPath, weightsPath);
            Debug.Log("Pose detection model loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not load pose model: {ex.Message}");
            net = null;
        }
    }

    void Update()
    {
        if (cap == null || !cap.IsOpened())
            return;

        // Update detection area to match current footprint position
        UpdateDetectionAreaFromFootprint();

        bool ret = cap.Read(currentFrame);
        if (!ret || currentFrame.Empty())
        {
            Debug.LogWarning("Could not read frame");
            return;
        }

        ProcessFrame();
    }

    private void UpdateDetectionAreaFromFootprint()
    {
        if (targetFootprintImage == null || projectorCamera == null)
            return;

        // Get the footprint's screen position
        Vector2 screenPos = GetScreenPositionOfFootprint();

        // Calculate detection box centered on footprint
        float halfWidth = detectionBoxSize.x / 2f;
        float halfHeight = detectionBoxSize.y / 2f;

        float xMin = screenPos.x - halfWidth;
        float xMax = screenPos.x + halfWidth;
        float yMin = screenPos.y - halfHeight;
        float yMax = screenPos.y + halfHeight;

        // Normalize to 0-1 range for the physical camera frame
        // Map screen coordinates to camera coordinates
        // The physical camera should be calibrated to match the projected area
        detectionAreaNorm = new Vector4(
            xMin / Screen.width,
            1f - (yMax / Screen.height), // Flip Y (OpenCV uses top-left origin)
            xMax / Screen.width,
            1f - (yMin / Screen.height)
        );

        // Clamp to valid range
        detectionAreaNorm.x = Mathf.Clamp01(detectionAreaNorm.x);
        detectionAreaNorm.y = Mathf.Clamp01(detectionAreaNorm.y);
        detectionAreaNorm.z = Mathf.Clamp01(detectionAreaNorm.z);
        detectionAreaNorm.w = Mathf.Clamp01(detectionAreaNorm.w);
    }

    private Vector2 GetScreenPositionOfFootprint()
    {
        // Handle different canvas render modes
        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return targetFootprintImage.position;
        }
        else if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera canvasCam = targetCanvas.worldCamera ?? projectorCamera;
            return RectTransformUtility.WorldToScreenPoint(canvasCam, targetFootprintImage.position);
        }
        else // WorldSpace
        {
            return projectorCamera.WorldToScreenPoint(targetFootprintImage.position);
        }
    }

    private void ProcessFrame()
    {
        frameCounter++;

        int h = currentFrame.Height;
        int w = currentFrame.Width;

        Mat displayMat = null;

        // Only create display mat if needed for debug
        if (debugDisplay != null || showDebugWindow)
        {
            displayMat = currentFrame.Clone();

            // Draw detection area rectangle
            int xMinPx = (int)(detectionAreaNorm.x * w);
            int yMinPx = (int)(detectionAreaNorm.y * h);
            int xMaxPx = (int)(detectionAreaNorm.z * w);
            int yMaxPx = (int)(detectionAreaNorm.w * h);

            Cv2.Rectangle(displayMat,
                new OpenCvSharp.Point(xMinPx, yMinPx),
                new OpenCvSharp.Point(xMaxPx, yMaxPx),
                new Scalar(0, 255, 0),
                2);

            Cv2.PutText(displayMat, "Target Area",
                new OpenCvSharp.Point(xMinPx + 5, yMinPx + 25),
                HersheyFonts.HersheySimplex, 0.6,
                new Scalar(0, 255, 0), 2);
        }

        // Process pose detection every N frames
        bool shouldProcessPose = (frameCounter % frameSkip == 0);

        if (net != null && shouldProcessPose)
        {
            try
            {
                lastDetectedPoints = DetectPose(currentFrame);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Detection error: {ex.Message}");
            }
        }

        // Check for foot detection in target area
        if (lastDetectedPoints != null && lastDetectedPoints.Length > 0)
        {
            bool footInArea = false;

            // Check left ankle
            if (LEFT_ANKLE < lastDetectedPoints.Length && lastDetectedPoints[LEFT_ANKLE].X >= 0)
            {
                float normX = (float)lastDetectedPoints[LEFT_ANKLE].X / w;
                float normY = (float)lastDetectedPoints[LEFT_ANKLE].Y / h;

                if (IsInDetectionArea(normX, normY))
                {
                    footInArea = true;

                    if (displayMat != null)
                    {
                        Cv2.Circle(displayMat, lastDetectedPoints[LEFT_ANKLE], 8,
                            new Scalar(0, 255, 0), -1); // Green when in area
                    }
                }
                else if (displayMat != null)
                {
                    Cv2.Circle(displayMat, lastDetectedPoints[LEFT_ANKLE], 6,
                        new Scalar(255, 255, 0), -1); // Yellow when detected but not in area
                }
            }

            // Check right ankle
            if (RIGHT_ANKLE < lastDetectedPoints.Length && lastDetectedPoints[RIGHT_ANKLE].X >= 0)
            {
                float normX = (float)lastDetectedPoints[RIGHT_ANKLE].X / w;
                float normY = (float)lastDetectedPoints[RIGHT_ANKLE].Y / h;

                if (IsInDetectionArea(normX, normY))
                {
                    footInArea = true;

                    if (displayMat != null)
                    {
                        Cv2.Circle(displayMat, lastDetectedPoints[RIGHT_ANKLE], 8,
                            new Scalar(0, 255, 0), -1);
                    }
                }
                else if (displayMat != null)
                {
                    Cv2.Circle(displayMat, lastDetectedPoints[RIGHT_ANKLE], 6,
                        new Scalar(255, 255, 0), -1);
                }
            }

            // Trigger event if foot detected and cooldown has passed
            if (footInArea && Time.time - lastDetectionTime > detectionCooldown)
            {
                if (showDebugInfo)
                    Debug.Log("FOOT DETECTED IN TARGET AREA! Triggering event.");

                lastDetectionTime = Time.time;
                OnFootDetectedInArea?.Invoke();
            }

            // Draw skeleton if enabled
            if (drawSkeleton && displayMat != null)
            {
                DrawSkeleton(displayMat, lastDetectedPoints);
            }
        }

        // Update debug displays if enabled
        if (displayMat != null)
        {
            if (showDebugWindow)
            {
                Cv2.ImShow("Foot Detection Debug", displayMat);
                Cv2.WaitKey(1);
            }

            if (debugDisplay != null)
            {
                UpdateTexture(displayMat);
            }

            displayMat.Dispose();
        }
    }

    private bool IsInDetectionArea(float x, float y)
    {
        return x >= detectionAreaNorm.x && x <= detectionAreaNorm.z &&
               y >= detectionAreaNorm.y && y <= detectionAreaNorm.w;
    }

    private OpenCvSharp.Point[] DetectPose(Mat frame)
    {
        if (net == null)
            return new OpenCvSharp.Point[BODY_PARTS.Length];

        int inWidth = 368;
        int inHeight = 368;
        float threshold = 0.1f;

        Mat inputBlob = CvDnn.BlobFromImage(frame, 1.0 / 255,
            new OpenCvSharp.Size(inWidth, inHeight), new Scalar(0, 0, 0), false, false);

        net.SetInput(inputBlob);
        Mat output = net.Forward();

        int H = output.Size(2);
        int W = output.Size(3);

        OpenCvSharp.Point[] points = new OpenCvSharp.Point[BODY_PARTS.Length];

        for (int i = 0; i < BODY_PARTS.Length; i++)
        {
            Mat probMap = new Mat(H, W, MatType.CV_32FC1);

            unsafe
            {
                IntPtr srcPtr = output.Ptr(0, i);
                float* src = (float*)srcPtr.ToPointer();
                float* dst = (float*)probMap.Data.ToPointer();
                int totalElements = H * W;

                for (int j = 0; j < totalElements; j++)
                {
                    dst[j] = src[j];
                }
            }

            OpenCvSharp.Point minLoc, maxLoc;
            Cv2.MinMaxLoc(probMap, out _, out double prob, out minLoc, out maxLoc);

            if (prob > threshold)
            {
                int x = (int)((frame.Width * maxLoc.X) / W);
                int y = (int)((frame.Height * maxLoc.Y) / H);
                points[i] = new OpenCvSharp.Point(x, y);
            }
            else
            {
                points[i] = new OpenCvSharp.Point(-1, -1);
            }

            probMap.Dispose();
        }

        inputBlob.Dispose();
        output.Dispose();

        return points;
    }

    private void DrawSkeleton(Mat image, OpenCvSharp.Point[] points)
    {
        foreach (var (partA, partB) in POSE_PAIRS)
        {
            if (partA < points.Length && partB < points.Length &&
                points[partA].X >= 0 && points[partB].X >= 0)
            {
                Cv2.Line(image, points[partA], points[partB],
                    new Scalar(245, 66, 230), 2);
            }
        }

        foreach (var point in points)
        {
            if (point.X >= 0)
            {
                Cv2.Circle(image, point, 4, new Scalar(245, 117, 66), -1);
            }
        }
    }

    private void UpdateTexture(Mat mat)
    {
        if (displayTexture == null || mat.Width != displayTexture.width || mat.Height != displayTexture.height)
        {
            displayTexture = new Texture2D(mat.Width, mat.Height, TextureFormat.RGB24, false);
            if (debugDisplay != null)
            {
                debugDisplay.texture = displayTexture;
            }
        }

        Mat rgbMat = new Mat();
        Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.BGR2RGB);
        Cv2.Flip(rgbMat, rgbMat, FlipMode.X);

        displayTexture.LoadRawTextureData(rgbMat.Data, rgbMat.Width * rgbMat.Height * 3);
        displayTexture.Apply();

        rgbMat.Dispose();
    }

    void OnDestroy()
    {
        cap?.Release();
        net?.Dispose();
        currentFrame?.Dispose();

        if (displayTexture != null)
            Destroy(displayTexture);

        Cv2.DestroyAllWindows();

        Debug.Log("Resources released successfully");
    }
}