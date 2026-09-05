using DG.Tweening;
using System;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    public static PlayerXP Instance { get; private set; }

    public float CurrentXP { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public float XPToNextLevel { get; private set; }

    public event Action<int> OnLevelUp;

    public float AmmoRecoverChance { get; private set; }
    public float FireRateBuffOnXPChance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DOTween.Init(false, false, LogBehaviour.Default);

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
        XPToNextLevel = GetRequiredXP(CurrentLevel);
    }

    private WeaponController[] Weapons => playerStats?.Weapons;

    public void AddExperience(float amount)
    {
        if (amount <= 0f) return;

        CurrentXP += amount;

        // Chance to recover ammo on XP gain
        if (AmmoRecoverChance > 0f && Weapons != null)
        {
            if (UnityEngine.Random.value < AmmoRecoverChance)
            {
                foreach (var w in Weapons)
                    if (w != null) w.AddAmmo(1);
            }
        }

        // Chance for fire rate buff on XP gain
        if (FireRateBuffOnXPChance > 0f && Weapons != null)
        {
            if (UnityEngine.Random.value < FireRateBuffOnXPChance)
            {
                foreach (var w in Weapons)
                    if (w != null) w.AddFireRatePercent(0.25f); // +25% fire rate
                StartCoroutine(RemoveFireRateBuff());
            }
        }

        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            XPToNextLevel = GetRequiredXP(CurrentLevel);
            OnLevelUp?.Invoke(CurrentLevel);
            Debug.Log($"Level up! Now level {CurrentLevel}");
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.LevelUp);
        }
    }

    public void AddAmmoRecoverChance(float amount) => AmmoRecoverChance = Mathf.Clamp01(AmmoRecoverChance + amount);

    public void AddFireRateBuffOnXPChance(float amount) => FireRateBuffOnXPChance = Mathf.Clamp01(FireRateBuffOnXPChance + amount);

    private System.Collections.IEnumerator RemoveFireRateBuff()
    {
        yield return new WaitForSeconds(1f);
        var weapons = Weapons;
        if (weapons != null)
            foreach (var w in weapons)
                if (w != null) w.AddFireRatePercent(-0.25f);
    }

    public float PickupValue(float baseAmount)
    {
        return baseAmount * (1f + playerStats.GrowthRate);
    }

    private float GetRequiredXP(int level)
    {
        if (level <= 3)
        {
            return level*2 + 3;
        }
        else if (level <= 20)
        {
            return 10 * level - 5; //level+2;
        }
        else if (level <= 40)
        {
            return 10 * level + 8;
        }
        else
        {
            return 10 * level + 11;
        }
    }
}
