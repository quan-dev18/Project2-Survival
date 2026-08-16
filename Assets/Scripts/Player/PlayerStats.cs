using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private CharacterSO characterStats;

    #region Base Stats 
    private float baseMaxHealth;
    private float baseMaxArmor;
    private float baseRecoveryRate;
    private float baseMoveSpeed;
    private float baseCollectRange;
    private float baseGrowthRate;
    #endregion

    #region Bonus Flat
    public float bonusMaxHealthFlat { get; private set; }
    public float bonusMaxArmorFlat { get; private set; }
    public float bonusRecoveryRateFlat { get; private set; }
    public float bonusCollectRangeFlat { get; private set; }
    #endregion

    #region Bonus % 
    public float bonusMaxHealthPercent { get; private set; }
    public float bonusMaxArmorPercent { get; private set; }
    public float bonusRecoveryRatePercent { get; private set; }
    public float bonusMoveSpeedPercent { get; private set; }
    public float bonusCollectRangePercent { get; private set; }
    public float bonusGrowthRatePercent { get; private set; }
    #endregion

    #region Stat Caps
    [Header("Stat Caps")]
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float maxMoveSpeed = 15f;
    [SerializeField] private float maxRecoveryRate = 50f;
    [SerializeField] private float maxCollectRange = 20f;
    [SerializeField] private float maxGrowthRate = 5f; // ví dụ: growth rate không quá +400% (x5)
    #endregion

    #region Final Stats 
    public float MaxHealth => Mathf.Max(1f,
        (baseMaxHealth + bonusMaxHealthFlat) * (1f + bonusMaxHealthPercent));

    public float MaxArmor => Mathf.Max(0f,
        (baseMaxArmor + bonusMaxArmorFlat) * (1f + bonusMaxArmorPercent));

    public float RecoveryRate => Mathf.Clamp(
        (baseRecoveryRate + bonusRecoveryRateFlat) * (1f + bonusRecoveryRatePercent),
        0f, maxRecoveryRate);

    public float MoveSpeed => Mathf.Clamp(
        baseMoveSpeed * (1f + bonusMoveSpeedPercent),
        minMoveSpeed, maxMoveSpeed);

    public float CollectRange => Mathf.Clamp(
        (baseCollectRange + bonusCollectRangeFlat) * (1f + bonusCollectRangePercent),
        0f, maxCollectRange);

    public float GrowthRate => Mathf.Clamp(
        baseGrowthRate * (1f + bonusGrowthRatePercent),
        0f, maxGrowthRate);
    #endregion

    #region Runtime State
    public float CurrentHealth { get; private set; }
    public float CurrentArmor { get; private set; }
    #endregion

    private void Awake()
    {
        LoadFromSO();
    }

    private void Update()
    {
        //regenerate health over time
        RegenOverTime();
    }

    public void LoadFromSO()
    {
        if (characterStats == null) return;

        baseMaxHealth = characterStats.MaxHealth;
        baseMaxArmor = characterStats.MaxArmor;
        baseRecoveryRate = characterStats.RecoveryRate;
        baseMoveSpeed = characterStats.MovementSpeed;
        baseCollectRange = characterStats.CollectRange;
        baseGrowthRate = characterStats.GrowthRate;

        CurrentHealth = MaxHealth;
        CurrentArmor = MaxArmor;
    }

    //bonus percent
    public void AddMaxHealthPercent(float amount) => bonusMaxHealthPercent += amount;
    public void AddMaxArmorPercent(float amount) => bonusMaxArmorPercent += amount;
    public void AddRecoveryRatePercent(float amount) => bonusRecoveryRatePercent += amount;
    public void AddMoveSpeedPercent(float amount) => bonusMoveSpeedPercent += amount;
    public void AddCollectRangePercent(float amount) => bonusCollectRangePercent += amount;
    public void AddGrowthRatePercent(float amount) => bonusGrowthRatePercent += amount;

    //bonus flat
    public void AddMaxHealthFlat(float amount)
    {
        bonusMaxHealthFlat += amount;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }

    public void AddMaxArmorFlat(float amount)
    {
        bonusMaxArmorFlat += amount;
        CurrentArmor = Mathf.Min(CurrentArmor + amount, MaxArmor);
    }

    public void AddRecoveryRateFlat(float amount) => bonusRecoveryRateFlat += amount;
    public void AddCollectRangeFlat(float amount) => bonusCollectRangeFlat += amount;

    public void TakeDamage(float amount)
    {
        float remaining = amount;
        if (CurrentArmor > 0f)
        {
            float absorbed = Mathf.Min(CurrentArmor, remaining);
            CurrentArmor -= absorbed;
            remaining -= absorbed;
        }
        CurrentHealth = Mathf.Max(CurrentHealth - remaining, 0f);
    }

    private void RegenOverTime()
    {
        if (CurrentHealth < MaxHealth)
            CurrentHealth = Mathf.Min(CurrentHealth + RecoveryRate * Time.deltaTime, MaxHealth);
    }
}