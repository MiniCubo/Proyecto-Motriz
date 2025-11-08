using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a detection zone created at a marker location
/// </summary>
[System.Serializable]
public class DetectionBox
{
    public int id;
    public Vector2 center;
    public Vector2 size;
    public Rect roi;
    public bool isOccupied;
    
    public DetectionBox(int id, Vector2 center, Vector2 size)
    {
        this.id = id;
        this.center = center;
        this.size = size;
        this.roi = new Rect(center.x - size.x / 2, center.y - size.y / 2, size.x, size.y);
        this.isOccupied = false;
    }
}

/// <summary>
/// Dynamic marker detection and tracking system for Unity
/// Works with webcam input to detect markers and track player position
/// </summary>
public class MarkerDetector : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private int cameraIndex = 0;
    [SerializeField] private int cameraWidth = 1920;
    [SerializeField] private int cameraHeight = 1080;
    [SerializeField] private int targetFPS = 30;
    
    [Header("Marker Detection")]
    [SerializeField] private Texture2D markerTemplate;
    [Tooltip("Minimum similarity required to detect marker (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float markerThreshold = 0.7f;
    [SerializeField] private float minMarkerDistance = 100f;
    
    [Header("Detection Box Settings")]
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(150, 150);
    [Tooltip("Minimum pixel difference to consider motion")]
    [SerializeField] private float motionThreshold = 0.1f;
    [Tooltip("Percentage of box that must change to be occupied")]
    [Range(0f, 1f)]
    [SerializeField] private float occupancyThreshold = 0.15f;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugWindow = true;
    [SerializeField] private bool drawDetectionBoxes = true;
    
    // Private members
    private WebCamTexture webcamTexture;
    private Texture2D currentFrame;
    private Texture2D backgroundFrame;
    private Texture2D processedFrame;
    private Color[] currentPixels;
    private Color[] backgroundPixels;
    
    private List<DetectionBox> detectionBoxes = new List<DetectionBox>();
    private int nextBoxId = 0;
    private bool backgroundCaptured = false;
    private bool markersInitialized = false;
    
    // Events for Unity integration
    public System.Action<List<DetectionBox>> OnBoxesUpdated;
    public System.Action<DetectionBox> OnBoxOccupied;
    public System.Action<DetectionBox> OnBoxVacated;
    
    void Start()
    {
        InitializeCamera();
        
        if (markerTemplate == null)
        {
            Debug.LogError("Marker template not assigned! Please assign a footprint icon texture.");
        }
    }
    
    void InitializeCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("No camera detected!");
            return;
        }
        
        string deviceName = devices[Mathf.Min(cameraIndex, devices.Length - 1)].name;
        webcamTexture = new WebCamTexture(deviceName, cameraWidth, cameraHeight, targetFPS);
        
        currentFrame = new Texture2D(cameraWidth, cameraHeight, TextureFormat.RGB24, false);
        processedFrame = new Texture2D(cameraWidth, cameraHeight, TextureFormat.RGB24, false);
        
        webcamTexture.Play();
        
        Debug.Log($"Camera initialized: {deviceName} at {cameraWidth}x{cameraHeight}");
    }
    
    void Update()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
            return;
        
        // Update current frame
        currentFrame.SetPixels(webcamTexture.GetPixels());
        currentFrame.Apply();
        currentPixels = currentFrame.GetPixels();
        
        // Handle keyboard input
        HandleInput();
        
        // Update detection boxes if initialized
        if (markersInitialized && detectionBoxes.Count > 0)
        {
            UpdateDetectionBoxes();
        }
        
        // Update visualization
        if (drawDetectionBoxes)
        {
            DrawVisualization();
        }
    }
    
    void HandleInput()
    {
        // Capture background
        if (Input.GetKeyDown(KeyCode.B))
        {
            CaptureBackground();
        }
        
        // Detect markers and create boxes
        if (Input.GetKeyDown(KeyCode.M))
        {
            DetectMarkersAndCreateBoxes();
        }
        
        // Reset boxes
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetDetectionBoxes();
        }
    }
    
    public void CaptureBackground()
    {
        backgroundFrame = new Texture2D(currentFrame.width, currentFrame.height);
        backgroundFrame.SetPixels(currentPixels);
        backgroundFrame.Apply();
        backgroundPixels = backgroundFrame.GetPixels();
        backgroundCaptured = true;
        
        Debug.Log("Background captured!");
    }
    
    public void DetectMarkersAndCreateBoxes()
    {
        if (markerTemplate == null)
        {
            Debug.LogError("No marker template assigned!");
            return;
        }
        
        List<Vector2> markers = FindMarkers();
        
        if (markers.Count > 0)
        {
            CreateDetectionBoxes(markers);
            markersInitialized = true;
            Debug.Log($"Created {detectionBoxes.Count} detection boxes");
        }
        else
        {
            Debug.LogWarning("No markers found! Make sure markers are visible and template matches.");
        }
    }
    
    List<Vector2> FindMarkers()
    {
        List<Vector2> markers = new List<Vector2>();
        
        // Simple template matching using normalized cross-correlation
        Color[] templatePixels = markerTemplate.GetPixels();
        int templateWidth = markerTemplate.width;
        int templateHeight = markerTemplate.height;
        
        int frameWidth = currentFrame.width;
        int frameHeight = currentFrame.height;
        
        // Scan the frame (with step size for performance)
        int step = 5; // Adjust for performance vs accuracy
        
        for (int y = 0; y < frameHeight - templateHeight; y += step)
        {
            for (int x = 0; x < frameWidth - templateWidth; x += step)
            {
                float similarity = CalculateTemplateSimilarity(x, y, templatePixels, templateWidth, templateHeight);
                
                if (similarity >= markerThreshold)
                {
                    Vector2 center = new Vector2(x + templateWidth / 2f, y + templateHeight / 2f);
                    markers.Add(center);
                }
            }
        }
        
        // Remove duplicates
        markers = RemoveDuplicateMarkers(markers);
        
        return markers;
    }
    
    float CalculateTemplateSimilarity(int startX, int startY, Color[] templatePixels, int templateWidth, int templateHeight)
    {
        float sum = 0f;
        int count = 0;
        
        for (int ty = 0; ty < templateHeight; ty++)
        {
            for (int tx = 0; tx < templateWidth; tx++)
            {
                int frameIndex = (startY + ty) * currentFrame.width + (startX + tx);
                int templateIndex = ty * templateWidth + tx;
                
                if (frameIndex >= 0 && frameIndex < currentPixels.Length)
                {
                    Color frameColor = currentPixels[frameIndex];
                    Color templateColor = templatePixels[templateIndex];
                    
                    // Calculate grayscale similarity
                    float frameGray = frameColor.grayscale;
                    float templateGray = templateColor.grayscale;
                    
                    sum += 1f - Mathf.Abs(frameGray - templateGray);
                    count++;
                }
            }
        }
        
        return count > 0 ? sum / count : 0f;
    }
    
    List<Vector2> RemoveDuplicateMarkers(List<Vector2> markers)
    {
        List<Vector2> filtered = new List<Vector2>();
        
        foreach (Vector2 marker in markers)
        {
            bool isDuplicate = false;
            
            foreach (Vector2 existing in filtered)
            {
                float distance = Vector2.Distance(marker, existing);
                if (distance < minMarkerDistance)
                {
                    isDuplicate = true;
                    break;
                }
            }
            
            if (!isDuplicate)
            {
                filtered.Add(marker);
            }
        }
        
        return filtered;
    }
    
    void CreateDetectionBoxes(List<Vector2> markers)
    {
        detectionBoxes.Clear();
        nextBoxId = 0;
        
        foreach (Vector2 marker in markers)
        {
            DetectionBox box = new DetectionBox(nextBoxId, marker, detectionBoxSize);
            detectionBoxes.Add(box);
            nextBoxId++;
        }
        
        OnBoxesUpdated?.Invoke(detectionBoxes);
    }
    
    public void ResetDetectionBoxes()
    {
        detectionBoxes.Clear();
        markersInitialized = false;
        Debug.Log("Detection boxes reset");
    }
    
    void UpdateDetectionBoxes()
    {
        if (!backgroundCaptured)
        {
            // Without background, use simple edge detection
            UpdateBoxesWithoutBackground();
        }
        else
        {
            // With background, use motion detection
            UpdateBoxesWithBackground();
        }
        
        OnBoxesUpdated?.Invoke(detectionBoxes);
    }
    
    void UpdateBoxesWithoutBackground()
    {
        foreach (DetectionBox box in detectionBoxes)
        {
            bool wasOccupied = box.isOccupied;
            box.isOccupied = DetectMotionInBox(box, null);
            
            // Trigger events
            if (box.isOccupied && !wasOccupied)
                OnBoxOccupied?.Invoke(box);
            else if (!box.isOccupied && wasOccupied)
                OnBoxVacated?.Invoke(box);
        }
    }
    
    void UpdateBoxesWithBackground()
    {
        foreach (DetectionBox box in detectionBoxes)
        {
            bool wasOccupied = box.isOccupied;
            box.isOccupied = DetectMotionInBox(box, backgroundPixels);
            
            // Trigger events
            if (box.isOccupied && !wasOccupied)
                OnBoxOccupied?.Invoke(box);
            else if (!box.isOccupied && wasOccupied)
                OnBoxVacated?.Invoke(box);
        }
    }
    
    bool DetectMotionInBox(DetectionBox box, Color[] backgroundPixels)
    {
        Rect roi = box.roi;
        int startX = Mathf.Max(0, (int)roi.x);
        int startY = Mathf.Max(0, (int)roi.y);
        int endX = Mathf.Min(currentFrame.width, (int)(roi.x + roi.width));
        int endY = Mathf.Min(currentFrame.height, (int)(roi.y + roi.height));
        
        float totalDifference = 0f;
        int pixelCount = 0;
        
        for (int y = startY; y < endY; y += 2) // Step by 2 for performance
        {
            for (int x = startX; x < endX; x += 2)
            {
                int index = y * currentFrame.width + x;
                
                if (index >= 0 && index < currentPixels.Length)
                {
                    if (backgroundPixels != null)
                    {
                        // Compare with background
                        float diff = Mathf.Abs(currentPixels[index].grayscale - backgroundPixels[index].grayscale);
                        totalDifference += diff;
                    }
                    else
                    {
                        // Simple edge detection without background
                        totalDifference += currentPixels[index].grayscale;
                    }
                    
                    pixelCount++;
                }
            }
        }
        
        float averageDifference = pixelCount > 0 ? totalDifference / pixelCount : 0f;
        return averageDifference > motionThreshold && (totalDifference / pixelCount) > occupancyThreshold;
    }
    
    void DrawVisualization()
    {
        processedFrame.SetPixels(currentPixels);
        
        foreach (DetectionBox box in detectionBoxes)
        {
            Color boxColor = box.isOccupied ? Color.green : new Color(1f, 0.65f, 0f); // Green or orange
            DrawRect(processedFrame, box.roi, boxColor, 3);
            
            // Draw center point
            DrawCircle(processedFrame, box.center, 5, Color.blue);
        }
        
        processedFrame.Apply();
    }
    
    void DrawRect(Texture2D texture, Rect rect, Color color, int thickness)
    {
        int startX = Mathf.Max(0, (int)rect.x);
        int startY = Mathf.Max(0, (int)rect.y);
        int endX = Mathf.Min(texture.width, (int)(rect.x + rect.width));
        int endY = Mathf.Min(texture.height, (int)(rect.y + rect.height));
        
        // Top and bottom lines
        for (int t = 0; t < thickness; t++)
        {
            for (int x = startX; x < endX; x++)
            {
                if (startY + t < texture.height)
                    texture.SetPixel(x, startY + t, color);
                if (endY - t >= 0)
                    texture.SetPixel(x, endY - t, color);
            }
        }
        
        // Left and right lines
        for (int t = 0; t < thickness; t++)
        {
            for (int y = startY; y < endY; y++)
            {
                if (startX + t < texture.width)
                    texture.SetPixel(startX + t, y, color);
                if (endX - t >= 0)
                    texture.SetPixel(endX - t, y, color);
            }
        }
    }
    
    void DrawCircle(Texture2D texture, Vector2 center, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int px = (int)center.x + x;
                    int py = (int)center.y + y;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }
    
    void OnGUI()
    {
        if (!showDebugWindow) return;
        
        // Display camera feed
        if (processedFrame != null && drawDetectionBoxes)
        {
            GUI.DrawTexture(new Rect(10, 10, 640, 480), processedFrame);
        }
        else if (webcamTexture != null)
        {
            GUI.DrawTexture(new Rect(10, 10, 640, 480), webcamTexture);
        }
        
        // Display instructions
        GUILayout.BeginArea(new Rect(660, 10, 300, 400));
        GUILayout.Label("Controls:", GUI.skin.box);
        GUILayout.Label("B - Capture Background");
        GUILayout.Label("M - Detect Markers");
        GUILayout.Label("R - Reset Boxes");
        GUILayout.Space(10);
        
        GUILayout.Label($"Background: {(backgroundCaptured ? "Captured" : "Not captured")}");
        GUILayout.Label($"Detection Boxes: {detectionBoxes.Count}");
        GUILayout.Label($"Occupied Boxes: {detectionBoxes.Count(b => b.isOccupied)}");
        
        GUILayout.EndArea();
    }
    
    // Public API for Unity integration
    public List<DetectionBox> GetDetectionBoxes() => detectionBoxes;
    public List<DetectionBox> GetOccupiedBoxes() => detectionBoxes.Where(b => b.isOccupied).ToList();
    public DetectionBox GetBoxById(int id) => detectionBoxes.FirstOrDefault(b => b.id == id);
    
    void OnDestroy()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
}
