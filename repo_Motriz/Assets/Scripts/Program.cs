using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;

public class BodyMotionTracker : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Vector4 detectionAreaNorm = new Vector4(0.6f, 0.7f, 1.0f, 1.0f);
    [SerializeField] private int cameraIndex = 0;
    
    [Header("Performance Settings")]
    [SerializeField] private int frameSkip = 2; // Process every N frames (2 = half speed, 3 = third speed)
    [SerializeField] private int cameraWidth = 640; // Lower = faster (try 320, 640, or 1280)
    [SerializeField] private int cameraHeight = 480;
    [SerializeField] private bool drawSkeleton = true; // Disable for better performance
    
    [Header("Model Files")]
    [SerializeField] private string protoFile = "pose_deploy_linevec.prototxt";
    [SerializeField] private string weightsFile = "pose_iter_440000.caffemodel";
    
    [Header("Display Settings")]
    [SerializeField] private UnityEngine.UI.RawImage displayImage;
    [SerializeField] private bool showDebugInfo = true;
    
    private int frameCounter = 0;
    private OpenCvSharp.Point[] lastDetectedPoints;
    
    private VideoCapture cap;
    private Net net;
    private Mat currentFrame;
    private Texture2D displayTexture;
    
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

    // Events for foot detection
    public event Action OnLeftFootDetected;
    public event Action OnRightFootDetected;

    void Start()
    {
        InitializeCamera();
        InitializePoseDetection();
    }

    private void InitializeCamera()
    {
        cap = new VideoCapture(cameraIndex);

        if (cap == null || !cap.IsOpened())
        {
            Debug.LogError($"Error: No se pudo abrir la cámara en índice {cameraIndex}");
            return;
        }

        // Set camera resolution for better performance
        cap.Set(VideoCaptureProperties.FrameWidth, cameraWidth);
        cap.Set(VideoCaptureProperties.FrameHeight, cameraHeight);
        cap.Set(VideoCaptureProperties.Fps, 30);

        Debug.Log("Cámara inicializada correctamente");
        
        // Get camera dimensions and create texture
        currentFrame = new Mat();
        cap.Read(currentFrame);
        if (!currentFrame.Empty())
        {
            Debug.Log($"Resolución de cámara: {currentFrame.Width}x{currentFrame.Height}");
            displayTexture = new Texture2D(currentFrame.Width, currentFrame.Height, TextureFormat.RGB24, false);
            if (displayImage != null)
            {
                displayImage.texture = displayTexture;
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
            Debug.Log("Modelo de pose detection cargado correctamente");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"No se pudo cargar el modelo de pose: {ex.Message}");
            Debug.LogWarning("El programa funcionará pero sin detección de pose.");
            net = null;
        }
    }

    void Update()
    {
        if (cap == null || !cap.IsOpened())
            return;

        bool ret = cap.Read(currentFrame);
        if (!ret || currentFrame.Empty())
        {
            Debug.LogWarning("No se pudo leer frame");
            return;
        }

        ProcessFrame();
    }

    private void ProcessFrame()
    {
        frameCounter++;
        
        int h = currentFrame.Height;
        int w = currentFrame.Width;

        Mat displayMat = currentFrame.Clone();

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

        Cv2.PutText(displayMat, "Detection Area",
            new OpenCvSharp.Point(xMinPx + 5, yMinPx + 25),
            HersheyFonts.HersheySimplex, 0.6,
            new Scalar(0, 255, 0), 2);

        // Only process pose detection every N frames for performance
        bool shouldProcessPose = (frameCounter % frameSkip == 0);
        
        bool footTouched = false;
        if (net != null && shouldProcessPose)
        {
            try
            {
                lastDetectedPoints = DetectPose(currentFrame);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error en detección: {ex.Message}");
            }
        }

        // Use last detected points for drawing and checking
        if (lastDetectedPoints != null && lastDetectedPoints.Length > 0)
        {
            bool leftFootDetected = false;
            bool rightFootDetected = false;
            
            // Check left ankle
            if (LEFT_ANKLE < lastDetectedPoints.Length && lastDetectedPoints[LEFT_ANKLE].X >= 0)
            {
                float normX = (float)lastDetectedPoints[LEFT_ANKLE].X / w;
                float normY = (float)lastDetectedPoints[LEFT_ANKLE].Y / h;

                if (IsInDetectionArea(normX, normY))
                {
                    if (showDebugInfo)
                        Debug.Log($"Left foot in area! Position: ({normX:F2}, {normY:F2})");

                    leftFootDetected = true;
                    OnLeftFootDetected?.Invoke();

                    Cv2.Circle(displayMat, lastDetectedPoints[LEFT_ANKLE], 8,
                        new Scalar(0, 0, 255), -1);
                }
                else
                {
                    // Draw in different color when detected but not in area
                    Cv2.Circle(displayMat, lastDetectedPoints[LEFT_ANKLE], 6,
                        new Scalar(255, 255, 0), -1);
                }
            }

            // Check right ankle (REMOVED the !footTouched condition)
            if (RIGHT_ANKLE < lastDetectedPoints.Length &&
                lastDetectedPoints[RIGHT_ANKLE].X >= 0)
            {
                float normX = (float)lastDetectedPoints[RIGHT_ANKLE].X / w;
                float normY = (float)lastDetectedPoints[RIGHT_ANKLE].Y / h;

                if (IsInDetectionArea(normX, normY))
                {
                    if (showDebugInfo)
                        Debug.Log($"Right foot in area! Position: ({normX:F2}, {normY:F2})");

                    rightFootDetected = true;
                    OnRightFootDetected?.Invoke();

                    Cv2.Circle(displayMat, lastDetectedPoints[RIGHT_ANKLE], 8,
                        new Scalar(0, 0, 255), -1);
                }
                else
                {
                    // Draw in different color when detected but not in area
                    Cv2.Circle(displayMat, lastDetectedPoints[RIGHT_ANKLE], 6,
                        new Scalar(255, 255, 0), -1);
                }
            }

            // Draw skeleton if enabled
            if (drawSkeleton)
            {
                DrawSkeleton(displayMat, lastDetectedPoints);
            }
        }

        // Convert Mat to Texture2D and display
        UpdateTexture(displayMat);
        displayMat.Dispose();
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
            if (displayImage != null)
            {
                displayImage.texture = displayTexture;
            }
        }

        // Convert BGR to RGB
        Mat rgbMat = new Mat();
        Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.BGR2RGB);

        // Flip vertically for Unity's coordinate system
        Cv2.Flip(rgbMat, rgbMat, FlipMode.X);

        // Load to texture
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
        
        Debug.Log("Recursos liberados correctamente");
    }
}