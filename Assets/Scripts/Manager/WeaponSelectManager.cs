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

    private void Start()
    {
        GenerateListUI();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnConfirmSelect);
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
            }

            slotObj.transform.localScale = Vector3.one;
            slotObj.transform.localPosition = Vector3.zero;
        }

        if (weaponList.Count > 0 && weaponList[0] != null) OnSelectWeapon(weaponList[0]);
    }

    private void OnSelectWeapon(WeaponSO data)
    {
        currentSelectedWeapon = data;

        // Cập nhật trạng thái nút bấm
        if (data.IsUnlocked)
        {
            selectButton.interactable = true;
            if (selectButtonText != null) selectButtonText.text = "Chọn";
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
                selectButtonText.text = "Đã chọn";
            }

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