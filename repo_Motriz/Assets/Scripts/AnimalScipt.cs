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
    private Dictionary<int, bool> choosen;
    int current;
    
    private void Start()
    {
        timer = 0f;
        choosen = new Dictionary<int, bool>();
        animalsButtons = new List<Button>();
        audioSources = new List<AudioSource>();
        foreach (var animal in animals)
        {
            audioSources.Add(animal.GetComponent<AudioSource>());
            animalsButtons.Add(animal.GetComponent<Button>());
        }

        timer = 0.5f;
        int rand = Random.Range(0, animals.Length);
        current = rand;
        choosen.Add(rand, true);
        text.text = animalsButtons[rand].gameObject.GetComponent<Image>().sprite.name;
        animalsButtons[rand].interactable = true;
        audioSources[rand].Play();
        //AppearText();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
    }

    public void NextAnimal()
    {
        if (timer > 0f) return;
        if (choosen.Count == animals.Length) 
        {
            Victory();
            return;
        }
        timer = 0.5f;
        if (!animalsButtons[current].gameObject.activeSelf) return; 
        animalsButtons[current].interactable = false;
        int rand = Random.Range(0, animals.Length);
        while (choosen.ContainsKey(rand)) rand = Random.Range(0, animals.Length);
        current = rand;
        choosen.Add(rand, true);
        text.text = animalsButtons[rand].gameObject.GetComponent<Image>().sprite.name;
        animalsButtons[rand].interactable = true;
        audioSources[rand].Play();
        AppearText();
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
