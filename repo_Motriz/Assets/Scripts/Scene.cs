using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "Scene", menuName = "Scriptable Objects/Scene")]
public class Scene : ScriptableObject
{
    private static Scene _instance;
    [SerializeField] public AudioMixer mixer;

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
    public event Action<float> OnSFXChange;
    public event Action<float> OnMusicChange;


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

    public void SetMusic(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mixer.SetFloat("MusicVolume", dB);
        OnMusicChange?.Invoke(volume);
    }

    public void SetSFX(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mixer.SetFloat("SFXVolume", dB);
        OnSFXChange?.Invoke(volume);
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

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ChangeSlider(float value)
    {

    }
}
