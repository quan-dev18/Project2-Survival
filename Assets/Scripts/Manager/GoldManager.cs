using System;
using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [SerializeField]
    private string GoldKey = "PlayerGold";

    public int Gold { get; private set; }
    public int SessionGold { get; private set; }

    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Gold = PlayerPrefs.GetInt(GoldKey, 0);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        SessionGold += amount;
        PlayerPrefs.SetInt(GoldKey, Gold);
        PlayerPrefs.Save();
        OnGoldChanged?.Invoke(Gold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount) return false;
        Gold -= amount;
        PlayerPrefs.SetInt(GoldKey, Gold);
        PlayerPrefs.Save();
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public bool HasEnoughGold(int amount) => Gold >= amount;

    public void ResetSessionGold() => SessionGold = 0;
}
