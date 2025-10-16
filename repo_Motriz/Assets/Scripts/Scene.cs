using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Scene", menuName = "Scriptable Objects/Scene")]
public class Scene : ScriptableObject
{
    private static Scene _instance;
    [SerializeField] private AudioMixer mixer;

    public static Scene Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load the ScriptableObject from a known Resources path
                _instance = Resources.Load<Scene>("Scene");

                if (_instance == null)
                    Debug.LogError("Scene ScriptableObject not found in Resources folder!");
            }

            return _instance;
        }
    }



    public void LoadScene(string scene)
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

    public void SetVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mixer.SetFloat("MasterVolume", dB);
    }

    public void PlayMusic(AudioSource audio)
    {
        audio.Play();
    }

    public void PlayMusic(AudioClip clip, AudioSource audio)
    {
        audio.clip = clip;
        audio.loop = false;
        audio.Play();
    }

    public void StopMusic(AudioSource audio)
    {
        audio.Stop();
    }

    public void DisplayUI(GameObject UI)
    {
        UI.SetActive(true);
    }

    public void ToggleUI(GameObject UI)
    {
        UI.SetActive(!UI.activeInHierarchy);
    }
}
