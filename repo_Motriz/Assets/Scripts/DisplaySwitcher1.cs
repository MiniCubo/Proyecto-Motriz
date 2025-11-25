using UnityEngine;

public class DisplaySwitcher1 : MonoBehaviour
{
    private bool swapped;

    void Start()
    {
        // Activate all available displays
        for (int i = 0; i < Display.displays.Length; i++)
            Display.displays[i].Activate();

        Debug.Log($"Activated {Display.displays.Length} displays.");
    }

    public void SwapDisplays()
    {
        // Get all active Cameras and Canvases efficiently
        var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (var cam in cams)
        {
            if (cam.targetDisplay == 1)
                cam.targetDisplay = 2;
            else if (cam.targetDisplay == 2)
                cam.targetDisplay = 1;
        }

        foreach (var canvas in canvases)
        {
            if (canvas.targetDisplay == 1)
                canvas.targetDisplay = 2;
            else if (canvas.targetDisplay == 2)
                canvas.targetDisplay = 1;
        }

        swapped = !swapped;
        Debug.Log($"Displays swapped (state: {swapped})");
    }
}
