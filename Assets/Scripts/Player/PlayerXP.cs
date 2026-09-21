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
    public event Action OnXPChanged;

    public float AmmoRecoverChance { get; private set; }
    public float FireRateBuffOnXPChance { get; private set; }

    private static readonly WaitForSeconds s_WaitOneSecond = new WaitForSeconds(1f);

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
        OnXPChanged?.Invoke();

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

        ProcessPendingLevelUps();
    }

    public void ProcessPendingLevelUps()
    {
        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            XPToNextLevel = GetRequiredXP(CurrentLevel);
            OnLevelUp?.Invoke(CurrentLevel);

            AudioManager.Instance?.PlayLevelUp();

            // Log level up event
            int totalUpgrades = LevelUpPanel.Instance != null ? LevelUpPanel.Instance.OwnedUpgrades.Count : 0;
            float timeAlive = GameManager.Instance != null ? GameManager.Instance.TotalElapsedTime : 0f;
            FirebaseAnalyticsHelper.LogLevelUp(CurrentLevel, timeAlive, totalUpgrades);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Level up! Now level {CurrentLevel}");
#endif
            // Chỉ show LevelUpPanel khi game đang Playing (không phải Tutorial)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                GameManager.Instance.SetState(GameState.LevelUp);
        }
    }

    public void AddAmmoRecoverChance(float amount) => AmmoRecoverChance = Mathf.Clamp01(AmmoRecoverChance + amount);

    public void AddFireRateBuffOnXPChance(float amount) => FireRateBuffOnXPChance = Mathf.Clamp01(FireRateBuffOnXPChance + amount);

    private System.Collections.IEnumerator RemoveFireRateBuff()
    {
        yield return s_WaitOneSecond;
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
