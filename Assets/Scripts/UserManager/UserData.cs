using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class UserData : MonoBehaviour
{
    public static UserData Instance { get; private set; }

    private GameData data;

    public int Gold => data.playerGold;
    public int SessionGold { get; private set; }

    public int SelectedHeroIndex
    {
        get => data.selectedHeroIndex;
        set
        {
            data.selectedHeroIndex = value;
            Save();
        }
    }

    public int SelectedWeaponIndex
    {
        get => data.selectedWeaponIndex;
        set
        {
            data.selectedWeaponIndex = value;
            Save();
        }
    }

    public event Action<int> OnGoldChanged;
    public event Action<int> OnSessionGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        data = SaveSystem.Load();
        if (data == null) data = new GameData();
        data.unlockedHeroes ??= new bool[0];
        data.unlockedWeapons ??= new bool[0];
        MigrateFromPlayerPrefs();
        MigratePerkLevelsFromPlayerPrefs();
    }

    private void MigrateFromPlayerPrefs()
    {
        bool migrated = false;

        if (PlayerPrefs.HasKey("PlayerGold"))
        {
            int oldGold = PlayerPrefs.GetInt("PlayerGold", 0);
            if (oldGold > 0 && data.playerGold == 0)
            {
                data.playerGold = oldGold;
                migrated = true;
            }
            PlayerPrefs.DeleteKey("PlayerGold");
        }

        if (PlayerPrefs.HasKey("SelectedHeroIndex"))
        {
            data.selectedHeroIndex = PlayerPrefs.GetInt("SelectedHeroIndex", 0);
            PlayerPrefs.DeleteKey("SelectedHeroIndex");
            migrated = true;
        }

        if (PlayerPrefs.HasKey("SelectedWeaponIndex"))
        {
            data.selectedWeaponIndex = PlayerPrefs.GetInt("SelectedWeaponIndex", 0);
            PlayerPrefs.DeleteKey("SelectedWeaponIndex");
            migrated = true;
        }

        string savedWeaponName = PlayerPrefs.GetString("SelectedWeaponName", "");
        PlayerPrefs.DeleteKey("SelectedWeaponName");

        for (int i = 0; i < 10; i++)
        {
            if (PlayerPrefs.GetInt($"UnlockedHero_{i}", -1) != -1)
            {
                bool unlocked = PlayerPrefs.GetInt($"UnlockedHero_{i}", 0) == 1;
                if (data.unlockedHeroes == null || i >= data.unlockedHeroes.Length)
                {
                    bool[] newArr = new bool[i + 1];
                    if (data.unlockedHeroes != null)
                        data.unlockedHeroes.CopyTo(newArr, 0);
                    data.unlockedHeroes = newArr;
                }
                data.unlockedHeroes[i] = unlocked;
                PlayerPrefs.DeleteKey($"UnlockedHero_{i}");
                migrated = true;
            }

            if (PlayerPrefs.GetInt($"UnlockedWeapon_{i}", -1) != -1)
            {
                bool unlocked = PlayerPrefs.GetInt($"UnlockedWeapon_{i}", 0) == 1;
                if (data.unlockedWeapons == null || i >= data.unlockedWeapons.Length)
                {
                    bool[] newArr = new bool[i + 1];
                    if (data.unlockedWeapons != null)
                        data.unlockedWeapons.CopyTo(newArr, 0);
                    data.unlockedWeapons = newArr;
                }
                data.unlockedWeapons[i] = unlocked;
                PlayerPrefs.DeleteKey($"UnlockedWeapon_{i}");
                migrated = true;
            }
        }

        if (migrated)
        {
            Save();
        }

        OnGoldChanged?.Invoke(data.playerGold);
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        SaveSystem.Save(data);
    }

    private void SyncArray(ref bool[] arr, int targetSize, System.Func<int,bool> defaultFactory)
    {
        if (arr == null) arr = new bool[0];
        if (arr.Length == targetSize) return;
        bool[] next = new bool[targetSize];
        for (int i = 0; i < targetSize; i++)
        {
            if (i < arr.Length) next[i] = arr[i];
            else next[i] = defaultFactory(i);
        }
        // also promote any default-unlocked that should be true even if previously false
        for (int i = 0; i < System.Math.Min(arr.Length, targetSize); i++)
            if (defaultFactory(i)) next[i] = true;
        arr = next;
    }

    public void InitHeroDefaults(List<HeroSelectSO> heroes)
    {
        if (heroes == null) return;
        int count = heroes.Count;
        SyncArray(ref data.unlockedHeroes, count, i => heroes[i] != null && heroes[i].isUnlocked);
        // clamp selected index
        if (data.selectedHeroIndex < 0 || data.selectedHeroIndex >= count)
            data.selectedHeroIndex = 0;
        Save();
    }

    public void InitWeaponDefaults(List<WeaponSO> weapons)
    {
        if (weapons == null) return;
        int count = weapons.Count;
        SyncArray(ref data.unlockedWeapons, count, i => weapons[i] != null && weapons[i].IsUnlocked);
        if (data.selectedWeaponIndex < 0 || data.selectedWeaponIndex >= count)
            data.selectedWeaponIndex = 0;
        Save();
    }

    #region Gold

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        data.playerGold += amount;
        OnGoldChanged?.Invoke(data.playerGold);
        Save();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || data.playerGold < amount) return false;
        data.playerGold -= amount;
        OnGoldChanged?.Invoke(data.playerGold);
        Save();
        return true;
    }

    public bool HasEnoughGold(int amount) => data.playerGold >= amount;

    public void AddSessionGold(int amount)
    {
        SessionGold += amount;
        OnSessionGoldChanged?.Invoke(SessionGold);
    }

    public void ClaimSessionGold()
    {
        if (SessionGold <= 0) return;
        data.playerGold += SessionGold;
        SessionGold = 0;
        OnGoldChanged?.Invoke(data.playerGold);
        OnSessionGoldChanged?.Invoke(0);
        Save();
    }

    public void ResetSessionGold()
    {
        SessionGold = 0;
        OnSessionGoldChanged?.Invoke(0);
    }

    #endregion

    #region Unlock Hero

    public bool IsHeroUnlocked(int index)
    {
        if (data == null || data.unlockedHeroes == null || index < 0 || index >= data.unlockedHeroes.Length)
            return false;
        return data.unlockedHeroes[index];
    }

    public bool UnlockHero(int index, int cost)
    {
        if (!HasEnoughGold(cost)) return false;
        if (IsHeroUnlocked(index)) return false;

        data.playerGold -= cost;
        OnGoldChanged?.Invoke(data.playerGold);

        if (data.unlockedHeroes == null || index >= data.unlockedHeroes.Length)
        {
            bool[] newArr = new bool[index + 1];
            if (data.unlockedHeroes != null)
                data.unlockedHeroes.CopyTo(newArr, 0);
            data.unlockedHeroes = newArr;
        }

        data.unlockedHeroes[index] = true;
        Save();
        return true;
    }

    public void SetHeroUnlocked(int index, bool unlocked)
    {
        if (data.unlockedHeroes == null || index >= data.unlockedHeroes.Length)
        {
            bool[] newArr = new bool[index + 1];
            if (data.unlockedHeroes != null)
                data.unlockedHeroes.CopyTo(newArr, 0);
            data.unlockedHeroes = newArr;
        }

        data.unlockedHeroes[index] = unlocked;
        Save();
    }

    #endregion

    #region Unlock Weapon

    public bool IsWeaponUnlocked(int index)
    {
        if (data == null || data.unlockedWeapons == null || index < 0 || index >= data.unlockedWeapons.Length)
            return false;
        return data.unlockedWeapons[index];
    }

    public bool UnlockWeapon(int index, int cost)
    {
        if (!HasEnoughGold(cost)) return false;
        if (IsWeaponUnlocked(index)) return false;

        data.playerGold -= cost;
        OnGoldChanged?.Invoke(data.playerGold);

        if (data.unlockedWeapons == null || index >= data.unlockedWeapons.Length)
        {
            bool[] newArr = new bool[index + 1];
            if (data.unlockedWeapons != null)
                data.unlockedWeapons.CopyTo(newArr, 0);
            data.unlockedWeapons = newArr;
        }

        data.unlockedWeapons[index] = true;
        Save();
        return true;
    }

    public void SetWeaponUnlocked(int index, bool unlocked)
    {
        if (data.unlockedWeapons == null || index >= data.unlockedWeapons.Length)
        {
            bool[] newArr = new bool[index + 1];
            if (data.unlockedWeapons != null)
                data.unlockedWeapons.CopyTo(newArr, 0);
            data.unlockedWeapons = newArr;
        }

        data.unlockedWeapons[index] = unlocked;
        Save();
    }

    #endregion

    #region Perk Levels

    /// <summary>Lấy level của 1 Perk theo PerkID (mặc định 0 nếu chưa nâng).</summary>
    public int GetPerkLevel(string perkId)
    {
        if (string.IsNullOrEmpty(perkId) || data.perkIds == null) return 0;
        int index = data.perkIds.IndexOf(perkId);
        return index >= 0 && index < data.perkLevels.Count ? data.perkLevels[index] : 0;
    }

    /// <summary>Ghi level 1 Perk và lưu ngay vào gamedata.json. Level ≤ 0 sẽ bị xóa.</summary>
    public void SetPerkLevel(string perkId, int level)
    {
        if (string.IsNullOrEmpty(perkId)) return;
        data.perkIds ??= new List<string>();
        data.perkLevels ??= new List<int>();

        int index = data.perkIds.IndexOf(perkId);
        if (level <= 0)
        {
            if (index >= 0)
            {
                data.perkIds.RemoveAt(index);
                data.perkLevels.RemoveAt(index);
            }
            Save();
            return;
        }

        if (index >= 0) data.perkLevels[index] = level;
        else
        {
            data.perkIds.Add(perkId);
            data.perkLevels.Add(level);
        }
        Save();
    }

    /// <summary>
    /// Nâng cấp 1 lần (thêm 1 cấp) và lưu. Dùng cho luồng mua Perk.
    /// </summary>
    public void AddPerkLevel(string perkId, int maxLevel)
    {
        int next = GetPerkLevel(perkId) + 1;
        SetPerkLevel(perkId, Mathf.Min(next, maxLevel));
    }

    [Serializable]
    private class PerkLevelSave
    {
        public List<string> ids = new List<string>();
        public List<int> levels = new List<int>();
    }

    /// <summary>Chuyển dữ liệu perk lưu cũ (PlayerPrefs) sang gamedata.json một lần, rồi xóa key cũ.</summary>
    private void MigratePerkLevelsFromPlayerPrefs()
    {
        const string oldKey = "PerkLevels";
        if (!PlayerPrefs.HasKey(oldKey)) return;

        try
        {
            PerkLevelSave old = JsonUtility.FromJson<PerkLevelSave>(PlayerPrefs.GetString(oldKey));
            PlayerPrefs.DeleteKey(oldKey);
            if (old == null || old.ids == null || old.levels == null) return;

            bool changed = false;
            for (int i = 0; i < old.ids.Count && i < old.levels.Count; i++)
            {
                string id = old.ids[i];
                int level = old.levels[i];
                if (string.IsNullOrEmpty(id) || level <= 0) continue;
                int existing = GetPerkLevel(id);
                if (level > existing)
                {
                    SetPerkLevel(id, level);
                    changed = true;
                }
            }
            if (changed) Debug.Log("[UserData] Đã nhập perk level từ dữ liệu cũ.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UserData] Migrate perk levels failed: {e.Message}");
        }
    }

    #endregion
}
