using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    // World Information
    [Header("World Information")]
    public GameObject worldInformationUI;
    public Animator worldInformationAnim;
    public TMP_Text worldSizeText;
    public TMP_Text cellCountText;
    public TMP_Text foodCountText;
    [Header("World State")]
    public TMP_Text timeScaleText;
    public Slider timeScaleSlider;
    public TMP_Text energyCostText;
    public Slider energyCostSlider;
    public TMP_Text foodSpawnRateText;
    public Slider foodSpawnRateSlider;
    public TMP_Text mutationFrequencyText;
    public Slider mutationFrequencySlider;
    [Header("Buttons")]
    public TMP_Text toggleSimulationText;

    // Cell Information
    [Header("Cell Information")]
    public GameObject cellInformationUI;
    //public Animator cellInformationAnim;
    public TMP_Text generationText;
    public TMP_Text genomeText;
    public TMP_Text colorText;
    public TMP_Text moveSpeedText;
    public TMP_Text maxSpeedText;
    public TMP_Text viewDistanceText;
    public TMP_Text wanderDeviationText;
    public TMP_Text energyText;

    // Helpers
    [HideInInspector] public Cell tempCellScript;
    // public bool isAnimating = false;
    // private bool animCooldown = false;
    // public bool worldOpen = false;
    // private float time;
    // private bool cellOpen = true;

    void Start() 
    {
        // Set Current View
        Master.Instance.globalView = true;
        Master.Instance.cellView = false;

        // Information
        timeScaleSlider.value = Master.Instance.timeScale;
        energyCostSlider.value = Master.Instance.energyDialator;
        foodSpawnRateSlider.value = Master.Instance.foodSpawnRate;
        mutationFrequencySlider.value = Master.Instance.mutationFrequency;

        // // Animations
        // isAnimating = false;
        // animCooldown = false;
        // worldOpen = false;
        // time = 0f;
    }

    void Update() 
    {
        // Show World Info?
        if (Master.Instance.globalView) 
        {
            worldInformationUI.SetActive(true);
            SetWorldInformation();
            // OpenWorldInfo();
        } else 
        {
            worldInformationUI.SetActive(false);
            // CloseWorldInfo();
        }

        // Show Cell Info?
        if (Master.Instance.cellView) 
        {
            cellInformationUI.SetActive(true);
            SetCellInformation();
        } else 
        {
            cellInformationUI.SetActive(false);
        }
        
        // Update Information
        Master.Instance.timeScale = timeScaleSlider.value;
        Master.Instance.energyDialator = energyCostSlider.value;
        Master.Instance.foodSpawnRate = foodSpawnRateSlider.value;
        Master.Instance.mutationFrequency = mutationFrequencySlider.value;

        // // Animation
        // AnimationCooldown();
    }

    void SetWorldInformation() 
    {
        // World Information
        worldSizeText.text = "World Size: " + Master.Instance.worldSize.ToString();
        cellCountText.text = "Cell Count: " + Master.Instance.cellCount.ToString();
        foodCountText.text = "Food Count: " + Master.Instance.foodCount.ToString();

        // World State
        timeScaleText.text = "Time Scale: " + (Master.Instance.timeScale * 100f).ToString("F0") + "%";
        energyCostText.text = "Energy Cost: " + (Master.Instance.energyDialator * 100f).ToString("F0") + "%";
        foodSpawnRateText.text = "Food Spawn Rate: " + (Master.Instance.foodSpawnRate).ToString("F2") + "s";
        mutationFrequencyText.text = "Mutation Frequency: " + (Master.Instance.mutationFrequency * 100f).ToString("F0") + "%";
    }

    void SetCellInformation() 
    {
        // Cell Information
        generationText.text = "Generation: " + tempCellScript.generation;
        genomeText.text = tempCellScript.genome;
        colorText.text = "Color: #" + tempCellScript.color;
        moveSpeedText.text = "Move Speed: " + tempCellScript.moveSpeed.ToString("F0");
        viewDistanceText.text = "View Distance: " + tempCellScript.viewDistance.ToString("F0");
        wanderDeviationText.text = "Wander Deviation: " + tempCellScript.wanderDeviation.ToString("F0") + "°";
        energyText.text = "Energy: " + (tempCellScript.energy * 100f).ToString("F0") + "%";
    }

    public void ToggleSimulation() {
        Master.Instance.runningSimulation = !Master.Instance.runningSimulation;
        if (Master.Instance.runningSimulation)
        {
            toggleSimulationText.text = "Stop Simulation";
            if (Master.Instance.resetTrue) 
            {
                Master.Instance.resetTrue = false;
                StartSimulation();
            }
        } else
        {
            toggleSimulationText.text = "Start Simulation";
        }
    }

    public void StartSimulation()
    {
        Master.Instance.runningSimulation = true;
        Master.Instance.mapMasterScript.SpawnEntities();
    }

    public void ResetSimulation() 
    {
        Master.Instance.resetTrue = true;
        Master.Instance.runningSimulation = false;
        Master.Instance.mapMasterScript.KillEntities();
        toggleSimulationText.text = "Start Simulation";
    }

    // // UI ANIMATION (Only Plays One Frame???)

    // public void OpenWorldInfo()
    // {
    //     if (!isAnimating && !worldOpen)
    //     {
    //         Debug.Log("Open World Info Called");
    //         worldOpen = true;
    //         isAnimating = true;
    //         worldInformationAnim.Play("Open");
    //         animCooldown = true;
    //     }
    // }
    
    // public void CloseWorldInfo()
    // {
    //     if (!isAnimating && worldOpen)
    //     {
    //         Debug.Log("Close World Info Called");
    //         worldOpen = false;
    //         isAnimating = true;
    //         worldInformationAnim.Play("Close");
    //         animCooldown = true;
    //     }
    // }

    // void AnimationCooldown()
    // {
    //     if (animCooldown)
    //     {
    //         time += Time.unscaledDeltaTime;
    //         if (time > 1f)
    //         {
    //             time = 0f;
    //             isAnimating = false;
    //             animCooldown = false;
    //         }
    //     }
    // }
}
