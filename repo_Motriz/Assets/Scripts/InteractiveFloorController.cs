using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Example script showing how to use MarkerDetector to control game objects
/// This creates interactive elements at each detection box location
/// </summary>
public class InteractiveFloorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MarkerDetector markerDetector;
    [SerializeField] private Camera projectorCamera;
    
    [Header("Game Object Settings")]
    [SerializeField] private GameObject interactiveObjectPrefab;
    [SerializeField] private float spawnHeight = 0f;
    [SerializeField] private Vector2 playAreaSize = new Vector2(10f, 10f);
    
    [Header("Coordinate Mapping")]
    [Tooltip("Camera resolution for coordinate conversion")]
    [SerializeField] private Vector2 cameraResolution = new Vector2(1920, 1080);
    
    private Dictionary<int, GameObject> spawnedObjects = new Dictionary<int, GameObject>();
    
    void Start()
    {
        if (markerDetector == null)
        {
            markerDetector = FindObjectOfType<MarkerDetector>();
        }
        
        if (projectorCamera == null)
        {
            projectorCamera = Camera.main;
        }
        
        // Subscribe to detection events
        markerDetector.OnBoxesUpdated += HandleBoxesUpdated;
        markerDetector.OnBoxOccupied += HandleBoxOccupied;
        markerDetector.OnBoxVacated += HandleBoxVacated;
    }
    
    void HandleBoxesUpdated(List<DetectionBox> boxes)
    {
        // Create or update game objects for each detection box
        foreach (DetectionBox box in boxes)
        {
            if (!spawnedObjects.ContainsKey(box.id))
            {
                Vector3 worldPos = CameraToWorldPosition(box.center);
                GameObject obj = Instantiate(interactiveObjectPrefab, worldPos, Quaternion.identity);
                obj.name = $"InteractiveZone_{box.id}";
                spawnedObjects[box.id] = obj;
                
                // Add a component to handle interaction
                InteractiveZone zone = obj.AddComponent<InteractiveZone>();
                zone.boxId = box.id;
            }
        }
        
        // Remove objects for boxes that no longer exist
        List<int> idsToRemove = new List<int>();
        foreach (var kvp in spawnedObjects)
        {
            if (!boxes.Exists(b => b.id == kvp.Key))
            {
                Destroy(kvp.Value);
                idsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int id in idsToRemove)
        {
            spawnedObjects.Remove(id);
        }
    }
    
    void HandleBoxOccupied(DetectionBox box)
    {
        if (spawnedObjects.TryGetValue(box.id, out GameObject obj))
        {
            InteractiveZone zone = obj.GetComponent<InteractiveZone>();
            if (zone != null)
            {
                zone.OnPlayerEnter();
            }
        }
    }
    
    void HandleBoxVacated(DetectionBox box)
    {
        if (spawnedObjects.TryGetValue(box.id, out GameObject obj))
        {
            InteractiveZone zone = obj.GetComponent<InteractiveZone>();
            if (zone != null)
            {
                zone.OnPlayerExit();
            }
        }
    }
    
    /// <summary>
    /// Convert camera pixel coordinates to Unity world coordinates
    /// </summary>
    Vector3 CameraToWorldPosition(Vector2 pixelPos)
    {
        // Normalize pixel coordinates (0-1)
        float normalizedX = pixelPos.x / cameraResolution.x;
        float normalizedY = pixelPos.y / cameraResolution.y;
        
        // Map to play area
        float worldX = (normalizedX - 0.5f) * playAreaSize.x;
        float worldZ = (normalizedY - 0.5f) * playAreaSize.y;
        
        return new Vector3(worldX, spawnHeight, worldZ);
    }
    
    /// <summary>
    /// Convert Unity world coordinates to camera pixel coordinates
    /// Useful for placing markers in the real world
    /// </summary>
    public Vector2 WorldToCameraPosition(Vector3 worldPos)
    {
        float normalizedX = (worldPos.x / playAreaSize.x) + 0.5f;
        float normalizedY = (worldPos.z / playAreaSize.y) + 0.5f;
        
        float pixelX = normalizedX * cameraResolution.x;
        float pixelY = normalizedY * cameraResolution.y;
        
        return new Vector2(pixelX, pixelY);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (markerDetector != null)
        {
            markerDetector.OnBoxesUpdated -= HandleBoxesUpdated;
            markerDetector.OnBoxOccupied -= HandleBoxOccupied;
            markerDetector.OnBoxVacated -= HandleBoxVacated;
        }
    }
}

/// <summary>
/// Component attached to each interactive zone
/// Handles what happens when player steps on/off the zone
/// </summary>
public class InteractiveZone : MonoBehaviour
{
    public int boxId;
    private Renderer rend;
    private Color originalColor;
    private bool isActive = false;
    
    [Header("Visual Feedback")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.white;
    public float pulseSpeed = 2f;
    
    [Header("Effects")]
    public ParticleSystem activationEffect;
    public AudioClip activationSound;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }
    
    void Update()
    {
        if (isActive && rend != null)
        {
            // Pulse effect when active
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            rend.material.color = Color.Lerp(activeColor, inactiveColor, pulse);
        }
    }
    
    public void OnPlayerEnter()
    {
        isActive = true;
        Debug.Log($"Player entered zone {boxId}");
        
        // Visual feedback
        if (rend != null)
        {
            rend.material.color = activeColor;
        }
        
        // Particle effect
        if (activationEffect != null)
        {
            activationEffect.Play();
        }
        
        // Sound effect
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }
        
        // Add your game logic here
        // Examples:
        // - Spawn enemies
        // - Trigger cutscene
        // - Award points
        // - Change music
        // - Open door
    }
    
    public void OnPlayerExit()
    {
        isActive = false;
        Debug.Log($"Player left zone {boxId}");
        
        // Reset visual
        if (rend != null)
        {
            rend.material.color = inactiveColor;
        }
        
        // Stop particle effect
        if (activationEffect != null)
        {
            activationEffect.Stop();
        }
        
        // Add your game logic here
    }
}

/// <summary>
/// Advanced example: Multi-player foot tracking
/// Tracks individual feet and their positions
/// </summary>
public class MultiPlayerFootTracker : MonoBehaviour
{
    [SerializeField] private MarkerDetector markerDetector;
    [SerializeField] private GameObject playerIndicatorPrefab;
    
    private Dictionary<int, PlayerFootData> playerFeet = new Dictionary<int, PlayerFootData>();
    
    private class PlayerFootData
    {
        public int boxId;
        public GameObject indicator;
        public Vector3 worldPosition;
        public float lastSeenTime;
    }
    
    void Update()
    {
        if (markerDetector == null) return;
        
        List<DetectionBox> occupiedBoxes = markerDetector.GetOccupiedBoxes();
        
        // Update existing feet
        foreach (DetectionBox box in occupiedBoxes)
        {
            if (!playerFeet.ContainsKey(box.id))
            {
                // New foot detected
                PlayerFootData footData = new PlayerFootData
                {
                    boxId = box.id,
                    indicator = Instantiate(playerIndicatorPrefab),
                    worldPosition = CameraToWorldPosition(box.center),
                    lastSeenTime = Time.time
                };
                playerFeet[box.id] = footData;
            }
            else
            {
                // Update existing foot
                playerFeet[box.id].worldPosition = CameraToWorldPosition(box.center);
                playerFeet[box.id].lastSeenTime = Time.time;
            }
            
            // Update indicator position
            playerFeet[box.id].indicator.transform.position = playerFeet[box.id].worldPosition;
        }
        
        // Remove feet that haven't been seen recently
        List<int> toRemove = new List<int>();
        foreach (var kvp in playerFeet)
        {
            if (Time.time - kvp.Value.lastSeenTime > 0.5f)
            {
                Destroy(kvp.Value.indicator);
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (int id in toRemove)
        {
            playerFeet.Remove(id);
        }
    }
    
    Vector3 CameraToWorldPosition(Vector2 pixelPos)
    {
        // Same conversion as InteractiveFloorController
        Vector2 cameraResolution = new Vector2(1920, 1080);
        Vector2 playAreaSize = new Vector2(10f, 10f);
        
        float normalizedX = pixelPos.x / cameraResolution.x;
        float normalizedY = pixelPos.y / cameraResolution.y;
        
        float worldX = (normalizedX - 0.5f) * playAreaSize.x;
        float worldZ = (normalizedY - 0.5f) * playAreaSize.y;
        
        return new Vector3(worldX, 0f, worldZ);
    }
    
    public int GetPlayerCount()
    {
        return playerFeet.Count;
    }
    
    public List<Vector3> GetAllPlayerPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var foot in playerFeet.Values)
        {
            positions.Add(foot.worldPosition);
        }
        return positions;
    }
}
