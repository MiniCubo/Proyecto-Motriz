using UnityEngine;

public class AnimationEventsHandleScript : MonoBehaviour
{
    [SerializeField] private GameObject UI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnAnimationEvent()
    {
        Scene.Instance.ToggleUI(gameObject);
        Scene.Instance.ToggleUI(UI);
    }
}
