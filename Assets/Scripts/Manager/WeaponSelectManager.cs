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

    [Header("Gun Showcase Popup")]
    [Tooltip("Pop-up hiển thị chi tiết thông số súng. Hiện lên ngay khi chọn 1 súng (OnSelectWeapon).")]
    [SerializeField] private GunShowcaseUI gunShowcase;

    [Header("Tween Animation")]
    [SerializeField] private UISlideTween slideTween;

    private WeaponSO currentSelectedWeapon;
    private List<WeaponSlotUI> allSlots = new List<WeaponSlotUI>();
    private int selectedIndex = -1;
    private Transform lastClickedContainer;
    private Dictionary<Transform, int> containerSelectedIndex = new Dictionary<Transform, int>();

    private void OnEnable()
    {
        if (UserData.Instance != null)
        {
            UserData.Instance.InitWeaponDefaults(weaponList);
            PlayerEquipment.SelectedWeaponIndex = UserData.Instance.SelectedWeaponIndex;
        }

        RestoreSelectedWeaponIcon();
        GenerateListUI();
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

    private void RestoreSelectedWeaponIcon()
    {
        int savedIndex = UserData.Instance != null ? UserData.Instance.SelectedWeaponIndex : 0;
        if (savedIndex >= 0 && savedIndex < weaponList.Count && weaponList[savedIndex] != null)
        {
            if (outsideWeaponIcon != null) outsideWeaponIcon.sprite = weaponList[savedIndex].WeaponIcon;
        }
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
            OnSelectWeapon(weaponList[savedIndex]);
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
        if (gunShowcase != null)
            gunShowcase.Show(data, unlocked);
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
                outsideWeaponIcon.sprite = currentSelectedWeapon.WeaponIcon;

            // Sau khi mua thành công: mở lại pop-up để hiện chỉ số thật (thay cho "?").
            if (gunShowcase != null)
                gunShowcase.Show(currentSelectedWeapon, true);

            UserData.Instance.SelectedWeaponIndex = weaponIndex;
            PlayerEquipment.SelectedWeaponIndex = weaponIndex;
            return;
        }

        if (outsideWeaponIcon != null)
            outsideWeaponIcon.sprite = currentSelectedWeapon.WeaponIcon;

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
