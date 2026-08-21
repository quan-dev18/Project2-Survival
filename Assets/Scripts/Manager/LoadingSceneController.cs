using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    public static string targetScene;

    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text loadingText;

    private void Start()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu";
        }

        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        float progress = 0f;

        while (!operation.isDone)
        {
            progress = Mathf.MoveTowards(progress, operation.progress, Time.deltaTime);
            UpdateUI(progress);

            if (operation.progress >= 0.9f)
            {
                UpdateUI(1f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateUI(float progress)
    {
        if (progressBar != null)
            progressBar.fillAmount = progress;

        if (loadingText != null)
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
    }
}
