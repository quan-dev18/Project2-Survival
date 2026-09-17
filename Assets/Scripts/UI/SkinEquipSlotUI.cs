using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị 1 ô skin trong panel Chọn &amp; Trang bị Skin (nằm trong ScrollRect).
/// Ô gồm: nền đổi màu theo Tier, icon skin, khung khóa (khi chưa mua) và nút bấm chọn.
///
/// Trạng thái chọn được thể hiện bằng cách LÀM TỐI nền + icon:
///  – Skin ĐANG được chọn: nền và icon bị tối đi (xem <see cref="selectedDimFactor"/>).
///  – Skin CHƯA được chọn: giữ nguyên độ sáng gốc (nền theo Tier, icon gốc).
///
/// Script này KHÔNG tự quyết định logic mua/trang bị — mọi thao tác được đẩy về
/// <see cref="SkinSelectionManager"/> qua callback <c>onClick</c>.
/// </summary>
public class SkinEquipSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Ảnh nền của slot — sẽ được tô màu theo Tier của skin.")]
    [SerializeField] private Image bgImage;

    [Tooltip("Icon hiển thị của skin.")]
    [SerializeField] private Image skinIcon;

    [Tooltip("Khung tối/ổ khóa — hiện khi skin CHƯA được sở hữu.")]
    [SerializeField] private GameObject lockOverlay;

    [Tooltip("Nút bấm để chọn slot này.")]
    [SerializeField] private Button button;

    [Header("Selection Visual")]
    [Tooltip("Hệ số làm tối skin ĐANG được chọn (0 = đen, 1 = giữ nguyên). Skin chưa chọn luôn sáng 100%.")]
    [Range(0f, 1f)]
    [SerializeField] private float selectedDimFactor = 0.45f;

    // ──────────────────── Runtime State ────────────────────
    private WeaponSkinData skinData;
    private bool isOwned;
    private bool isSelected;
    private Color baseBgColor = Color.white;
    private Color baseIconColor = Color.white;
    private System.Action<SkinEquipSlotUI> onClickCallback;

    /// <summary>Dữ liệu skin mà slot này đang giữ (có thể null nếu chưa Setup).</summary>
    public WeaponSkinData SkinData => skinData;

    /// <summary>Skin này đã được người chơi sở hữu chưa.</summary>
    public bool IsOwned => isOwned;

    // ──────────────────── Setup ────────────────────

    /// <summary>
    /// Khởi tạo slot với đầy đủ dữ liệu hiển thị và callback khi bấm.
    /// </summary>
    /// <param name="skin">Dữ liệu skin cần hiển thị.</param>
    /// <param name="isOwned">Người chơi đã sở hữu skin này chưa.</param>
    /// <param name="tierColor">Màu nền tương ứng với <see cref="SkinTier"/> của skin.</param>
    /// <param name="onClick">Callback được gọi khi người chơi bấm vào slot.</param>
    public void Setup(WeaponSkinData skin, bool isOwned, Color tierColor, System.Action<SkinEquipSlotUI> onClick)
    {
        skinData = skin;
        this.isOwned = isOwned;
        onClickCallback = onClick;

        // Lưu màu gốc (nền theo Tier + màu icon hiện có trong Inspector) để khôi phục khi được chọn.
        baseBgColor = tierColor;
        baseIconColor = skinIcon != null ? skinIcon.color : Color.white;

        ApplyBaseVisual();
        SetSelected(false);
        BindButton();
    }

    /// <summary>Cập nhật lại trạng thái sở hữu (dùng sau khi mua skin thành công).</summary>
    public void SetOwned(bool owned)
    {
        isOwned = owned;
        if (lockOverlay != null) lockOverlay.SetActive(!isOwned);
    }

    // ──────────────────── Selection ────────────────────

    /// <summary>
    /// Đặt trạng thái chọn: skin đang chọn bị làm tối, các skin còn lại sáng bình thường.
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySelectionVisual();
    }

    // ──────────────────── Internal ────────────────────

    /// <summary>Áp dụng icon + ổ khóa (không đụng tới màu nền theo Tier).</summary>
    private void ApplyBaseVisual()
    {
        // Icon skin; nếu skin thiếu icon thì bỏ trống an toàn.
        if (skinIcon != null && skinData != null)
            skinIcon.sprite = skinData.skinIcon;

        // Ổ khóa chỉ hiện khi chưa sở hữu.
        if (lockOverlay != null) lockOverlay.SetActive(!isOwned);
    }

    /// <summary>Tô màu nền/icon theo trạng thái chọn (tối nếu đang chọn, sáng nếu không).</summary>
    private void ApplySelectionVisual()
    {
        if (bgImage != null)
            bgImage.color = isSelected ? Darken(baseBgColor) : baseBgColor;

        if (skinIcon != null)
            skinIcon.color = isSelected ? Darken(baseIconColor) : baseIconColor;
    }

    /// <summary>Làm tối 1 màu theo <see cref="selectedDimFactor"/>, giữ nguyên alpha.</summary>
    private Color Darken(Color color)
    {
        return new Color(
            color.r * selectedDimFactor,
            color.g * selectedDimFactor,
            color.b * selectedDimFactor,
            color.a);
    }

    /// <summary>Gắn sự kiện click, xóa listener cũ để tránh chồng callback khi tái sử dụng slot.</summary>
    private void BindButton()
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }
}
