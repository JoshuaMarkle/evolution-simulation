using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    [Header("World Information UI")]
    public GameObject worldInformationUI;
    public TMP_Text worldSizeText;
    public TMP_Text cellCountText;
    public TMP_Text foodCountText;
    public TMP_Text foodSpawnRateText;
    public TMP_Text mutationFrequencyText;

    [Header("Cell Information UI")]
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
    }

    void SetWorldInformation() 
    {
        // Assign TMP_Text Values
        worldSizeText.text = "World Size: " + Master.Instance.worldSize.ToString();
        cellCountText.text = "Cell Count: " + Master.Instance.cellCount.ToString();
        foodCountText.text = "Food Count: " + Master.Instance.foodCount.ToString();
        foodSpawnRateText.text = "Food Spawn Rate: " + (Master.Instance.foodSpawnRate).ToString() + "s";
        mutationFrequencyText.text = "Mutation Frequency: " + (Master.Instance.mutationFrequency * 100f).ToString() + "%";
    }

    void SetCellInformation() 
    {
        genomeText.text = tempCellScript.genome;
        colorText.text = "Color: " + tempCellScript.color;
        moveSpeedText.text = "Move Speed: " + tempCellScript.moveSpeed.ToString();
        maxSpeedText.text = "Max Speed: " + tempCellScript.maxSpeed.ToString();
        viewDistanceText.text = "View Distance: " + tempCellScript.viewDistance.ToString();
        wanderDeviationText.text = "Wander Deviation: " + tempCellScript.wanderDeviation.ToString();
        energyText.text = "Energy: " + tempCellScript.energy.ToString();
    }

}
