using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles maintenance mode triggered by Firebase Remote Config key: _maintenance_mode.
/// Displays a full-screen maintenance overlay and blocks user from entering the game.
/// </summary>
public class MaintenanceModeHandler : MonoBehaviour
{
    public static MaintenanceModeHandler Instance { get; private set; }

    public bool IsInMaintenance { get; private set; }

    private GameObject maintenanceCanvas;
    private TextMeshProUGUI messageText;

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
            CheckMaintenance();
        }

        // Periodic check every 5 minutes
        StartCoroutine(PeriodicCheckRoutine());
    }

    private void OnDestroy()
    {
        if (FirebaseRemoteConfigHelper.Instance != null)
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched -= HandleConfigFetched;
    }

    private void HandleConfigFetched()
    {
        CheckMaintenance();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            CheckMaintenance();
        }
    }

    private IEnumerator PeriodicCheckRoutine()
    {
        var wait = new WaitForSecondsRealtime(300f);
        while (true)
        {
            yield return wait;
            CheckMaintenance();
        }
    }

    /// <summary>
    /// Check whether maintenance mode is active. Returns true if in maintenance.
    /// </summary>
    public bool CheckMaintenance()
    {
        if (FirebaseRemoteConfigHelper.Instance == null) return false;

        bool active = FirebaseRemoteConfigHelper.Instance.IsMaintenanceMode;
        string message = FirebaseRemoteConfigHelper.Instance.MaintenanceMessage;

        if (active)
        {
            if (!IsInMaintenance)
            {
                IsInMaintenance = true;
                FirebaseAnalyticsHelper.LogMaintenanceModeTriggered();
                Debug.LogWarning("[MaintenanceModeHandler] Maintenance mode ACTIVATED: " + message);
            }
            ShowMaintenanceUI(message);
        }
        else
        {
            if (IsInMaintenance)
            {
                IsInMaintenance = false;
                HideMaintenanceUI();
            }
        }

        return active;
    }

    private void ShowMaintenanceUI(string message)
    {
        if (maintenanceCanvas == null)
            CreateMaintenanceUI();

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? "The game is currently under maintenance. Please check back later." : message;

        if (maintenanceCanvas != null)
            maintenanceCanvas.SetActive(true);
    }

    private void HideMaintenanceUI()
    {
        if (maintenanceCanvas != null)
            maintenanceCanvas.SetActive(false);
    }

    private void CreateMaintenanceUI()
    {
        maintenanceCanvas = new GameObject("MaintenanceCanvas");
        DontDestroyOnLoad(maintenanceCanvas);

        Canvas canvas = maintenanceCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99999;

        CanvasScaler scaler = maintenanceCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        maintenanceCanvas.AddComponent<GraphicRaycaster>();

        // Background panel
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(maintenanceCanvas.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Content panel
        GameObject content = new GameObject("Content");
        content.transform.SetParent(bg.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(800, 600);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(content.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "BẢO TRÌ HỆ THỐNG";
        title.fontSize = 54;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.8f, 0.2f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(750, 100);

        // Message
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(content.transform, false);
        messageText = msgObj.AddComponent<TextMeshProUGUI>();
        messageText.fontSize = 32;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchoredPosition = new Vector2(0, 10);
        msgRect.sizeDelta = new Vector2(700, 200);

        // Retry Button
        GameObject btnObj = new GameObject("RetryButton");
        btnObj.transform.SetParent(content.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.9f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -150);
        btnRect.sizeDelta = new Vector2(300, 80);

        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "Thử lại";
        btnTxt.fontSize = 32;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            if (FirebaseRemoteConfigHelper.Instance != null)
                FirebaseRemoteConfigHelper.Instance.Initialize();
            CheckMaintenance();
        });
    }
}

