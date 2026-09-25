using System.Collections.Generic;
using UnityEngine;

public class SynergyManager : MonoBehaviour
{
    public static SynergyManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    private HashSet<string> ownedMaxLevelUpgrades = new HashSet<string>();

    // Synergy active states
    private bool spreadshooterActive;
    private bool gunMasteryActive;
    private bool summonMasteryActive;
    private bool gottaGoFastActive;
    private bool fatActive;

    // Gotta Go Fast stacking
    private float gottaGoFastTimer;
    private int gottaGoFastStacks;
    private const int GOTTA_GO_FAST_MAX_STACKS = 3;
    private const float GOTTA_GO_FAST_STACK_INTERVAL = 5f;

    private WeaponController[] Weapons => playerStats?.Weapons;

    private void Awake()
    {
        Instance = this;
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
    }

    private void ApplyToAllWeapons(System.Action<WeaponController> action)
    {
        var weaponsList = Weapons;
        if (weaponsList != null)
            foreach (var w in weaponsList)
                if (w != null) action(w);
    }

    public void RegisterMaxLevelUpgrade(string upgradeName)
    {
        if (!ownedMaxLevelUpgrades.Contains(upgradeName))
        {
            ownedMaxLevelUpgrades.Add(upgradeName);
            CheckSynergies();
        }
    }

    public void UnregisterMaxLevelUpgrade(string upgradeName)
    {
        if (ownedMaxLevelUpgrades.Contains(upgradeName))
        {
            ownedMaxLevelUpgrades.Remove(upgradeName);
            CheckSynergies();
        }
    }

    private void CheckSynergies()
    {
        // Spreadshooter: Multi Shot III + Fast Hands III
        bool wantSpreadshooter = Has("Multi Shot III") && Has("Fast Hands III");
        if (wantSpreadshooter != spreadshooterActive) SetSpreadshooter(wantSpreadshooter);

        // Gun Mastery: Heavy Hitter III + Special Mags III + Quick Shot III
        bool wantGunMastery = Has("Heavy Hitter III") && Has("Special Mags III") && Has("Quick Shot III");
        if (wantGunMastery != gunMasteryActive) SetGunMastery(wantGunMastery);

        // Summon Mastery: Guardian Summon III + Holy Spirit III
        bool wantSummonMastery = Has("Guardian Summon III") && Has("Holy Spirit III");
        if (wantSummonMastery != summonMasteryActive) SetSummonMastery(wantSummonMastery);

        // Gotta Go Fast: Haste III + Speedy Bullets III
        bool wantGottaGoFast = Has("Haste III") && Has("Speedy Bullets III");
        if (wantGottaGoFast != gottaGoFastActive) SetGottaGoFast(wantGottaGoFast);

        // Fat: Vitality Boost III + Magnetic III
        bool wantFat = Has("Vitality Boost III") && Has("Magnetic III");
        if (wantFat != fatActive) SetFat(wantFat);
    }

    private bool Has(string name) => ownedMaxLevelUpgrades.Contains(name);

// ----- Spreadshooter: Multi Shot III + Fast Hands III -----
    // Reload speed +69%, Bullet dmg +50%, Max ammo -50%
    private void SetSpreadshooter(bool active)
    {
        if (spreadshooterActive == active) return;
        spreadshooterActive = active;
        if (active)
            FirebaseAnalyticsHelper.LogSynergyActivated("Spreadshooter", "Multi Shot III, Fast Hands III");
        ApplyToAllWeapons(w =>
        {
            w.AddReloadSpeedPercent(active ? 0.69f : -0.69f);
            w.AddBulletDamagePercent(active ? 0.50f : -0.50f);
            w.AddMagazineSizePercent(active ? -0.50f : 0.50f);
        });
    }

    // ----- Gun Mastery: Heavy Hitter III + Special Mags III + Quick Shot III -----
    // Bullet dmg +30%, Fire rate +15%, Reload rate +15%, Max ammo +1, Piercing +1
    private void SetGunMastery(bool active)
    {
        if (gunMasteryActive == active) return;
        gunMasteryActive = active;
        if (active)
            FirebaseAnalyticsHelper.LogSynergyActivated("Gun Mastery", "Heavy Hitter III, Special Mags III, Quick Shot III");
        ApplyToAllWeapons(w =>
        {
            w.AddBulletDamagePercent(active ? 0.30f : -0.30f);
            w.AddFireRatePercent(active ? 0.15f : -0.15f);
            w.AddReloadSpeedPercent(active ? 0.15f : -0.15f);
            w.AddBulletCount(active ? 1 : -1);
            w.AddBulletPierce(active ? 1 : -1);
        });
    }

    // ----- Summon Mastery: Guardian Summon_3 + Holy Spirit_3 -----
    // Summon dmg +35%, Summon Aspd +35%, Bullet dmg -35%
    private void SetSummonMastery(bool active)
    {
        if (summonMasteryActive == active) return;
        summonMasteryActive = active;
        if (active)
            FirebaseAnalyticsHelper.LogSynergyActivated("Summon Mastery", "Guardian Summon III, Holy Spirit III");
        ApplyToAllWeapons(w => w.AddBulletDamagePercent(active ? -0.35f : 0.35f));
        
        Spirit[] spirits = FindObjectsByType<Spirit>(FindObjectsSortMode.None);
        foreach (var s in spirits)
            s.ApplySynergyMultipliers(active ? 1.35f : 1f, active ? 1.35f : 1f);
        
        MysteryCube[] cubes = FindObjectsByType<MysteryCube>(FindObjectsSortMode.None);
        foreach (var c in cubes)
            c.ApplySynergyMultipliers(active ? 1.35f : 1f, active ? 1.35f : 1f);
    }

    // ----- Gotta Go Fast: Haste_3 + Speedy Bullets_3 -----
    // Fire rate +10% and piercing +1 every 5s, up to 30% and +3, reset on hit
    private void SetGottaGoFast(bool active)
    {
        if (gottaGoFastActive == active) return;
        gottaGoFastActive = active;
        if (active)
            FirebaseAnalyticsHelper.LogSynergyActivated("Gotta Go Fast", "Haste III, Speedy Bullets III");
        if (!active)
        {
            ApplyToAllWeapons(w =>
            {
                w.AddFireRatePercent(-gottaGoFastStacks * 0.10f);
                w.AddBulletPierce(-gottaGoFastStacks);
            });
            gottaGoFastStacks = 0;
            gottaGoFastTimer = 0f;
        }
    }

    // ----- Fat: Vitality Boost_3 + Magnetic_3 -----
    // Max HP +100, Character size +25%, Pickup range +25%
    private void SetFat(bool active)
    {
        if (fatActive == active) return;
        fatActive = active;
        if (active)
            FirebaseAnalyticsHelper.LogSynergyActivated("Fat", "Vitality Boost III, Magnetic III");
        if (playerStats != null)
        {
            playerStats.AddMaxHealthFlat(active ? 100f : -100f);
            playerStats.AddCharacterSizePercent(active ? 0.25f : -0.25f);
            playerStats.AddCollectRangePercent(active ? 0.25f : -0.25f);
        }
    }

    private void Update()
    {
        // Gotta Go Fast stacking
        if (gottaGoFastActive)
        {
            gottaGoFastTimer += Time.deltaTime;
            if (gottaGoFastTimer >= 5f && gottaGoFastStacks < 3)
            {
                gottaGoFastTimer = 0f;
                gottaGoFastStacks++;
                ApplyToAllWeapons(w =>
                {
                    w.AddFireRatePercent(0.10f); // +10% per stack
                    w.AddBulletPierce(1); // +1 pierce per stack
                });
            }
        }
    }

    public void OnPlayerHit()
    {
        if (gottaGoFastActive && gottaGoFastStacks > 0)
        {
            ApplyToAllWeapons(w =>
            {
                w.AddFireRatePercent(-gottaGoFastStacks * 0.10f);
                w.AddBulletPierce(-gottaGoFastStacks);
            });
            gottaGoFastStacks = 0;
            gottaGoFastTimer = 0f;
        }
    }
}