using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 dòng upgrade đang có (dùng cho danh sách ở ScrollView).
/// Gắn lên Prefab gồm: IconImage (tùy chọn) + NameText + DescText.
/// Size của dòng được PauseMenuManager đặt theo tier (cao nhất = 1, thấp hơn nhỏ dần).
/// </summary>
public class UpgradeUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Image iconImage;

    private RectTransform _rect;

    public RectTransform Rect => _rect != null ? _rect : (_rect = GetComponent<RectTransform>());

    private void Awake()
    {
        // Tự tìm con theo tên nếu chưa gán trong Inspector (tránh null)
        if (nameText == null)
        {
            Transform child = transform.Find("NameText");
            if (child != null) nameText = child.GetComponent<TMP_Text>();
        }
        if (descText == null)
        {
            Transform child = transform.Find("DescText");
            if (child != null) descText = child.GetComponent<TMP_Text>();
        }
        if (iconImage == null)
        {
            Transform child = transform.Find("Icon");
            if (child != null) iconImage = child.GetComponent<Image>();
        }
    }

    public void SetData(UpgradeSO upgrade)
    {
        if (upgrade == null) return;

        if (nameText != null) nameText.text = upgrade.UpgradeName;
        if (descText != null) descText.text = upgrade.Description;
        if (iconImage != null && upgrade.Icon != null)
            iconImage.sprite = upgrade.Icon;
    }
}