using UnityEngine;

public class audioScript : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioSource continiousAudio;
    [SerializeField] AudioSource audioPlayer;
    [SerializeField] AudioClip audioClip;

    public void PlayAudio()
    {
        audioSource.Play(0);
    }
    //public void Start()
    //{
    //    continiousAudio.Play(0);    
    //}

    private void Awake()
    {
        if (audioPlayer != null && audioClip != null)
        {
            audioPlayer.loop = false;
            audioPlayer.clip = audioClip;
            audioPlayer.PlayDelayed(0.1f);
        }
    }

}
