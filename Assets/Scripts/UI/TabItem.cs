using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class TabItem : MonoBehaviour
{
    [Header("Nội dung Panel")]
    public GameObject tabContent; // Panel hiển thị tương ứng (Tab_Shop, Tab_Heroes,...)

    [Header("Trạng thái UI")]
    public GameObject unselectState;
    public GameObject selectedState;

    [Header("Hiệu ứng Scale")]
    public bool animateScale = true;
    public Vector3 selectedScale = new Vector3(1f, 1.2f, 1f);
    public Vector3 normalScale = Vector3.one;
    [SerializeField] private Ease selectedScaleEase = Ease.OutBack;
    [SerializeField] private Ease deselectScaleEase = Ease.InBack;

    [Header("Slide Animation")]
    [SerializeField] private Vector2 slideOffset = new Vector2(300f, 0f);
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private Ease slideEase = Ease.OutQuad;

    public Button Button { get; private set; }

    private RectTransform contentRect;
    private Vector2 contentOriginalPos;

    private void Awake()
    {
        Button = GetComponent<Button>();

        if (tabContent != null)
        {
            contentRect = tabContent.GetComponent<RectTransform>();
            if (contentRect != null)
                contentOriginalPos = contentRect.anchoredPosition;
        }
    }

    // Gọi khi Tab được chọn: phóng to selectedState (gợn sóng)
    public void Select(int direction = 1, bool animate = true)
    {
        if (tabContent != null) tabContent.SetActive(true);
        if (unselectState != null) unselectState.SetActive(false);
        if (selectedState != null) selectedState.SetActive(true);

        if (animateScale && selectedState != null)
        {
            selectedState.transform.DOKill();
            if (animate)
            {
                selectedState.transform.localScale = Vector3.zero;
                selectedState.transform.DOScale(selectedScale, slideDuration)
                    .SetEase(selectedScaleEase)
                    .SetUpdate(true);
            }
            else
            {
                selectedState.transform.localScale = selectedScale;
            }
        }

        if (animate && contentRect != null)
        {
            contentRect.DOKill();
            contentRect.anchoredPosition = contentOriginalPos + slideOffset * direction;
            contentRect.DOAnchorPos(contentOriginalPos, slideDuration).SetEase(slideEase).SetUpdate(true);
        }
    }

    // Gọi khi Tab bị bỏ chọn: thu nhỏ selectedState rồi mới ẩn
    public void Deselect(bool animate = true)
    {
        if (unselectState != null) unselectState.SetActive(true);
        if (tabContent != null) tabContent.SetActive(false);

        if (animateScale) unselectState.transform.localScale = normalScale;

        if (selectedState != null)
        {
            selectedState.transform.DOKill();
            if (animate && animateScale)
            {
                selectedState.transform.DOScale(Vector3.zero, slideDuration)
                    .SetEase(deselectScaleEase)
                    .SetUpdate(true)
                    .OnComplete(() => selectedState.SetActive(false));
            }
            else
            {
                selectedState.SetActive(false);
            }
        }

        if (contentRect != null) contentRect.DOKill();
    }
}