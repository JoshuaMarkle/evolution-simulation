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
    public TMP_Text worldSizeText;
    public TMP_Text cellCountText;
    public TMP_Text foodCountText;
    [Header("World State")]
    public TMP_Text timeScaleText;
    public Slider timeScaleSlider;
    public TMP_Text foodSpawnRateText;
    public Slider foodSpawnRateSlider;
    public TMP_Text mutationFrequencyText;
    public Slider mutationFrequencySlider;
    [Header("Buttons")]
    public TMP_Text toggleSimulationText;

    // World Information
    [Header("Cell Information")]
    public GameObject cellInformationUI;
    public TMP_Text genomeText;
    public TMP_Text colorText;
    public TMP_Text moveSpeedText;
    public TMP_Text maxSpeedText;
    public TMP_Text viewDistanceText;
    public TMP_Text wanderDeviationText;
    public TMP_Text energyText;

    [HideInInspector] public Cell tempCellScript;

    void Start() 
    {
        // Set Current View
        Master.Instance.globalView = true;
        Master.Instance.cellView = false;

        // Information
        foodSpawnRateSlider.value = Master.Instance.foodSpawnRate;
        mutationFrequencySlider.value = Master.Instance.mutationFrequency;
        timeScaleSlider.value = Master.Instance.timeScale;
    }

    void Update() 
    {
        // Global Focus Screen
        if (Master.Instance.globalView) 
        {
            worldInformationUI.SetActive(true);
            SetWorldInformation();
        } else 
        {
            worldInformationUI.SetActive(false);
        }

        // Cell Focus Screen
        if (Master.Instance.cellView) 
        {
            cellInformationUI.SetActive(true);
            SetCellInformation();
        } else 
        {
            cellInformationUI.SetActive(false);
        }
        
        // Update Information
        Master.Instance.foodSpawnRate = foodSpawnRateSlider.value;
        Master.Instance.mutationFrequency = mutationFrequencySlider.value;
        Master.Instance.timeScale = timeScaleSlider.value;
    }

    void SetWorldInformation() 
    {
        // World Information
        worldSizeText.text = "World Size: " + Master.Instance.worldSize.ToString();
        cellCountText.text = "Cell Count: " + Master.Instance.cellCount.ToString();
        foodCountText.text = "Food Count: " + Master.Instance.foodCount.ToString();

        // World State
        timeScaleText.text = "Time Scale: " + (Master.Instance.timeScale * 100f).ToString("F2") + "%";
        foodSpawnRateText.text = "Food Spawn Rate: " + (Master.Instance.foodSpawnRate).ToString("F2") + "s";
        mutationFrequencyText.text = "Mutation Frequency: " + (Master.Instance.mutationFrequency * 100f).ToString("F2") + "%";
    }

    void SetCellInformation() 
    {
        genomeText.text = tempCellScript.genome;
        colorText.text = "Color: " + tempCellScript.color;
        moveSpeedText.text = "Move Speed: " + tempCellScript.moveSpeed.ToString();
        maxSpeedText.text = "Max Speed: " + tempCellScript.maxSpeed.ToString();
        viewDistanceText.text = "View Distance: " + tempCellScript.viewDistance.ToString();
        wanderDeviationText.text = "Wander Deviation: " + tempCellScript.wanderDeviation.ToString() + "°";
        energyText.text = "Energy: " + (tempCellScript.energy * 100f).ToString("F2") + "%";
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
}