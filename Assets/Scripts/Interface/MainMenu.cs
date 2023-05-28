using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MainMenu : MonoBehaviour
{
    // Screens
    [Header("Screens")]
    public GameObject titleScreen;
    public GameObject optionsScreen;

    // Options
    [Header("Options")]
    public bool isFullscreen;
    public TMP_Text fullscreenText;
    public bool soundOn;
    public TMP_Text soundText;

    void Start()
    {
        EnterTitleScreen();
        isFullscreen = true;
        soundOn = true;
    }

    public void EnterTitleScreen()
    {
        titleScreen.SetActive(true);
        optionsScreen.SetActive(false);
    }      
    
    public void EnterOptionsScreen()
    {
        titleScreen.SetActive(false);
        optionsScreen.SetActive(true);
    }

    public void StartSimulation() 
    {
        SceneManager.LoadScene("Simulation");
    }

    public void Exit() 
    {
        Application.Quit();
    }

    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        Screen.fullScreen = isFullscreen;
        if (isFullscreen)
        {
            fullscreenText.text = "Fullscreen";
        } else
        {
            fullscreenText.text = "Windowed";
        }
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
        if (soundOn)
        {
            soundText.text = "Sound On";
        } else
        {
            soundText.text = "Sound Off";
        }
    }
}
