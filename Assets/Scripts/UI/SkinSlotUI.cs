using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị 1 ô skin trong SkinShop (nằm trong ScrollRect).
/// Mỗi ô gồm: icon skin, tên skin (kèm tên súng tuỳ chọn) và 1 nút duy nhất:
///   - Đã sở hữu         → nút ghi "Đã sở hữu", không bấm được.
///   - Ads bật (chưa mua) → nút chuyển sang "Xem Ads (x/y)" (thay cho nút mua gold).
///   - Ads tắt (chưa mua) → nút hiện giá tiền, bấm để mua.
/// </summary>
public class SkinSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image skinIcon;
    [SerializeField] private TextMeshProUGUI skinNameText;
    [Tooltip("(Tuỳ chọn) Tên khẩu súng chứa skin này, giúp phân biệt khi shop hiện skin của nhiều súng.")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [Tooltip("Nút duy nhất: hiện giá (chưa mua), 'Xem Ads' (bật ads) hoặc 'Đã sở hữu'.")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [Tooltip("(Tuỳ chọn) Ảnh khoá hiện khi skin chưa được sở hữu.")]
    [SerializeField] private GameObject lockOverlay;

    private WeaponSkinData skinData;
    private WeaponSO parentWeapon;
    private System.Action<SkinSlotUI> onClickCallback;
    private System.Action<SkinSlotUI> onAdClickCallback;
    private bool isOwned;

    /// <summary>Nút actionButton đang ở chế độ Xem Ads (thay cho mua bằng gold).</summary>
    private bool adModeActive;

    /// <summary>Skin này đã được người chơi sở hữu chưa.</summary>
    public bool IsOwned => isOwned;

    private static bool IsAdUnlockEnabled =>
        FirebaseRemoteConfigHelper.Instance != null && FirebaseRemoteConfigHelper.Instance.IsSkinAdUnlockEnabled;

    // ──────────────────── Setup ────────────────────

    /// <summary>
    /// Khởi tạo slot với dữ liệu skin và khẩu súng cha.
    /// </summary>
    public void Setup(WeaponSkinData skin, WeaponSO weapon, bool owned, System.Action<SkinSlotUI> callback, System.Action<SkinSlotUI> adCallback = null)
    {
        skinData = skin;
        parentWeapon = weapon;
        isOwned = owned;
        onClickCallback = callback;
        onAdClickCallback = adCallback;

        ApplyVisual();

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }
    }

    /// <summary>Chế độ ads → xem ads, ngược lại mua bằng gold (đã sở hữu thì không làm gì).</summary>
    private void OnActionClicked()
    {
        if (isOwned) return;

        if (adModeActive) onAdClickCallback?.Invoke(this);
        else onClickCallback?.Invoke(this);
    }

    /// <summary>Làm mới text nút theo chế độ hiện tại (gọi sau khi xem 1 ads xong).</summary>
    public void UpdateAdButtonVisual()
    {
        if (!IsAdUnlockEnabled) return;
        if (skinData == null || parentWeapon == null) return;

        if (isOwned)
        {
            adModeActive = false;
            if (actionButtonText != null) actionButtonText.text = "Đã sở hữu";
            if (actionButton != null) actionButton.interactable = false;
            return;
        }

        string compositeId = UserData.MakeSkinCompositeId(parentWeapon.WeaponID, skinData.skinID);
        int watched = AdUnlockTracker.Instance != null ? AdUnlockTracker.Instance.GetAdWatchCount(compositeId) : 0;
        int required = FirebaseRemoteConfigHelper.Instance != null ? FirebaseRemoteConfigHelper.Instance.SkinAdWatchCount : 2;

        adModeActive = true;
        if (actionButtonText != null)
            actionButtonText.text = $"Xem Ads ({watched}/{required})";
        if (actionButton != null)
            actionButton.interactable = true;
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

        // ═══ Nút: "Đã sở hữu" / "Xem Ads" (bật ads) / giá tiền ═══
        if (isOwned)
        {
            adModeActive = false;
            if (actionButtonText != null) actionButtonText.text = "Đã sở hữu";
            if (actionButton != null) actionButton.interactable = false;
        }
        else if (IsAdUnlockEnabled)
        {
            // Ads bật → nút mua gold bị thay bằng nút Xem Ads.
            UpdateAdButtonVisual();
        }
        else
        {
            adModeActive = false;
            if (actionButtonText != null)
                actionButtonText.text = FormatHelper.FormatGold(skinData.price);
            if (actionButton != null)
                actionButton.interactable = UserData.Instance != null && UserData.Instance.HasEnoughGold(skinData.price);
        }
    }
}