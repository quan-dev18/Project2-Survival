using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpPanel : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;

    [Header("Upgrade Pool")]
    [SerializeField] private List<UpgradeSO> upgradePool;

    private readonly List<UpgradeSO> choices = new List<UpgradeSO>();
    private readonly HashSet<UpgradeSO> ownedUpgrades = new HashSet<UpgradeSO>();
    private PlayerStats playerStats;
    private WeaponController weapon;

    private void Awake()
    {
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
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.LevelUp)
        {
            RollChoices();
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
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
        }
    }
}