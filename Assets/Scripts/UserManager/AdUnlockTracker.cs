using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks ad watch counts for unlocking heroes and skins.
/// Persisted in GameData via UserData.
/// </summary>
public class AdUnlockTracker : MonoBehaviour
{
    public static AdUnlockTracker Instance { get; private set; }

    private readonly Dictionary<string, int> watchCounts = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromUserData();
    }

    public void LoadFromUserData()
    {
        watchCounts.Clear();
        if (UserData.Instance != null)
        {
            var data = UserData.Instance.GetData();
            if (data != null && data.adWatchKeys != null && data.adWatchCounts != null)
            {
                int count = Mathf.Min(data.adWatchKeys.Count, data.adWatchCounts.Count);
                for (int i = 0; i < count; i++)
                {
                    watchCounts[data.adWatchKeys[i]] = data.adWatchCounts[i];
                }
            }
        }
    }

    public void SaveToUserData()
    {
        if (UserData.Instance != null)
        {
            var data = UserData.Instance.GetData();
            if (data != null)
            {
                data.adWatchKeys = new List<string>(watchCounts.Keys);
                data.adWatchCounts = new List<int>(watchCounts.Values);
                UserData.Instance.Save();
            }
        }
    }

    public int GetAdWatchCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        if (watchCounts.TryGetValue(itemId, out int count))
            return count;
        return 0;
    }

    public void RecordAdWatch(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        int count = GetAdWatchCount(itemId) + 1;
        watchCounts[itemId] = count;
        SaveToUserData();
    }

    public bool CanUnlockByAd(string itemId, int requiredCount)
    {
        return GetAdWatchCount(itemId) >= requiredCount;
    }

    public void ResetAdWatch(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        watchCounts.Remove(itemId);
        SaveToUserData();
    }
}

