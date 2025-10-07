using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    public void SetVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mixer.SetFloat("MasterVolume", dB);
    }
}
