using UnityEngine;

/// <summary>
/// Handles debug mode toggled remotely via Firebase Remote Config key: _turn_on_debug_mode_ (or _turn_on_debug_mode).
/// When turned ON: unlocks all maps for current session, grants 100M gold once, logs analytics.
/// When turned OFF: restores normal map unlock progression, logs analytics.
/// </summary>
public class DebugModeHandler : MonoBehaviour
{
    public static DebugModeHandler Instance { get; private set; }

    private bool isDebugActive = false;
    private const string PREF_DEBUG_GOLD_GRANTED = "DebugMode_GoldGranted_Session";

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
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched += EvaluateDebugMode;
            EvaluateDebugMode();
        }
    }

    private void OnDestroy()
    {
        if (FirebaseRemoteConfigHelper.Instance != null)
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched -= EvaluateDebugMode;
    }

    /// <summary>
    /// Check the current remote config debug mode status and apply transitions.
    /// </summary>
    public void EvaluateDebugMode()
    {
        if (FirebaseRemoteConfigHelper.Instance == null) return;

        bool targetDebug = FirebaseRemoteConfigHelper.Instance.IsDebugMode;

        if (targetDebug && !isDebugActive)
        {
            // Transition: false -> true
            isDebugActive = true;
            MapSelectionManager.debugMode = true;

            // Grant 100M gold once per debug session
            if (PlayerPrefs.GetInt(PREF_DEBUG_GOLD_GRANTED, 0) == 0 && UserData.Instance != null)
            {
                UserData.Instance.AddGold(100_000_000);
                PlayerPrefs.SetInt(PREF_DEBUG_GOLD_GRANTED, 1);
                PlayerPrefs.Save();
            }

            FirebaseAnalyticsHelper.LogDebugModeActivated();
        }
        else if (!targetDebug && isDebugActive)
        {
            // Transition: true -> false
            isDebugActive = false;
            MapSelectionManager.debugMode = false;
            PlayerPrefs.DeleteKey(PREF_DEBUG_GOLD_GRANTED);
            PlayerPrefs.Save();

            FirebaseAnalyticsHelper.LogDebugModeDeactivated();
        }
        else
        {
            // Ensure static state stays consistent
            MapSelectionManager.debugMode = isDebugActive;
        }
    }
}

