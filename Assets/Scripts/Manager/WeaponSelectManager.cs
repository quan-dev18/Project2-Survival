using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<WeaponSO> weaponList;

    [Header("UI Containers")]
    [SerializeField] private Transform slotContainer; 
    [SerializeField] private GameObject weaponSlotPrefab; 

    [Header("Select Button")]
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI selectButtonText;

    [Header("Outside Equipment UI")]
    [SerializeField] private Image outsideWeaponIcon; 

    private WeaponSO currentSelectedWeapon;
    private WeaponSlotUI currentSelectedSlot;

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

    private void GenerateListUI()
    {
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        foreach (var weapon in weaponList)
        {
            if (weapon == null) continue;

            GameObject slotObj = Instantiate(weaponSlotPrefab, slotContainer);
            WeaponSlotUI slotScript = slotObj.GetComponent<WeaponSlotUI>();
            
            if (slotScript != null)
            {
                slotScript.Setup(weapon, OnSelectWeapon);
                if (weapon == weaponList[0]) currentSelectedSlot = slotScript;
            }

            slotObj.transform.localScale = Vector3.one;
            slotObj.transform.localPosition = Vector3.zero;
        }

        if (weaponList.Count > 0 && weaponList[0] != null) OnSelectWeapon(weaponList[0]);
    }

    private void OnSelectWeapon(WeaponSO data)
    {
        currentSelectedWeapon = data;

        // Tìm slot tương ứng và cập nhật selectedFrame
        if (currentSelectedSlot != null) currentSelectedSlot.SetSelected(false);
        int index = weaponList.IndexOf(data);
        if (index >= 0)
        {
            Transform slotTransform = slotContainer.GetChild(index);
            WeaponSlotUI slotScript = slotTransform.GetComponent<WeaponSlotUI>();
            if (slotScript != null)
            {
                currentSelectedSlot = slotScript;
                if (data.IsUnlocked) slotScript.SetSelected(true);
            }
        }

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

            gameObject.SetActive(false);
        }
    }
}