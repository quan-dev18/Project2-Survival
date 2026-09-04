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

    [Header("Outside Equipment UI")]
    [SerializeField] private Image outsideWeaponIcon; 

    [Header("Tween Animation")]
    [SerializeField] private UISlideTween slideTween;

    private WeaponSO currentSelectedWeapon;
    private List<WeaponSlotUI> allSlots = new List<WeaponSlotUI>();
    private int selectedIndex = -1;

    private void Start()
    {
        RestoreSelectedWeaponIcon();

        GenerateListUI();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnConfirmSelect);
        }
    }

    // Khôi phục Icon vũ khí đã chọn lên màn hình Equipment khi mở panel
    private void RestoreSelectedWeaponIcon()
    {
        int savedIndex = PlayerPrefs.GetInt("SelectedWeaponIndex", PlayerEquipment.SelectedWeaponIndex);
        if (savedIndex >= 0 && savedIndex < weaponList.Count && weaponList[savedIndex] != null)
        {
            if (outsideWeaponIcon != null) outsideWeaponIcon.sprite = weaponList[savedIndex].WeaponIcon;
        }
    }

    private int GetSavedSelectedIndex()
    {
        return PlayerPrefs.GetInt("SelectedWeaponIndex", PlayerEquipment.SelectedWeaponIndex);
    }

    private void GenerateListUI()
    {
        foreach (Transform container in slotContainer)
        {
            foreach (Transform child in container) Destroy(child.gameObject);
        }

        if (weaponList.Count == 0) return;

        int savedIndex = GetSavedSelectedIndex();

        // Chọn sẵn vũ khí đã lưu để dùng trong gameplay khi mở panel
        if (savedIndex >= 0 && savedIndex < weaponList.Count && weaponList[savedIndex] != null)
        {
            currentSelectedWeapon = weaponList[savedIndex];
            selectedIndex = savedIndex;
        }

        allSlots.Clear();

        foreach (Transform container in slotContainer)
        {
            if (container == null) continue;

            for (int i = 0; i < weaponList.Count; i++)
            {
                WeaponSO weapon = weaponList[i];
                if (weapon == null) continue;

                GameObject slotObj = Instantiate(weaponSlotPrefab, container);
                WeaponSlotUI slotScript = slotObj.GetComponent<WeaponSlotUI>();

                if (slotScript != null)
                {
                    slotScript.Setup(weapon, OnSelectWeapon);
                    allSlots.Add(slotScript);
                }

                slotObj.transform.localScale = Vector3.one;
                slotObj.transform.localPosition = Vector3.zero;
            }
        }

        // Cập nhật khung cho tất cả slot dựa trên vũ khí đã lưu (mọi container đồng bộ)
        RefreshAllFrames();

        // Cập nhật trạng thái nút bấm
        if (currentSelectedWeapon != null)
        {
            bool isAlreadySelected = (selectedIndex == GetSavedSelectedIndex());

            if (selectButton != null) selectButton.interactable = true;
            if (selectButtonText != null)
                selectButtonText.text = isAlreadySelected ? "Đã chọn" : "Chọn";
        }
    }

    // Đồng bộ khung: tắt (chọn) vũ khí ở selectedIndex, bật tất cả còn lại — trên mọi container
    private void RefreshAllFrames()
    {
        foreach (WeaponSlotUI slot in allSlots)
        {
            if (slot == null) continue;

            WeaponSO weapon = slot.GetWeaponData();
            int index = weapon != null ? weaponList.IndexOf(weapon) : -1;
            bool isSelected = (index == selectedIndex);

            slot.SetSelected(isSelected);
        }
    }

    private void OnSelectWeapon(WeaponSO data)
    {
        currentSelectedWeapon = data;
        selectedIndex = weaponList.IndexOf(data);

        // Đồng bộ khung trên mọi container cho vũ khí được chọn
        RefreshAllFrames();

        // Cập nhật trạng thái nút bấm
        if (data.IsUnlocked)
        {
            int weaponIndex = weaponList.IndexOf(data);
            bool isAlreadySelected = (weaponIndex == PlayerEquipment.SelectedWeaponIndex);

            selectButton.interactable = !isAlreadySelected;
            if (selectButtonText != null)
                selectButtonText.text = isAlreadySelected ? "Đã chọn" : "Chọn";
        }
        else
        {
            selectButton.interactable = false;
            if (selectButtonText != null) selectButtonText.text = "Đã khóa";
        }
    }

    private void OnConfirmSelect()
    {
        if (currentSelectedWeapon != null && currentSelectedWeapon.IsUnlocked)
        {
            if (outsideWeaponIcon != null)
            {
                outsideWeaponIcon.sprite = currentSelectedWeapon.WeaponIcon;
            }

            // Hiển thị trạng thái "Đã chọn" và chặn bấm lại
            if (selectButton != null) selectButton.interactable = false;
            if (selectButtonText != null) selectButtonText.text = "Đã chọn";

            int weaponIndex = weaponList.IndexOf(currentSelectedWeapon);
            PlayerEquipment.SelectedWeaponIndex = weaponIndex;
            PlayerPrefs.SetInt("SelectedWeaponIndex", weaponIndex);
            PlayerPrefs.SetString("SelectedWeaponName", currentSelectedWeapon.name);
            PlayerPrefs.Save();

            Debug.Log("Đã chọn vũ khí: " + currentSelectedWeapon.WeaponName);

            // Đóng panel với hiệu ứng
            if (slideTween != null)
                slideTween.Hide();
            else
                gameObject.SetActive(false);
        }
    }

    public void ClosePanel()
    {
        if (slideTween != null)
            slideTween.Hide();
        else
            gameObject.SetActive(false);
    }
}