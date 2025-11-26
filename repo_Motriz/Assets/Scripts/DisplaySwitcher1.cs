using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DisplaySwitcher1 : MonoBehaviour
{
    [Header("Display Swap Configuration")]
    [Tooltip("Which display is the iPad? (usually 1)")]
    public int iPadDisplay = 1;

    [Tooltip("Which display is the Projector? (usually 2)")]
    public int projectorDisplay = 2;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool swapped = false;
    private bool hasConfiguredScene = false;

    // Track original assignments for the current scene
    private Dictionary<Camera, int> cameraOriginalDisplays = new Dictionary<Camera, int>();
    private Dictionary<Canvas, int> canvasOriginalDisplays = new Dictionary<Canvas, int>();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Activate all available displays
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            LogDebug($"Activated display {i}");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Call this from your settings button to swap iPad and Projector displays
    /// </summary>
    public void SwapDisplays()
    {
        swapped = !swapped;
        LogDebug($"=== SWAP TRIGGERED === New state: {(swapped ? "SWAPPED" : "NORMAL")}");

        ApplySwapToAllObjects();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        LogDebug($"=== SCENE LOADED: {scene.name} ===");
        hasConfiguredScene = false; // Reset for new scene
        StartCoroutine(HandleSceneLoad());
    }

    private System.Collections.IEnumerator HandleSceneLoad()
    {
        // Wait for scene to fully initialize (Start methods, etc.)
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Step 1: Capture what the scene designer set up (ONLY ONCE PER SCENE)
        CaptureSceneConfiguration();

        // Step 2: If we're in swapped mode, apply the swap
        if (swapped)
        {
            LogDebug("Applying swapped state to newly loaded scene");
            ApplySwapToAllObjects();
        }

        // Step 3: Verify setup
        yield return new WaitForEndOfFrame();
        ValidateCameraSetup();

        hasConfiguredScene = true;
    }

    private void CaptureSceneConfiguration()
    {
        // CRITICAL: Only capture the ORIGINAL configuration once per scene
        // Don't re-capture after swapping, or we'll treat the swapped state as "original"
        if (hasConfiguredScene)
        {
            LogDebug("Scene already configured, skipping re-capture");
            return;
        }

        // Clear old data from previous scene
        cameraOriginalDisplays.Clear();
        canvasOriginalDisplays.Clear();

        // Find all cameras and canvases in the current scene (including inactive ones)
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        LogDebug($"Capturing original configuration: {cameras.Length} cameras and {canvases.Length} canvases");

        // Store each camera's original display assignment
        foreach (var cam in cameras)
        {
            cameraOriginalDisplays[cam] = cam.targetDisplay;
            LogDebug($"  Camera '{cam.name}' originally on Display {cam.targetDisplay}");
        }

        // Store each canvas's original display assignment
        foreach (var canvas in canvases)
        {
            canvasOriginalDisplays[canvas] = canvas.targetDisplay;
            LogDebug($"  Canvas '{canvas.name}' originally on Display {canvas.targetDisplay}");
        }
    }

    private void ApplySwapToAllObjects()
    {
        LogDebug($"Applying swap (swapped={swapped})");

        // Process all cameras
        foreach (var kvp in cameraOriginalDisplays)
        {
            Camera cam = kvp.Key;
            int originalDisplay = kvp.Value;

            if (cam == null) continue; // Skip if camera was destroyed

            int newDisplay = swapped ? SwapDisplayNumber(originalDisplay) : originalDisplay;
            cam.targetDisplay = newDisplay;

            LogDebug($"  Camera '{cam.name}': Display {originalDisplay} → {newDisplay}");
        }

        // Process all canvases
        foreach (var kvp in canvasOriginalDisplays)
        {
            Canvas canvas = kvp.Key;
            int originalDisplay = kvp.Value;

            if (canvas == null) continue; // Skip if canvas was destroyed

            int newDisplay = swapped ? SwapDisplayNumber(originalDisplay) : originalDisplay;
            canvas.targetDisplay = newDisplay;

            LogDebug($"  Canvas '{canvas.name}': Display {originalDisplay} → {newDisplay}");
        }

        // Force immediate visual update
        StartCoroutine(ForceCanvasRefreshDelayed());
    }

    /// <summary>
    /// Forces all canvases to refresh with a slight delay for reliability
    /// </summary>
    private System.Collections.IEnumerator ForceCanvasRefreshDelayed()
    {
        // Wait one frame for display changes to register
        yield return null;

        // Find ALL canvases, including inactive ones (like the settings canvas when closed)
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        LogDebug($"Force refreshing {allCanvases.Length} canvases");

        foreach (var canvas in allCanvases)
        {
            if (canvas == null) continue;

            // Skip if the canvas's GameObject is inactive (like closed settings menu)
            // But still process if just the canvas component is disabled
            if (!canvas.gameObject.activeInHierarchy)
            {
                LogDebug($"  Skipping inactive canvas '{canvas.name}'");
                continue;
            }

            // Disable and re-enable to force refresh
            canvas.enabled = false;
        }

        // Wait one more frame
        yield return null;

        // Re-enable all canvases
        foreach (var canvas in allCanvases)
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy) continue;

            canvas.enabled = true;

            // Force layout rebuild
            RectTransform rt = canvas.GetComponent<RectTransform>();
            if (rt != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            LogDebug($"  Refreshed canvas '{canvas.name}' on Display {canvas.targetDisplay}");
        }

        // Final validation
        ValidateCameraSetup();
    }

    private int SwapDisplayNumber(int originalDisplay)
    {
        // Only swap iPad and Projector, leave everything else alone
        if (originalDisplay == iPadDisplay)
            return projectorDisplay;
        else if (originalDisplay == projectorDisplay)
            return iPadDisplay;
        else
            return originalDisplay; // Computer display stays the same
    }

    private void ValidateCameraSetup()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        // Count cameras per display
        Dictionary<int, int> camerasPerDisplay = new Dictionary<int, int>();

        foreach (var cam in cameras)
        {
            if (cam.enabled)
            {
                if (!camerasPerDisplay.ContainsKey(cam.targetDisplay))
                    camerasPerDisplay[cam.targetDisplay] = 0;

                camerasPerDisplay[cam.targetDisplay]++;
            }
        }

        // Report findings
        LogDebug("=== VALIDATION ===");
        for (int i = 0; i < 3; i++) // Check first 3 displays
        {
            if (camerasPerDisplay.ContainsKey(i))
            {
                LogDebug($"Display {i}: {camerasPerDisplay[i]} active camera(s) ✓");
            }
            else
            {
                Debug.LogWarning($"[DisplaySwitcher] Display {i}: NO CAMERAS RENDERING ✗");
            }
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[DisplaySwitcher] {message}");
        }
    }

    // ========== PUBLIC UTILITY METHODS ==========

    /// <summary>
    /// Check if displays are currently swapped
    /// </summary>
    public bool IsSwapped()
    {
        return swapped;
    }

    /// <summary>
    /// Force a specific swap state without toggling
    /// </summary>
    public void SetSwapState(bool shouldBeSwapped)
    {
        if (swapped != shouldBeSwapped)
        {
            SwapDisplays();
        }
    }

    /// <summary>
    /// Reset to normal (unswapped) state
    /// </summary>
    public void ResetToNormal()
    {
        if (swapped)
        {
            SwapDisplays();
        }
    }

    /// <summary>
    /// Call this if you need to manually refresh displays
    /// </summary>
    public void ManualRefresh()
    {
        LogDebug("Manual refresh requested");
        StartCoroutine(ForceCanvasRefreshDelayed());
    }
}