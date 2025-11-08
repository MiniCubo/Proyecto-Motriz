using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalScipt : MonoBehaviour
{
    [SerializeField] private GameObject[] animals;
    private List<Button> animalsButtons;
    private List<AudioSource> audioSources;
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject popup;

    private float timer;
    int current;
    int count;
    
    private void Awake()
    {
        timer = 0f;
        current = -1;
        animalsButtons = new List<Button>();
        audioSources = new List<AudioSource>();
        foreach (var animal in animals)
        {
            audioSources.Add(animal.GetComponent<AudioSource>());
            animalsButtons.Add(animal.GetComponent<Button>());
        }

        count = 0;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
    }

    public bool NextAnimal(int next)
    {
        if (timer > 0f) return false;
        if (count == animals.Length) 
        {
            Victory();
            return false;
        }
        if (animalsButtons == null)
        {
            animalsButtons = new List<Button>();
            audioSources = new List<AudioSource>();
            foreach (var animal in animals)
            {
                audioSources.Add(animal.GetComponent<AudioSource>());
                animalsButtons.Add(animal.GetComponent<Button>());
            }
        }
        timer = 0.5f;
        if(current >= 0) animalsButtons[current].interactable = false;
        count++;
        current = next;
        text.text = animalsButtons[next].gameObject.GetComponent<Image>().sprite.name;
        animalsButtons[next].interactable = true;
        audioSources[next].Play();
        AppearText();
        return true;
    }

    public void AppearText()
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    private void Victory()
    {
        popup.SetActive(true );
    }
}
