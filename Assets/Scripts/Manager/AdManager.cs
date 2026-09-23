using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

/// <summary>
/// Owns the AdMob banner: shows it on the main menu, hides it everywhere else.
/// Attach to a GameObject in the MainMenu scene (created once, persists).
/// In the Editor, a mock banner is drawn so layout overlap can be estimated
/// (real ads only render on device).
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    // Config below is consumed in device-only (#if) code paths; unused in the Editor by design.
#pragma warning disable CS0414
    [Header("Ad Units")]
    [Tooltip("Google test banner. Replace with your real unit ID for release builds.")]
    [SerializeField] private string androidBannerId = "ca-app-pub-3940256099942544/6300978111";
    [Tooltip("Google test rewarded. Replace with your real unit ID for release builds.")]
    [SerializeField] private string androidRewardedId = "ca-app-pub-3940256099942544/5224354917";
    [Tooltip("Rewarded unit for the victory double. Test ID until release.")]
    [SerializeField] private string androidVictoryRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Header("Reward")]
    [Tooltip("Coins granted per completed rewarded ad.")]
    [SerializeField] private int rewardCoinAmount = 100;
#pragma warning restore CS0414

    [Header("Scenes")]
    [Tooltip("Banner is visible only in this scene.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

#if UNITY_EDITOR
    [Header("Editor Preview (mock ad, Editor only)")]
    [Tooltip("Simulated screen density, in dpi. Typical Android phone ~420.")]
    [SerializeField] private float previewDpi = 420f;
    [Tooltip("Standard banner is 320x50 dp.")]
    [SerializeField] private bool showPreviewInEditor = true;
    private GameObject previewObj;
#endif

    private BannerView bannerView;

    private class RewardedSlot
    {
        public string label;
        public string adUnitId;
        public RewardedAd ad;
        public bool loading;
    }

    private RewardedSlot shopSlot;
    private RewardedSlot victorySlot;

    /// <summary>True when the shop rewarded ad is loaded and ready to show.</summary>
    public bool IsRewardedReady => IsSlotReady(shopSlot);

    /// <summary>True when the victory-double rewarded ad is loaded and ready to show.</summary>
    public bool IsVictoryRewardedReady => IsSlotReady(victorySlot);

    private static bool IsSlotReady(RewardedSlot slot)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return slot != null && slot.ad != null && slot.ad.CanShowAd();
#else
        return false;
#endif
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void Start()
    {
        MobileAds.Initialize(_ =>
        {
            CreateBanner();
            UpdateBannerVisibility();
            shopSlot = new RewardedSlot { label = "Shop", adUnitId = androidRewardedId };
            victorySlot = new RewardedSlot { label = "Victory", adUnitId = androidVictoryRewardedId };
            LoadSlot(shopSlot);
            LoadSlot(victorySlot);
        });
#if UNITY_EDITOR
        CreatePreview();
        UpdatePreviewVisibility();
#endif
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (Instance == this)
        {
            bannerView?.Destroy();
            bannerView = null;
            DestroySlot(shopSlot);
            DestroySlot(victorySlot);
        }
    }

    private static void DestroySlot(RewardedSlot slot)
    {
        if (slot == null) return;
        slot.ad?.Destroy();
        slot.ad = null;
    }

    /// <summary>Shows the shop rewarded ad; grants <see cref="rewardCoinAmount"/> coins on completion.</summary>
    public void ShowRewardedAd()
    {
        FirebaseAnalyticsHelper.LogAdRewardedShown("shop");
        ShowSlot(shopSlot, _ =>
        {
            if (UserData.Instance != null)
            {
                UserData.Instance.AddGold(rewardCoinAmount);
                FirebaseAnalyticsHelper.LogAdRewardedCompleted("shop", rewardCoinAmount);
            }
        }, null);
    }

    /// <summary>
    /// Shows the victory-double rewarded ad.
    /// onEarned runs on completion; onFinished runs when the ad dismisses for any
    /// reason (after a reward too). Both are invoked on the Unity main thread.
    /// </summary>
    public void ShowVictoryRewardedAd(System.Action onEarned, System.Action onFinished)
    {
        FirebaseAnalyticsHelper.LogAdRewardedShown("victory");
        if (!ShowSlot(victorySlot, _ =>
        {
            try { onEarned?.Invoke(); }
            finally { onFinished?.Invoke(); }
        },
        onFinished))
        {
            // Not ready (or Editor): release the caller UI immediately.
            onFinished?.Invoke();
        }
    }

    /// <summary>
    /// Shows a rewarded ad to unlock content (hero or skin).
    /// </summary>
    public void ShowUnlockRewardedAd(string placement, System.Action onEarned, System.Action onFinished = null)
    {
        FirebaseAnalyticsHelper.LogAdRewardedShown(placement);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ShowSlot(shopSlot, _ =>
        {
            try { onEarned?.Invoke(); }
            finally { onFinished?.Invoke(); }
        },
        onFinished))
        {
            onFinished?.Invoke();
        }
#else
        onEarned?.Invoke();
        onFinished?.Invoke();
#endif
    }

    /// <returns>False when there was nothing to show.</returns>
    private bool ShowSlot(RewardedSlot slot, System.Action<Reward> onReward, System.Action onFinished)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (slot != null && slot.ad != null && slot.ad.CanShowAd())
        {
            slot.ad.OnAdFullScreenContentClosed += () =>
                MobileAdsEventExecutor.ExecuteInUpdate(() => onFinished?.Invoke());
            slot.ad.OnAdFullScreenContentFailed += (_) =>
                MobileAdsEventExecutor.ExecuteInUpdate(() => onFinished?.Invoke());
            slot.ad.Show((Reward reward) =>
            {
                // Ad callbacks arrive off the main thread - marshal before touching game state/UI.
                MobileAdsEventExecutor.ExecuteInUpdate(() => onReward?.Invoke(reward));
            });
            return true;
        }
        Debug.LogWarning($"[AdManager] {slot?.label} rewarded ad not ready yet, loading...");
        if (slot != null) LoadSlot(slot);
        return false;
#else
        Debug.LogWarning("[AdManager] Rewarded ads only run on Android builds (use a dev build to test).");
        return false;
#endif
    }

    private void LoadSlot(RewardedSlot slot)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (slot == null || slot.loading) return;
        slot.loading = true;
        slot.ad?.Destroy();
        slot.ad = null;
        RewardedAd.Load(slot.adUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            slot.loading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdManager] {slot.label} rewarded ad failed to load: " + error);
                return;
            }
            slot.ad = ad;
            slot.ad.OnAdFullScreenContentClosed += () => LoadSlot(slot);
            slot.ad.OnAdFullScreenContentFailed += (_) => LoadSlot(slot);
        });
#endif
    }

    private void OnSceneChanged(Scene _, Scene __)
    {
        UpdateBannerVisibility();
#if UNITY_EDITOR
        UpdatePreviewVisibility();
#endif
    }

    private void CreateBanner()
    {
        if (FirebaseRemoteConfigHelper.Instance != null && !FirebaseRemoteConfigHelper.Instance.IsBannerEnabled)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        bannerView?.Destroy();
        bannerView = new BannerView(androidBannerId, AdSize.Banner, AdPosition.Top);
        bannerView.LoadAd(new AdRequest());
#else
        Debug.Log("[AdManager] Banner ads only run on Android builds (skipped in Editor).");
#endif
    }

    private void UpdateBannerVisibility()
    {
        if (bannerView == null) return;

        bool bannerEnabled = FirebaseRemoteConfigHelper.Instance == null || FirebaseRemoteConfigHelper.Instance.IsBannerEnabled;
        if (bannerEnabled && SceneManager.GetActiveScene().name == mainMenuSceneName)
            bannerView.Show();
        else
            bannerView.Hide();
    }

    /// <summary>
    /// Refresh ad states when remote config changes.
    /// </summary>
    public void RefreshAdSettings()
    {
        UpdateBannerVisibility();
#if UNITY_EDITOR
        UpdatePreviewVisibility();
#endif
    }

    /// <summary>
    /// Shows an interstitial ad via InterstitialAdManager if cooldown and config permit.
    /// </summary>
    public void ShowInterstitialAd(System.Action onClosed = null)
    {
        if (InterstitialAdManager.Instance != null)
            InterstitialAdManager.Instance.ShowInterstitialAd(onClosed);
        else
            onClosed?.Invoke();
    }

    /// <summary>
    /// Shows an app open ad via AppOpenAdManager if config permits.
    /// </summary>
    public void ShowAppOpenAd()
    {
        if (AppOpenAdManager.Instance != null)
            AppOpenAdManager.Instance.ShowAd();
    }

#if UNITY_EDITOR
    // Draws a 320x50 dp box at top-center so overlap can be judged in Play mode.
    // Approximation only: real height varies slightly by device density.
    private void CreatePreview()
    {
        if (previewObj != null) return;

        var canvasObj = new GameObject("AdPreviewCanvas");
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<GraphicRaycaster>();

        previewObj = new GameObject("AdPreviewBanner");
        previewObj.transform.SetParent(canvasObj.transform, false);
        var rect = previewObj.AddComponent<RectTransform>();
        float pxPerDp = Mathf.Max(80f, previewDpi) / 160f;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float pw = 320f * pxPerDp, ph = 50f * pxPerDp;
        // Fit inside narrow/short Game views so Free Aspect can't exaggerate it.
        float maxW = Screen.width * 0.95f;
        if (pw > maxW && maxW > 0f)
        {
            float s = maxW / pw;
            pw = maxW;
            ph *= s;
        }
        rect.sizeDelta = new Vector2(pw, ph);
        rect.anchoredPosition = Vector2.zero;

        var img = previewObj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        img.raycastTarget = false;

        var txtObj = new GameObject("Label");
        txtObj.transform.SetParent(previewObj.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.text = "AD PREVIEW 320x50";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 24;
        txt.color = Color.black;
        txt.raycastTarget = false;
    }

    private void UpdatePreviewVisibility()
    {
        if (previewObj == null) return;
        previewObj.transform.parent.gameObject.SetActive(
            showPreviewInEditor && SceneManager.GetActiveScene().name == mainMenuSceneName);
    }
#endif
}
