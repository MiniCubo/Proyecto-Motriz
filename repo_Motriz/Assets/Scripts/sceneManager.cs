using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneManager : MonoBehaviour
{
    public string scene;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name.Equals("Main Menu")) DontDestroyOnLoad(GameObject.Find("Music Player"));
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
        Debug.Log(Display.displays.Length);
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(scene);
    }

    public void Print()
    {
        Debug.Log("Clicked");
    }

    public void KeepObject(string objectName)
    {
        DontDestroyOnLoad(GameObject.Find(objectName));
    }

    public void DestroyObject(string objectName)
    {
        Destroy(GameObject.Find(objectName));
    }

    
}