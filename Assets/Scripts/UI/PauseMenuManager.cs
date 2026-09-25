using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự sinh các dòng chỉ số của nhân vật (lấy từ PlayerStats)
/// vào ScrollView khi mở Pause Menu. Xóa hết dòng cũ trước khi tạo mới.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform content;          // Content của ScrollView (gán tay được)
    [SerializeField] private GameObject statsPanel;      // Panel chứa ScrollView — nếu không gán Content tay thì tự lấy từ đây
    [SerializeField] private Transform upgradeContent;   // Content của ScrollView upgrade (gán tay được)
    [SerializeField] private GameObject upgradePanel;    // Panel chứa ScrollView upgrade — nếu không gán Content tay thì tự lấy từ đây
    [SerializeField] private StatUIItem statItemPrefab;  // Prefab dòng chỉ số
    [SerializeField] private UpgradeUIItem upgradeItemPrefab; // Prefab dòng upgrade (Icon + Name + Desc)
    [Header("Tier upgrade")]
    [Tooltip("Scale của tier THẤP (vd: Haste II, Haste I). Tier cao nhất luôn = 1. Ví dụ 0.82 = nhỏ 18%.")]
    [SerializeField] private float lowTierScale = 0.82f;
    [SerializeField] private TMP_Text warningText;       // (tùy chọn) hiện cảnh báo nếu thiếu dữ liệu
    [Tooltip("Bật nếu muốn manager tự bật/tắt statsPanel khi pause/resume. Nếu panel nằm trong PausePanel thì để OFF")]
    [SerializeField] private bool toggleStatsPanel = false;

    [Header("Màu tiêu đề")]
    [SerializeField] private Color headerColor = new Color(1f, 0.85f, 0.2f); // Vàng — tiêu đề nhân vật
    [SerializeField] private Color weaponColor = new Color(0.4f, 0.9f, 1f);    // Xanh dương — tiêu đề vũ khí

    [Header("Player")]
    [SerializeField] private PlayerStats playerStats;    // Component PlayerStats trên nhân vật (để trống sẽ tự tìm)

    [Header("Danh sách chỉ số")]
    [Tooltip("Thêm/bớt hoặc sắp xếp lại các mục muốn hiện. ActiveWeapon = vũ khí đang dùng, Upgrades = các nâng cấp đang có.")]
    [SerializeField] private List<StatEntry> statEntries = new List<StatEntry>
    {
        new StatEntry { type = StatType.Health },
        new StatEntry { type = StatType.Armor },
        new StatEntry { type = StatType.Recovery },
        new StatEntry { type = StatType.MoveSpeed },
        new StatEntry { type = StatType.CollectRange },
        new StatEntry { type = StatType.GrowthRate },
        new StatEntry { type = StatType.GoldGain },
        new StatEntry { type = StatType.ActiveWeapon },
        new StatEntry { type = StatType.Upgrades },
    };

    // Loại chỉ số có thể chọn trong List
    public enum StatType
    {
        Health,       // Máu hiện tại / tối đa
        Armor,        // Giáp hiện tại / tối đa
        Recovery,     // Hồi máu / giây
        MoveSpeed,    // Tốc độ di chuyển
        CollectRange, // Bán kính nhặt
        GrowthRate,   // Tốc độ EXP
        GoldGain,     // Vàng nhận thêm (%)
        ActiveWeapon, // Chỉ số của vũ khí đang sử dụng
        Upgrades,     // Các upgrade người chơi đang có (từ LevelUpPanel)
    }

    [System.Serializable]
    public class StatEntry
    {
        public StatType type;
        [Tooltip("Tên hiển thị; để trống thì dùng tên mặc định")]
        public string customName;
    }

    private readonly StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
        GameManager.OnStateChanged += OnGameStateChanged;
        ResolveContent();
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    // Lấy Content từ statsPanel nếu chưa gán tay. Gọi lại được nếu gán sau ở Inspector.
    private void ResolveContent()
    {
        if (content != null) return;

        if (statsPanel != null)
        {
            // statsPanel là ScrollRect hoặc có con là ScrollRect -> lấy .content
            ScrollRect sr = statsPanel.GetComponent<ScrollRect>();
            if (sr == null) sr = statsPanel.GetComponentInChildren<ScrollRect>(true);
            if (sr != null && sr.content != null)
                content = sr.content;
            else
                ShowWarning("PauseMenuManager: statsPanel không chứa ScrollRect nào. Gán tay ô Content.");
        }
        else
        {
            // Chưa gán gì -> tìm ScrollView trong chính object này
            ScrollRect sr = GetComponentInChildren<ScrollRect>(true);
            if (sr != null && sr.content != null) content = sr.content;
        }

        // Content dành riêng cho danh sách upgrade (giống cách lấy statsPanel)
        if (upgradeContent == null && upgradePanel != null)
        {
            ScrollRect sr = upgradePanel.GetComponent<ScrollRect>();
            if (sr == null) sr = upgradePanel.GetComponentInChildren<ScrollRect>(true);
            if (sr != null && sr.content != null)
                upgradeContent = sr.content;
            else
                ShowWarning("PauseMenuManager: upgradePanel không chứa ScrollRect nào. Gán tay ô Upgrade Content.");
        }
    }

    // Mở Pause Menu -> sinh lại toàn bộ danh sách chỉ số
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Paused)
        {
            ResolveContent();
            if (toggleStatsPanel && statsPanel != null && !statsPanel.activeInHierarchy)
                statsPanel.SetActive(true);
            if (toggleStatsPanel && upgradePanel != null && !upgradePanel.activeInHierarchy)
                upgradePanel.SetActive(true);
            RefreshStats();
        }
        else if (toggleStatsPanel && (statsPanel != null && statsPanel.activeInHierarchy || upgradePanel != null && upgradePanel.activeInHierarchy))
        {
            // Tự tắt panel khi resume (chỉ khi bật toggleStatsPanel)
            if (statsPanel != null && statsPanel.activeInHierarchy)
                statsPanel.SetActive(false);
            if (upgradePanel != null && upgradePanel.activeInHierarchy)
                upgradePanel.SetActive(false);
        }
    }

    public void RefreshStats()
    {
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();

        // Thiếu cấu hình -> báo lỗi 1 lần, tránh spam
        if (content == null || statItemPrefab == null)
        {
            ShowWarning("PauseMenuManager: thiếu Content hoặc StatUIItemPrefab!");
            return;
        }
        if (playerStats == null)
        {
            ShowWarning("PauseMenuManager: không tìm thấy PlayerStats!");
            ClearAllRows();
            return;
        }

        ClearAllRows();

        // ==== Sinh theo List Stat (người dùng thêm/bớt ở Inspector) ====
        bool playerHeaderShown = false;
        bool weaponHeaderShown = false;

        foreach (StatEntry entry in statEntries)
        {
            if (entry == null) continue;

            // Phần vũ khí: tách riêng với phần nhân vật
            if (entry.type == StatType.ActiveWeapon)
            {
                if (!weaponHeaderShown)
                {
                    AddSectionHeader("— VŨ KHÍ ĐANG DÙNG —", weaponColor);
                    weaponHeaderShown = true;
                }
                AddActiveWeaponStats();
                continue;
            }

            // Phần upgrade đang có
            if (entry.type == StatType.Upgrades)
            {
                AddOwnedUpgrades();
                continue;
            }

            // Tiêu đề phần nhân vật (chỉ hiện khi có ít nhất 1 stat player)
            if (!playerHeaderShown)
            {
                AddSectionHeader("— CHỈ SỐ NHÂN VẬT —", headerColor);
                playerHeaderShown = true;
            }

            switch (entry.type)
            {
                case StatType.Health:
                    AddStat(GetName(entry, "Máu"),
                        FormatNumber(playerStats.CurrentHealth) + " / " + FormatNumber(playerStats.MaxHealth));
                    break;
                case StatType.Armor:
                    AddStat(GetName(entry, "Giáp"),
                        FormatNumber(playerStats.CurrentArmor) + " / " + FormatNumber(playerStats.MaxArmor));
                    break;
                case StatType.Recovery:
                    AddStat(GetName(entry, "Hồi máu"), FormatNumber(playerStats.RecoveryRate) + "/s");
                    break;
                case StatType.MoveSpeed:
                    AddStat(GetName(entry, "Tốc độ di chuyển"), FormatNumber(playerStats.MoveSpeed));
                    break;
                case StatType.CollectRange:
                    AddStat(GetName(entry, "Bán kính nhặt"), FormatNumber(playerStats.CollectRange));
                    break;
                case StatType.GrowthRate:
                    AddStat(GetName(entry, "Tốc độ EXP"), FormatNumber(playerStats.GrowthRate) + "x");
                    break;
                case StatType.GoldGain:
                    AddStat(GetName(entry, "Vàng nhận thêm"), FormatPercent(playerStats.bonusGoldGainPercent, true));
                    break;
            }
        }

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    // Tên hiển thị: dùng customName nếu được ghi, không thì tên mặc định
    private string GetName(StatEntry entry, string defaultName)
        => string.IsNullOrEmpty(entry.customName) ? defaultName : entry.customName;

    // Chỉ số chi tiết của vũ khí đang sử dụng (được phân chia rõ riêng phần)
    private void AddActiveWeaponStats()
    {
        // Nếu đang dùng súng lửa (slot đang active) → hiện nó như vũ khí chính
        if (playerStats != null)
        {
            foreach (FlamethrowerController f in playerStats.Flamethrowers)
            {
                if (f == null || !f.gameObject.activeInHierarchy) continue;

                string flameName = f.WeaponStats != null ? f.WeaponStats.WeaponName : f.gameObject.name;
                AddSectionHeader(flameName, weaponColor);

                AddStat("Sát thương", FormatNumber(f.EffectiveDamagePerTick));
                AddStat("Tốc độ bắn", FormatNumber(1f / Mathf.Max(0.05f, f.tickInterval)) + "/s");
                AddStat("Đạn", f.CurrentAmmo + " / " + f.MagazineSize);
                AddStat("Thời gian nạp đạn", FormatNumber(f.reloadTime) + "s");
                AddStat("Tầm bắn", FormatNumber(f.fireRange));
                return;
            }
        }

        WeaponController w = playerStats != null ? playerStats.ActiveWeapon : null;
        if (w == null) return;

        string weaponName = w.WeaponStats != null ? w.WeaponStats.WeaponName : w.gameObject.name;
        AddSectionHeader(weaponName, weaponColor);

        float damage = w.WeaponStats != null && w.WeaponStats.BulletSO != null
            ? w.WeaponStats.BulletSO.Damage * (1f + w.bonusBulletDamagePercent)
            : 0f;

        AddStat("Sát thương", FormatNumber(damage));
        AddStat("Tốc độ bắn", FormatNumber(1f / Mathf.Max(0.1f, w.fireRate)) + "/s");
        AddStat("Đạn", w.CurrentAmmo + " / " + w.MagazineSize);
        AddStat("Thời gian nạp đạn", FormatNumber(w.reloadTime) + "s");
        AddStat("Tầm bắn", FormatNumber(w.fireRange));
        AddStat("Xuyên giáp", "+" + w.bonusBulletPierce.ToString());
    }

    // Danh sách upgrade người chơi đang có (từ LevelUpPanel.OwnedUpgrades)
    /// Hiển thị: tier cao nhất giữ nguyên size (scale 1), các tier thấp hơn thu nhỏ lại
    /// và xếp ở bên dưới (không nhánh, không indent, không bấm mở).
    private void AddOwnedUpgrades()
    {
        if (upgradeContent == null)
        {
            ShowWarning("PauseMenuManager: muốn hiện Upgrades nhưng chưa gán Upgrade Content (hoặc Upgrade Panel)!");
            return;
        }
        if (upgradeItemPrefab == null)
        {
            ShowWarning("PauseMenuManager: muốn hiện Upgrades nhưng chưa gán upgradeItemPrefab!");
            return;
        }

        LevelUpPanel panel = LevelUpPanel.Instance;
        if (panel == null) return;

        HashSet<UpgradeSO> owned = new HashSet<UpgradeSO>();
        foreach (UpgradeSO up in panel.OwnedUpgrades)
        {
            if (up != null) owned.Add(up);
        }
        if (owned.Count == 0) return;

        // Thông tin theo "gia đình" (family): các upgrade liên quan tới nhau xếp liền kề.
        // Tier cao nhất: scale 1. Mọi tier còn lại: thu nhỏ chung 1 mức (lowTierScale).
        // VD: Haste III - II - I đứng liền nhau, III to nhất rồi tới II, I.
        List<UpgradeSO> topTiers = new List<UpgradeSO>();
        foreach (UpgradeSO up in owned)
        {
            bool isRequiredByOther = false;
            foreach (UpgradeSO other in owned)
            {
                if (other == up || other.Requires == null) continue;
                foreach (UpgradeSO req in other.Requires)
                {
                    if (req == up) { isRequiredByOther = true; break; }
                }
                if (isRequiredByOther) break;
            }
            if (!isRequiredByOther) topTiers.Add(up);
        }

        HashSet<UpgradeSO> placed = new HashSet<UpgradeSO>();
        foreach (UpgradeSO top in topTiers)
            AppendFamily(top, owned, placed);

        // Fallback: upgrade "mồ côi" (không thuộc gia đình nào) thì xếp cuối, coi như tier thấp.
        foreach (UpgradeSO up in owned)
        {
            if (placed.Add(up))
                CreateUpgradeRow(up, true);
        }
    }

    /// <summary>
    /// Xếp 1 gia đình upgrade liền kề: tier cao nhất (to) đứng trước,
    /// các tier thấp hơn (nằm trong Requires của nó, đệ quy) xếp ngay sau, đều thu nhỏ.
    /// </summary>
    private void AppendFamily(UpgradeSO top, HashSet<UpgradeSO> owned, HashSet<UpgradeSO> placed)
    {
        if (top == null || !placed.Add(top)) return;

        CreateUpgradeRow(top, false); // tier cao nhất: scale 1

        if (top.Requires == null) return;
        foreach (UpgradeSO req in top.Requires)
        {
            if (req == null || !owned.Contains(req)) continue;
            if (!placed.Add(req)) continue;

            CreateUpgradeRow(req, true); // tier thấp: thu nhỏ
            AppendLowerTiers(req, owned, placed);
        }
    }

    /// <summary>Xếp tiếp các tier thấp hơn nữa (vd: II -> I), tất cả cùng 1 mức thu nhỏ.</summary>
    private void AppendLowerTiers(UpgradeSO up, HashSet<UpgradeSO> owned, HashSet<UpgradeSO> placed)
    {
        if (up.Requires == null) return;
        foreach (UpgradeSO req in up.Requires)
        {
            if (req == null || !owned.Contains(req)) continue;
            if (!placed.Add(req)) continue;

            CreateUpgradeRow(req, true);
            AppendLowerTiers(req, owned, placed);
        }
    }

    private void CreateUpgradeRow(UpgradeSO upgrade, bool isLowTier)
    {
        UpgradeUIItem item = Instantiate(upgradeItemPrefab, upgradeContent);
        item.SetData(upgrade);
        item.Rect.localScale = Vector3.one * (isLowTier ? lowTierScale : 1f);
    }

    // Xóa toàn bộ dòng cũ (chạy ngược để Destroy từng cái dưới Content)
    private void ClearAllRows()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
        if (upgradeContent != null)
        {
            for (int i = upgradeContent.childCount - 1; i >= 0; i--)
            {
                Transform child = upgradeContent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }

    // Tạo 1 dòng chỉ số "Tên - Giá trị"
    private void AddStat(string name, string value)
    {
        StatUIItem item = CreateItem();
        if (item != null)
            item.SetData(name, value);
    }

    // Tạo 1 dòng tiêu đề (tên + màu riêng để tách phần)
    private void AddSectionHeader(string name, Color color)
    {
        StatUIItem item = CreateItem();
        if (item != null)
            item.SetTitle(name, color);
    }

    private StatUIItem CreateItem()
    {
        if (statItemPrefab == null || content == null) return null;
        return Instantiate(statItemPrefab, content);
    }

    // Số nguyên hiện không dấu phẩy, số thập phân hiện 1 chữ số
    private string FormatNumber(float value)
    {
        sb.Clear();
        if (Mathf.Approximately(value, Mathf.Round(value)))
            sb.Append(value.ToString("0"));
        else
            sb.Append(value.ToString("0.0"));
        return sb.ToString();
    }

    // percent dạng 0-1 (vd 0.05 -> +5%); bật multiplyBy100 để chuyển sang dạng % (vd 0.5 -> +50%)
    private string FormatPercent(float percent, bool multiplyBy100 = false)
    {
        float value = multiplyBy100 ? percent * 100f : percent;
        return (value >= 0f ? "+" : "") + FormatNumber(value) + "%";
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning(message, this);
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
        }
    }
}