using UnityEngine;
using UnityEngine.Events;

public class FootstepsScript : MonoBehaviour
{
    [SerializeField] private RectTransform[] footstepsPositions;
    [SerializeField] private BodyMotionTracker2 footDetector;
    public UnityEvent gameWonEvent;
    private RectTransform rt;
    int cont = 0;
    private float timer;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        rt.position = footstepsPositions[0].position;
        timer = 0f;

        if (footDetector == null)
        {
            footDetector = FindFirstObjectByType<BodyMotionTracker2>();
        }

        // Subscribe your function to the event
        footDetector.OnFootDetectedInArea += FootstepReached;
    }
    private void Update()
    {
        timer -= Time.deltaTime;
    }

    public void FootstepReached()
    {
        if (!gameObject.activeSelf) return;
        if(cont == footstepsPositions.Length -1 ) gameWonEvent.Invoke();
        else if(timer  < 0f)
        {
            cont++;
            timer = 0.1f;
            rt.position = footstepsPositions[cont].position;
            if (footstepsPositions[cont].name.Contains("Wide")) rt.sizeDelta = new Vector2(footstepsPositions[cont].GetComponent<RectTransform>().sizeDelta.x, 100);
            else rt.sizeDelta = new Vector2(100, 100);
        }
    }

}
