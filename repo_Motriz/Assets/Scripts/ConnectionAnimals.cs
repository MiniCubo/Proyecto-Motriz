using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionAnimals : MonoBehaviour
{
    [SerializeField] private GameObject[] animals;
    [SerializeField] private AnimalScipt[] scripts;
    private Dictionary<int, bool> choosen;

    private void Start()
    {
        choosen = new Dictionary<int, bool>();
    }

    public void NextAnimal()
    {
        foreach(var script in scripts)
        {
            if (!script.gameObject.activeSelf) return;
        }
        int rand = Random.Range(0, animals.Length);
        if(choosen.Count == animals.Length)
        {
            foreach (var script in scripts)
            {
                if (script.NextAnimal(rand)) return;
                Debug.Log($"Success with : {animals[rand].gameObject.name}({rand})");
            }
        }
        else
        {
            while (choosen.ContainsKey(rand)) rand = Random.Range(0, animals.Length);
            foreach (var script in scripts)
            {
                if (!script.NextAnimal(rand)) return;
                Debug.Log($"Success with : {animals[rand].gameObject.name}({rand})");
            }
            choosen.Add(rand, true);

        }

    }

}
