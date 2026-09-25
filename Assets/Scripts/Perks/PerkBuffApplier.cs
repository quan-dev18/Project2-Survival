using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// (Hướng 2) Persist-Singleton: TỰ tạo 1 GameObject DontDestroyOnLoad khi game bắt đầu
/// (RuntimeInitializeOnLoadMethod), nên KHÔNG phụ thuộc panel/tab có bị ẩn hay không.
/// Cứ mỗi 0.25s kiểm tra:
/// - Level Perk (được lưu trong GameData qua UserData) có tăng không -> áp đúng phần tăng
///   lên PlayerStats / WeaponController của player hiện tại.
/// - Player MỚI xuất hiện (đầu run / respawn) -> áp toàn bộ level đã lưu một lần.
/// Không cần tham chiếu PerksManager nên chạy được ở bất kỳ scene nào.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PerkBuffApplier : MonoBehaviour
{
    public static PerkBuffApplier Instance { get; private set; }

    [Tooltip("(Nên) kéo cùng List PerkData với PerksManager. Để trống hệ thống vẫn tự tìm manager trong scene để copy.")]
    [SerializeField] private List<PerkDataSO> perkPool;

    private readonly Dictionary<string, int> _levelByID = new Dictionary<string, int>();
    private readonly Dictionary<UpgradeType, float> _appliedStats = new Dictionary<UpgradeType, float>();

    private PlayerStats _playerStats;
    private GameObject _lastPlayer;
    private float _timer;

    private bool _poolResolved;

    private const float CheckInterval = 0.25f;

    private static bool _pendingAutoSpawn;
    private bool _isAutoSpawned;

    /// <summary>Nếu chưa ai gắn tay trong scene, tự tạo 1 instance bền vững khi game start.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<PerkBuffApplier>() != null) return;
        _pendingAutoSpawn = true;
        GameObject go = new GameObject("[PerkBuffApplier]");
        go.AddComponent<PerkBuffApplier>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        _isAutoSpawned = _pendingAutoSpawn;
        _pendingAutoSpawn = false;

        if (Instance != null && Instance != this)
        {
            if (Instance._isAutoSpawned)
            {
                // Instance gắn tay THẮNG cái tự sinh (để giữ pool đã kéo trong Inspector).
                Destroy(Instance.gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = CheckInterval;
        CheckAndApply();
    }

    private void CheckAndApply()
    {
        // Nếu chưa có pool (chưa kịp kéo tay), copy từ PerksManager bất kỳ đang có trong scene.
        if (!_poolResolved)
        {
            ResolvePool();
            if (perkPool == null || perkPool.Count == 0) return;
        }

        LoadLevels();

        PlayerStats ps = ResolvePlayer();
        if (ps == null)
        {
            _playerStats = null;
            _lastPlayer = null;
            return;
        }

        if (_lastPlayer != ps.gameObject)
        {
            // Player mới (đầu run / respawn) -> reset và áp toàn bộ.
            _lastPlayer = ps.gameObject;
            _appliedStats.Clear();
            ApplyDelta(ps, true);
        }
        else
        {
            // Player cũ -> chỉ áp phần level vừa tăng.
            ApplyDelta(ps, false);
        }
    }

    /// <summary>
    /// So sánh tổng buff đáng lẽ phải có (level * amountPerLevel) với phần đã áp cho player này,
    /// rồi áp đúng phần chênh lệch. Level không bao giờ giảm nên delta luôn dương.
    /// </summary>
    private void ApplyDelta(PlayerStats ps, bool reset)
    {
        if (reset) _appliedStats.Clear();
        WeaponController[] weapons = ps.Weapons;

        foreach (PerkDataSO perk in perkPool)
        {
            if (perk == null || perk.StatMods == null) continue;

            int level = _levelByID.TryGetValue(perk.PerkID, out int l) ? l : 0;
            if (level <= 0) continue;

            foreach (PerkDataSO.PerkStatMod mod in perk.StatMods)
            {
                if (mod == null) continue;

                float total = mod.AmountPerLevel * level;
                _appliedStats.TryGetValue(mod.Stat, out float applied);
                float delta = total - applied;
                if (Mathf.Abs(delta) < 0.0001f) continue;

                Apply(ps, weapons, mod.Stat, delta);
                _appliedStats[mod.Stat] = total;
            }
        }
    }

    private void ResolvePool()
    {
        PerksManager manager = FindObjectOfType<PerksManager>();
        if (manager != null && manager.PerkPool != null && manager.PerkPool.Count > 0)
        {
            perkPool = new List<PerkDataSO>(manager.PerkPool);
        }
        _poolResolved = perkPool != null && perkPool.Count > 0;
    }

    /// <summary>Đọc level từ GameData (UserData) cho từng Perk trong pool. Pool nhỏ nên rebuild mỗi tick là rẻ.</summary>
    private void LoadLevels()
    {
        if (UserData.Instance == null) return;
        _levelByID.Clear();
        foreach (PerkDataSO perk in perkPool)
        {
            if (perk == null) continue;
            int level = UserData.Instance.GetPerkLevel(perk.PerkID);
            if (level > 0) _levelByID[perk.PerkID] = level;
        }
    }

    private PlayerStats ResolvePlayer()
    {
        if (_playerStats != null) return _playerStats;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return null;
        _playerStats = player.GetComponent<PlayerStats>();
        return _playerStats;
    }

    /// <summary>Áp buff 1 chỉ số. Quy đổi percent giống hệt hệ thống Upgrade cũ (xem LevelUpPanel).</summary>
    private void Apply(PlayerStats ps, WeaponController[] weapons, UpgradeType stat, float amount)
    {
        float pct = amount / 100f;

        switch (stat)
        {
            case UpgradeType.MaxHealthPercent: ps.AddMaxHealthPercent(pct); break;
            case UpgradeType.MaxArmorPercent: ps.AddMaxArmorPercent(pct); break;
            case UpgradeType.RecoveryRatePercent: ps.AddRecoveryRatePercent(pct); break;
            case UpgradeType.MoveSpeedPercent: ps.AddMoveSpeedPercent(pct); break;
            case UpgradeType.CollectRangePercent: ps.AddCollectRangePercent(pct); break;
            case UpgradeType.GrowthRatePercent: ps.AddGrowthRatePercent(pct); break;
            case UpgradeType.FireRatePercent: ApplyToAllWeapons(weapons, w => w.AddFireRatePercent(pct)); break;
            case UpgradeType.FireRangePercent: ApplyToAllWeapons(weapons, w => w.AddFireRangePercent(pct)); break;
            case UpgradeType.ReloadSpeedPercent: ApplyToAllWeapons(weapons, w => w.AddReloadSpeedPercent(pct)); break;
            case UpgradeType.MagazineSizePercent: ApplyToAllWeapons(weapons, w => w.AddMagazineSizePercent(pct)); break;
            case UpgradeType.BulletCount: ApplyToAllWeapons(weapons, w => w.AddBulletCount(Mathf.RoundToInt(amount))); break;
            case UpgradeType.BulletPierce: ApplyToAllWeapons(weapons, w => w.AddBulletPierce(Mathf.RoundToInt(amount))); break;
            case UpgradeType.BulletSpeedPercent: ApplyToAllWeapons(weapons, w => w.AddBulletSpeedPercent(pct)); break;
            case UpgradeType.BulletDamagePercent: ApplyToAllWeapons(weapons, w => w.AddBulletDamagePercent(pct)); break;
            case UpgradeType.BulletExecutePercent: ApplyToAllWeapons(weapons, w => w.AddBulletExecutePercent(pct)); break;
            case UpgradeType.BulletKnockbackPercent: ApplyToAllWeapons(weapons, w => w.AddBulletKnockbackPercent(pct)); break;
            case UpgradeType.BulletSizePercent: ApplyToAllWeapons(weapons, w => w.AddBulletSizePercent(pct)); break;
            case UpgradeType.BulletInfinitePierceOnKill: ApplyToAllWeapons(weapons, w => w.AddBulletInfinitePierceOnKill(pct)); break;
            case UpgradeType.BulletExplosionOnKill:
                ApplyToAllWeapons(weapons, w => { w.AddBulletExplosionDamagePercent(pct); w.AddBulletExplosionRadius(1.5f); });
                break;
            case UpgradeType.CharacterSizePercent: ps.AddCharacterSizePercent(pct); break;
            case UpgradeType.DamageTakenFireRatePercent: ps.AddDamageTakenFireRatePercent(pct); break;
            case UpgradeType.DamageTakenBulletDamagePercent: ps.AddDamageTakenBulletDamagePercent(pct); break;
            case UpgradeType.FreeShotChanceWhileStill: ApplyToAllWeapons(weapons, w => w.AddFreeShotChanceWhileStill(pct)); break;
            case UpgradeType.AmmoRecoverOnXP: PlayerXP.Instance?.AddAmmoRecoverChance(pct); break;
            case UpgradeType.FireRateBuffOnXP: PlayerXP.Instance?.AddFireRateBuffOnXPChance(pct); break;
            case UpgradeType.LastAmmoBurst: ApplyToAllWeapons(weapons, w => w.AddLastAmmoBurst(pct)); break;
            case UpgradeType.BackShot: ApplyToAllWeapons(weapons, w => w.AddBackShot(pct)); break;
            case UpgradeType.DamageBuffAfterReload: ApplyToAllWeapons(weapons, w => w.AddDamageBuffAfterReload(pct)); break;
            case UpgradeType.ReloadSpeedStackOnKill: ApplyToAllWeapons(weapons, w => w.AddReloadSpeedStackOnKill(pct)); break;
            case UpgradeType.InvulnerableWhileReloading: ps.AddInvulnerableWhileReloading(pct); break;
            case UpgradeType.BurnAura: ps.AddBurnAuraChance(pct); break;
            case UpgradeType.StackingBuffOnTime: ps.AddStackingBuffPercent(pct); break;
            case UpgradeType.BulletSpreadPercent:
            case UpgradeType.BulletSpread:
                ApplyToAllWeapons(weapons, w => w.AddBulletSpread(amount)); break;
            case UpgradeType.BulletBounceCount:
                ApplyToAllWeapons(weapons, w => w.AddBulletBounceCount(Mathf.RoundToInt(amount))); break;
            case UpgradeType.MysteryCube: ps.AddMysteryCube(pct); break;
            case UpgradeType.MysteryCubeDmgStack: break;
            case UpgradeType.MysteryCubeAsStack: break;
            case UpgradeType.ArmorRegenPerSecond: ps.AddArmorRegenPerSecond(amount); break;
            case UpgradeType.SpiritSummon: ps.AddSpiritSummon(pct); break;
            case UpgradeType.SpiritHeal: ps.AddSpiritHeal(pct); break;
            case UpgradeType.SpiritBurn: ps.AddSpiritBurn(pct); break;
            case UpgradeType.SpiritEmpowered: ps.AddSpiritEmpowered(pct); break;
            case UpgradeType.GoldGainPercent: ps.AddGoldGainPercent(pct); break;
            case UpgradeType.HealMaxHealthPercentPerSecond: ps.AddHealMaxHealthPercent(pct); break;
            case UpgradeType.Revive: ps.AddRevive(amount); break;
            case UpgradeType.VisionRangePercent: CameraController.ApplyVisionBonus(pct); break;
            case UpgradeType.DoubleShieldArmor: ps.AddDoubleShieldArmor(pct); break;
            case UpgradeType.ThornsDamage: ps.AddThornsDamage(amount); break;
        }
    }

    private void ApplyToAllWeapons(WeaponController[] weapons, System.Action<WeaponController> action)
    {
        if (weapons == null) return;
        foreach (WeaponController w in weapons)
            if (w != null) action(w);
    }
}