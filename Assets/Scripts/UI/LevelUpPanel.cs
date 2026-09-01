using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpPanel : MonoBehaviour
{
    public static LevelUpPanel Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;

    [Header("Upgrade Pool")]
    [SerializeField] private List<UpgradeSO> upgradePool;

    [Header("Rain Effect")]
    [SerializeField] private ParticleSystem rainEffect;

    private readonly List<UpgradeSO> choices = new List<UpgradeSO>();
    private readonly HashSet<UpgradeSO> ownedUpgrades = new HashSet<UpgradeSO>();
    private PlayerStats playerStats;
    private WeaponController weapon;

    private void Awake()
    {
        Instance = this;
        GameManager.OnStateChanged += OnGameStateChanged;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            weapon = player.GetComponentInChildren<WeaponController>();
        }

        button1.onClick.AddListener(() => Choose(0));
        button2.onClick.AddListener(() => Choose(1));
        button3.onClick.AddListener(() => Choose(2));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    public UpgradeSO GetRandomUpgrade()
    {
        if (upgradePool == null || upgradePool.Count == 0) return null;

        List<UpgradeSO> eligible = new List<UpgradeSO>();
        foreach (UpgradeSO upgrade in upgradePool)
        {
            if (upgrade == null) continue;
            if (ownedUpgrades.Contains(upgrade)) continue;

            if (upgrade.Requires != null && upgrade.Requires.Count > 0)
            {
                bool met = false;
                foreach (UpgradeSO req in upgrade.Requires)
                {
                    if (req != null && ownedUpgrades.Contains(req))
                    {
                        met = true;
                        break;
                    }
                }
                if (!met) continue;
            }

            eligible.Add(upgrade);
        }

        if (eligible.Count == 0) return null;
        return eligible[Random.Range(0, eligible.Count)];
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.LevelUp)
        {
            RollChoices();
            gameObject.SetActive(true);
            rainEffect?.Play();
        }
        else
        {
            gameObject.SetActive(false);
            rainEffect?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void RollChoices()
    {
        choices.Clear();

        if (upgradePool == null || upgradePool.Count == 0)
        {
            Debug.LogWarning("LevelUpPanel: upgrade pool is empty");
            return;
        }

        List<UpgradeSO> eligible = new List<UpgradeSO>();
        foreach (UpgradeSO upgrade in upgradePool)
        {
            if (upgrade == null) continue;
            if (ownedUpgrades.Contains(upgrade)) continue;

            if (upgrade.Requires != null && upgrade.Requires.Count > 0)
            {
                bool met = false;
                foreach (UpgradeSO req in upgrade.Requires)
                {
                    if (req != null && ownedUpgrades.Contains(req))
                    {
                        met = true;
                        break;
                    }
                }
                if (!met) continue;
            }

            eligible.Add(upgrade);
        }

        for (int i = 0; i < 3 && eligible.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, eligible.Count);
            choices.Add(eligible[index]);
            eligible.RemoveAt(index);
        }

        SetButton(button1, 0);
        SetButton(button2, 1);
        SetButton(button3, 2);
    }

    private void SetButton(Button button, int choiceIndex)
    {
        if (button == null) return;

        if (choiceIndex >= choices.Count)
        {
            button.gameObject.SetActive(false);
            return;
        }

        UpgradeSO upgrade = choices[choiceIndex];
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = $"<b>{upgrade.UpgradeName}</b>\n{upgrade.Description}";

        button.gameObject.SetActive(true);
    }

    private void Choose(int choiceIndex)
    {
        if (choiceIndex >= choices.Count) return;

        UpgradeSO upgrade = choices[choiceIndex];
        ApplyUpgrade(upgrade);
        ownedUpgrades.Add(upgrade);
        choices.Clear();
        GameManager.Instance.SetState(GameState.Playing);
    }

    private void ApplyUpgrade(UpgradeSO upgrade)
    {
        if (upgrade.StatMods == null || upgrade.StatMods.Count == 0)
        {
            Debug.LogWarning($"Upgrade '{upgrade.UpgradeName}' has no stat mods");
            return;
        }

        foreach (UpgradeSO.StatMod mod in upgrade.StatMods)
            ApplyStat(mod.Stat, mod.Amount);

        // Register max-level upgrades for synergies
        SynergyManager.Instance?.RegisterMaxLevelUpgrade(upgrade.UpgradeName);

        Debug.Log($"Applied upgrade: {upgrade.UpgradeName}");
    }

    private void ApplyStat(UpgradeType stat, float amount)
    {
        float pct = amount / 100f;

        switch (stat)
        {
            case UpgradeType.MaxHealthPercent:
                playerStats?.AddMaxHealthPercent(pct);
                break;
            case UpgradeType.MaxArmorPercent:
                playerStats?.AddMaxArmorPercent(pct);
                break;
            case UpgradeType.RecoveryRatePercent:
                playerStats?.AddRecoveryRatePercent(pct);
                break;
            case UpgradeType.MoveSpeedPercent:
                playerStats?.AddMoveSpeedPercent(pct);
                break;
            case UpgradeType.CollectRangePercent:
                playerStats?.AddCollectRangePercent(pct);
                break;
            case UpgradeType.GrowthRatePercent:
                playerStats?.AddGrowthRatePercent(pct);
                break;
            case UpgradeType.FireRatePercent:
                weapon?.AddFireRatePercent(pct);
                break;
            case UpgradeType.FireRangePercent:
                weapon?.AddFireRangePercent(pct);
                break;
            case UpgradeType.ReloadSpeedPercent:
                weapon?.AddReloadSpeedPercent(pct);
                break;
            case UpgradeType.MagazineSizePercent:
                weapon?.AddMagazineSizePercent(pct);
                break;
            case UpgradeType.BulletCount:
                weapon?.AddBulletCount(Mathf.RoundToInt(amount));
                break;
            case UpgradeType.BulletPierce:
                weapon?.AddBulletPierce(Mathf.RoundToInt(amount));
                break;
            case UpgradeType.BulletSpeedPercent:
                weapon?.AddBulletSpeedPercent(pct);
                break;
            case UpgradeType.BulletDamagePercent:
                weapon?.AddBulletDamagePercent(pct);
                break;
            case UpgradeType.BulletExecutePercent:
                weapon?.AddBulletExecutePercent(pct);
                break;
            case UpgradeType.BulletKnockbackPercent:
                weapon?.AddBulletKnockbackPercent(pct);
                break;
            case UpgradeType.BulletSizePercent:
                weapon?.AddBulletSizePercent(pct);
                break;
            case UpgradeType.BulletInfinitePierceOnKill:
                weapon?.AddBulletInfinitePierceOnKill(pct);
                break;
            case UpgradeType.BulletExplosionOnKill:
                weapon?.AddBulletExplosionDamagePercent(pct / 100f);
                weapon?.AddBulletExplosionRadius(1.0f);
                break;
            case UpgradeType.CharacterSizePercent:
                playerStats?.AddCharacterSizePercent(pct);
                break;
            case UpgradeType.DamageTakenFireRatePercent:
                playerStats?.AddDamageTakenFireRatePercent(pct);
                break;
            case UpgradeType.DamageTakenBulletDamagePercent:
                playerStats?.AddDamageTakenBulletDamagePercent(pct);
                break;
            case UpgradeType.FreeShotChanceWhileStill:
                weapon?.AddFreeShotChanceWhileStill(pct);
                break;
            case UpgradeType.AmmoRecoverOnXP:
                PlayerXP.Instance?.AddAmmoRecoverChance(pct);
                break;
            case UpgradeType.FireRateBuffOnXP:
                PlayerXP.Instance?.AddFireRateBuffOnXPChance(pct);
                break;
            case UpgradeType.LastAmmoBurst:
                weapon?.AddLastAmmoBurst(pct);
                break;
            case UpgradeType.BackShot:
                weapon?.AddBackShot(pct);
                break;
            case UpgradeType.DamageBuffAfterReload:
                weapon?.AddDamageBuffAfterReload(pct);
                break;
            case UpgradeType.ReloadSpeedStackOnKill:
                weapon?.AddReloadSpeedStackOnKill(pct);
                break;
            case UpgradeType.InvulnerableWhileReloading:
                playerStats?.AddInvulnerableWhileReloading(pct);
                break;
            case UpgradeType.BurnAura:
                playerStats?.AddBurnAuraChance(pct);
                break;
            case UpgradeType.StackingBuffOnTime:
                playerStats?.AddStackingBuffPercent(pct);
                break;
            case UpgradeType.BulletSpread:
                weapon?.AddBulletSpread(amount); // flat degrees
                break;
            case UpgradeType.MysteryCube:
                playerStats?.AddMysteryCube(pct);
                break;
            case UpgradeType.MysteryCubeDmgStack:
                break; // intrinsic to cube
            case UpgradeType.MysteryCubeAsStack:
                break; // intrinsic to cube
            case UpgradeType.ArmorRegenPerSecond:
                playerStats?.AddArmorRegenPerSecond(amount); // raw armor/sec
                break;
            case UpgradeType.SpiritSummon:
                playerStats?.AddSpiritSummon(pct);
                break;
            case UpgradeType.SpiritHeal:
                playerStats?.AddSpiritHeal(pct);
                break;
            case UpgradeType.SpiritBurn:
                playerStats?.AddSpiritBurn(pct);
                break;
            case UpgradeType.SpiritEmpowered:
                playerStats?.AddSpiritEmpowered(pct);
                break;
        }
    }

    public static void ApplyUpgradeDirect(UpgradeSO upgrade)
    {
        if (upgrade == null || upgrade.StatMods == null || upgrade.StatMods.Count == 0) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerStats ps = player.GetComponent<PlayerStats>();
        WeaponController wc = player.GetComponentInChildren<WeaponController>();

        foreach (UpgradeSO.StatMod mod in upgrade.StatMods)
            ApplyStatDirect(ps, wc, mod.Stat, mod.Amount);
    }

    private static void ApplyStatDirect(PlayerStats ps, WeaponController wc, UpgradeType stat, float amount)
    {
        float pct = amount / 100f;

        switch (stat)
        {
            case UpgradeType.MaxHealthPercent:
                ps?.AddMaxHealthPercent(pct);
                break;
            case UpgradeType.MaxArmorPercent:
                ps?.AddMaxArmorPercent(pct);
                break;
            case UpgradeType.RecoveryRatePercent:
                ps?.AddRecoveryRatePercent(pct);
                break;
            case UpgradeType.MoveSpeedPercent:
                ps?.AddMoveSpeedPercent(pct);
                break;
            case UpgradeType.CollectRangePercent:
                ps?.AddCollectRangePercent(pct);
                break;
            case UpgradeType.GrowthRatePercent:
                ps?.AddGrowthRatePercent(pct);
                break;
            case UpgradeType.FireRatePercent:
                wc?.AddFireRatePercent(pct);
                break;
            case UpgradeType.FireRangePercent:
                wc?.AddFireRangePercent(pct);
                break;
            case UpgradeType.ReloadSpeedPercent:
                wc?.AddReloadSpeedPercent(pct);
                break;
            case UpgradeType.MagazineSizePercent:
                wc?.AddMagazineSizePercent(pct);
                break;
            case UpgradeType.BulletCount:
                wc?.AddBulletCount(Mathf.RoundToInt(amount));
                break;
            case UpgradeType.BulletPierce:
                wc?.AddBulletPierce(Mathf.RoundToInt(amount));
                break;
            case UpgradeType.BulletSpeedPercent:
                wc?.AddBulletSpeedPercent(pct);
                break;
            case UpgradeType.BulletDamagePercent:
                wc?.AddBulletDamagePercent(pct);
                break;
            case UpgradeType.BulletExecutePercent:
                wc?.AddBulletExecutePercent(pct);
                break;
            case UpgradeType.BulletKnockbackPercent:
                wc?.AddBulletKnockbackPercent(pct);
                break;
            case UpgradeType.BulletSizePercent:
                wc?.AddBulletSizePercent(pct);
                break;
            case UpgradeType.BulletInfinitePierceOnKill:
                wc?.AddBulletInfinitePierceOnKill(pct);
                break;
            case UpgradeType.BulletExplosionOnKill:
                wc?.AddBulletExplosionDamagePercent(pct / 100f);
                wc?.AddBulletExplosionRadius(1.0f);
                break;
            case UpgradeType.CharacterSizePercent:
                ps?.AddCharacterSizePercent(pct);
                break;
            case UpgradeType.DamageTakenFireRatePercent:
                ps?.AddDamageTakenFireRatePercent(pct);
                break;
            case UpgradeType.DamageTakenBulletDamagePercent:
                ps?.AddDamageTakenBulletDamagePercent(pct);
                break;
            case UpgradeType.FreeShotChanceWhileStill:
                wc?.AddFreeShotChanceWhileStill(pct);
                break;
            case UpgradeType.AmmoRecoverOnXP:
                PlayerXP.Instance?.AddAmmoRecoverChance(pct);
                break;
            case UpgradeType.FireRateBuffOnXP:
                PlayerXP.Instance?.AddFireRateBuffOnXPChance(pct);
                break;
            case UpgradeType.LastAmmoBurst:
                wc?.AddLastAmmoBurst(pct);
                break;
            case UpgradeType.BackShot:
                wc?.AddBackShot(pct);
                break;
            case UpgradeType.DamageBuffAfterReload:
                wc?.AddDamageBuffAfterReload(pct);
                break;
            case UpgradeType.ReloadSpeedStackOnKill:
                wc?.AddReloadSpeedStackOnKill(pct);
                break;
            case UpgradeType.InvulnerableWhileReloading:
                ps?.AddInvulnerableWhileReloading(pct);
                break;
            case UpgradeType.BurnAura:
                ps?.AddBurnAuraChance(pct);
                break;
            case UpgradeType.StackingBuffOnTime:
                ps?.AddStackingBuffPercent(pct);
                break;
            case UpgradeType.BulletSpread:
                wc?.AddBulletSpread(amount);
                break;
            case UpgradeType.MysteryCube:
                ps?.AddMysteryCube(pct);
                break;
            case UpgradeType.MysteryCubeDmgStack:
                break;
            case UpgradeType.MysteryCubeAsStack:
                break;
            case UpgradeType.ArmorRegenPerSecond:
                ps?.AddArmorRegenPerSecond(amount);
                break;
            case UpgradeType.SpiritSummon:
                ps?.AddSpiritSummon(pct);
                break;
            case UpgradeType.SpiritHeal:
                ps?.AddSpiritHeal(pct);
                break;
            case UpgradeType.SpiritBurn:
                ps?.AddSpiritBurn(pct);
                break;
            case UpgradeType.SpiritEmpowered:
                ps?.AddSpiritEmpowered(pct);
                break;
        }
    }
}