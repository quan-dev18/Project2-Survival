using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    public static string targetScene;

    [SerializeField] private Image progressBar;
    [SerializeField] private float lerpDuration = 1f;

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

        float elapsed = 0f;

        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lerpDuration);
            float smoothT = t * t * (3f - 2f * t);
            UpdateUI(smoothT);
            yield return null;
        }

        UpdateUI(1f);

        yield return new WaitForSeconds(0.5f);

        operation.allowSceneActivation = true;
    }

    private void UpdateUI(float progress)
    {
        if (progressBar != null)
            progressBar.fillAmount = progress;

    }
}
