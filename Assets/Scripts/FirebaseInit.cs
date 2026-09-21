using Firebase;
using Firebase.Analytics;
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
                Debug.Log("Firebase Analytics initialized successfully.");
            }
            else
            {
                Debug.LogError($"Firebase initialization failed: {dependencyStatus}");
            }
        });
    }
}
