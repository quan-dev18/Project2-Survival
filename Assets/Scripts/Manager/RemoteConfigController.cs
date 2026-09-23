using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central evaluator that runs on every scene load / loading screen.
/// Evaluates and applies all remote config switches (maintenance, force update, debug mode, ads, etc.).
/// </summary>
public class RemoteConfigController : MonoBehaviour
{
    public static RemoteConfigController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureHandlersExist();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        EvaluateAllConfigs();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EvaluateAllConfigs();
    }

    /// <summary>
    /// Spawns handler singletons if they are not already created.
    /// </summary>
    public void EnsureHandlersExist()
    {
        if (DebugModeHandler.Instance == null)
        {
            var go = new GameObject("DebugModeHandler");
            go.AddComponent<DebugModeHandler>();
        }

        if (MaintenanceModeHandler.Instance == null)
        {
            var go = new GameObject("MaintenanceModeHandler");
            go.AddComponent<MaintenanceModeHandler>();
        }

        if (ForceUpdateHandler.Instance == null)
        {
            var go = new GameObject("ForceUpdateHandler");
            go.AddComponent<ForceUpdateHandler>();
        }

        if (ABTestManager.Instance == null)
        {
            var go = new GameObject("ABTestManager");
            go.AddComponent<ABTestManager>();
        }

        if (AdUnlockTracker.Instance == null)
        {
            var go = new GameObject("AdUnlockTracker");
            go.AddComponent<AdUnlockTracker>();
        }

        if (InterstitialAdManager.Instance == null)
        {
            var go = new GameObject("InterstitialAdManager");
            go.AddComponent<InterstitialAdManager>();
        }

        if (AppOpenAdManager.Instance == null)
        {
            var go = new GameObject("AppOpenAdManager");
            go.AddComponent<AppOpenAdManager>();
        }
    }

    /// <summary>
    /// Check which features are turned ON and which are turned OFF according to Remote Config.
    /// Returns false if the game is blocked by maintenance or force update.
    /// </summary>
    public bool EvaluateAllConfigs()
    {
        EnsureHandlersExist();

        if (FirebaseRemoteConfigHelper.Instance == null)
        {
            Debug.Log("[RemoteConfigController] FirebaseRemoteConfigHelper not available yet.");
            return true;
        }

        var rc = FirebaseRemoteConfigHelper.Instance;

        // 1. Check Maintenance
        if (MaintenanceModeHandler.Instance != null && MaintenanceModeHandler.Instance.CheckMaintenance())
        {
            return false; // Block game entry
        }

        // 2. Check Force Update
        if (ForceUpdateHandler.Instance != null && ForceUpdateHandler.Instance.CheckForceUpdate())
        {
            return false; // Block game entry
        }

        // 3. Apply Debug Mode
        if (DebugModeHandler.Instance != null)
        {
            DebugModeHandler.Instance.EvaluateDebugMode();
        }

        // 4. Apply A/B Test Group
        if (ABTestManager.Instance != null)
        {
            ABTestManager.Instance.UpdateGroup();
        }

        return true;
    }
}

