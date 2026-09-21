using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WeaponSelectManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<WeaponSO> weaponList;

    [Header("UI Containers")]
    [SerializeField] private List<Transform> slotContainer;
    [SerializeField] private GameObject weaponSlotPrefab;

    [Header("Select Button")]
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI selectButtonText;

    [Header("Gold Cost Display")]
    [SerializeField] private TextMeshProUGUI goldCostText;
    [SerializeField] private GameObject goldCostContainer;

    [Header("Outside Equipment UI")]
    [SerializeField] private Image outsideWeaponIcon;
    [Tooltip("Tên súng hiển thị ở nút 'Thay đổi' bên ngoài (kéo TMP_Text của nút vào).")]
    [SerializeField] private TextMeshProUGUI outsideWeaponNameText;

    [Header("Gun Showcase Popup")]
    [Tooltip("Pop-up hiển thị chi tiết thông số súng. Hiện lên ngay khi chọn 1 súng (OnSelectWeapon).")]
    [SerializeField] private GunShowcaseUI gunShowcase;

    [Header("Tween Animation")]
    [SerializeField] private UISlideTween slideTween;

    private WeaponSO currentSelectedWeapon;

    /// <summary>
    /// Vũ khí đang được chọn. Nếu chưa có (manager chưa chạy OnEnable, hoặc vừa mở panel)
    /// thì fallback về vũ khí đã lưu trong UserData để panel Skin luôn có dữ liệu.
    /// </summary>
    public WeaponSO CurrentWeapon
    {
        get
        {
            if (currentSelectedWeapon != null) return currentSelectedWeapon;

            if (weaponList == null || weaponList.Count == 0) return null;

            int index = UserData.Instance != null ? UserData.Instance.SelectedWeaponIndex : 0;
            index = Mathf.Clamp(index, 0, weaponList.Count - 1);
            return weaponList[index];
        }
    }

    /// <summary>Bắn ra mỗi khi người chơi chọn 1 vũ khí (để panel Skin cập nhật theo).</summary>
    public event System.Action<WeaponSO> OnWeaponSelected;

    private List<WeaponSlotUI> allSlots = new List<WeaponSlotUI>();
    private int selectedIndex = -1;
    private Transform lastClickedContainer;
    private Dictionary<Transform, int> containerSelectedIndex = new Dictionary<Transform, int>();

    private void OnEnable()
    {
        if (gunShowcase != null) gunShowcase.SetWeaponSelect(this);

        if (UserData.Instance != null)
        {
            UserData.Instance.InitWeaponDefaults(weaponList);
            PlayerEquipment.SelectedWeaponIndex = UserData.Instance.SelectedWeaponIndex;
        }

        RestoreSelectedWeaponIcon();
        GenerateListUI();
        RefreshOutsideName();
    }

    private void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnConfirmSelect);
        }
    }

    private bool IsWeaponUnlocked(int index)
    {
        if (UserData.Instance != null)
            return UserData.Instance.IsWeaponUnlocked(index);
        if (index >= 0 && index < weaponList.Count && weaponList[index] != null)
            return weaponList[index].IsUnlocked;
        return false;
    }

    /// <summary>Kiểm tra súng đã mở khoá chưa theo <see cref="WeaponSO"/> (dùng cho Gun Showcase).</summary>
    public bool IsWeaponUnlocked(WeaponSO weapon)
    {
        if (weapon == null) return false;
        int index = weaponList != null ? weaponList.IndexOf(weapon) : -1;
        return IsWeaponUnlocked(index);
    }

    private void RestoreSelectedWeaponIcon()
    {
        int savedIndex = UserData.Instance != null ? UserData.Instance.SelectedWeaponIndex : 0;
        if (savedIndex >= 0 && savedIndex < weaponList.Count && weaponList[savedIndex] != null)
        {
            if (outsideWeaponIcon != null)
                outsideWeaponIcon.sprite = GetEquippedDisplaySprite(weaponList[savedIndex]);
        }
    }

    /// <summary>Sprite hiển thị của súng ở UI ngoài: ưu tiên skin đang trang bị, fallback sprite gốc.</summary>
    public Sprite GetEquippedDisplaySprite(WeaponSO weapon)
    {
        if (weapon == null) return null;

        if (UserData.Instance != null && weapon.SkinList != null)
        {
            string skinId = UserData.Instance.GetEquippedSkinId(weapon.WeaponID);
            if (!string.IsNullOrEmpty(skinId))
            {
                for (int i = 0; i < weapon.SkinList.Count; i++)
                {
                    WeaponSkinData s = weapon.SkinList[i];
                    if (s != null && s.skinID == skinId && s.weaponSprite != null)
                        return s.weaponSprite;
                }
            }
        }

        return weapon.WeaponIcon;
    }

    /// <summary>Đổi ảnh súng hiển thị ở UI ngoài (gọi khi người chơi chọn skin).</summary>
    public void SetOutsideWeaponIcon(Sprite sprite)
    {
        if (outsideWeaponIcon != null && sprite != null)
            outsideWeaponIcon.sprite = sprite;
    }

    // Cập nhật tên súng hiển thị ở nút 'Thay đổi' bên ngoài
    private void RefreshOutsideName()
    {
        if (outsideWeaponNameText == null) return;

        WeaponSO weapon = currentSelectedWeapon;
        if (weapon == null)
        {
            int savedIndex = GetSavedSelectedIndex();
            if (savedIndex >= 0 && savedIndex < weaponList.Count && weaponList[savedIndex] != null)
                weapon = weaponList[savedIndex];
        }

        outsideWeaponNameText.text = weapon != null ? weapon.WeaponName : string.Empty;
    }

    private int GetSavedSelectedIndex()
    {
        return UserData.Instance != null ? UserData.Instance.SelectedWeaponIndex : 0;
    }

    private void GenerateListUI()
    {
        foreach (Transform container in slotContainer)
        {
            foreach (Transform child in container) DestroyImmediate(child.gameObject);
        }

        if (weaponList.Count == 0) return;

        allSlots.Clear();
        containerSelectedIndex.Clear();

        foreach (Transform container in slotContainer)
        {
            if (container == null) continue;
            containerSelectedIndex[container] = -1;

            for (int i = 0; i < weaponList.Count; i++)
            {
                WeaponSO weapon = weaponList[i];
                if (weapon == null) continue;

                bool unlocked = IsWeaponUnlocked(i);

                GameObject slotObj = Instantiate(weaponSlotPrefab, container);
                WeaponSlotUI slotScript = slotObj.GetComponent<WeaponSlotUI>();

                if (slotScript != null)
                {
                    Transform capturedContainer = container;
                    slotScript.Setup(weapon, unlocked, (WeaponSO data) =>
                    {
                        lastClickedContainer = capturedContainer;
                        OnSelectWeapon(data);
                    });
                    allSlots.Add(slotScript);
                }

                slotObj.transform.localScale = Vector3.one;
                slotObj.transform.localPosition = Vector3.zero;
            }
        }

        ResetToSavedWeapon();
    }

    public void ResetToSavedWeapon()
    {
        if (weaponList.Count == 0) return;

        int savedIndex = GetSavedSelectedIndex();
        savedIndex = Mathf.Clamp(savedIndex, 0, weaponList.Count - 1);

        if (savedIndex >= 0 && savedIndex < weaponList.Count && weaponList[savedIndex] != null)
        {
            lastClickedContainer = null;
            OnSelectWeapon(weaponList[savedIndex], false);
        }
    }

    private void RefreshAllFrames()
    {
        foreach (WeaponSlotUI slot in allSlots)
        {
            if (slot == null) continue;
            WeaponSO weapon = slot.GetWeaponData();
            int index = weapon != null ? weaponList.IndexOf(weapon) : -1;
            slot.SetSelected(index == selectedIndex);
        }
    }

    private void RefreshContainerFrames(Transform container)
    {
        if (!containerSelectedIndex.ContainsKey(container)) return;
        int selectedIdx = containerSelectedIndex[container];

        foreach (WeaponSlotUI slot in allSlots)
        {
            if (slot == null) continue;
            if (slot.transform.parent != container) continue;
            WeaponSO weapon = slot.GetWeaponData();
            int index = weapon != null ? weaponList.IndexOf(weapon) : -1;
            slot.SetSelected(index == selectedIdx);
        }
    }

    private void OnSelectWeapon(WeaponSO data)
    {
        OnSelectWeapon(data, true);
    }

    /// <param name="showShowcase">
    /// Có hiện pop-up chi tiết súng không. Khi mở panel / khôi phục súng đã lưu thì truyền
    /// <c>false</c> để pop-up không tự bật lên khi người chơi chưa bấm chọn gì.
    /// </param>
    private void OnSelectWeapon(WeaponSO data, bool showShowcase)
    {
        currentSelectedWeapon = data;
        selectedIndex = weaponList.IndexOf(data);

        if (lastClickedContainer != null)
        {
            containerSelectedIndex[lastClickedContainer] = selectedIndex;
            RefreshContainerFrames(lastClickedContainer);
        }
        else
        {
            foreach (Transform container in slotContainer)
                containerSelectedIndex[container] = selectedIndex;
            RefreshAllFrames();
        }

        bool unlocked = IsWeaponUnlocked(selectedIndex);
        if (unlocked)
        {
            bool isAlreadySelected = UserData.Instance != null && (selectedIndex == UserData.Instance.SelectedWeaponIndex);

            selectButton.interactable = !isAlreadySelected;
            if (selectButtonText != null)
                selectButtonText.text = isAlreadySelected ? "Đã chọn" : "Chọn";

            if (goldCostContainer != null)
                goldCostContainer.SetActive(false);
        }
        else
        {
            selectButton.interactable = UserData.Instance != null && UserData.Instance.HasEnoughGold(data.GoldCost);
            if (selectButtonText != null)
                selectButtonText.text = $"Mua {FormatHelper.FormatGold(data.GoldCost)}";

            if (goldCostContainer != null)
                goldCostContainer.SetActive(true);
            if (goldCostText != null)
                goldCostText.text = FormatHelper.FormatGold(data.GoldCost);
        }

        // Hiện pop-up chi tiết súng ngay sau khi chọn 1 súng.
        // Súng LOCKED sẽ hiện toàn bộ stat dưới dạng "?".
        // Bỏ qua khi chỉ khôi phục súng đã lưu lúc mở panel.
        if (showShowcase && gunShowcase != null)
            gunShowcase.Show(data, unlocked);

        RefreshOutsideName();

        OnWeaponSelected?.Invoke(data);
    }

    private void OnConfirmSelect()
    {
        if (currentSelectedWeapon == null || UserData.Instance == null) return;

        int weaponIndex = weaponList.IndexOf(currentSelectedWeapon);
        bool unlocked = IsWeaponUnlocked(weaponIndex);

        if (!unlocked)
        {
            if (!UserData.Instance.HasEnoughGold(currentSelectedWeapon.GoldCost))
                return;

            UserData.Instance.UnlockWeapon(weaponIndex, currentSelectedWeapon.GoldCost);

            // Log weapon unlocked event
            FirebaseAnalyticsHelper.LogWeaponUnlocked(currentSelectedWeapon.WeaponName, currentSelectedWeapon.WeaponName, currentSelectedWeapon.GoldCost);
            FirebaseAnalyticsHelper.LogGoldSpent(currentSelectedWeapon.GoldCost, "weapon", currentSelectedWeapon.WeaponName, UserData.Instance.Gold);

            foreach (WeaponSlotUI slot in allSlots)
            {
                if (slot != null && slot.GetWeaponData() == currentSelectedWeapon)
                {
                    slot.SetUnlocked(true);
                    break;
                }
            }

            if (selectButtonText != null)
                selectButtonText.text = "Đã chọn";
            selectButton.interactable = false;

            if (goldCostContainer != null)
                goldCostContainer.SetActive(false);

            if (outsideWeaponIcon != null)
                outsideWeaponIcon.sprite = GetEquippedDisplaySprite(currentSelectedWeapon);

            // Sau khi mua thành công: mở lại pop-up để hiện chỉ số thật (thay cho "?").
            if (gunShowcase != null)
                gunShowcase.Show(currentSelectedWeapon, true);

            UserData.Instance.SelectedWeaponIndex = weaponIndex;
            PlayerEquipment.SelectedWeaponIndex = weaponIndex;
            return;
        }

        if (outsideWeaponIcon != null)
            outsideWeaponIcon.sprite = GetEquippedDisplaySprite(currentSelectedWeapon);

        if (selectButton != null) selectButton.interactable = false;
        if (selectButtonText != null) selectButtonText.text = "Đã chọn";

        UserData.Instance.SelectedWeaponIndex = weaponIndex;
        PlayerEquipment.SelectedWeaponIndex = weaponIndex;

        UserData.Instance.Save();

        if (slideTween != null)
            slideTween.Hide();
        else
            gameObject.SetActive(false);
    }

    public void ClosePanel()
    {
        ResetToSavedWeapon();

        if (slideTween != null)
            slideTween.Hide();
        else
            gameObject.SetActive(false);
    }
}
