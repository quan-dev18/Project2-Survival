using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemySO enemySO;

    //base stats
    public float baseMaxHealth { get; private set; }
    public float baseMovementSpeed { get; private set; }
    public float baseAttackDamage { get; private set; }
    public float baseAttackSpeed { get; private set; }
    public float attackRange { get; private set; }
    public EnemyType enemyType { get; private set; }

    //percent bonus
    public float bonusMaxHealthPercent { get; private set; }
    public float bonusMovementSpeedPercent { get; private set; }
    public float bonusAttackDamagePercent { get; private set; }
    public float bonusAttackSpeedPercent { get; private set; }

    //flat bonus
    public float bonusMaxHealthFlat { get; private set; }
    public float bonusAttackDamageFlat { get; private set; }

    //final stats
    public float maxHealth => (baseMaxHealth + bonusMaxHealthFlat) * (1f + bonusMaxHealthPercent);
    public float movementSpeed => Mathf.Min(baseMovementSpeed * (1f + bonusMovementSpeedPercent), 10f);
    public float attackDamage => (baseAttackDamage + bonusAttackDamageFlat) * (1f + bonusAttackDamagePercent);
    public float attackSpeed => Mathf.Min(baseAttackSpeed * (1f + bonusAttackSpeedPercent), 2.5f);

    public float currentHealth { get; private set; }

    private void Awake()
    {
        LoadFromSO();
        currentHealth = maxHealth;
    }

    public void LoadFromSO()
    {
        if (enemySO == null) return;

        baseMaxHealth = enemySO.MaxHealth;
        baseMovementSpeed = enemySO.MovementSpeed;
        baseAttackDamage = enemySO.AttackDamage;
        baseAttackSpeed = enemySO.AttackSpeed;
        attackRange = enemySO.AttackRange;
        enemyType = enemySO.EnemyType;
    }

    // ----- Các hàm cộng bonus -----
    public void AddMaxHealthPercent(float amount) => bonusMaxHealthPercent += amount;
    public void AddMovementSpeedPercent(float amount) => bonusMovementSpeedPercent += amount;
    public void AddAttackDamagePercent(float amount) => bonusAttackDamagePercent += amount;
    public void AddAttackSpeedPercent(float amount) => bonusAttackSpeedPercent += amount;

    public void AddMaxHealthFlat(float amount)
    {
        bonusMaxHealthFlat += amount;
        currentHealth += amount; 
    }
    public void AddAttackDamageFlat(float amount) => bonusAttackDamageFlat += amount;

    public void SetCurrentHealth(float value) => currentHealth = Mathf.Max(0f, value);
    public void ResetHealth() => currentHealth = maxHealth;
}