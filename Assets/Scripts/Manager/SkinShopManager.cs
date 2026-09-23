using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý Skin Shop: gom skin của TẤT CẢ vũ khí trong weaponList thành 1 danh sách slot
/// trong ScrollRect. Mỗi slot = icon + tên + 1 nút (hiện giá / "Đã sở hữu").
/// Trạng thái đã mua được lưu qua <see cref="UserData"/> với composite ID: weaponID_skinID.
/// Gắn script này lên panel nội dung của tab Skin Shop.
/// </summary>
public class SkinShopManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Danh sách toàn bộ vũ khí trong game. Shop sẽ gom skin của tất cả các súng này.")]
    [SerializeField] private List<WeaponSO> weaponList;

    [Header("UI Containers")]
    [Tooltip("Transform Content của ScrollRect – nơi instantiate các slot skin.")]
    [SerializeField] private Transform skinContent;
    [Tooltip("Prefab SkinSlotUI.")]
    [SerializeField] private GameObject skinSlotPrefab;

    [Header("Panel Tween")]
    [Tooltip("(Tuỳ chọn) Panel động (UISlideTween). Nếu để trống sẽ tự tắt gameObject.")]
    [SerializeField] private UISlideTween slideTween;

    // ──────────────────── Unity Callbacks ────────────────────

    /// <summary>Chạy mỗi khi panel shop được bật (mở tab).</summary>
    private void OnEnable()
    {
        GenerateSkinList();
    }

    // ──────────────────── Public API ────────────────────

    /// <summary>Đóng tab (nếu có UISlideTween thì chạy animation ẩn).</summary>
    public void ClosePanel()
    {
        if (slideTween != null)
            slideTween.Hide();
        else
            gameObject.SetActive(false);
    }

    // ──────────────────── Logic ────────────────────

    /// <summary>
    /// Duyệt weaponList → mỗi vũ khí → skinList, sinh tất cả skin vào ScrollRect.
    /// </summary>
    private void GenerateSkinList()
    {
        if (weaponList == null || skinContent == null) return;

        // Xóa slot cũ (đảo ngược để không bị skip child khi DestroyImmediate làm collection co lại)
        for (int i = skinContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(skinContent.GetChild(i).gameObject);

        // Chống lặp bằng 2 HashSet:
        //  1. seenWeapons  – mỗi WeaponSO asset chỉ được sinh 1 lần (phòng kéo trùng asset).
        //  2. seenComposite – mỗi (weaponID_skinID) chỉ hiện 1 slot.
        HashSet<WeaponSO> seenWeapons = new HashSet<WeaponSO>();
        HashSet<string> seenComposite = new HashSet<string>();

        for (int w = 0; w < weaponList.Count; w++)
        {
            WeaponSO weapon = weaponList[w];
            if (weapon == null || weapon.SkinList == null) continue;
            if (!seenWeapons.Add(weapon)) continue;   // cùng asset => bỏ qua

            string weaponId = weapon.WeaponID;

            for (int s = 0; s < weapon.SkinList.Count; s++)
            {
                WeaponSkinData skin = weapon.SkinList[s];
                if (skin == null || string.IsNullOrEmpty(skin.skinID)) continue;

                string compositeId = UserData.MakeSkinCompositeId(weaponId, skin.skinID);
                if (!seenComposite.Add(compositeId)) continue;

                // Đọc trạng thái "đã sở hữu" từ dữ liệu người chơi
                // (skin mặc định luôn được coi là đã sở hữu).
                bool owned = skin.isDefault ||
                             (UserData.Instance != null && UserData.Instance.IsSkinOwned(weaponId, skin.skinID));

                GameObject slotObj = Instantiate(skinSlotPrefab, skinContent);
                if (slotObj == null) continue;

                SkinSlotUI slotScript = slotObj.GetComponent<SkinSlotUI>();
                if (slotScript == null) continue;

                slotScript.Setup(skin, weapon, owned, OnSlotClicked, OnAdSlotClicked);

                // Reset transform về an toàn (tránh layout lệch trong ScrollRect)
                slotObj.transform.localScale = Vector3.one;
                slotObj.transform.localPosition = Vector3.zero;
            }
        }
    }

    /// <summary>Xử lý khi bấm nút 1 slot skin: mua skin.</summary>
    private void OnSlotClicked(SkinSlotUI slot)
    {
        if (slot == null || UserData.Instance == null) return;
        if (slot.IsOwned) return;   // phòng ngừa: đã sở hữu thì không mua lại

        WeaponSkinData skin = slot.GetSkinData();
        WeaponSO weapon = slot.GetParentWeapon();
        if (skin == null || weapon == null) return;

        if (!UserData.Instance.HasEnoughGold(skin.price))
        {
            return;
        }

        if (!UserData.Instance.BuySkin(weapon.WeaponID, skin.skinID, skin.price))
        {
            Debug.LogWarning("[SkinShop] Mua skin thất bại.");
            return;
        }

        string compositeId = UserData.MakeSkinCompositeId(weapon.WeaponID, skin.skinID);
        if (AdUnlockTracker.Instance != null)
            AdUnlockTracker.Instance.ResetAdWatch(compositeId);

        // Log skin purchased event
        FirebaseAnalyticsHelper.LogSkinPurchased(weapon.WeaponID, skin.skinID, skin.tier.ToString(), skin.price);
        FirebaseAnalyticsHelper.LogGoldSpent(skin.price, "skin", skin.skinID, UserData.Instance.Gold);

        // Mua thành công → cập nhật ngay UI của slot này
        slot.SetOwned(true);
    }

    /// <summary>Xử lý khi bấm nút xem quảng cáo để mở khóa skin.</summary>
    private void OnAdSlotClicked(SkinSlotUI slot)
    {
        if (slot == null || UserData.Instance == null) return;
        if (slot.IsOwned) return;

        WeaponSkinData skin = slot.GetSkinData();
        WeaponSO weapon = slot.GetParentWeapon();
        if (skin == null || weapon == null) return;

        string compositeId = UserData.MakeSkinCompositeId(weapon.WeaponID, skin.skinID);
        int required = FirebaseRemoteConfigHelper.Instance != null ? FirebaseRemoteConfigHelper.Instance.SkinAdWatchCount : 2;

        AdManager ads = AdManager.Instance;
        if (ads != null)
        {
            ads.ShowUnlockRewardedAd("skin_unlock", () =>
            {
                if (AdUnlockTracker.Instance != null)
                {
                    AdUnlockTracker.Instance.RecordAdWatch(compositeId);
                    int current = AdUnlockTracker.Instance.GetAdWatchCount(compositeId);

                    if (current >= required)
                    {
                        UserData.Instance.UnlockSkin(weapon.WeaponID, skin.skinID);
                        AdUnlockTracker.Instance.ResetAdWatch(compositeId);
                        FirebaseAnalyticsHelper.LogSkinUnlockedByAd(weapon.WeaponID, skin.skinID, current);

                        slot.SetOwned(true);
                    }
                    else
                    {
                        slot.UpdateAdButtonVisual();
                    }
                }
            });
        }
        else
        {
            Debug.LogWarning("[SkinShop] AdManager not found.");
        }
    }
}