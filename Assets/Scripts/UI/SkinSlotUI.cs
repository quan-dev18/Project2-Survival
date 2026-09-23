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
    [Tooltip("Nút xem quảng cáo để mở khóa skin (tùy chọn / tự tạo nếu bật remote config).")]
    [SerializeField] private Button adButton;
    [SerializeField] private TextMeshProUGUI adButtonText;
    [Tooltip("(Tuỳ chọn) Ảnh khoá hiện khi skin chưa được sở hữu.")]
    [SerializeField] private GameObject lockOverlay;

    private WeaponSkinData skinData;
    private WeaponSO parentWeapon;
    private System.Action<SkinSlotUI> onClickCallback;
    private System.Action<SkinSlotUI> onAdClickCallback;
    private bool isOwned;

    /// <summary>Skin này đã được người chơi sở hữu chưa.</summary>
    public bool IsOwned => isOwned;

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
            actionButton.onClick.AddListener(() =>
            {
                // Đã sở hữu thì không làm gì khi bấm
                if (!isOwned)
                    onClickCallback?.Invoke(this);
            });
        }

        SetupAdButton();
    }

    private void SetupAdButton()
    {
        bool adUnlockEnabled = FirebaseRemoteConfigHelper.Instance != null && FirebaseRemoteConfigHelper.Instance.IsSkinAdUnlockEnabled;
        if (!adUnlockEnabled || isOwned)
        {
            if (adButton != null) adButton.gameObject.SetActive(false);
            return;
        }

        if (adButton == null && actionButton != null)
        {
            GameObject clone = Instantiate(actionButton.gameObject, actionButton.transform.parent);
            clone.name = "SkinAdButton";
            adButton = clone.GetComponent<Button>();
            adButtonText = clone.GetComponentInChildren<TextMeshProUGUI>();

            RectTransform rt = clone.GetComponent<RectTransform>();
            RectTransform srcRt = actionButton.GetComponent<RectTransform>();
            if (rt != null && srcRt != null && (actionButton.transform.parent == null || actionButton.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() == null))
            {
                rt.anchoredPosition = srcRt.anchoredPosition + new Vector2(0f, srcRt.rect.height + 6f);
            }
        }

        if (adButton != null)
        {
            adButton.gameObject.SetActive(true);
            adButton.interactable = true;
            adButton.onClick.RemoveAllListeners();
            adButton.onClick.AddListener(() =>
            {
                if (!isOwned)
                    onAdClickCallback?.Invoke(this);
            });
            UpdateAdButtonVisual();
        }
    }

    public void UpdateAdButtonVisual()
    {
        if (adButton == null || skinData == null || parentWeapon == null) return;
        if (isOwned)
        {
            adButton.gameObject.SetActive(false);
            return;
        }

        string compositeId = UserData.MakeSkinCompositeId(parentWeapon.WeaponID, skinData.skinID);
        int watched = AdUnlockTracker.Instance != null ? AdUnlockTracker.Instance.GetAdWatchCount(compositeId) : 0;
        int required = FirebaseRemoteConfigHelper.Instance != null ? FirebaseRemoteConfigHelper.Instance.SkinAdWatchCount : 2;

        if (adButtonText != null)
            adButtonText.text = $"Ads ({watched}/{required})";
    }

    /// <summary>Lấy dữ liệu skin đang giữ.</summary>
    public WeaponSkinData GetSkinData() => skinData;

    /// <summary>Lấy WeaponSO cha.</summary>
    public WeaponSO GetParentWeapon() => parentWeapon;

    /// <summary>Đánh dấu skin đã được mua (sau khi mua thành công).</summary>
    public void SetOwned(bool owned)
    {
        isOwned = owned;
        if (adButton != null) adButton.gameObject.SetActive(false);
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