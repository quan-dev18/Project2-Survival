using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compares Firebase Remote Config key: _force_update_version with Application.version.
/// If current version is older than the required version, displays a non-dismissible update popup.
/// </summary>
public class ForceUpdateHandler : MonoBehaviour
{
    public static ForceUpdateHandler Instance { get; private set; }

    public bool IsUpdateRequired { get; private set; }

    private GameObject updateCanvas;
    private TextMeshProUGUI updateMessageText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (FirebaseRemoteConfigHelper.Instance != null)
        {
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched += HandleConfigFetched;
            CheckForceUpdate();
        }
    }

    private void OnDestroy()
    {
        if (FirebaseRemoteConfigHelper.Instance != null)
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched -= HandleConfigFetched;
    }

    private void HandleConfigFetched()
    {
        CheckForceUpdate();
    }

    /// <summary>
    /// Checks if an update is required. Returns true if current version is older.
    /// </summary>
    public bool CheckForceUpdate()
    {
        if (FirebaseRemoteConfigHelper.Instance == null) return false;

        string requiredVersion = FirebaseRemoteConfigHelper.Instance.ForceUpdateVersion;
        if (string.IsNullOrEmpty(requiredVersion))
        {
            IsUpdateRequired = false;
            HideUpdateUI();
            return false;
        }

        string currentVersion = Application.version;
        if (IsVersionOlder(currentVersion, requiredVersion))
        {
            if (!IsUpdateRequired)
            {
                IsUpdateRequired = true;
                FirebaseAnalyticsHelper.LogForceUpdateTriggered(requiredVersion, currentVersion);
                Debug.LogWarning($"[ForceUpdateHandler] Update REQUIRED: Current {currentVersion} < Required {requiredVersion}");
            }
            ShowUpdateUI(requiredVersion, currentVersion);
            return true;
        }
        else
        {
            IsUpdateRequired = false;
            HideUpdateUI();
            return false;
        }
    }

    private bool IsVersionOlder(string current, string required)
    {
        try
        {
            Version currentV = new Version(NormalizeVersion(current));
            Version requiredV = new Version(NormalizeVersion(required));
            return currentV < requiredV;
        }
        catch
        {
            return string.Compare(current, required, StringComparison.OrdinalIgnoreCase) < 0;
        }
    }

    private string NormalizeVersion(string v)
    {
        if (string.IsNullOrEmpty(v)) return "0.0.0.0";
        string[] parts = v.Trim().Split('.');
        if (parts.Length == 1) return $"{parts[0]}.0.0.0";
        if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0.0";
        if (parts.Length == 3) return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
        return v;
    }

    private void ShowUpdateUI(string req, string cur)
    {
        if (updateCanvas == null)
            CreateUpdateUI();

        if (updateMessageText != null)
            updateMessageText.text = $"Đã có phiên bản mới ({req})!\nPhiên bản hiện tại ({cur}) không còn được hỗ trợ.\nVui lòng cập nhật để tiếp tục chơi.";

        if (updateCanvas != null)
            updateCanvas.SetActive(true);
    }

    private void HideUpdateUI()
    {
        if (updateCanvas != null)
            updateCanvas.SetActive(false);
    }

    private void CreateUpdateUI()
    {
        updateCanvas = new GameObject("ForceUpdateCanvas");
        DontDestroyOnLoad(updateCanvas);

        Canvas canvas = updateCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99998;

        CanvasScaler scaler = updateCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        updateCanvas.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(updateCanvas.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(bg.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(800, 600);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(content.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "CẬP NHẬT PHIÊN BẢN MỚI";
        title.fontSize = 50;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.2f, 0.8f, 1f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(750, 100);

        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(content.transform, false);
        updateMessageText = msgObj.AddComponent<TextMeshProUGUI>();
        updateMessageText.fontSize = 32;
        updateMessageText.alignment = TextAlignmentOptions.Center;
        updateMessageText.color = Color.white;
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchoredPosition = new Vector2(0, 10);
        msgRect.sizeDelta = new Vector2(700, 200);

        GameObject btnObj = new GameObject("UpdateButton");
        btnObj.transform.SetParent(content.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.7f, 0.35f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -150);
        btnRect.sizeDelta = new Vector2(320, 85);

        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "CẬP NHẬT NGAY";
        btnTxt.fontSize = 32;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            string url = $"market://details?id={Application.identifier}";
#if UNITY_IOS
            url = $"itms-apps://itunes.apple.com/app/id{Application.identifier}";
#endif
            Application.OpenURL(url);
        });
    }
}

