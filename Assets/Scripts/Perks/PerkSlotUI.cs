using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// View hiển thị một Slot Perk trong ScrollRect.
/// Gắn lên Prefab `UpgradeSlotPrefab`.
/// Chịu trách nhiệm: hiển thị dữ liệu, đổi màu nền theo level, highlight khi chọn,
/// và phát Event khi người dùng bấm chọn. KHÔNG chứa logic nâng cấp.
/// </summary>
[RequireComponent(typeof(Button))]
public class PerkSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image backgroundImage;      // Nền Slot (đổi màu theo level)
    [SerializeField] private Image iconImage;            // Icon Perk
    [Tooltip("Hiển thị buff % đang có. VD: \"+10%\", \"MAX\" ở cấp tối đa.")]
    [SerializeField] private TextMeshProUGUI levelText;
    [Tooltip("Viền/highlight khi Slot được chọn. Giữ vĩnh viễn sau khi Perk đã được nâng tối thiểu 1 lần.")]
    [SerializeField] private Image selectedBorder;

    [Header("Color Config")]
    [Tooltip("Bảng màu theo ngưỡng level (12, 18, 24, 36...). Gắn 1 lần trên Prefab.")]
    [SerializeField] private PerkColorConfigSO colorConfig;

    [Header("Option (tùy chọn)")]
    [Tooltip("Màu nền & Icon của Slot khi Perk CHƯA được nâng (level 0).")]
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("Đổi màu Icon khi Player không đủ tiền nâng cấp Perk đã mở này.")]
    [SerializeField] private Color disabledIconColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private readonly Color _normalIconColor = Color.white;

    /// <summary>Event khi Slot này được bấm chọn. Dữ liệu truyền đi chính là chính nó (this).</summary>
    public event Action<PerkSlotUI> OnSlotClicked;

    private Button _button;
    private PerkDataSO _data;
    private RectTransform _rect;
    private bool _isOwned;
    private bool _isSelected;

    public PerkDataSO Data => _data;
    public RectTransform Rect => _rect;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(NotifyClicked);
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(NotifyClicked);
    }

    /// <summary>
    /// Nhận dữ liệu từ Manager để hiển thị lên Slot lần đầu.
    /// Slot chỉ hiển thị BG / Border / Icon; chỉ số Level sẽ bật lên sau khi nâng cấp.
    /// </summary>
    public void Setup(PerkDataSO data)
    {
        _data = data;
        if (iconImage != null) iconImage.sprite = data != null ? data.Icon : null;
    }

    /// <summary>
    /// Đổi màu nền Slot theo ngưỡng level hiện tại + màu viền chọn tương ứng.
    /// Text % đồng màu với viền (SelectionColor). Slot chưa nâng (level 0) dùng màu xám.
    /// </summary>
    private void ApplyColorForLevel(int level)
    {
        if (!_isOwned)
        {
            if (backgroundImage != null)
                backgroundImage.color = lockedColor;
            if (levelText != null)
                levelText.color = lockedColor;
            return;
        }

        if (colorConfig == null) return;
        PerkLevelColor entry = colorConfig.GetColorForLevel(level);
        if (entry == null) return;

        if (backgroundImage != null)
            backgroundImage.color = entry.BackgroundColor;
        if (selectedBorder != null && selectedBorder.color != entry.SelectionColor)
            selectedBorder.color = entry.SelectionColor;
        if (levelText != null)
            levelText.color = entry.SelectionColor;
    }

    /// <summary>
    /// Cập nhật nội dung hiển thị của Slot:
    /// - Chưa nâng (level 0): nền + icon màu XÁM.
    /// - Đã nâng: màu nền theo ngưỡng level, icon sáng (hoặc mờ nếu không đủ tiền).
    /// - Level text luôn hiển thị dạng "0/40", "2/40"... và "MAX" khi đạt max.
    /// - Viền: giữ vĩnh viễn khi đã nâng (level > 0), màu theo ngưỡng level hiện tại.
    /// </summary>
    public void Refresh(int currentLevel, int gold, bool isMaxLevel)
    {
        if (_data == null) return;

        if (levelText != null)
        {
            float buff = _data.GetBuffPercentAtLevel(currentLevel);
            if (isMaxLevel)
                levelText.text = "MAX";
            else if (currentLevel <= 0)
                levelText.text = "0%";
            else
                levelText.text = buff >= 0f ? $"+{buff:#.#}%" : $"{buff:#.#}%";
        }

        _isOwned = isMaxLevel || currentLevel > 0;
        ApplyColorForLevel(currentLevel);
        ApplyBorderState();

        bool affordable = isMaxLevel || gold >= _data.GetCostForLevel(currentLevel);
        bool iconEnabled = _isOwned && affordable;
        if (iconImage != null)
            iconImage.color = iconEnabled ? _normalIconColor : disabledIconColor;
    }

    /// <summary>Bật/tắt trạng thái chọn (highlight viền). Viền của slot đã nâng không bị tắt.</summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        ApplyBorderState();
    }

    /// <summary>
    /// Viền hiển thị khi Slot đang được chọn HOẶC đã được nâng cấp (vĩnh viễn).
    /// </summary>
    private void ApplyBorderState()
    {
        if (selectedBorder != null)
            selectedBorder.gameObject.SetActive(_isSelected || _isOwned);
    }

    private void NotifyClicked() => OnSlotClicked?.Invoke(this);

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        // Nếu Button đã xử lý onClick thì không cần gọi lại.
        if (_button != null) return;
        NotifyClicked();
    }
}