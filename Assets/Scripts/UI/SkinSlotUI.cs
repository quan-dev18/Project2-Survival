using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị 1 ô skin trong SkinShop (nằm trong ScrollRect).
/// Mỗi ô gồm: icon skin, tên skin (kèm tên súng tuỳ chọn) và 1 nút duy nhất:
///   - Chưa mua  → nút hiện giá tiền, bấm để mua.
///   - Đã sở hữu → nút ghi "Đã sở hữu", không bấm được.
/// </summary>
public class SkinSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image skinIcon;
    [SerializeField] private TextMeshProUGUI skinNameText;
    [Tooltip("(Tuỳ chọn) Tên khẩu súng chứa skin này, giúp phân biệt khi shop hiện skin của nhiều súng.")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [Tooltip("Nút duy nhất: hiện giá (chưa mua) hoặc 'Đã sở hữu'.")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [Tooltip("(Tuỳ chọn) Ảnh khoá hiện khi skin chưa được sở hữu.")]
    [SerializeField] private GameObject lockOverlay;

    private WeaponSkinData skinData;
    private WeaponSO parentWeapon;
    private System.Action<SkinSlotUI> onClickCallback;
    private bool isOwned;

    /// <summary>Skin này đã được người chơi sở hữu chưa.</summary>
    public bool IsOwned => isOwned;

    // ──────────────────── Setup ────────────────────

    /// <summary>
    /// Khởi tạo slot với dữ liệu skin và khẩu súng cha.
    /// </summary>
    /// <param name="skin">Dữ liệu skin.</param>
    /// <param name="weapon">WeaponSO chứa skin này.</param>
    /// <param name="owned">Người chơi đã sở hữu skin chưa.</param>
    /// <param name="callback">Callback khi bấm nút (chỉ gọi khi chưa sở hữu).</param>
    public void Setup(WeaponSkinData skin, WeaponSO weapon, bool owned, System.Action<SkinSlotUI> callback)
    {
        skinData = skin;
        parentWeapon = weapon;
        isOwned = owned;
        onClickCallback = callback;

        ApplyVisual();

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                // Đã sở hữu thì không làm gì khi bấm
                if (!isOwned)
                    onClickCallback?.Invoke(this);
            });
        }
    }

    /// <summary>Lấy dữ liệu skin đang giữ.</summary>
    public WeaponSkinData GetSkinData() => skinData;

    /// <summary>Lấy WeaponSO cha.</summary>
    public WeaponSO GetParentWeapon() => parentWeapon;

    /// <summary>Đánh dấu skin đã được mua (sau khi mua thành công).</summary>
    public void SetOwned(bool owned)
    {
        isOwned = owned;
        ApplyVisual();
    }

    // ──────────────────── Visual ────────────────────

    private void ApplyVisual()
    {
        if (skinData == null) return;

        // ═══ Icon skin (fallback về icon súng nếu skin thiếu ảnh riêng) ═══
        if (skinIcon != null)
        {
            skinIcon.sprite = skinData.skinIcon != null
                ? skinData.skinIcon
                : (parentWeapon != null ? parentWeapon.WeaponIcon : null);
        }

        // ═══ Tên skin + tên súng tuỳ chọn ═══
        if (skinNameText != null)
            skinNameText.text = skinData.skinName;

        if (weaponNameText != null)
            weaponNameText.text = parentWeapon != null ? parentWeapon.WeaponName : string.Empty;

        // ═══ Lock overlay (chỉ hiện khi chưa sở hữu) ═══
        if (lockOverlay != null)
            lockOverlay.SetActive(!isOwned);

        // ═══ Nút: giá tiền (chưa mua) hoặc "Đã sở hữu" ═══
        if (isOwned)
        {
            if (actionButtonText != null) actionButtonText.text = "Đã sở hữu";
            if (actionButton != null) actionButton.interactable = false;
        }
        else
        {
            if (actionButtonText != null)
                actionButtonText.text = FormatHelper.FormatGold(skinData.price);
            if (actionButton != null)
                actionButton.interactable = UserData.Instance != null && UserData.Instance.HasEnoughGold(skinData.price);
        }
    }
}