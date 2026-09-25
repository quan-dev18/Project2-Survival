using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

/// <summary>
/// Manages App Open Ads shown when resuming the app from background.
/// Controlled by Remote Config keys: _ads_enabled, _ads_app_open_enabled, _ads_app_open_delay.
/// </summary>
public class AppOpenAdManager : MonoBehaviour
{
    public static AppOpenAdManager Instance { get; private set; }

    // Both fields are only read inside UNITY_ANDROID builds; silence CS0414 elsewhere.
#pragma warning disable CS0414
    [SerializeField] private string androidAppOpenId = "ca-app-pub-3940256099942544/9257395921";
#pragma warning restore CS0414

    private AppOpenAd appOpenAd;
    private DateTime loadTime;
    private bool isShowing = false;
#pragma warning disable CS0414
    private bool isLoading = false;
#pragma warning restore CS0414
    private Coroutine delayRoutine;

    public bool IsReady => appOpenAd != null && appOpenAd.CanShowAd() && (DateTime.UtcNow - loadTime).TotalHours < 4;

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
        LoadAd();
    }

    private void OnDestroy()
    {
        DestroyAd();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            // App resumed from background
            TriggerAppOpenWithDelay();
        }
    }

    public void LoadAd()
    {
        if (FirebaseRemoteConfigHelper.Instance != null && (!FirebaseRemoteConfigHelper.Instance.IsAdsEnabled || !FirebaseRemoteConfigHelper.Instance.IsAppOpenEnabled))
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (isLoading || IsReady) return;

        isLoading = true;
        DestroyAd();

        AdRequest request = new AdRequest();
        AppOpenAd.Load(androidAppOpenId, request, (AppOpenAd ad, LoadAdError error) =>
        {
            isLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AppOpenAdManager] Failed to load app open ad: " + error);
                return;
            }

            appOpenAd = ad;
            loadTime = DateTime.UtcNow;
            RegisterEventHandlers(appOpenAd);
        });
#endif
    }

    private void RegisterEventHandlers(AppOpenAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isShowing = false;
                LoadAd();
            });
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isShowing = false;
                LoadAd();
            });
        };
    }

    public void TriggerAppOpenWithDelay()
    {
        if (delayRoutine != null)
            StopCoroutine(delayRoutine);

        delayRoutine = StartCoroutine(ShowWithDelayRoutine());
    }

    private IEnumerator ShowWithDelayRoutine()
    {
        int delay = 5;
        if (FirebaseRemoteConfigHelper.Instance != null)
        {
            if (!FirebaseRemoteConfigHelper.Instance.IsAdsEnabled || !FirebaseRemoteConfigHelper.Instance.IsAppOpenEnabled)
                yield break;
            delay = FirebaseRemoteConfigHelper.Instance.AppOpenDelay;
        }

        yield return new WaitForSecondsRealtime(delay);

        ShowAd();
    }

    public void ShowAd()
    {
        if (isShowing) return;

        if (FirebaseRemoteConfigHelper.Instance != null && (!FirebaseRemoteConfigHelper.Instance.IsAdsEnabled || !FirebaseRemoteConfigHelper.Instance.IsAppOpenEnabled))
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (IsReady)
        {
            isShowing = true;
            FirebaseAnalyticsHelper.LogAppOpenAdShown();
            appOpenAd.Show();
        }
        else
        {
            Debug.LogWarning("[AppOpenAdManager] App open ad is not ready yet, loading...");
            LoadAd();
        }
#endif
    }

    private void DestroyAd()
    {
        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
            appOpenAd = null;
        }
    }
}

