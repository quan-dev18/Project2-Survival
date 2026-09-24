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

    [HideInInspector] public float difficultyHpMultiplier = 1f;
    [HideInInspector] public float difficultySpeedMultiplier = 1f;
    [HideInInspector] public float difficultyDamageMultiplier = 1f;

    //final stats
    public float maxHealth => (baseMaxHealth + bonusMaxHealthFlat) * (1f + bonusMaxHealthPercent) * difficultyHpMultiplier;
    public float movementSpeed => Mathf.Min(baseMovementSpeed * (1f + bonusMovementSpeedPercent) * speedMultiplier * difficultySpeedMultiplier, 10f);
    public float attackDamage => (baseAttackDamage + bonusAttackDamageFlat) * (1f + bonusAttackDamagePercent) * difficultyDamageMultiplier;
    public float attackSpeed => Mathf.Min(baseAttackSpeed * (1f + bonusAttackSpeedPercent), 2.5f);

    [HideInInspector] public float speedMultiplier = 1f;

    public float currentHealth { get; private set; }

    private float nextAttackTime;

    public bool CanAttack => Time.time >= nextAttackTime;

    private SpriteRenderer[] spriteRenderers;
    private float lastSortingY = float.MinValue;
    private int lastSortingOrder = int.MinValue;

    private void Awake()
    {
        LoadFromSO();
        currentHealth = maxHealth;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        // Top-down game: gravity must never act on enemies, no matter which
        // code path touches bodyType (e.g. circle-wall capture/restore).
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;
    }

    private void Update()
    {
        UpdateLayerOrder();
    }

    public void UpdateLayerOrder()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        float currentY = transform.position.y;
        if (Mathf.Abs(currentY - lastSortingY) < 0.05f) return;
        lastSortingY = currentY;

        int order = Mathf.RoundToInt(-currentY * 100f);
        if (order == lastSortingOrder) return;
        lastSortingOrder = order;

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

    public void ResetBonuses()
    {
        bonusMaxHealthPercent = 0f;
        bonusMovementSpeedPercent = 0f;
        bonusAttackDamagePercent = 0f;
        bonusAttackSpeedPercent = 0f;
        bonusMaxHealthFlat = 0f;
        bonusAttackDamageFlat = 0f;
        difficultyHpMultiplier = 1f;
        difficultySpeedMultiplier = 1f;
        difficultyDamageMultiplier = 1f;
    }

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

        playerHealth.DealDamage(attackDamage, this);
        CameraShake.Shake(0.5f, 0.3f);
        float interval = attackSpeed > 0f ? 1f / attackSpeed : 1f;
        nextAttackTime = Time.time + interval;
    }
}