using UnityEngine;

/// <summary>
/// Manages A/B Testing group assignment driven by Firebase Remote Config key: _ab_test_group.
/// </summary>
public class ABTestManager : MonoBehaviour
{
    public static ABTestManager Instance { get; private set; }

    public string CurrentGroup { get; private set; } = "control";

    private const string PREF_LAST_LOGGED_GROUP = "ABTest_LastLoggedGroup";

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
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched += UpdateGroup;
            UpdateGroup();
        }
    }

    private void OnDestroy()
    {
        if (FirebaseRemoteConfigHelper.Instance != null)
            FirebaseRemoteConfigHelper.Instance.OnConfigFetched -= UpdateGroup;
    }

    public void UpdateGroup()
    {
        if (FirebaseRemoteConfigHelper.Instance == null) return;

        string newGroup = FirebaseRemoteConfigHelper.Instance.ABTestGroup;
        if (string.IsNullOrEmpty(newGroup)) newGroup = "control";

        CurrentGroup = newGroup;

        // Log if group changed or first time
        string lastLogged = PlayerPrefs.GetString(PREF_LAST_LOGGED_GROUP, "");
        if (lastLogged != CurrentGroup)
        {
            PlayerPrefs.SetString(PREF_LAST_LOGGED_GROUP, CurrentGroup);
            PlayerPrefs.Save();
            FirebaseAnalyticsHelper.LogABTestGroupAssigned(CurrentGroup);
        }
    }

    /// <summary>
    /// Checks whether the user is in the specified test group (case-insensitive).
    /// </summary>
    public bool IsInGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName)) return false;
        return string.Equals(CurrentGroup, groupName, System.StringComparison.OrdinalIgnoreCase);
    }
}

