using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VolumeMaster : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private void Start()
    {
        StartCoroutine(SetVolumes());
    }

    private IEnumerator SetVolumes()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        Scene.Instance.OnMusicChange += SetMusic;
        Scene.Instance.OnSFXChange += SetSFX;
        Scene.Instance.mixer.GetFloat("SFXVolume", out float dBSFX);
        Scene.Instance.mixer.GetFloat("MusicVolume", out float dBMusic);
        float sfxValue = Mathf.Pow(10f, dBSFX / 20f);
        float musicValue = Mathf.Pow(10f, dBMusic / 20f);

        sfxSlider.value = sfxValue;
        musicSlider.value = musicValue;
    }

    private void OnDestroy()
    {
        Scene.Instance.OnMusicChange -= SetMusic;
        Scene.Instance.OnSFXChange -= SetSFX;
    }

    private void SetSFX(float volume)
    {
        sfxSlider.value = volume;
    }

    private void SetMusic(float volume)
    {
        musicSlider.value = volume;
    }
}