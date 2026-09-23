using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    public static string targetScene;

    [SerializeField] private Image progressBar;
    [Tooltip("Image hiển thị ảnh loading. Ảnh sẽ được chọn NGẪU NHIÊN từ Loading Images mỗi lần vào loading.")]
    [SerializeField] private Image loadingImage;
    [Tooltip("List ảnh loading. Mỗi lần vào scene loading sẽ chọn 1 ảnh ngẫu nhiên trong list này.")]
    [SerializeField] private Sprite[] loadingImages;
    [SerializeField] private float lerpDuration = 1f;
    [SerializeField] private float firstTimeLerpDuration = 3f;
    [Tooltip("Nếu tích chọn, trạng thái lần đầu sẽ lưu vĩnh viễn vào PlayerPrefs. Nếu bỏ tích, mỗi lần mở game/bật Play mode đều tính lần đầu.")]
    [SerializeField] private bool persistFirstTimeAcrossSessions = false;

    private const string KEY_FIRST_TIME_LOADED = "FirstTimeLoadingCompleted";
    private static bool sessionFirstLoadDone = false;

    private bool IsFirstTime()
    {
        if (persistFirstTimeAcrossSessions)
        {
            return PlayerPrefs.GetInt(KEY_FIRST_TIME_LOADED, 0) == 0;
        }
        return !sessionFirstLoadDone;
    }

    private void MarkFirstTimeDone()
    {
        sessionFirstLoadDone = true;
        if (persistFirstTimeAcrossSessions)
        {
            PlayerPrefs.SetInt(KEY_FIRST_TIME_LOADED, 1);
            PlayerPrefs.Save();
        }
    }

    [ContextMenu("Reset First Time Status")]
    public static void ResetFirstTimeStatus()
    {
        sessionFirstLoadDone = false;
        PlayerPrefs.DeleteKey(KEY_FIRST_TIME_LOADED);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu";
        }

        PickRandomLoadingImage();

        StartCoroutine(LoadSceneAsync());
    }

    /// <summary>
    /// Chọn 1 ảnh loading ngẫu nhiên từ danh sách Loading Images.
    /// Bỏ qua nếu chưa gán list hoặc chưa gán Image đích.
    /// </summary>
    private void PickRandomLoadingImage()
    {
        if (loadingImage == null || loadingImages == null || loadingImages.Length == 0)
            return;

        Sprite picked = loadingImages[Random.Range(0, loadingImages.Length)];
        if (picked != null)
            loadingImage.sprite = picked;
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        bool isFirst = IsFirstTime();
        float duration = isFirst ? firstTimeLerpDuration : lerpDuration;
        float elapsed = 0f;

        // Chạy mượt mà từ đầu đến cuối theo lerp time (smoothstep)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t);
            UpdateUI(smoothT);
            yield return null;
        }

        UpdateUI(1f);

        // Đảm bảo dữ liệu scene thực tế trong RAM đã sẵn sàng trước khi cho phép kích hoạt
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        yield return new WaitForSeconds(isFirst ? 0.3f : 0.15f);

        // Kiểm tra Remote Config mỗi lần loading: cái nào bật, cái nào tắt
        if (RemoteConfigController.Instance == null)
        {
            var rcGo = new GameObject("RemoteConfigController");
            rcGo.AddComponent<RemoteConfigController>();
        }

        if (RemoteConfigController.Instance != null)
        {
            while (!RemoteConfigController.Instance.EvaluateAllConfigs())
            {
                // Nếu đang bảo trì hoặc bắt buộc cập nhật, giữ màn hình loading và hiển thị thông báo
                yield return new WaitForSeconds(1f);
            }
        }

        MarkFirstTimeDone();
        operation.allowSceneActivation = true;
    }

    private void UpdateUI(float progress)
    {
        if (progressBar != null)
            progressBar.fillAmount = progress;

    }
}
