using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText; // Text hiển thị tên/title của từng súng
    [SerializeField] private GameObject lockOverlay; 
    [SerializeField] private GameObject selectedFrameObj; // GameObject khung viền khi được chọn
    [SerializeField] private Button slotButton;

    public void Setup(WeaponSO data, System.Action<WeaponSO> onClickCallback)
    {
        if (data != null)
        {
            // Cập nhật Icon súng
            if (weaponIcon != null) weaponIcon.sprite = data.WeaponIcon;

            // Cập nhật Title/Tên súng ngay trên ô slot
            if (weaponNameText != null) weaponNameText.text = data.WeaponName;

            // Bật/tắt ổ khóa
            if (lockOverlay != null) lockOverlay.SetActive(!data.IsUnlocked);
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => onClickCallback?.Invoke(data));
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrameObj != null) selectedFrameObj.SetActive(selected);
    }
}