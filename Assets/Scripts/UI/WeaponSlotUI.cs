using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject selectedFrameObj;
    [SerializeField] private Button slotButton;

    private WeaponSO weaponData;
    private bool isUnlocked;

    public WeaponSO GetWeaponData() => weaponData;

    public void Setup(WeaponSO data, bool unlocked, System.Action<WeaponSO> onClickCallback)
    {
        weaponData = data;
        isUnlocked = unlocked;
        ApplyVisual();
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => onClickCallback?.Invoke(data));
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (weaponData == null) return;
        if (weaponIcon != null) weaponIcon.sprite = weaponData.WeaponIcon;
        if (weaponNameText != null) weaponNameText.text = weaponData.WeaponName;
        if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrameObj != null) selectedFrameObj.SetActive(selected);
    }
}
