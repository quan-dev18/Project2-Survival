using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private TMP_Text label;

    private Sequence sequence;

    private void Awake()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();
    }

    public void Show(float amount, PopupStyle style, bool isCrit, bool isHeal)
    {
        if (label == null) return;

        Vector3 basePosition = transform.position;

        string text = "";
        if (isHeal && !string.IsNullOrEmpty(style.healPrefix))
            text += style.healPrefix;
        text += Mathf.RoundToInt(amount);
        if (isCrit && !string.IsNullOrEmpty(style.critSuffix))
            text += style.critSuffix;

        label.text = text;
        label.fontSize = style.fontSize * (isCrit ? style.critFontSizeMultiplier : 1f);
        label.color = style.color;
        label.alpha = 1f;

        if (sequence != null)
            sequence.Kill();

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(transform.DOMoveY(basePosition.y + style.floatDistance, style.lifetime)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true))
            .Join(DOTween.To(() => label.alpha, x => label.alpha = x, 0f, style.lifetime)
                .SetEase(Ease.InQuad)
                .SetUpdate(true))
            .OnComplete(() =>
            {
                if (ObjectPooling.Instance != null)
                    ObjectPooling.Instance.Despawn(gameObject);
                else
                    Destroy(gameObject);
            });
    }

    public void OnSpawned()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }

        if (label != null)
            label.alpha = 1f;
    }
}