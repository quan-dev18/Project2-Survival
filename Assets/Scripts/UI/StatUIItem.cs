using TMPro;
using UnityEngine;

/// <summary>
/// 1 dòng chỉ số trong ScrollView (Pause Menu).
/// Hỗ trợ 2 kiểu Prefab:
///  - Có 2 text rời: NameText (tên) + ValueText (giá trị).
///  - Chỉ có 1 text duy nhất: hiển thị dạng "Tên: Giá trị".
/// </summary>
public class StatUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    private void Awake()
    {
        // Tự tìm nếu chưa gán trong Inspector (tránh null)
        if (nameText == null)
        {
            // Ưu tiên text ở gốc (trường hợp prefab chỉ có 1 text)
            nameText = GetComponent<TMP_Text>();
            if (nameText == null)
            {
                Transform child = transform.Find("NameText");
                if (child != null) nameText = child.GetComponent<TMP_Text>();
            }
        }
        if (valueText == null)
        {
            Transform child = transform.Find("ValueText");
            if (child != null) valueText = child.GetComponent<TMP_Text>();
        }
    }

    /// <summary>Cập nhật nội dung dòng chỉ số.</summary>
    public void SetData(string name, string value)
    {
        if (nameText != null && valueText != null)
        {
            // Kiểu 2 text rời
            nameText.text = name;
            valueText.text = value;
        }
        else if (nameText != null)
        {
            // Kiểu 1 text duy nhất: "Tên: Giá trị" (giá trị rỗng -> chỉ in tên)
            nameText.text = string.IsNullOrEmpty(value) ? name : name + ": " + value;
        }
        else if (valueText != null)
        {
            valueText.text = string.IsNullOrEmpty(name) ? value : name + ": " + value;
        }
    }

    /// <summary>Dùng cho dòng tiêu đề (header): xóa luôn text giá trị, chỉ còn lại text tên và tô màu riêng.</summary>
    public void SetTitle(string title, Color color)
    {
        // Header không cần giá trị -> xóa hẳn GameObject ValueText
        if (valueText != null)
        {
            Destroy(valueText.gameObject);
            valueText = null;
        }

        if (nameText != null)
        {
            nameText.text = title;
            nameText.color = color;
        }
    }
}