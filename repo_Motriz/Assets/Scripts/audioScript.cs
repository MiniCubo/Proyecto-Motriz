using UnityEngine;

public class audioScript : MonoBehaviour
{
    public AudioClip[] songs;
    public AudioSource continiousAudio;
    [SerializeField] AudioSource audioPlayer;
    [SerializeField] AudioClip audioClip;


    private void Awake()
    {
        if (audioPlayer != null && audioClip != null)
        {
            audioPlayer.loop = false;
            audioPlayer.clip = audioClip;
            audioPlayer.PlayDelayed(0.1f);
        }
        if (songs.Length == 0) return;
        int rand = Random.Range(0, songs.Length);
        continiousAudio.clip = songs[rand];
        continiousAudio.PlayDelayed(1f);
    }

}
