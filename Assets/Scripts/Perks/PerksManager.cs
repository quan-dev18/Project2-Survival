using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller quản lý Bảng Perks: dựng danh sách Slot, xử lý chọn/nâng cấp,
/// kiểm tra tiền, cập nhật UI Nút Nâng cấp và phát Event khi Perk được nâng.
/// Tách biệt hoàn toàn: Data (PerkDataSO) / View (PerkSlotUI) / Controller (class này).
/// </summary>
public class PerksManager : MonoBehaviour
{
    /// <summary>Event phát ra sau khi Perk được nâng cấp thành công. Để PlayerStats/Synergy lắng nghe.</summary>
    public event Action<PerkDataSO, int> OnPerkUpgraded;

    [Header("Data")]
    [Tooltip("Danh sách toàn bộ Perk có trong game (dùng để dựng các Slot).")]
    [SerializeField] private List<PerkDataSO> perkPool;

    [Header("Scroll View & Slot")]
    [SerializeField] private ScrollRect perkScrollRect;      // ScrollRect chứa danh sách Perk
    [SerializeField] private RectTransform content;          // Content của ScrollRect
    [SerializeField] private PerkSlotUI slotPrefab;          // Prefab Slot (có gắn PerkSlotUI)

    [Header("Upgrade Button")]
    [Tooltip("Nút Nâng cấp. Nút này cần bấm (click) mới kích hoạt, và chứa TMP_Text làm label.")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;   // Hiển thị giá tiền / "MAX"
    [SerializeField] private string upgradeButtonFormat = "Nâng cấp: {0}"; // {0} là giá tiền

    [Header("Currency & Colors")]
    [Tooltip("Màu chữ giá tiền bình thường (đủ tiền).")]
    [SerializeField] private Color affordableColor = Color.white;
    [Tooltip("Màu chữ giá tiền khi không đủ tiền.")]
    [SerializeField] private Color notAffordableColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Description Panel (panel mô tả nâng cấp)")]
    [Tooltip("Panel mô tả Perk khi chọn 1 Slot. Hiện khi có Slot được chọn, ẩn khi hủy chọn.")]
    [SerializeField] private GameObject perkDescPanel;
    [Tooltip("Text DUY NHẤT gộp cả tên Perk + mô tả + khoảng buff (name/des/buff trong 1 Text).")]
    [SerializeField] private TextMeshProUGUI perkDescText;

    private readonly List<PerkSlotUI> _slotInstances = new List<PerkSlotUI>();
    private readonly Dictionary<PerkDataSO, int> _perkLevels = new Dictionary<PerkDataSO, int>();

    private PerkSlotUI _selectedSlot;
    private int _currentGold;
    private bool _initialized;
    private bool _showBuffPreview = true; // true = hiện range "cur → next"; false = sau khi đã mua, chỉ hiện buff hiện tại.

    public PerkSlotUI SelectedSlot => _selectedSlot;
    public IReadOnlyList<PerkDataSO> PerkPool => perkPool;

    /// <summary>Nạp level đã lưu (trong gamedata.json qua UserData) khi khởi tạo, chạy cả khi panel đang ẩn.</summary>
    private void Awake()
    {
        LoadPerkLevels();
    }

    /// <summary>
    /// TỰ khởi tạo ngay khi panel được bật lên (không cần gọi thủ công):
    /// - Lần bật đầu: dựng danh sách Slot.
    /// - Mỗi lần bật: reset về trạng thái mặc định (chưa chọn Slot, nút Upgrade tắt).
    /// </summary>
    private void OnEnable()
    {
        if (!_initialized)
        {
            _initialized = true;
            PopulateSlots();
        }
        DeselectCurrentSlot();
        RefreshUpgradeButton();

        if (upgradeButton != null) upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        if (UserData.Instance != null)
        {
            _currentGold = UserData.Instance.Gold;
            UserData.Instance.OnGoldChanged += OnGoldChanged;
        }
    }

    private void OnDisable()
    {
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        if (UserData.Instance != null) UserData.Instance.OnGoldChanged -= OnGoldChanged;
    }

    /// <summary>
    /// Mở Bảng Perks: hiện panel (nếu đang ẩn) và reset về trạng thái mặc định.
    /// Slot đã được dựng sẵn ở OnEnable nên hàm này chỉ cần hiện + reset.
    /// </summary>
    public void OpenPerksPanel()
    {
        gameObject.SetActive(true);
        DeselectCurrentSlot();
        RefreshUpgradeButton();
    }

    /// <summary>Đóng Bảng Perks (ẩn panel).</summary>
    public void ClosePerksPanel()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Xóa Slot cũ và dựng lại toàn bộ Slot từ perkPool vào Content.
    /// Level của từng Perk được giữ nguyên qua Dictionary (không reset khi mở lại).
    /// </summary>
    private void PopulateSlots()
    {
        ClearSlots();

        if (content == null || slotPrefab == null)
        {
            Debug.LogWarning("PerksManager: thiếu Content hoặc slotPrefab.");
            return;
        }

        foreach (PerkDataSO perk in perkPool)
        {
            if (perk == null) continue;

            PerkSlotUI slot = Instantiate(slotPrefab, content);
            slot.name = $"Slot_{perk.PerkName}";

            slot.Setup(perk);
            slot.OnSlotClicked += OnSlotClicked;

            RefreshSlot(slot);
            _slotInstances.Add(slot);
        }

        if (perkScrollRect != null) perkScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClearSlots()
    {
        foreach (PerkSlotUI slot in _slotInstances)
        {
            if (slot == null) continue;
            slot.OnSlotClicked -= OnSlotClicked;
            Destroy(slot.gameObject);
        }
        _slotInstances.Clear();
    }

    /// <summary>
    /// Xử lý khi một Slot được bấm chọn:
    /// 1. Bỏ highlight Slot cũ, highlight Slot mới.
    /// 2. Kích hoạt Nút Nâng cấp và hiển thị giá tiền hiện tại.
    /// </summary>
    private void OnSlotClicked(PerkSlotUI slot)
    {
        if (slot == null) return;

        if (_selectedSlot == slot)
        {
            // Bấm lại Slot đang chọn -> hủy chọn (về trạng thái mặc định)
            DeselectCurrentSlot();
            RefreshUpgradeButton();
            return;
        }

        DeselectCurrentSlot();

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);
        _showBuffPreview = true; // chọn mới -> hiện lại preview "cur → next" trước khi mua.
        RefreshUpgradeButton();
    }

    private void DeselectCurrentSlot()
    {
        if (_selectedSlot != null) _selectedSlot.SetSelected(false);
        _selectedSlot = null;
    }

    /// <summary>Nút Nâng cấp bấm. Kiểm tra tiền -> Trừ tiền -> Tăng level -> Cập nhật UI + phát Event.</summary>
    public void HandleUpgradeClicked()
    {
        if (_selectedSlot == null || _selectedSlot.Data == null) return;

        PerkDataSO perk = _selectedSlot.Data;
        int level = GetLevel(perk);

        if (level >= perk.MaxLevel) return;

        int cost = perk.GetCostForLevel(level);
        if (cost < 0) return;

        // Kiểm tra & trừ tiền (UserData.SpendGold tự trừ và lưu khi đủ tiền)
        if (UserData.Instance == null || !UserData.Instance.SpendGold(cost))
        {
            RefreshUpgradeButton(); // Không đủ tiền -> cập nhật lại trạng thái nút
            return;
        }

        _perkLevels[perk] = level + 1;
        UserData.Instance?.AddPerkLevel(perk.PerkID, perk.MaxLevel);
        OnPerkUpgraded?.Invoke(perk, level + 1); // Thông báo để cập nhật chỉ số Player

        // Sau khi MUA XONG: panel chỉ hiện buff hiện tại (không còn preview "cur → next").
        _showBuffPreview = false;

        // Log perk upgraded event
        FirebaseAnalyticsHelper.LogPerkUpgraded(perk.PerkID, perk.PerkName, level + 1, cost);
        FirebaseAnalyticsHelper.LogGoldSpent(cost, "perk", perk.PerkID, UserData.Instance.Gold);

        RefreshSlot(_selectedSlot);              // Cập nhật cấp hiển thị trên Slot
        RefreshUpgradeButton();                  // Tính lại giá mới lên Nút
    }

    /// <summary>
    /// Cập nhật trạng thái Nút Nâng cấp dựa trên Slot đang chọn:
    /// - Không có Slot: Disabled + ẩn giá tiền.
    /// - Max Level: text "MAX" + Disabled.
    /// - Không đủ tiền: Disabled + text giá màu đỏ.
    /// - Đủ tiền: Enabled + text giá màu bình thường.
    /// </summary>
    private void RefreshUpgradeButton()
    {
        if (upgradeButton == null) return;

        RefreshDescPanel();

        bool hasSelection = _selectedSlot != null && _selectedSlot.Data != null;

        if (upgradeButtonText != null) upgradeButtonText.text = string.Empty;

        if (!hasSelection)
        {
            SetButtonEnabled(false);
            return;
        }

        PerkDataSO perk = _selectedSlot.Data;
        int level = GetLevel(perk);

        if (level >= perk.MaxLevel)
        {
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = "MAX";
                upgradeButtonText.color = notAffordableColor;
            }
            SetButtonEnabled(false);
            return;
        }

        int cost = perk.GetCostForLevel(level);
        bool affordable = UserData.Instance != null && UserData.Instance.HasEnoughGold(cost);

        if (upgradeButtonText != null)
        {
            upgradeButtonText.text = string.Format(upgradeButtonFormat, FormatHelper.FormatGold(cost));
            upgradeButtonText.color = affordable ? affordableColor : notAffordableColor;
        }

        SetButtonEnabled(affordable);
    }

    /// <summary>Lấy level hiện tại của một Perk (mặc định 0 nếu chưa từng nâng).</summary>
    public int GetLevel(PerkDataSO perk)
    {
        if (perk == null) return 0;
        return _perkLevels.TryGetValue(perk, out int level) ? level : 0;
    }

    /// <summary>Setter dành cho hệ thống nạp dữ liệu (Save/Load) nếu cần. Ghi thẳng xuống GameData.</summary>
    public void SetLevel(PerkDataSO perk, int level)
    {
        if (perk == null) return;
        _perkLevels[perk] = Mathf.Clamp(level, 0, perk.MaxLevel);
        UserData.Instance?.SetPerkLevel(perk.PerkID, _perkLevels[perk]);
    }

    /// <summary>Khi vàng của Player thay đổi -> cập nhật lại Slot đang chọn và trạng thái Nút.</summary>
    private void OnGoldChanged(int gold)
    {
        _currentGold = gold;
        if (_selectedSlot != null) RefreshSlot(_selectedSlot);
        RefreshUpgradeButton();
    }

    /// <summary>
    /// Hiển thị/ẩn Panel mô tả Perk theo Slot đang chọn:
    /// - Không có Slot chọn: ẩn panel.
    /// - Có Slot chọn: hiện NHẬT KÝ 1 Text gồm Tên + Mô tả + Khoảng buff.
    /// </summary>
    private void RefreshDescPanel()
    {
        if (perkDescPanel == null) return;

        PerkSlotUI selected = _selectedSlot;
        if (selected == null || selected.Data == null)
        {
            perkDescPanel.SetActive(false);
            return;
        }

        perkDescPanel.SetActive(true);

        PerkDataSO perk = selected.Data;
        if (perkDescText != null)
            perkDescText.text = BuildBuffRangeText(perk, _showBuffPreview);
    }

    /// <summary>
    /// Dựng text khoảng buff: mỗi dòng ứng với 1 chỉ số (StatMod).
///   - showPreview = true (TRƯỚC khi mua): "+36% Máu - +69% Máu" (buff hiện tại → sau khi nâng cấp).
///   - showPreview = false (SAU khi mua xong): chỉ hiện "+69% Máu".
///   - Perk CHƯA được nâng (level 0): luôn hiện "0%".
///   - Đã đạt MAX: hiện buff hiện tại kèm "(MAX)".
    /// </summary>
    private string BuildBuffRangeText(PerkDataSO perk, bool showPreview)
    {
        if (perk == null) return string.Empty;

        int level = GetLevel(perk);
        bool isMax = level >= perk.MaxLevel;
        int nextLevel = Mathf.Min(level + 1, perk.MaxLevel);

        if (perk.StatMods == null || perk.StatMods.Count == 0)
            return isMax ? "MAX" : string.Empty;

        StringBuilder sb = new StringBuilder();
        foreach (PerkDataSO.PerkStatMod mod in perk.StatMods)
        {
            if (mod == null) continue;

            string label = GetStatLabel(mod.Stat);
            bool percent = IsPercentStat(mod.Stat);
            float cur = mod.AmountPerLevel * level;

            if (!showPreview || isMax || level <= 0)
            {
                sb.AppendLine($"{label}: {FormatBuff(cur, percent)}{(isMax ? " (MAX)" : "")}");
                continue;
            }

            float next = mod.AmountPerLevel * nextLevel;
            sb.AppendLine($"{label}: {FormatBuff(cur, percent)} -> {FormatBuff(next, percent)}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatBuff(float value, bool percent)
    {
        string sign = value > 0f ? "+" : "";
        return percent
            ? sign + value.ToString("0.#") + "%"
            : sign + value.ToString("0");
    }

    /// <summary>Tên tiếng Việt hiển thị cho từng loại chỉ số.</summary>
    private static string GetStatLabel(UpgradeType stat)
    {
        switch (stat)
        {
            case UpgradeType.MaxHealthPercent: return "Máu tối đa";
            case UpgradeType.MaxArmorPercent: return "Giáp tối đa";
            case UpgradeType.RecoveryRatePercent: return "Hồi máu";
            case UpgradeType.MoveSpeedPercent: return "Tốc độ di chuyển";
            case UpgradeType.CollectRangePercent: return "Bán kính nhặt";
            case UpgradeType.GrowthRatePercent: return "Tốc độ EXP";
            case UpgradeType.FireRatePercent: return "Tốc độ bắn";
            case UpgradeType.FireRangePercent: return "Tầm bắn";
            case UpgradeType.ReloadSpeedPercent: return "Tốc độ nạp đạn";
            case UpgradeType.MagazineSizePercent: return "Băng đạn";
            case UpgradeType.BulletCount: return "Số đạn";
            case UpgradeType.BulletPierce: return "Xuyên giáp";
            case UpgradeType.BulletSpeedPercent: return "Tốc độ đạn";
            case UpgradeType.BulletDamagePercent: return "Sát thương đạn";
            case UpgradeType.BulletExecutePercent: return "Tỉ lệ thiêu hủy";
            case UpgradeType.BulletKnockbackPercent: return "Đẩy lùi";
            case UpgradeType.BulletSizePercent: return "Kích thước đạn";
            case UpgradeType.BulletInfinitePierceOnKill: return "Đạn xuyên vĩnh viễn khi hạ kẻ";
            case UpgradeType.BulletExplosionOnKill: return "Nổ khi hạ kẻ";
            case UpgradeType.BulletSpreadPercent:
            case UpgradeType.BulletSpread: return "Độ tản đạn";
            case UpgradeType.BulletBounceCount: return "Số lần nảy";
            case UpgradeType.FreeShotChanceWhileStill: return "Bắn miễn phí khi đứng yên";
            case UpgradeType.AmmoRecoverOnXP: return "Nhận đạn khi nhặt EXP";
            case UpgradeType.FireRateBuffOnXP: return "Tăng tốc bắn khi nhặt EXP";
            case UpgradeType.LastAmmoBurst: return "Chùm đạn cuối băng";
            case UpgradeType.BackShot: return "Bắn phía sau";
            case UpgradeType.DamageBuffAfterReload: return "Sát thương tăng sau khi nạp đạn";
            case UpgradeType.ReloadSpeedStackOnKill: return "Nạp đạn nhanh theo mạng hạ";
            case UpgradeType.InvulnerableWhileReloading: return "Bất tử khi nạp đạn";
            case UpgradeType.BurnAura: return "Vòng lửa";
            case UpgradeType.StackingBuffOnTime: return "Buff cộng dồn theo thời gian";
            case UpgradeType.MysteryCube: return "Hộp bí ẩn";
            case UpgradeType.MysteryCubeDmgStack: return "Hộp bí ẩn (sát thương cộng dồn)";
            case UpgradeType.MysteryCubeAsStack: return "Hộp bí ẩn (xếp chồng)";
            case UpgradeType.ArmorRegenPerSecond: return "Hồi giáp/giây";
            case UpgradeType.SpiritSummon: return "Triệu hồi linh";
            case UpgradeType.SpiritHeal: return "Linh hồi máu";
            case UpgradeType.SpiritBurn: return "Linh gây bỏng";
            case UpgradeType.SpiritEmpowered: return "Linh cường hóa";
            case UpgradeType.CharacterSizePercent: return "Kích thước nhân vật";
            case UpgradeType.DamageTakenFireRatePercent: return "Tốc độ bắn khi bị trúng đòn";
            case UpgradeType.DamageTakenBulletDamagePercent: return "Sát thương khi bị trúng đòn";
            case UpgradeType.GoldGainPercent: return "Vàng nhận thêm";
            case UpgradeType.Revive: return "Hồi sinh";
            case UpgradeType.VisionRangePercent: return "Tầm nhìn";
            case UpgradeType.DoubleShieldArmor: return "Mảnh giáp đôi";
            case UpgradeType.ThornsDamage: return "Phản sát thương";
            default: return stat.ToString();
        }
    }

    /// <summary>Chỉ số nào hiển thị dạng % (hiện là percent chance / percent stat).</summary>
    private static bool IsPercentStat(UpgradeType stat)
    {
        switch (stat)
        {
            case UpgradeType.BulletCount:
            case UpgradeType.BulletPierce:
            case UpgradeType.BulletBounceCount:
            case UpgradeType.ArmorRegenPerSecond:
            case UpgradeType.ThornsDamage:
            case UpgradeType.Revive:
            case UpgradeType.TC_1:
            case UpgradeType.TC_2A:
            case UpgradeType.TC_2B:
            case UpgradeType.TC_3:
                return false;
            default:
                return true;
        }
    }

    private void RefreshSlot(PerkSlotUI slot)
    {
        if (slot == null || slot.Data == null) return;
        PerkDataSO perk = slot.Data;
        int level = GetLevel(perk);
        slot.Refresh(level, _currentGold, level >= perk.MaxLevel);
    }

    private void SetButtonEnabled(bool enabled)
    {
        upgradeButton.interactable = enabled;
    }

    /// <summary>
    /// Nạp level đã lưu từ GameData (qua UserData) theo PerkID và khớp với perk trong pool.
    /// Sau này muốn nhiều profile thì chỉ cần đổi nguồn dữ liệu ở UserData.
    /// </summary>
    private void LoadPerkLevels()
    {
        if (UserData.Instance == null) return;
        _perkLevels.Clear();
        foreach (PerkDataSO perk in perkPool)
        {
            if (perk == null) continue;
            int level = Mathf.Clamp(UserData.Instance.GetPerkLevel(perk.PerkID), 0, perk.MaxLevel);
            if (level > 0) _perkLevels[perk] = level;
        }
    }
}