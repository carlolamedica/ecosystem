using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance;

    [SerializeField] private GameObject prefab;
    [SerializeField] private float spawnRange;
    [SerializeField] private int initialPopulation;
    [Space]
    [SerializeField] private int nOfChildren;

    public enum SpawnType
    {
        Plant, MuadDib, ShaiHulud
    };

    public SpawnType spawnType;

    private void Awake()
    {
        firstTime = true;
        if (spawnType == SpawnType.MuadDib)
            Instance = this;

        nOfChildren = transform.childCount;
    }

    public void UpdateChildren()
    {
        nOfChildren = transform.childCount;
    }

    private void Start()
    {
        if (spawnType == SpawnType.Plant)
            StartCoroutine(SpawnPlant());
        else if (spawnType == SpawnType.MuadDib || spawnType == SpawnType.ShaiHulud)
            SpawnAnimal();

    }

    bool firstTime = true;
    IEnumerator SpawnPlant()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            Vector2 position = new(Random.Range(-spawnRange, spawnRange), Random.Range(-spawnRange, spawnRange));
            Instantiate(prefab, position, Quaternion.identity, transform);
        }
        if (firstTime)
            StartCoroutine(StatsManager.Instance.UpdateStats());
        yield return new WaitForSeconds(2f);
        firstTime = false;
        StartCoroutine(SpawnPlant());

    }

    private void SpawnAnimal()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            Vector2 position = new(Random.Range(-spawnRange, spawnRange), Random.Range(-spawnRange, spawnRange));
            GameObject g = Instantiate(prefab, position, Quaternion.identity, transform);
            g.transform.name = prefab.name + i;
            bool isFemale = i % 2 == 0;

            if (spawnType == SpawnType.MuadDib)
            {
                g.GetComponent<MuadDib>().dna.isFemale = isFemale;
                if (isFemale)
                    g.GetComponent<SpriteRenderer>().color = Color.magenta;
                else
                    g.GetComponent<SpriteRenderer>().color = Color.blue;
            }
            else
            {
                g.GetComponent<ShaiHulud>().dna.isFemale = isFemale;
                if (isFemale)
                    g.GetComponent<SpriteRenderer>().color = Color.white;
                else
                    g.GetComponent<SpriteRenderer>().color = Color.black;
            }

        }
    }
}
