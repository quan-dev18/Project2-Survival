using System;
using System.Collections.Generic;
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

    private readonly List<PerkSlotUI> _slotInstances = new List<PerkSlotUI>();
    private readonly Dictionary<PerkDataSO, int> _perkLevels = new Dictionary<PerkDataSO, int>();

    private PerkSlotUI _selectedSlot;
    private int _currentGold;
    private bool _initialized;

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