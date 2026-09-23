using Firebase;
using Firebase.Analytics;
using GoogleMobileAds.Common;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            var go = new GameObject("FirebaseInit");
            go.AddComponent<FirebaseInit>();
            DontDestroyOnLoad(go);
        }
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
    }

    private volatile bool pendingMainThreadInit = false;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                IsInitialized = true;
                pendingMainThreadInit = true;
            }
            else
            {
                Debug.LogError($"Firebase initialization failed: {dependencyStatus}");
            }
        });
    }

    void Update()
    {
        if (pendingMainThreadInit)
        {
            pendingMainThreadInit = false;
            InitializeRemoteConfigHandlers();
        }
    }

    private void InitializeRemoteConfigHandlers()
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
    }
}
