using UnityEngine;

public class PausePanel : MonoBehaviour
{
    public void OnResume()
    {
        GameManager.Instance?.SetState(GameState.Playing);
    }
    public void OnQuit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}