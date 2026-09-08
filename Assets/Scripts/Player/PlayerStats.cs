using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private List<CharacterSO> characterList = new List<CharacterSO>();
    [SerializeField] private WeaponController[] weapons;
    [SerializeField] private GameObject mysteryCubePrefab;
    [SerializeField] private GameObject spiritPrefab;

    private CharacterSO characterStats;

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
    public float bonusArmorRegenPerSecond { get; private set; }
    private float burnAuraTimer;
    public float bonusStackingBuffPercent { get; private set; }
    private float stackingBuffPercent;
    public bool bonusMysteryCube { get; private set; }
    public bool bonusSpiritSummon { get; private set; }
    public bool bonusSpiritHeal { get; private set; }
    public bool bonusSpiritBurn { get; private set; }
    public bool bonusSpiritEmpowered { get; private set; }
    public float bonusGoldGainPercent { get; private set; }
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

    #region Events
    public event System.Action<float, float> OnHealthChanged;
    #endregion

    public WeaponController[] Weapons
    {
        get
        {
            // Always return ALL weapons (including inactive) to avoid single-weapon cache bug
            var all = FindObjectsByType<WeaponController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all != null && all.Length > 0)
            {
                weapons = all;
                return weapons;
            }
            if (weapons == null || weapons.Length == 0)
                weapons = GetComponentsInChildren<WeaponController>(true);
            return weapons;
        }
    }
    private WeaponController[] GetAllWeapons() => Weapons;
    public int ActiveWeaponIndex { get; private set; }
    public WeaponController ActiveWeapon => Weapons != null && ActiveWeaponIndex >= 0 && ActiveWeaponIndex < Weapons.Length
        ? Weapons[ActiveWeaponIndex] : null;

    public void SetActiveWeaponIndex(int index)
    {
        ActiveWeaponIndex = index;
    }

    public void SetWeapons(WeaponController[] newWeapons)
    {
        weapons = newWeapons;
    }

    public void RegisterAllWeapons()
    {
        var found = GetComponentsInChildren<WeaponController>(true);
        if (found != null && found.Length > 0)
            weapons = found;
    }

    #region Damage Taken Buff
    [SerializeField] private float damageTakenBuffDuration = 2f;
    private float damageTakenTimer;
    private float appliedFireRateBuff;
    private float appliedDamageBuff;
    private bool isDamageBuffActive;
    #endregion

    private void Awake()
    {
        if (weapons == null || weapons.Length == 0)
        {
            var found = GetComponentsInChildren<WeaponController>(true);
            if (found != null && found.Length > 0)
                weapons = found;
        }

        int heroIndex = PlayerPrefs.GetInt("SelectedHeroIndex", 0);
        if (characterList != null && heroIndex >= 0 && heroIndex < characterList.Count)
            characterStats = characterList[heroIndex];

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
                var allW = GetAllWeapons();
                if (allW != null && isDamageBuffActive)
                {
                    foreach (var w in allW)
                    {
                        if (w != null)
                        {
                            w.AddFireRatePercent(-appliedFireRateBuff);
                            w.AddBulletDamagePercent(-appliedDamageBuff);
                        }
                    }
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
            float targetPercent = Mathf.Min(stackingBuffPercent + 0.02f * Time.deltaTime, 0.3f); // 2% per second
            float delta = targetPercent - stackingBuffPercent;
            if (delta > 0f)
            {
                stackingBuffPercent = targetPercent;
                var allW2 = GetAllWeapons();
                if (allW2 != null)
                {
                    foreach (var w in allW2)
                    {
                        if (w != null) w.AddBulletDamagePercent(delta);
                    }
                }
                bonusMoveSpeedPercent += delta;
            }
        }

        // Armor regen: if not at max armor, regenerate
        if (bonusArmorRegenPerSecond > 0f && CurrentArmor < MaxArmor)
        {
            float regenAmount = bonusArmorRegenPerSecond * Time.deltaTime;
            CurrentArmor = Mathf.Min(CurrentArmor + regenAmount, MaxArmor);
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
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    //bonus percent
    public void AddMaxHealthPercent(float amount)
    {
        float oldMax = MaxHealth;
        bonusMaxHealthPercent += amount;
        float newMax = MaxHealth;
        CurrentHealth = newMax; //Mathf.Min(CurrentHealth + (newMax - oldMax), newMax);
        OnHealthChanged?.Invoke(CurrentHealth, newMax);
    }

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
        // Scale the root container so hitbox (Collider2D) and visual both scale
        Transform root = transform;
        while (root.parent != null)
            root = root.parent;
        root.localScale = Vector3.one * (1f + bonusCharacterSizePercent);
    }

    public void AddDamageTakenFireRatePercent(float amount) => bonusDamageTakenFireRatePercent += amount;

    public void AddDamageTakenBulletDamagePercent(float amount) => bonusDamageTakenBulletDamagePercent += amount;

    public void AddInvulnerableWhileReloading(float amount) => bonusInvulnerableWhileReloading = amount > 0f;

    public void AddBurnAuraChance(float amount) => bonusBurnAuraChance += amount;

    public void AddArmorRegenPerSecond(float amount) => bonusArmorRegenPerSecond += amount;

    public void AddStackingBuffPercent(float amount) => bonusStackingBuffPercent += amount;

    public void AddMysteryCube(float amount)
    {
        bonusMysteryCube = amount > 0f;
        if (bonusMysteryCube && mysteryCubePrefab != null)
        {
            GameObject cube = Instantiate(mysteryCubePrefab, transform.position, Quaternion.identity);
            MysteryCube mc = cube.GetComponent<MysteryCube>();
            if (mc != null)
            {
                mc.Initialize(transform);
            }
        }
    }

    public void AddSpiritSummon(float amount)
    {
        bonusSpiritSummon = amount > 0f;
        if (bonusSpiritSummon && spiritPrefab != null)
        {
            GameObject spirit = Instantiate(spiritPrefab, transform.position, Quaternion.identity);
            Spirit s = spirit.GetComponent<Spirit>();
            if (s != null)
            {
                s.Initialize(transform);
            }
        }
    }

    public void AddSpiritHeal(float amount)
    {
        bonusSpiritHeal = amount > 0f;
        Spirit[] spirits = FindObjectsByType<Spirit>(FindObjectsSortMode.None);
        foreach (Spirit s in spirits)
        {
            s.EnableHolyHeal();
        }
    }

    public void AddSpiritBurn(float amount)
    {
        bonusSpiritBurn = amount > 0f;
        Spirit[] spirits = FindObjectsByType<Spirit>(FindObjectsSortMode.None);
        foreach (Spirit s in spirits)
        {
            s.EnableHolyBurn();
        }
    }

    public void AddSpiritEmpowered(float amount)
    {
        bonusSpiritEmpowered = amount > 0f;
        Spirit[] spirits = FindObjectsByType<Spirit>(FindObjectsSortMode.None);
        foreach (Spirit s in spirits)
        {
            s.EnableEmpowered();
        }
    }

    public void AddGoldGainPercent(float amount) => bonusGoldGainPercent += amount;

    /// <summary>Tính số vàng thực nhận sau khi cộng buff +% vàng.</summary>
    public int GetGoldGainAmount(int baseAmount)
        => Mathf.RoundToInt(baseAmount * (1f + bonusGoldGainPercent));

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return;

        // Invulnerable while reloading - any weapon reloading = invuln
        {
            var allWInv = GetAllWeapons();
            if (bonusInvulnerableWhileReloading && allWInv != null)
            {
                foreach (var w in allWInv)
                {
                    if (w != null && w.IsReloading) return;
                }
            }
        }

        float remaining = amount;
        if (CurrentArmor > 0f)
        {
            float absorbed = Mathf.Min(CurrentArmor, remaining);
            CurrentArmor -= absorbed;
            remaining -= absorbed;
        }
        CurrentHealth = Mathf.Max(CurrentHealth - remaining, 0f);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        GetComponentInChildren<SpriteFlashEffect>()?.Flash();
        // Trigger synergies on hit
        SynergyManager.Instance?.OnPlayerHit();
        // Reset stacking buff on hit
        if (stackingBuffPercent > 0f)
        {
            var allWReset = GetAllWeapons();
            if (allWReset != null)
            {
                foreach (var w in allWReset)
                {
                    if (w != null) w.AddBulletDamagePercent(-stackingBuffPercent);
                }
            }
            bonusMoveSpeedPercent -= stackingBuffPercent;
            stackingBuffPercent = 0f;
        }

        // Trigger the damage-taken buff
        OnDamageTaken();
    }

    public void OnDamageTaken()
    {
        // Apply the damage-taken buffs for the duration (only if not already active)
        var allWDmg = GetAllWeapons();
        if (allWDmg != null && !isDamageBuffActive)
        {
            appliedFireRateBuff = bonusDamageTakenFireRatePercent;
            appliedDamageBuff = bonusDamageTakenBulletDamagePercent;
            foreach (var w in allWDmg)
            {
                if (w != null)
                {
                    w.AddFireRatePercent(appliedFireRateBuff);
                    w.AddBulletDamagePercent(appliedDamageBuff);
                }
            }
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
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void AddMaxArmorFlat(float amount)
    {
        bonusMaxArmorFlat += amount;
        CurrentArmor = Mathf.Min(CurrentArmor + amount, MaxArmor);
    }

    public void AddRecoveryRateFlat(float amount) => bonusRecoveryRateFlat += amount;

    public void AddCollectRangeFlat(float amount) => bonusCollectRangeFlat += amount;

    [Header("Passive Heal")]
    [SerializeField] private float passiveHealPerSecond = 1f;

    private void RegenOverTime()
    {
        if (isDead) return;
        if (CurrentHealth < MaxHealth)
        {
            float heal = (RecoveryRate + passiveHealPerSecond) * Time.deltaTime;
            CurrentHealth = Mathf.Min(CurrentHealth + heal, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}