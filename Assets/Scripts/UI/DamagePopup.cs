using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private TMP_Text label;

    private Sequence sequence;
    private float totalDamage;
    private int hitCount;
    private PopupType popupType;
    private bool isCrit;
    private bool isHeal;

    public Vector3 SpawnPosition { get; private set; }
    public float SpawnTime { get; private set; }
    public PopupType PopupType => popupType;

    private void Awake()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();
    }

    public void Show(float amount, PopupStyle style, PopupType type, Vector3 spawnPos)
    {
        if (label == null) return;

        popupType = type;
        isCrit = type == PopupType.Crit;
        isHeal = type == PopupType.Heal;
        totalDamage = amount;
        hitCount = 1;
        SpawnPosition = spawnPos;
        SpawnTime = Time.time;

        transform.position = spawnPos;
        UpdateDisplay(style);

        if (sequence != null)
            sequence.Kill();

        PlayAnimation(style);
    }

    public bool CanMerge(Vector3 newPos, float mergeRadius, float mergeWindow, PopupType type)
    {
        if (popupType != type) return false;
        if (isCrit) return false;
        if (type == PopupType.Crit) return false;
        if (type == PopupType.PlayerDamage) return false;

        float sqrDist = (newPos - SpawnPosition).sqrMagnitude;
        float timeSince = Time.time - SpawnTime;

        return sqrDist <= mergeRadius * mergeRadius && timeSince <= mergeWindow;
    }

    public void Merge(float additionalDamage, PopupStyle style)
    {
        totalDamage += additionalDamage;
        hitCount++;

        SpawnPosition = transform.position;
        SpawnTime = Time.time;

        UpdateDisplay(style);

        if (sequence != null)
            sequence.Kill();

        PlayAnimation(style);

        transform.localScale = Vector3.one * 1.2f;
        transform.DOScale(Vector3.one, 0.1f);
    }

    private void UpdateDisplay(PopupStyle style)
    {
        string text = "";
        if (isHeal && !string.IsNullOrEmpty(style.healPrefix))
            text += style.healPrefix;

        text += Mathf.RoundToInt(totalDamage);

        if (hitCount > 1)
            text += $" x {hitCount}";

        if (isCrit && !string.IsNullOrEmpty(style.critSuffix))
            text += style.critSuffix;

        label.text = text;
        label.fontSize = style.fontSize * (isCrit ? style.critFontSizeMultiplier : 1f);
        label.color = style.color;
        label.alpha = 1f;
    }

    private void PlayAnimation(PopupStyle style)
    {
        Vector3 basePos = transform.position;
        float duration = style.lifetime;

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(transform.DOMoveY(basePos.y + style.floatDistance, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true))
            .Join(DOTween.To(() => label.alpha, x => label.alpha = x, 0f, duration)
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

        totalDamage = 0;
        hitCount = 0;

        if (label != null)
            label.alpha = 1f;
    }

    private void OnDisable()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }
    }
}
