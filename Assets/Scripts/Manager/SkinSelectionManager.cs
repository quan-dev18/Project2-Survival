using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý panel Chọn &amp; Trang bị Skin cho 1 khẩu vũ khí:
///  – Dựng danh sách slot skin trong ScrollRect (<see cref="SkinEquipSlotUI"/>).
///  – Hiển thị súng + tên skin đang chọn ở khu Showcase phía trên.
///  – Bấm vào 1 slot (button phủ cả ô) = chọn + TRANG BỊ NGAY, đồng thời cập nhật Gun Showcase.
///
/// Cách dùng: từ màn hình chọn vũ khí, gọi <see cref="PopulateSkins"/> với WeaponSO tương ứng.
///
/// ── HƯỚNG DẪN CÀI BẢNG MÀU TIER TRONG INSPECTOR ──
///   1. Chọn object gắn script này → mục "Tier Colors".
///   2. Set Size = 4 rồi thêm đủ 4 phần tử ứng với: Common, Rare, Epic, Legendary.
///   3. Với mỗi phần tử chọn đúng "Tier" và gán "Color" mong muốn, ví dụ:
///        Common    → trắng/xám  (200,200,200)
///        Rare      → xanh dương ( 70,130,255)
///        Epic      → tím        (170, 70,255)
///        Legendary → vàng cam   (255,180, 40)
///   * Nếu bỏ trống, code sẽ tự dùng màu mặc định (xem <see cref="GetTierColor"/>).
/// </summary>
public class SkinSelectionManager : MonoBehaviour
{
    // ──────────────────── Cấu hình màu theo Tier ────────────────────
    /// <summary>1 dòng cấu hình màu cho 1 Tier (serialize được trong Inspector).</summary>
    [System.Serializable]
    public class TierColorEntry
    {
        public SkinTier tier;
        public Color color = Color.white;
    }

    [Header("Data / Prefab")]
    [Tooltip("Content của ScrollRect — nơi sinh các slot skin.")]
    [SerializeField] private Transform content;

    [Tooltip("Prefab slot skin (đã gắn SkinEquipSlotUI).")]
    [SerializeField] private SkinEquipSlotUI slotPrefab;

    [Header("Showcase (phía trên)")]
    [Tooltip("Ảnh hiển thị cây súng theo skin đang chọn.")]
    [SerializeField] private Image showcaseWeaponImage;

    [Tooltip("Tên skin đang chọn.")]
    [SerializeField] private TextMeshProUGUI showcaseSkinNameText;

    [Header("Gun Showcase UI")]
    [Tooltip("(Tuỳ chọn) Pop-up chỉ số súng. Khi chọn skin sẽ đổi ảnh súng tương ứng.")]
    [SerializeField] private GunShowcaseUI gunShowcase;

    [Header("Equip Status Label")]
    [Tooltip("(Tuỳ chọn) Nút/ô hiển thị trạng thái. Vì bấm skin là trang bị ngay nên đây chỉ để hiện chữ.")]
    [SerializeField] private Button equipButton;

    [Tooltip("Text trạng thái: 'Chưa sở hữu' / 'Đang dùng'.")]
    [SerializeField] private TextMeshProUGUI equipButtonText;

    [Header("Tier Colors")]
    [Tooltip("Bảng màu theo Tier. Nên thêm đủ 4 dòng: Common / Rare / Epic / Legendary.")]
    [SerializeField] private List<TierColorEntry> tierColors = new List<TierColorEntry>();

    [Header("Nguồn dữ liệu (tuỳ chọn)")]
    [Tooltip("Kéo object chứa WeaponSelectManager vào đây để dùng OpenForSelectedWeapon().")]
    [SerializeField] private WeaponSelectManager weaponSelect;

    // ──────────────────── Runtime State ────────────────────
    private WeaponSO currentWeapon;
    private SkinEquipSlotUI selectedSlot;
    private WeaponSkinData selectedSkin;
    private bool selectedSkinOwned;

    // ──────────────────── Unity Callbacks ────────────────────

    /// <summary>Tự nạp danh sách skin mỗi khi panel được bật và lắng nghe sự kiện đổi súng.</summary>
    private void OnEnable()
    {
        if (weaponSelect != null)
            weaponSelect.OnWeaponSelected += PopulateSkins;

        OpenForSelectedWeapon();
    }

    private void OnDisable()
    {
        if (weaponSelect != null)
            weaponSelect.OnWeaponSelected -= PopulateSkins;
    }

    // ──────────────────── Public API ────────────────────

    /// <summary>
    /// Dựng danh sách skin cho 1 khẩu vũ khí (skin mặc định = sprite gốc + skin authored)
    /// và chọn sẵn skin đang trang bị (hoặc skin mặc định).
    /// LƯU Ý: hàm này chỉ chọn/xem trước, KHÔNG tự trang bị (trang bị chỉ khi người chơi bấm slot).
    /// </summary>
    /// <param name="weapon">Vũ khí cần hiển thị danh sách skin.</param>
    public void PopulateSkins(WeaponSO weapon)
    {
        currentWeapon = weapon;

        ClearContent();

        if (currentWeapon == null)
        {
            SelectSlot(null, null);
            return;
        }

        if (content == null || slotPrefab == null)
        {
            Debug.LogWarning("[SkinSelectionManager] Chưa gán 'Content' hoặc 'Slot Prefab'. " +
                             "Hãy kéo Content của ScrollRect và prefab slot vào Inspector.");
            SelectSlot(null, null);
            return;
        }

        // Danh sách hiển thị: skin MẶC ĐỊNH (sprite gốc vũ khí) + các skin authored.
        List<WeaponSkinData> displaySkins = BuildDisplaySkins(currentWeapon);

        if (displaySkins.Count == 0)
        {
            SelectSlot(null, null);
            return;
        }

        // Ưu tiên chọn skin đang trang bị; nếu chưa trang bị thì chọn skin mặc định.
        string equippedId = UserData.Instance != null
            ? UserData.Instance.GetEquippedSkinId(currentWeapon.WeaponID)
            : null;

        SkinEquipSlotUI defaultSlot = null;

        for (int i = 0; i < displaySkins.Count; i++)
        {
            WeaponSkinData skin = displaySkins[i];
            if (skin == null || string.IsNullOrEmpty(skin.skinID)) continue;

            bool owned = skin.isDefault ||
                         (UserData.Instance != null &&
                          UserData.Instance.IsSkinOwned(currentWeapon.WeaponID, skin.skinID));

            SkinEquipSlotUI slot = Instantiate(slotPrefab, content);
            if (slot == null) continue;

            slot.transform.localScale = Vector3.one;
            slot.transform.localPosition = Vector3.zero;

            slot.Setup(skin, owned, GetTierColor(skin.tier), OnSlotSelected);

            if (defaultSlot == null) defaultSlot = slot;                 // dự phòng: slot đầu tiên
            if (skin.isDefault) defaultSlot = slot;                      // ưu tiên: skin mặc định
            if (!string.IsNullOrEmpty(equippedId) && skin.skinID == equippedId)
                defaultSlot = slot;                                      // ưu tiên cao nhất: skin đang trang bị
        }

        SelectSlot(defaultSlot, defaultSlot != null ? defaultSlot.SkinData : null);
    }

    /// <summary>
    /// Mở/refresh panel cho VŨ KHÍ ĐANG CHỌN ở màn chọn súng.
    /// Gán object này vào <c>onClick</c> của nút mở panel Skin.
    /// </summary>
    public void OpenForSelectedWeapon()
    {
        WeaponSO weapon = weaponSelect != null ? weaponSelect.CurrentWeapon : null;
        PopulateSkins(weapon);
    }

    /// <summary>
    /// Tạo danh sách skin để hiển thị: đặt skin MẶC ĐỊNH lên đầu (nếu là skin ảo chưa có trong skinList),
    /// sau đó thêm toàn bộ skin authored — bỏ qua skin trùng ID với mặc định.
    /// </summary>
    private List<WeaponSkinData> BuildDisplaySkins(WeaponSO weapon)
    {
        List<WeaponSkinData> result = new List<WeaponSkinData>();

        WeaponSkinData def = weapon.DefaultSkin;
        if (def != null && !string.IsNullOrEmpty(def.skinID))
            result.Add(def);

        if (weapon.SkinList != null)
        {
            for (int i = 0; i < weapon.SkinList.Count; i++)
            {
                WeaponSkinData skin = weapon.SkinList[i];
                if (skin == null || string.IsNullOrEmpty(skin.skinID)) continue;
                if (def != null && skin.skinID == def.skinID) continue; // tránh trùng với mặc định
                result.Add(skin);
            }
        }

        return result;
    }

    // ──────────────────── Logic: Chọn Slot ────────────────────

    /// <summary>Callback khi người chơi bấm 1 slot: chọn, trang bị ngay (nếu đã sở hữu) và cập nhật UI.</summary>
    private void OnSlotSelected(SkinEquipSlotUI slot)
    {
        if (slot == null || slot.SkinData == null) return;

        SelectSlot(slot, slot.SkinData);

        // Bấm slot = trang bị ngay (chỉ khi đã sở hữu).
        if (slot.IsOwned)
            EquipSelectedSkin();

        UpdateEquipStatus(); // cập nhật lại nhãn sau khi trang bị
    }

    /// <summary>Đặt slot đang chọn, cập nhật highlight + Showcase + nhãn trạng thái (không trang bị).</summary>
    private void SelectSlot(SkinEquipSlotUI slot, WeaponSkinData skin)
    {
        // Bỏ highlight (làm sáng lại) slot cũ.
        if (selectedSlot != null && selectedSlot != slot)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;
        selectedSkin = skin;
        selectedSkinOwned = slot != null && slot.IsOwned;

        // Làm nổi bật slot mới.
        if (selectedSlot != null)
            selectedSlot.SetSelected(true);

        UpdateLocalShowcase();
        UpdateGunShowcase();
        UpdateEquipStatus();

        // Skin đã sở hữu (được trang bị) thì cập nhật luôn ảnh súng ở UI ngoài.
        if (selectedSkinOwned)
            UpdateOutsidePreview();
    }

    // ──────────────────── Logic: Trang bị ────────────────────

    /// <summary>Trang bị skin đang chọn vào UserData (bỏ qua nếu đã trang bị rồi).</summary>
    private void EquipSelectedSkin()
    {
        if (selectedSkin == null || currentWeapon == null || UserData.Instance == null) return;

        // Đã đang dùng thì không lưu lại để tránh ghi save vô ích.
        if (UserData.Instance.IsSkinEquipped(currentWeapon.WeaponID, selectedSkin.skinID)) return;

        UserData.Instance.EquipSkin(currentWeapon.WeaponID, selectedSkin.skinID);
    }

    // ──────────────────── Logic: Showcase ────────────────────

    /// <summary>Cập nhật ảnh súng + tên skin ở khu Showcase phía trên theo skin đang chọn.</summary>
    private void UpdateLocalShowcase()
    {
        if (showcaseWeaponImage != null)
        {
            if (selectedSkin != null && selectedSkin.weaponSprite != null)
                showcaseWeaponImage.sprite = selectedSkin.weaponSprite;
            else if (currentWeapon != null)
                showcaseWeaponImage.sprite = currentWeapon.WeaponIcon;
        }

        if (showcaseSkinNameText != null)
            showcaseSkinNameText.text = selectedSkin != null ? selectedSkin.skinName : string.Empty;
    }

    /// <summary>Đẩy sprite của skin đang chọn ra ảnh súng ở UI ngoài (outside equipment preview).</summary>
    private void UpdateOutsidePreview()
    {
        if (weaponSelect == null) return;

        Sprite sprite = selectedSkin != null && selectedSkin.weaponSprite != null
            ? selectedSkin.weaponSprite
            : (currentWeapon != null ? currentWeapon.WeaponIcon : null);

        weaponSelect.SetOutsideWeaponIcon(sprite);
    }

    /// <summary>Đổi ảnh súng trên Gun Showcase theo skin đang chọn (giữ nguyên chỉ số/animation).</summary>
    private void UpdateGunShowcase()
    {
        if (gunShowcase == null) return;

        Sprite icon = selectedSkin != null && selectedSkin.weaponSprite != null
            ? selectedSkin.weaponSprite
            : (currentWeapon != null ? currentWeapon.WeaponIcon : null);

        if (icon != null)
            gunShowcase.SetIcon(icon);
    }

    // ──────────────────── Logic: Nhãn trạng thái ────────────────────

    /// <summary>
    /// Cập nhật nhãn trạng thái (không phải nút bấm vì đã trang bị ngay khi chọn):
    ///   – Chưa mua   → "Chưa sở hữu".
    ///   – Đang dùng  → "Đang dùng".
    /// </summary>
    private void UpdateEquipStatus()
    {
        if (equipButtonText == null && equipButton == null) return;

        if (selectedSkin == null || currentWeapon == null || UserData.Instance == null)
        {
            SetEquipStatus(string.Empty);
            return;
        }

        if (!selectedSkinOwned)
        {
            SetEquipStatus("Chưa sở hữu");
            return;
        }

        bool isEquipped = UserData.Instance.IsSkinEquipped(currentWeapon.WeaponID, selectedSkin.skinID);
        SetEquipStatus(isEquipped ? "Đang dùng" : "Đang chọn");
    }

    /// <summary>Ghi text trạng thái; nút (nếu có) luôn không bấm được vì chỉ để hiển thị.</summary>
    private void SetEquipStatus(string text)
    {
        if (equipButtonText != null) equipButtonText.text = text;
        if (equipButton != null) equipButton.interactable = false;
    }

    // ──────────────────── Logic: Dựng / Xóa UI ────────────────────

    /// <summary>Xóa sạch toàn bộ slot cũ trong Content bằng vòng lặp ngược.</summary>
    private void ClearContent()
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);
    }

    /// <summary>Lấy màu nền theo Tier; nếu Inspector chưa cấu hình thì dùng màu mặc định.</summary>
    private Color GetTierColor(SkinTier tier)
    {
        if (tierColors != null)
        {
            for (int i = 0; i < tierColors.Count; i++)
            {
                if (tierColors[i] != null && tierColors[i].tier == tier)
                    return tierColors[i].color;
            }
        }

        // Fallback an toàn để UI luôn có màu dù chưa set trong Inspector.
        switch (tier)
        {
            case SkinTier.Rare: return new Color(0.27f, 0.51f, 1f);
            case SkinTier.Epic: return new Color(0.67f, 0.27f, 1f);
            case SkinTier.Legendary: return new Color(1f, 0.71f, 0.16f);
            default: return new Color(0.78f, 0.78f, 0.78f);
        }
    }
}
