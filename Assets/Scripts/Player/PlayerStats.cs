using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private CharacterSO characterStats;

    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public float MaxArmor { get; private set; }
    public float CurrentArmor { get; private set; }
    public float RecoveryRate { get; private set; }
    public float MoveSpeed { get; private set; }
    public float CollectRange { get; private set; }
    public float GrowthRate { get; private set; }

    private void Awake()
    {
        LoadFromSO();
    }

    private void Update()
    {
        RegenOverTime();
    }

    public void LoadFromSO()
    {
        if (characterStats == null) return;

        MaxHealth = characterStats.MaxHealth;
        CurrentHealth = MaxHealth;
        MaxArmor = characterStats.MaxArmor;
        CurrentArmor = MaxArmor;
        RecoveryRate = characterStats.RecoveryRate;
        MoveSpeed = characterStats.MovementSpeed;
        CollectRange = characterStats.CollectRange;
        GrowthRate = characterStats.GrowthRate;
    }

    public void AddMaxHealth(float amount)
    {
        MaxHealth += amount;
        CurrentHealth += amount;
    }

    public void AddMaxArmor(float amount)
    {
        MaxArmor += amount;
        CurrentArmor += amount;
    }

    public void AddRecoveryRate(float amount) => RecoveryRate += amount;
    public void AddMoveSpeed(float amount) => MoveSpeed += amount;
    public void AddCollectRange(float amount) => CollectRange += amount;
    public void AddGrowthRate(float amount) => GrowthRate += amount;

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
