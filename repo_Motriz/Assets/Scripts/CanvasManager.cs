using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private GameObject otherCanvas;
    [SerializeField] private GameObject currentCanvas;

    public void SwapCanvas()
    {
        if (otherCanvas != null && currentCanvas != null)
        {
            otherCanvas.SetActive(true);
            currentCanvas.SetActive(false);
        }
    }
}
