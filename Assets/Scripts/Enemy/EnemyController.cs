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
    public float xpReward { get; private set; }

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

    private float nextAttackTime;

    private SpriteRenderer[] spriteRenderers;

    private void Awake()
    {
        LoadFromSO();
        currentHealth = maxHealth;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        UpdateLayerOrder();
    }

    public void UpdateLayerOrder()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i].sortingOrder != order)
                spriteRenderers[i].sortingOrder = order;
        }
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
        xpReward = enemySO.XpReward;
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

    public void Attack()
    {
        if (ObjectPooling.Instance == null)
        {
            return;
        }

        if (ObjectPooling.Instance.targetTransform == null)
        {
            return;
        }

        PlayerHealth playerHealth = ObjectPooling.Instance.targetTransform.GetComponentInChildren<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.DealDamage(attackDamage);
        float interval = attackSpeed > 0f ? 1f / attackSpeed : 1f;
        nextAttackTime = Time.time + interval;
    }
}