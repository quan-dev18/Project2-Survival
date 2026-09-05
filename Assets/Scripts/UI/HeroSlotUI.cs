using UnityEngine;
using UnityEngine.UI;

public class HeroSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image heroIconImage;    // Component Image chứa Avatar nhân vật
    [SerializeField] private GameObject lockIconObj; // GameObject Icon ổ khóa
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedFrameObj; // GameObject khung viền khi được chọn

    private HeroSelectSO heroData;

    public HeroSelectSO GetHeroData()
    {
        return heroData;
    }

    public void SetIconAlpha(float alpha)
    {
        if (heroIconImage != null)
        {
            Transform parent = heroIconImage.transform.parent;
            Image parentImage = parent != null ? parent.GetComponent<Image>() : null;
            if (parentImage != null)
            {
                Color c = Color.black;
                c.a = alpha;
                parentImage.color = c;
            }
        }
    }

    public void Setup(HeroSelectSO data, System.Action<HeroSelectSO> onClickCallback)
    {
        heroData = data;

        // Xử lý bật/tắt Icon theo trạng thái Mở Khóa
        if (data.isUnlocked)
        {
            // MỞ KHÓA: Hiện Icon nhân vật, Bật màu sáng, Ẩn icon khóa
            if (heroIconImage != null)
            {
                heroIconImage.gameObject.SetActive(true);
                heroIconImage.sprite = data.heroIcon;
            }
            if (lockIconObj != null) lockIconObj.SetActive(false);
            if (selectedFrameObj != null) selectedFrameObj.SetActive(false);
        }
        else
        {
            // BỊ KHÓA: Ẩn Icon nhân vật, Hiện icon Ổ khóa
            if (heroIconImage != null) heroIconImage.gameObject.SetActive(false);
            if (lockIconObj != null) lockIconObj.SetActive(true);
        }

        // Gán sự kiện khi bấm nút
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(data));
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrameObj != null) selectedFrameObj.SetActive(selected);
    }
}