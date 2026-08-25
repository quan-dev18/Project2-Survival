using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrades/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    [SerializeField] private string upgradeName;
    public string UpgradeName => upgradeName;

    [TextArea]
    [SerializeField] private string description;
    public string Description => description;

    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;

    [Header("Stat Buffs")]
    [SerializeField] private List<StatMod> statMods;
    public List<StatMod> StatMods => statMods;

    [Header("Evolution Prerequisites")]
    [Tooltip("ANY of these upgrades must be owned for this upgrade to appear in the pool. Empty = always available.")]
    [SerializeField] private List<UpgradeSO> requires;
    public List<UpgradeSO> Requires => requires;

    [System.Serializable]
    public class StatMod
    {
        [SerializeField] private UpgradeType stat;
        public UpgradeType Stat => stat;

        [Tooltip("Percent (10 = +10%) for percent stats, flat amount for counts (bullets, pierce). Can be negative for trade-offs.")]
        [SerializeField] private float amount;
        public float Amount => amount;
    }
}

public enum UpgradeType
{
    MaxHealthPercent,
    MaxArmorPercent,
    RecoveryRatePercent,
    MoveSpeedPercent,
    CollectRangePercent,
    GrowthRatePercent,
    FireRatePercent,
    FireRangePercent,
    ReloadSpeedPercent,
    MagazineSizePercent,
    BulletCount,
    BulletPierce,
    BulletSpeedPercent,
    BulletDamagePercent,
    BulletExecutePercent,
    BulletKnockbackPercent,
    BulletSizePercent,
    BulletInfinitePierceOnKill,
    BulletExplosionOnKill,
    BulletSpreadPercent,
    BulletBounceCount,
FreeShotChanceWhileStill,
    AmmoRecoverOnXP,
    FireRateBuffOnXP,
    LastAmmoBurst,
    BackShot,
    DamageBuffAfterReload,
    ReloadSpeedStackOnKill,
    InvulnerableWhileReloading,
    BurnAura,
    StackingBuffOnTime,
    BulletSpread,
    CharacterSizePercent,
    DamageTakenFireRatePercent,
    DamageTakenBulletDamagePercent
}