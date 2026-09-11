using UnityEngine;
using UnityEngine.UI;

public class HeroSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image heroIconImage;
    [SerializeField] private GameObject lockIconObj;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedFrameObj;

    private HeroSelectSO heroData;
    private bool isUnlocked;

    public HeroSelectSO GetHeroData() => heroData;

    public void Setup(HeroSelectSO data, bool unlocked, System.Action<HeroSelectSO> onClickCallback)
    {
        heroData = data;
        isUnlocked = unlocked;
        ApplyVisual();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(data));
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (heroData == null) return;
        if (isUnlocked)
        {
            if (heroIconImage != null)
            {
                heroIconImage.gameObject.SetActive(true);
                heroIconImage.sprite = heroData.heroIcon;
            }
            if (lockIconObj != null) lockIconObj.SetActive(false);
        }
        else
        {
            if (heroIconImage != null) heroIconImage.gameObject.SetActive(false);
            if (lockIconObj != null) lockIconObj.SetActive(true);
        }
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

    public void SetSelected(bool selected)
    {
        if (selectedFrameObj != null) selectedFrameObj.SetActive(selected);
    }
}
