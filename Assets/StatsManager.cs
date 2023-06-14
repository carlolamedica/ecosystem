using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    [Header("References")]
    [SerializeField] private TMP_Text currentPopulationTxt;
    [SerializeField] private TMP_Text averageSpeedTxt, averageSensoryDistanceTxt, averageDeathSpeedTxt, maleFemaleTxt, fpsTxt;
    [SerializeField] private Slider timeSlider;

    [Header("Stats")]
    public float averageSpeed;
    public float averageSensoryDistance;
    public int currentPopulation;
    public float averageSpeedDeath;
    public float maleFemale;


    [Space]
    [SerializeField] private GameObject currentBG;
    [SerializeField] private GameObject bgPrefab;

    [HideInInspector] public float speedSum, distanceSum, deathSpeedSum, deadPopulation;
    [Space] public float nOfMales;

    private void Awake()
    {
        Instance = this;
        frameDeltaTimeArray = new float[50];
    }

    private float[] frameDeltaTimeArray;
    private int lastFrameIndex;
    private void Update()
    {
        if (currentBG == null)
            currentBG = Instantiate(bgPrefab);

        // FPS
        frameDeltaTimeArray[lastFrameIndex] = Time.unscaledDeltaTime;
        lastFrameIndex = (lastFrameIndex + 1) % frameDeltaTimeArray.Length;

        fpsTxt.text = Mathf.RoundToInt(CalculateFPS()).ToString();
    }

    private float CalculateFPS()
    {
        float total = 0f;
        foreach (float deltaTime in frameDeltaTimeArray)
        {
            total += deltaTime;
        }
        return frameDeltaTimeArray.Length / total;
    }

    public IEnumerator UpdateStats()
    {
        Spawner.Instance.UpdateChildren();
        if (currentPopulation > 0)
        {
            currentPopulationTxt.text = "Population: " + currentPopulation.ToString();

            averageSpeed = speedSum / currentPopulation;
            averageSensoryDistance = distanceSum / currentPopulation;
            averageSpeedDeath = deathSpeedSum / deadPopulation;
            maleFemale = nOfMales / (currentPopulation - nOfMales);

            averageSensoryDistanceTxt.text = "Average Sensory Distance: " + averageSensoryDistance.ToString();
            averageSpeedTxt.text = "Average Speed: " + averageSpeed.ToString();
            averageDeathSpeedTxt.text = "Average Speed on Death: " + averageSpeedDeath.ToString();

            maleFemaleTxt.text = "M/F: " + maleFemale.ToString();

            if (currentPopulation <= 1 || maleFemale == 0 || maleFemale >= Mathf.Infinity)
                SceneManager.LoadScene(0);
        }
        yield return new WaitForSeconds(1.5f);

        StartCoroutine(UpdateStats());
    }

    public void UpdateTimeScale()
    {
        Time.timeScale = timeSlider.value;
    }
}
