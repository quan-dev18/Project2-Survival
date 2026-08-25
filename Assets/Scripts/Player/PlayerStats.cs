using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private CharacterSO characterStats;
    [SerializeField] private WeaponController weapon;

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
    public float bonusCharacterSizePercent { get; private set; }
    public float bonusDamageTakenFireRatePercent { get; private set; }
    public float bonusDamageTakenBulletDamagePercent { get; private set; }
    public bool bonusInvulnerableWhileReloading { get; private set; }
    public float bonusBurnAuraChance { get; private set; }
    private float burnAuraTimer;
    public float bonusStackingBuffPercent { get; private set; }
    private float stackingBuffPercent;
    #endregion

    #region Stat Caps
    [Header("Stat Caps")]
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float maxMoveSpeed = 15f;
    [SerializeField] private float maxRecoveryRate = 50f;
    [SerializeField] private float maxCollectRange = 20f;
    [SerializeField] private float maxGrowthRate = 5f;
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
    private bool isDead;
    #endregion

    #region Damage Taken Buff
    [SerializeField] private float damageTakenBuffDuration = 2f;
    private float damageTakenTimer;
    private float appliedFireRateBuff;
    private float appliedDamageBuff;
    private bool isDamageBuffActive;
    #endregion

    private void Awake()
    {
        if (weapon == null) weapon = GetComponentInChildren<WeaponController>();
        LoadFromSO();
    }

    private void Update()
    {
        //regenerate health over time
        RegenOverTime();

        //update damage taken buff timer
        if (damageTakenTimer > 0f)
        {
            damageTakenTimer -= Time.deltaTime;
            if (damageTakenTimer <= 0f)
            {
                // Remove the temporary buffs using exactly what was applied
                if (weapon != null && isDamageBuffActive)
                {
                    weapon.AddFireRatePercent(-appliedFireRateBuff);
                    weapon.AddBulletDamagePercent(-appliedDamageBuff);
                    isDamageBuffActive = false;
                }
                damageTakenTimer = 0f;
            }
        }

        // Burn aura: 20% base chance per second + 3% per move speed
        if (bonusBurnAuraChance > 0f)
        {
            burnAuraTimer += Time.deltaTime;
            if (burnAuraTimer >= 1f)
            {
                burnAuraTimer = 0f;
                float chance = 0.2f + MoveSpeed * 0.03f;
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 4f);
                foreach (Collider2D hit in hits)
                {
                    EnemyHealth enemy = hit.GetComponentInChildren<EnemyHealth>();
                    if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
                    if (enemy != null && UnityEngine.Random.value < chance)
                    {
                        enemy.TakeDamage(5f); // burn tick damage
                    }
                }
            }
        }

        // Stacking buff: +2% per second, max 30%
        if (bonusStackingBuffPercent > 0f)
        {
            float targetPercent = Mathf.Min(stackingBuffPercent + 0.02f * Time.deltaTime * 60f, 0.3f); // 2% per second
            float delta = targetPercent - stackingBuffPercent;
            if (delta > 0f)
            {
                stackingBuffPercent = targetPercent;
                if (weapon != null)
                    weapon.AddBulletDamagePercent(delta);
                bonusMoveSpeedPercent += delta;
            }
        }
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

    public void AddCharacterSizePercent(float amount)
    {
        bonusCharacterSizePercent += amount;
        ApplyCharacterScale();
    }

    private void ApplyCharacterScale()
    {
        transform.localScale = Vector3.one * (1f + bonusCharacterSizePercent);
    }

    public void AddDamageTakenFireRatePercent(float amount) => bonusDamageTakenFireRatePercent += amount;

    public void AddDamageTakenBulletDamagePercent(float amount) => bonusDamageTakenBulletDamagePercent += amount;

    public void AddInvulnerableWhileReloading(float amount) => bonusInvulnerableWhileReloading = amount > 0f;

    public void AddBurnAuraChance(float amount) => bonusBurnAuraChance += amount;

    public void AddStackingBuffPercent(float amount) => bonusStackingBuffPercent += amount;

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return;

        // Invulnerable while reloading
        if (bonusInvulnerableWhileReloading && weapon != null && weapon.IsReloading)
            return;

        float remaining = amount;
        if (CurrentArmor > 0f)
        {
            float absorbed = Mathf.Min(CurrentArmor, remaining);
            CurrentArmor -= absorbed;
            remaining -= absorbed;
        }
        CurrentHealth = Mathf.Max(CurrentHealth - remaining, 0f);

        // Reset stacking buff on hit
        if (stackingBuffPercent > 0f)
        {
            if (weapon != null)
                weapon.AddBulletDamagePercent(-stackingBuffPercent);
            bonusMoveSpeedPercent -= stackingBuffPercent;
            stackingBuffPercent = 0f;
        }

        // Trigger the damage-taken buff
        OnDamageTaken();
    }

    public void OnDamageTaken()
    {
        // Apply the damage-taken buffs for the duration (only if not already active)
        if (weapon != null && !isDamageBuffActive)
        {
            appliedFireRateBuff = bonusDamageTakenFireRatePercent;
            appliedDamageBuff = bonusDamageTakenBulletDamagePercent;
            weapon.AddFireRatePercent(appliedFireRateBuff);
            weapon.AddBulletDamagePercent(appliedDamageBuff);
            isDamageBuffActive = true;
        }
        // Always refresh the timer
        damageTakenTimer = damageTakenBuffDuration;
    }

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

    private void RegenOverTime()
    {
        if (CurrentHealth < MaxHealth)
            CurrentHealth = Mathf.Min(CurrentHealth + RecoveryRate * Time.deltaTime, MaxHealth);
    }
}