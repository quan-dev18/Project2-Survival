using Firebase;
using Firebase.Analytics;
using GoogleMobileAds.Common;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance { get; private set; }
    public bool IsInitialized { get; private set; }

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

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                IsInitialized = true;

                // Initialize Remote Config and handlers on main thread
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (FirebaseRemoteConfigHelper.Instance == null)
                    {
                        var go = new GameObject("FirebaseRemoteConfigHelper");
                        go.AddComponent<FirebaseRemoteConfigHelper>();
                    }
                    FirebaseRemoteConfigHelper.Instance.Initialize();

                    if (RemoteConfigController.Instance == null)
                    {
                        var rcGo = new GameObject("RemoteConfigController");
                        rcGo.AddComponent<RemoteConfigController>();
                    }
                });
            }
            else
            {
                Debug.LogError($"Firebase initialization failed: {dependencyStatus}");
            }
        });
    }
}
