using UnityEngine;
using UnityEngine.Events;

public class FootstepsScript : MonoBehaviour
{
    [SerializeField] private RectTransform[] footstepsPositions;
    public UnityEvent gameWonEvent;
    private RectTransform rt;
    int cont = 0;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        rt.position = footstepsPositions[0].position;
    }

    public void FootstepReached()
    {
        if(cont == footstepsPositions.Length -1) gameWonEvent.Invoke();
        else
        {
            cont++;
            rt.position = footstepsPositions[cont].position;
        }
    }

}
