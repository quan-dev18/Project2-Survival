using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class UISlideTween : MonoBehaviour
{
    [Header("Slide Settings")]
    [SerializeField] private Vector2 slideOffset = new Vector2(0f, -1000f);
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private Ease ease = Ease.OutBack;

    [Header("Target")]
    [Tooltip("RectTransform cần slide (nếu trống thì slide chính GameObject này)")]
    [SerializeField] private RectTransform target;

    [Header("Fade")]
    [SerializeField] private bool useFade = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = target != null ? target : GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        PlaySlideIn();
    }

    public void PlaySlideIn()
    {
        rectTransform.DOKill();

        rectTransform.anchoredPosition = originalPosition + slideOffset;

        if (useFade && canvasGroup != null)
            canvasGroup.alpha = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Join(rectTransform.DOAnchorPos(originalPosition, duration).SetEase(ease));

        if (useFade && canvasGroup != null)
            seq.Join(canvasGroup.DOFade(1f, duration));

        seq.SetUpdate(true);
    }

    public void Hide()
    {
        rectTransform.DOKill();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(rectTransform.DOAnchorPos(originalPosition + slideOffset, duration).SetEase(ease));

        if (useFade && canvasGroup != null)
            seq.Join(canvasGroup.DOFade(0f, duration));

        seq.SetUpdate(true);
        seq.OnComplete(() => gameObject.SetActive(false));
    }
}
