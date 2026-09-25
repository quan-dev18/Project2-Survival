using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

/// <summary>
/// Manages Interstitial Ads with Remote Config interval controls.
/// Controlled by keys: _ads_enabled, _ads_interstitial_enabled, _ads_interstitial_interval.
/// </summary>
public class InterstitialAdManager : MonoBehaviour
{
    public static InterstitialAdManager Instance { get; private set; }

    // Only read inside UNITY_ANDROID builds (see LoadAd); silence CS0414 elsewhere.
#pragma warning disable CS0414
    [SerializeField] private string androidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
#pragma warning restore CS0414

    private InterstitialAd interstitialAd;
    private float lastInterstitialTime = -999f;
#pragma warning disable CS0414
    private bool isLoading = false;
#pragma warning restore CS0414

    public bool IsReady => interstitialAd != null && interstitialAd.CanShowAd();

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

    public void LoadAd()
    {
        if (FirebaseRemoteConfigHelper.Instance != null && (!FirebaseRemoteConfigHelper.Instance.IsAdsEnabled || !FirebaseRemoteConfigHelper.Instance.IsInterstitialEnabled))
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (isLoading || (interstitialAd != null && interstitialAd.CanShowAd())) return;

        isLoading = true;
        DestroyAd();

        AdRequest request = new AdRequest();
        InterstitialAd.Load(androidInterstitialId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            isLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("[InterstitialAdManager] Failed to load interstitial ad: " + error);
                return;
            }

            interstitialAd = ad;
            RegisterEventHandlers(interstitialAd);
        });
#endif
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            FirebaseAnalyticsHelper.LogInterstitialAdClicked();
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                lastInterstitialTime = Time.time;
                LoadAd();
            });
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadAd();
            });
        };
    }

    /// <summary>
    /// Shows the interstitial ad if the interval has passed and ads are enabled in Remote Config.
    /// </summary>
    public bool ShowInterstitialAd(Action onClosed = null)
    {
        if (FirebaseRemoteConfigHelper.Instance != null)
        {
            if (!FirebaseRemoteConfigHelper.Instance.IsAdsEnabled || !FirebaseRemoteConfigHelper.Instance.IsInterstitialEnabled)
            {
                onClosed?.Invoke();
                return false;
            }

            int interval = FirebaseRemoteConfigHelper.Instance.InterstitialInterval;
            float elapsed = Time.time - lastInterstitialTime;
            if (elapsed < interval)
            {
                onClosed?.Invoke();
                return false;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            FirebaseAnalyticsHelper.LogInterstitialAdShown();
            lastInterstitialTime = Time.time;
            interstitialAd.Show();
            onClosed?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning("[InterstitialAdManager] Interstitial ad not ready yet, loading...");
            LoadAd();
            onClosed?.Invoke();
            return false;
        }
#else
        lastInterstitialTime = Time.time;
        onClosed?.Invoke();
        return true;
#endif
    }

    private void DestroyAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }
}

