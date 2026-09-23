using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles debug mode toggled remotely via Firebase Remote Config key: _turn_on_debug_mode_ (or _turn_on_debug_mode).
/// When turned ON: unlocks all maps for current session, grants 100M gold once, shows visual badge, logs analytics.
/// When turned OFF: restores normal map unlock progression, removes badge, revokes/deducts 100M debug gold, logs analytics.
/// </summary>
public class DebugModeHandler : MonoBehaviour
{
    public static DebugModeHandler Instance { get; private set; }

    private bool isDebugActive = false;
    private const string PREF_DEBUG_GOLD_GRANTED = "DebugMode_GoldGranted_Session";
    private const string PREF_PRE_DEBUG_GOLD = "DebugMode_PreDebugGold";
    private const string PREF_DEBUG_WAS_ACTIVE = "DebugMode_WasActive";
    private const int DEBUG_GOLD_AMOUNT = 100_000_000;

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
            if (FirebaseRemoteConfigHelper.Instance.IsFetched)
            {
                EvaluateDebugMode();
            }
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
            PlayerPrefs.SetInt(PREF_DEBUG_WAS_ACTIVE, 1);
            PlayerPrefs.Save();

            MapSelectionManager.debugMode = true;
            if (MapSelectionManager.Instance != null)
            {
                MapSelectionManager.Instance.RefreshDebugState();
            }

            // Grant 100M gold once per debug session
            if (PlayerPrefs.GetInt(PREF_DEBUG_GOLD_GRANTED, 0) == 0)
            {
                if (UserData.Instance != null)
                {
                    GrantGoldInternal();
                }
                else
                {
                    StartCoroutine(GrantGoldWhenReady());
                }
            }

            FirebaseAnalyticsHelper.LogDebugModeActivated();
        }
        else if (!targetDebug)
        {
            // Either transitioned true -> false, or debug mode was active in previous run / gold remains to be cleaned up
            bool wasActive = isDebugActive || PlayerPrefs.GetInt(PREF_DEBUG_WAS_ACTIVE, 0) == 1;
            bool hasDebugGold = PlayerPrefs.GetInt(PREF_DEBUG_GOLD_GRANTED, 0) == 1 
                                || PlayerPrefs.HasKey(PREF_PRE_DEBUG_GOLD)
                                || (UserData.Instance != null && UserData.Instance.Gold >= 50_000_000);

            if (wasActive || hasDebugGold)
            {
                isDebugActive = false;
                MapSelectionManager.debugMode = false;
                if (MapSelectionManager.Instance != null)
                {
                    MapSelectionManager.Instance.RefreshDebugState();
                }

                if (UserData.Instance != null)
                {
                    RevertGoldInternal();
                }
                else
                {
                    StartCoroutine(RevertGoldWhenReady());
                }

                PlayerPrefs.DeleteKey(PREF_DEBUG_WAS_ACTIVE);
                PlayerPrefs.DeleteKey(PREF_DEBUG_GOLD_GRANTED);
                PlayerPrefs.DeleteKey(PREF_PRE_DEBUG_GOLD);
                PlayerPrefs.Save();

                if (wasActive)
                {
                    FirebaseAnalyticsHelper.LogDebugModeDeactivated();
                }
            }
        }
        else
        {
            // Ensure state stays consistent
            MapSelectionManager.debugMode = isDebugActive;
            if (MapSelectionManager.Instance != null)
            {
                MapSelectionManager.Instance.RefreshDebugState();
            }
        }
    }

    private void GrantGoldInternal()
    {
        if (UserData.Instance == null) return;
        PlayerPrefs.SetInt(PREF_PRE_DEBUG_GOLD, UserData.Instance.Gold);
        UserData.Instance.AddGold(DEBUG_GOLD_AMOUNT);
        PlayerPrefs.SetInt(PREF_DEBUG_GOLD_GRANTED, 1);
        PlayerPrefs.Save();
    }

    private void RevertGoldInternal()
    {
        if (UserData.Instance == null) return;

        int currentGold = UserData.Instance.Gold;
        int preGold = PlayerPrefs.GetInt(PREF_PRE_DEBUG_GOLD, -1);

        if (preGold >= 0)
        {
            // Restore original gold plus any earned gold beyond the 100M grant
            int targetGold = Mathf.Max(preGold, Mathf.Max(0, currentGold - DEBUG_GOLD_AMOUNT));
            UserData.Instance.SetGold(targetGold);
        }
        else if (currentGold >= DEBUG_GOLD_AMOUNT)
        {
            // Deduct 100M debug gold
            UserData.Instance.SetGold(currentGold - DEBUG_GOLD_AMOUNT);
        }
        else if (currentGold >= 50_000_000)
        {
            // Fallback for cases where player spent some debug gold and preGold wasn't saved
            UserData.Instance.SetGold(0);
        }
    }

    private IEnumerator GrantGoldWhenReady()
    {
        while (UserData.Instance == null)
        {
            yield return null;
        }

        if (isDebugActive && PlayerPrefs.GetInt(PREF_DEBUG_GOLD_GRANTED, 0) == 0)
        {
            GrantGoldInternal();
        }
    }

    private IEnumerator RevertGoldWhenReady()
    {
        while (UserData.Instance == null)
        {
            yield return null;
        }

        RevertGoldInternal();
        PlayerPrefs.DeleteKey(PREF_DEBUG_WAS_ACTIVE);
        PlayerPrefs.DeleteKey(PREF_DEBUG_GOLD_GRANTED);
        PlayerPrefs.DeleteKey(PREF_PRE_DEBUG_GOLD);
        PlayerPrefs.Save();
    }

    private void OnGUI()
    {
        if (!isDebugActive) return;

        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0.85f, 0.15f, 0.15f, 0.95f);

        float scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        float width = 210f * Mathf.Max(1f, scale);
        float height = 40f * Mathf.Max(1f, scale);

        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(16 * Mathf.Max(1f, scale)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;

        if (GUI.Button(new Rect(20, 20, width, height), "★ DEBUG MODE ON", style))
        {
            if (DebugMenu.Instance != null)
            {
                DebugMenu.Instance.ToggleDebugMenu();
            }
        }
    }
}
