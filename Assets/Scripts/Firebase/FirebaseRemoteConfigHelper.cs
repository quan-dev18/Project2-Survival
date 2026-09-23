using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Centralized Firebase Remote Config helper.
/// Handles fetching, caching, typed getters, and automatic fallbacks.
/// Designed with safe reflection to run cleanly both with and without the Firebase.RemoteConfig DLL.
/// </summary>
public class FirebaseRemoteConfigHelper : MonoBehaviour
{
    public static FirebaseRemoteConfigHelper Instance { get; private set; }

    public bool IsFetched { get; private set; }
    public event Action OnConfigFetched;

    // ──────────────────── Keys from remote_config_game1liuqi_10.json ────────────────────
    public const string KEY_DEBUG_MODE = "_turn_on_debug_mode_";
    public const string KEY_DEBUG_MODE_ALT = "_turn_on_debug_mode";
    public const string KEY_ADS_ENABLED = "_ads_enabled";
    public const string KEY_ADS_BANNER_ENABLED = "_ads_banner_enabled";
    public const string KEY_ADS_INTERSTITIAL_ENABLED = "_ads_interstitial_enabled";
    public const string KEY_ADS_INTERSTITIAL_INTERVAL = "_ads_interstitial_interval";
    public const string KEY_ADS_REWARDED_INTERSTITIAL_ENABLED = "_ads_rewarded_interstitial_enabled";
    public const string KEY_ADS_APP_OPEN_ENABLED = "_ads_app_open_enabled";
    public const string KEY_ADS_APP_OPEN_DELAY = "_ads_app_open_delay";
    public const string KEY_DIFFICULTY_HP_MULTIPLIER = "_difficulty_hp_multiplier";
    public const string KEY_DIFFICULTY_DAMAGE_MULTIPLIER = "_difficulty_damage_multiplier";
    public const string KEY_DIFFICULTY_SPEED_MULTIPLIER = "_difficulty_speed_multiplier";
    public const string KEY_AB_TEST_GROUP = "_ab_test_group";
    public const string KEY_GOLD_KILL_RATE = "_gold_kill_rate";
    public const string KEY_GOLD_TIME_RATE = "_gold_time_rate";
    public const string KEY_DAILY_REWARD_MULTIPLIER = "_daily_reward_multiplier";
    public const string KEY_HERO_AD_UNLOCK_ENABLED = "_hero_ad_unlock_enabled";
    public const string KEY_HERO_AD_WATCH_COUNT = "_hero_ad_watch_count";
    public const string KEY_SKIN_AD_UNLOCK_ENABLED = "_skin_ad_unlock_enabled";
    public const string KEY_SKIN_AD_WATCH_COUNT = "_skin_ad_watch_count";
    public const string KEY_MAINTENANCE_MODE = "_maintenance_mode";
    public const string KEY_MAINTENANCE_MESSAGE = "_maintenance_message";
    public const string KEY_FORCE_UPDATE_VERSION = "_force_update_version";

    // ──────────────────── Typed Properties ────────────────────
    public bool IsDebugMode => GetBool(KEY_DEBUG_MODE, GetBool(KEY_DEBUG_MODE_ALT, false));
    public bool IsAdsEnabled => GetBool(KEY_ADS_ENABLED, true);
    public bool IsBannerEnabled => IsAdsEnabled && GetBool(KEY_ADS_BANNER_ENABLED, true);
    public bool IsInterstitialEnabled => IsAdsEnabled && GetBool(KEY_ADS_INTERSTITIAL_ENABLED, true);
    public int InterstitialInterval => GetInt(KEY_ADS_INTERSTITIAL_INTERVAL, 30);
    public bool IsRewardedInterstitialEnabled => IsAdsEnabled && GetBool(KEY_ADS_REWARDED_INTERSTITIAL_ENABLED, false);
    public bool IsAppOpenEnabled => IsAdsEnabled && GetBool(KEY_ADS_APP_OPEN_ENABLED, true);
    public int AppOpenDelay => GetInt(KEY_ADS_APP_OPEN_DELAY, 5);

    public float DifficultyHpMultiplier => GetFloat(KEY_DIFFICULTY_HP_MULTIPLIER, 1.0f);
    public float DifficultyDamageMultiplier => GetFloat(KEY_DIFFICULTY_DAMAGE_MULTIPLIER, 1.0f);
    public float DifficultySpeedMultiplier => GetFloat(KEY_DIFFICULTY_SPEED_MULTIPLIER, 1.0f);

    public string ABTestGroup => GetString(KEY_AB_TEST_GROUP, "control");

    public float GoldKillRate => GetFloat(KEY_GOLD_KILL_RATE, 1.0f);
    public float GoldTimeRate => GetFloat(KEY_GOLD_TIME_RATE, 1.0f);
    public float DailyRewardMultiplier => GetFloat(KEY_DAILY_REWARD_MULTIPLIER, 1.0f);

    public bool IsHeroAdUnlockEnabled => IsAdsEnabled && GetBool(KEY_HERO_AD_UNLOCK_ENABLED, true);
    public int HeroAdWatchCount => GetInt(KEY_HERO_AD_WATCH_COUNT, 3);
    public bool IsSkinAdUnlockEnabled => IsAdsEnabled && GetBool(KEY_SKIN_AD_UNLOCK_ENABLED, true);
    public int SkinAdWatchCount => GetInt(KEY_SKIN_AD_WATCH_COUNT, 2);

    public bool IsMaintenanceMode => GetBool(KEY_MAINTENANCE_MODE, false);
    public string MaintenanceMessage => GetString(KEY_MAINTENANCE_MESSAGE, "The game is currently under maintenance. Please check back later.");
    public string ForceUpdateVersion => GetString(KEY_FORCE_UPDATE_VERSION, "");

    // ──────────────────── Storage & Caching ────────────────────
    private readonly Dictionary<string, object> defaultValues = new Dictionary<string, object>()
    {
        { KEY_DEBUG_MODE, false },
        { KEY_DEBUG_MODE_ALT, false },
        { KEY_ADS_ENABLED, true },
        { KEY_ADS_BANNER_ENABLED, true },
        { KEY_ADS_INTERSTITIAL_ENABLED, true },
        { KEY_ADS_INTERSTITIAL_INTERVAL, 30 },
        { KEY_ADS_REWARDED_INTERSTITIAL_ENABLED, false },
        { KEY_ADS_APP_OPEN_ENABLED, true },
        { KEY_ADS_APP_OPEN_DELAY, 5 },
        { KEY_DIFFICULTY_HP_MULTIPLIER, 1.0f },
        { KEY_DIFFICULTY_DAMAGE_MULTIPLIER, 1.0f },
        { KEY_DIFFICULTY_SPEED_MULTIPLIER, 1.0f },
        { KEY_AB_TEST_GROUP, "control" },
        { KEY_GOLD_KILL_RATE, 1.0f },
        { KEY_GOLD_TIME_RATE, 1.0f },
        { KEY_DAILY_REWARD_MULTIPLIER, 1.0f },
        { KEY_HERO_AD_UNLOCK_ENABLED, true },
        { KEY_HERO_AD_WATCH_COUNT, 3 },
        { KEY_SKIN_AD_UNLOCK_ENABLED, true },
        { KEY_SKIN_AD_WATCH_COUNT, 2 },
        { KEY_MAINTENANCE_MODE, false },
        { KEY_MAINTENANCE_MESSAGE, "The game is currently under maintenance. Please check back later." },
        { KEY_FORCE_UPDATE_VERSION, "" }
    };

    private readonly Dictionary<string, string> cachedValues = new Dictionary<string, string>();
    private const string PREF_CACHE_PREFIX = "FirebaseRC_";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCachedFromPrefs();
    }

    private void LoadCachedFromPrefs()
    {
        foreach (var pair in defaultValues)
        {
            string prefKey = PREF_CACHE_PREFIX + pair.Key;
            if (PlayerPrefs.HasKey(prefKey))
            {
                cachedValues[pair.Key] = PlayerPrefs.GetString(prefKey);
            }
            else
            {
                cachedValues[pair.Key] = pair.Value?.ToString() ?? "";
            }
        }
    }

    /// <summary>
    /// Initialize and fetch Firebase Remote Config. Called from FirebaseInit when FirebaseApp is ready.
    /// </summary>
    public void Initialize()
    {
        StartCoroutine(InitAndFetchRoutine());
    }

    private IEnumerator InitAndFetchRoutine()
    {
        Type remoteConfigType = Type.GetType("Firebase.RemoteConfig.FirebaseRemoteConfig, Firebase.RemoteConfig");
        if (remoteConfigType == null)
        {
            // Scan loaded assemblies in case assembly name differs
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("Firebase.RemoteConfig.FirebaseRemoteConfig");
                if (t != null)
                {
                    remoteConfigType = t;
                    break;
                }
            }
        }

        if (remoteConfigType == null)
        {
            Debug.LogError("[FirebaseRemoteConfig] LỖI: Chưa có thư viện Firebase.RemoteConfig.dll trong dự án! Game đang chạy giá trị mặc định nội bộ và không thể kết nối Firebase.");
            IsFetched = true;
            OnConfigFetched?.Invoke();
            yield break;
        }

        object defaultInstance = null;
        try
        {
            PropertyInfo defaultProp = remoteConfigType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static);
            if (defaultProp != null)
                defaultInstance = defaultProp.GetValue(null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FirebaseRemoteConfig] Failed to get DefaultInstance: " + ex.Message);
        }

        if (defaultInstance == null)
        {
            Debug.LogWarning("[FirebaseRemoteConfig] FirebaseRemoteConfig.DefaultInstance is null.");
            IsFetched = true;
            OnConfigFetched?.Invoke();
            yield break;
        }

        // Set defaults
        object setDefaultsTask = null;
        try
        {
            MethodInfo setDefaultsMethod = remoteConfigType.GetMethod("SetDefaultsAsync", new Type[] { typeof(Dictionary<string, object>) });
            if (setDefaultsMethod != null)
            {
                setDefaultsTask = setDefaultsMethod.Invoke(defaultInstance, new object[] { defaultValues });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FirebaseRemoteConfig] SetDefaultsAsync error: " + ex.Message);
        }

        if (setDefaultsTask != null)
        {
            yield return WaitForTask(setDefaultsTask);
        }

        // Fetch and activate (TimeSpan.Zero ensures cache expiration = 0 so server is always queried)
        bool fetchCompleted = false;
        object fetchAsyncTask = null;
        object fetchAndActivateTask = null;
        MethodInfo activateMethod = null;

        try
        {
            MethodInfo fetchAsyncMethod = remoteConfigType.GetMethod("FetchAsync", new Type[] { typeof(TimeSpan) });
            if (fetchAsyncMethod != null)
            {
                fetchAsyncTask = fetchAsyncMethod.Invoke(defaultInstance, new object[] { TimeSpan.Zero });
                activateMethod = remoteConfigType.GetMethod("ActivateAsync", Type.EmptyTypes);
            }
            else
            {
                MethodInfo fetchMethod = remoteConfigType.GetMethod("FetchAndActivateAsync", Type.EmptyTypes);
                if (fetchMethod != null)
                {
                    fetchAndActivateTask = fetchMethod.Invoke(defaultInstance, null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FirebaseRemoteConfig] Fetch/Activate error: " + ex.Message);
        }

        if (fetchAsyncTask != null)
        {
            yield return WaitForTask(fetchAsyncTask);

            object activateTask = null;
            try
            {
                if (activateMethod != null)
                {
                    activateTask = activateMethod.Invoke(defaultInstance, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseRemoteConfig] ActivateAsync error: " + ex.Message);
            }

            if (activateTask != null)
            {
                yield return WaitForTask(activateTask);
            }

            fetchCompleted = true;
        }
        else if (fetchAndActivateTask != null)
        {
            yield return WaitForTask(fetchAndActivateTask);
            fetchCompleted = true;
        }

        if (fetchCompleted)
        {
            // Pull values into cached dictionary
            try
            {
                MethodInfo getValueMethod = remoteConfigType.GetMethod("GetValue", new Type[] { typeof(string) });
                if (getValueMethod != null)
                {
                    foreach (var key in defaultValues.Keys)
                    {
                        object configValObj = getValueMethod.Invoke(defaultInstance, new object[] { key });
                        if (configValObj != null)
                        {
                            PropertyInfo strProp = configValObj.GetType().GetProperty("StringValue");
                            if (strProp != null)
                            {
                                string strVal = strProp.GetValue(configValObj)?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(strVal))
                                {
                                    cachedValues[key] = strVal;
                                    PlayerPrefs.SetString(PREF_CACHE_PREFIX + key, strVal);
                                }
                            }
                        }
                    }
                    PlayerPrefs.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseRemoteConfig] Failed to cache values: " + ex.Message);
            }

            Debug.Log("[FirebaseRemoteConfig] Values fetched and activated successfully!");
        }

        IsFetched = true;
        OnConfigFetched?.Invoke();
    }

    private IEnumerator WaitForTask(object taskObj)
    {
        if (taskObj == null) yield break;

        PropertyInfo isCompletedProp = taskObj.GetType().GetProperty("IsCompleted");
        if (isCompletedProp == null) yield break;

        while (!(bool)isCompletedProp.GetValue(taskObj))
        {
            yield return null;
        }

        PropertyInfo isFaultedProp = taskObj.GetType().GetProperty("IsFaulted");
        if (isFaultedProp != null && (bool)isFaultedProp.GetValue(taskObj))
        {
            PropertyInfo exProp = taskObj.GetType().GetProperty("Exception");
            object ex = exProp?.GetValue(taskObj);
            Debug.LogError("[FirebaseRemoteConfig] Task thất bại: " + ex);
        }
    }

    // ──────────────────── Getters with Fallbacks ────────────────────

    public bool GetBool(string key, bool fallback = false)
    {
        if (cachedValues.TryGetValue(key, out string strVal))
        {
            if (bool.TryParse(strVal, out bool res))
                return res;
            if (strVal == "1") return true;
            if (strVal == "0") return false;
        }

        if (defaultValues.TryGetValue(key, out object def) && def is bool defBool)
            return defBool;

        return fallback;
    }

    public int GetInt(string key, int fallback = 0)
    {
        if (cachedValues.TryGetValue(key, out string strVal))
        {
            if (int.TryParse(strVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int res))
                return res;
        }

        if (defaultValues.TryGetValue(key, out object def) && def is int defInt)
            return defInt;

        return fallback;
    }

    public float GetFloat(string key, float fallback = 0f)
    {
        if (cachedValues.TryGetValue(key, out string strVal))
        {
            if (float.TryParse(strVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float res))
                return res;
        }

        if (defaultValues.TryGetValue(key, out object def))
        {
            if (def is float defFloat) return defFloat;
            if (def is double defDouble) return (float)defDouble;
            if (def is int defInt) return (float)defInt;
        }

        return fallback;
    }

    public string GetString(string key, string fallback = "")
    {
        if (cachedValues.TryGetValue(key, out string strVal))
            return strVal;

        if (defaultValues.TryGetValue(key, out object def) && def != null)
            return def.ToString();

        return fallback;
    }
}
