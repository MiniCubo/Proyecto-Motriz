using UnityEngine;

public class AnimationEventsHandleScript : MonoBehaviour
{
    [SerializeField] private GameObject[] UI;
    [SerializeField] private ConnectionAnimals conn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnAnimationEvent()
    {
        Scene.Instance.ToggleUI(gameObject);
        foreach (GameObject go in UI)
        {
            Scene.Instance.ToggleUI(go);
        }
        if (conn != null) conn.NextAnimal();
    }
}
