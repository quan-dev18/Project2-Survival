using System;
using System.Collections.Generic;
using UnityEngine;

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
        MigrateFromPlayerPrefs();
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

    public void InitHeroDefaults(List<HeroSelectSO> heroes)
    {
        if (data.unlockedHeroes != null && data.unlockedHeroes.Length > 0) return;
        data.unlockedHeroes = new bool[heroes.Count];
        for (int i = 0; i < heroes.Count; i++)
        {
            data.unlockedHeroes[i] = heroes[i] != null && heroes[i].isUnlocked;
        }
        Save();
    }

    public void InitWeaponDefaults(List<WeaponSO> weapons)
    {
        if (data.unlockedWeapons != null && data.unlockedWeapons.Length > 0) return;
        data.unlockedWeapons = new bool[weapons.Count];
        for (int i = 0; i < weapons.Count; i++)
        {
            data.unlockedWeapons[i] = weapons[i] != null && weapons[i].IsUnlocked;
        }
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
        if (data.unlockedHeroes == null || index < 0 || index >= data.unlockedHeroes.Length)
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
        if (data.unlockedWeapons == null || index < 0 || index >= data.unlockedWeapons.Length)
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
}
