using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsUI; // Panel Settings

    [SerializeField] private UISlideTween settingsSlideTween;

    public void StartGame()
    {
        LoadingSceneController.targetScene = "GameMap1";
        SceneManager.LoadScene("LoadingScene");
    }

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
    }
    
    public void CloseSettings()
    {
        if (settingsSlideTween != null)
            settingsSlideTween.Hide();
        else
            settingsUI.SetActive(false);
    }
}
