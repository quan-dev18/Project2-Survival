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

    [Header("Titles (separate from description)")]
    [SerializeField] private TMP_Text title1;
    [SerializeField] private TMP_Text title2;
    [SerializeField] private TMP_Text title3;

    [Header("Descriptions")]
    [SerializeField] private TMP_Text desc1;
    [SerializeField] private TMP_Text desc2;
    [SerializeField] private TMP_Text desc3;

    [Header("Icons (placeholder)")]
    [SerializeField] private Image icon1;
    [SerializeField] private Image icon2;
    [SerializeField] private Image icon3;

    [Header("Upgrade Pool")]
    [SerializeField] private List<UpgradeSO> upgradePool;

    [Header("Startup Upgrades (Testing)")]
    [Tooltip("Auto-applied 3s after stage starts (Playing). For testing.")]
    [SerializeField] private List<UpgradeSO> startupUpgrades;
    private bool startupApplied;
    private Coroutine startupRoutine;

    [Header("Rain Effect")]
    [SerializeField] private ParticleSystem rainEffect;

    private readonly List<UpgradeSO> choices = new List<UpgradeSO>();
    private readonly HashSet<UpgradeSO> ownedUpgrades = new HashSet<UpgradeSO>();
    private PlayerStats playerStats;

    private void Awake()
    {
        Instance = this;
        GameManager.OnStateChanged += OnGameStateChanged;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
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
            if(rainEffect != null)
            {
                rainEffect.gameObject.SetActive(true);
                var main = rainEffect.main;
                main.useUnscaledTime = true;
                rainEffect.Play();
            }
            
        }
        else
        {
            gameObject.SetActive(false);
            rainEffect?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (state == GameState.Playing && !startupApplied && startupUpgrades != null && startupUpgrades.Count > 0)
        {
            // LevelUpPanel is inactive at start, so StartCoroutine on this fails -> run on GameManager
            var runner = GameManager.Instance != null ? GameManager.Instance : (MonoBehaviour)this;
            if (startupRoutine != null) runner.StopCoroutine(startupRoutine);
            startupRoutine = runner.StartCoroutine(ApplyStartupUpgradesRoutine());
        }
        else if (state != GameState.Playing && startupRoutine != null)
        {
            var runner = GameManager.Instance != null ? GameManager.Instance : (MonoBehaviour)this;
            runner.StopCoroutine(startupRoutine);
            startupRoutine = null;
        }
    }

    private System.Collections.IEnumerator ApplyStartupUpgradesRoutine()
    {
        // Wait 3s after stage actually starts to avoid init races
        yield return new WaitForSecondsRealtime(3f);
        if (startupApplied) yield break;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) yield break;

        // Ensure playerStats resolved (player may spawn late)
        if (playerStats == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }

        foreach (var up in startupUpgrades)
        {
            if (up == null || ownedUpgrades.Contains(up)) continue;
            ApplyUpgrade(up);
            ownedUpgrades.Add(up);
            Debug.Log($"[Startup] Applied {up.UpgradeName}");
        }
        startupApplied = true;
        startupRoutine = null;
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

        // Resolve per-slot title/desc/icon
        TMP_Text title = choiceIndex == 0 ? title1 : choiceIndex == 1 ? title2 : title3;
        TMP_Text desc = choiceIndex == 0 ? desc1 : choiceIndex == 1 ? desc2 : desc3;
        Image icon = choiceIndex == 0 ? icon1 : choiceIndex == 1 ? icon2 : icon3;

        bool hasSplitFields = title != null || desc != null || icon != null;

        if (hasSplitFields)
        {
            if (title != null)
                title.text = $"<b>{upgrade.UpgradeName}</b>";
            if (desc != null)
                desc.text = upgrade.Description;
            if (icon != null)
                icon.sprite = upgrade.Icon; // placeholder - assign icons in UpgradeSO
            // If title assigned, clear legacy combined label to avoid duplicate text
            if (title != null)
            {
                var legacyLabels = button.GetComponentsInChildren<TMP_Text>(true);
                foreach (var lbl in legacyLabels)
                {
                    if (lbl != title && lbl != desc)
                    {
                        // Keep legacy label empty when split fields are used (optional)
                        // lbl.text = string.Empty;
                    }
                }
            }
        }
        else
        {
            // Fallback: old combined label behavior
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"<b>{upgrade.UpgradeName}</b>\n{upgrade.Description}";
        }

        button.gameObject.SetActive(true);
    }

    private void Choose(int choiceIndex)
    {
        if (choiceIndex >= choices.Count) return;

        UpgradeSO upgrade = choices[choiceIndex];
        ApplyUpgrade(upgrade);
        ownedUpgrades.Add(upgrade);
        choices.Clear();
        if (rainEffect != null) rainEffect.gameObject.SetActive(false);
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
        // Fetch via Player hierarchy so order matches PlayerEquipment (avoids scene-wide stray WeaponControllers)
        var player = GameObject.FindGameObjectWithTag("Player");
        WeaponController[] allWeapons = null;
        FlamethrowerController[] allFlames = null;
        if (player != null)
        {
            allWeapons = player.GetComponentsInChildren<WeaponController>(true);
            allFlames = player.GetComponentsInChildren<FlamethrowerController>(true);
        }
        if (allWeapons == null || allWeapons.Length == 0)
            allWeapons = playerStats?.Weapons;
        if (allFlames == null || allFlames.Length == 0)
            allFlames = playerStats?.Flamethrowers;
        System.Action<System.Action<WeaponController>> applyWeapons = act => ApplyToAllWeapons(allWeapons, act);
        System.Action<System.Action<FlamethrowerController>> applyFlames = act => { if(allFlames!=null) foreach(var f in allFlames) if(f!=null) act(f); };

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
                ApplyToAllWeapons(allWeapons, w => w.AddFireRatePercent(pct)); applyFlames(f => f.AddFireRatePercent(pct));
                break;
            case UpgradeType.FireRangePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddFireRangePercent(pct)); applyFlames(f => f.AddFireRangePercent(pct));
                break;
            case UpgradeType.ReloadSpeedPercent:
                ApplyToAllWeapons(allWeapons, w => w.AddReloadSpeedPercent(pct)); applyFlames(f => f.AddReloadSpeedPercent(pct));
                break;
            case UpgradeType.MagazineSizePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddMagazineSizePercent(pct)); applyFlames(f => f.AddMagazineSizePercent(pct));
                break;
            case UpgradeType.BulletCount:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletCount(Mathf.RoundToInt(amount))); applyFlames(f => f.AddBulletCount(Mathf.RoundToInt(amount)));
                break;
            case UpgradeType.BulletPierce:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletPierce(Mathf.RoundToInt(amount))); applyFlames(f => f.AddBulletPierce(Mathf.RoundToInt(amount)));
                break;
            case UpgradeType.BulletSpeedPercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletSpeedPercent(pct)); applyFlames(f => f.AddBulletSpeedPercent(pct));
                break;
            case UpgradeType.BulletDamagePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletDamagePercent(pct)); applyFlames(f => f.AddBulletDamagePercent(pct));
                break;
            case UpgradeType.BulletExecutePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletExecutePercent(pct)); applyFlames(f => f.AddBulletExecutePercent(pct));
                break;
            case UpgradeType.BulletKnockbackPercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletKnockbackPercent(pct)); applyFlames(f => f.AddBulletKnockbackPercent(pct));
                break;
            case UpgradeType.BulletSizePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletSizePercent(pct)); applyFlames(f => f.AddBulletSizePercent(pct));
                break;
            case UpgradeType.BulletInfinitePierceOnKill:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletInfinitePierceOnKill(pct)); applyFlames(f => f.AddBulletInfinitePierceOnKill(pct));
                break;
            case UpgradeType.BulletExplosionOnKill:
                ApplyToAllWeapons(allWeapons, w => { w.AddBulletExplosionDamagePercent(pct); w.AddBulletExplosionRadius(1.5f); }); applyFlames(f => { f.AddBulletExplosionDamagePercent(pct); f.AddBulletExplosionRadius(1.5f); });
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
                ApplyToAllWeapons(allWeapons, w => w.AddFreeShotChanceWhileStill(pct)); applyFlames(f => f.AddFreeShotChanceWhileStill(pct));
                break;
            case UpgradeType.AmmoRecoverOnXP:
                PlayerXP.Instance?.AddAmmoRecoverChance(pct);
                break;
            case UpgradeType.FireRateBuffOnXP:
                PlayerXP.Instance?.AddFireRateBuffOnXPChance(pct);
                break;
            case UpgradeType.LastAmmoBurst:
                ApplyToAllWeapons(allWeapons, w => w.AddLastAmmoBurst(pct)); applyFlames(f => f.AddLastAmmoBurst(pct));
                break;
            case UpgradeType.BackShot:
                ApplyToAllWeapons(allWeapons, w => w.AddBackShot(pct)); applyFlames(f => f.AddBackShot(pct));
                break;
            case UpgradeType.DamageBuffAfterReload:
                ApplyToAllWeapons(allWeapons, w => w.AddDamageBuffAfterReload(pct)); applyFlames(f => f.AddDamageBuffAfterReload(pct));
                break;
            case UpgradeType.ReloadSpeedStackOnKill:
                ApplyToAllWeapons(allWeapons, w => w.AddReloadSpeedStackOnKill(pct)); applyFlames(f => f.AddReloadSpeedStackOnKill(pct));
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
            case UpgradeType.BulletSpreadPercent:
            case UpgradeType.BulletSpread:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletSpread(amount)); applyFlames(f => f.AddBulletSpread(amount));
                break;
            case UpgradeType.BulletBounceCount:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletBounceCount(Mathf.RoundToInt(amount))); applyFlames(f => f.AddBulletBounceCount(Mathf.RoundToInt(amount)));
                break;
            case UpgradeType.MysteryCube:
                playerStats?.AddMysteryCube(pct);
                break;
            case UpgradeType.MysteryCubeDmgStack:
                break;
            case UpgradeType.MysteryCubeAsStack:
                break;
            case UpgradeType.ArmorRegenPerSecond:
                playerStats?.AddArmorRegenPerSecond(amount);
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
            case UpgradeType.TC_1:
                playerStats?.AddTC1(amount);
                break;
            case UpgradeType.TC_2A:
                playerStats?.AddTC2A(amount);
                break;
            case UpgradeType.TC_2B:
                playerStats?.AddTC2B(amount);
                break;
            case UpgradeType.TC_3:
                playerStats?.AddTC3(amount);
                break;
            case UpgradeType.GoldGainPercent:
                playerStats?.AddGoldGainPercent(pct);
                break;
        }
    }

    private static void ApplyToAllWeapons(WeaponController[] weapons, System.Action<WeaponController> action)
    {
        if (weapons == null) return;
        foreach (var w in weapons)
            if (w != null) action(w);
    }

    public static void ApplyUpgradeDirect(UpgradeSO upgrade)
    {
        if (upgrade == null || upgrade.StatMods == null || upgrade.StatMods.Count == 0) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerStats ps = player.GetComponent<PlayerStats>();
        WeaponController[] allWeapons = ps?.Weapons;

        foreach (UpgradeSO.StatMod mod in upgrade.StatMods)
            ApplyStatDirect(ps, allWeapons, mod.Stat, mod.Amount);
    }

    private static void ApplyStatDirect(PlayerStats ps, WeaponController[] allWeapons, UpgradeType stat, float amount)
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
                ApplyToAllWeapons(allWeapons, w => w.AddFireRatePercent(pct));
                break;
            case UpgradeType.FireRangePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddFireRangePercent(pct));
                break;
            case UpgradeType.ReloadSpeedPercent:
                ApplyToAllWeapons(allWeapons, w => w.AddReloadSpeedPercent(pct));
                break;
            case UpgradeType.MagazineSizePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddMagazineSizePercent(pct));
                break;
            case UpgradeType.BulletCount:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletCount(Mathf.RoundToInt(amount)));
                break;
            case UpgradeType.BulletPierce:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletPierce(Mathf.RoundToInt(amount)));
                break;
            case UpgradeType.BulletSpeedPercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletSpeedPercent(pct));
                break;
            case UpgradeType.BulletDamagePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletDamagePercent(pct));
                break;
            case UpgradeType.BulletExecutePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletExecutePercent(pct));
                break;
            case UpgradeType.BulletKnockbackPercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletKnockbackPercent(pct));
                break;
            case UpgradeType.BulletSizePercent:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletSizePercent(pct));
                break;
            case UpgradeType.BulletInfinitePierceOnKill:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletInfinitePierceOnKill(pct));
                break;
            case UpgradeType.BulletExplosionOnKill:
                ApplyToAllWeapons(allWeapons, w => { w.AddBulletExplosionDamagePercent(pct); w.AddBulletExplosionRadius(1.5f); });
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
                ApplyToAllWeapons(allWeapons, w => w.AddFreeShotChanceWhileStill(pct));
                break;
            case UpgradeType.AmmoRecoverOnXP:
                PlayerXP.Instance?.AddAmmoRecoverChance(pct);
                break;
            case UpgradeType.FireRateBuffOnXP:
                PlayerXP.Instance?.AddFireRateBuffOnXPChance(pct);
                break;
            case UpgradeType.LastAmmoBurst:
                ApplyToAllWeapons(allWeapons, w => w.AddLastAmmoBurst(pct));
                break;
            case UpgradeType.BackShot:
                ApplyToAllWeapons(allWeapons, w => w.AddBackShot(pct));
                break;
            case UpgradeType.DamageBuffAfterReload:
                ApplyToAllWeapons(allWeapons, w => w.AddDamageBuffAfterReload(pct));
                break;
            case UpgradeType.ReloadSpeedStackOnKill:
                ApplyToAllWeapons(allWeapons, w => w.AddReloadSpeedStackOnKill(pct));
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
            case UpgradeType.BulletSpreadPercent:
            case UpgradeType.BulletSpread:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletSpread(amount));
                break;
            case UpgradeType.BulletBounceCount:
                ApplyToAllWeapons(allWeapons, w => w.AddBulletBounceCount(Mathf.RoundToInt(amount)));
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
            case UpgradeType.TC_1:
                ps?.AddTC1(amount);
                break;
            case UpgradeType.TC_2A:
                ps?.AddTC2A(amount);
                break;
            case UpgradeType.TC_2B:
                ps?.AddTC2B(amount);
                break;
            case UpgradeType.TC_3:
                ps?.AddTC3(amount);
                break;
            case UpgradeType.GoldGainPercent:
                ps?.AddGoldGainPercent(pct);
                break;
        }
    }
}