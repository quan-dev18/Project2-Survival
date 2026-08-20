using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsUI; // Panel Settings
    public void StartGame()
    {
        SceneManager.LoadScene("GameMap1");
    }

    public void OpenSettings()
    {
        //Set Active cho Panel Settings
        settingsUI.SetActive(true);
    }
    
    public void CloseSettings()
    {
        //Set Active cho Panel Settings
        settingsUI.SetActive(false);
    }
}
