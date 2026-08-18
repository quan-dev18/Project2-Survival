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

    public void AddExperience(float amount)
    {
        if (amount <= 0f) return;

        CurrentXP += amount;
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

    public float PickupValue(float baseAmount)
    {
        return baseAmount * (1f + playerStats.GrowthRate);
    }

    private float GetRequiredXP(int level)
    {
        return 10 * level - 5;
    }
}
