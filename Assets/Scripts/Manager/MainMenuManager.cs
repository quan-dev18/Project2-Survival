using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsUI;

    [SerializeField] private UISlideTween settingsSlideTween;

    public void StartGame()
    {
        LoadingSceneController.targetScene = "GameMap1";
        SceneManager.LoadScene("LoadingScene");
    }

    public void PlayTutorial()
    {
        LoadingSceneController.targetScene = "GameTutorial";
        SceneManager.LoadScene("LoadingScene");
    }

    [ContextMenu("Reset Tutorial Status")]
    public void ResetTutorial()
    {
        TutorialController.SetTutorialCompleted(false);
        Debug.Log("[MainMenuManager] Trạng thái Tutorial đã được reset.");
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
