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

    public bool animalSound;
    public bool animalName;

    private float timer;
    int current;
    int count;

    private Animator animator;
    
    private void Awake()
    {
        timer = 0f;
        current = -1;
        animalsButtons = new List<Button>();
        audioSources = new List<AudioSource>();
        animator = GetComponent<Animator>();
        animalName = true;
        animalSound = true;
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
        if(animalSound) audioSources[next].Play();
        if(animalName) AppearText();
        return true;
    }

    public void SetAnimalSound(bool sound)
    {
        animalSound = sound;
    }

    public void SetAnimalName(bool name)
    {
        animalName = name;
    }

    public void AppearText()
    {
        Debug.Log(animator);
        if (animator == null) GetComponent<Animator>();
        animator.Play("UpDown");
    }

    private void Victory()
    {
        popup.SetActive(true );
    }
}
