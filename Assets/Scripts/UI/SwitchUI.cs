using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;
using TMPro;

public class CustomSwitchUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform sliderHandle; // Kéo GameObject 'Slider' vào đây
    [SerializeField] private Image fillImage;           // Kéo GameObject 'Fill' vào đây
    [SerializeField] private TextMeshProUGUI statusText;  // Kéo 'Text (TMP)' vào đây

    [Header("Settings")]
    [Tooltip("Khoảng cách Slider kéo sang phải khi ON (OFF mặc định là 0)")]
    [SerializeField] private float slideDistance = 80f; 
    [SerializeField] private float duration = 0.25f;
    public bool isOn = true;

    [Header("Events")]
    public UnityEvent<bool> onValueChanged;

    private void Start()
    {
        UpdateVisuals(false); 
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SetState(!isOn, true, true);
    }

    public void SetState(bool state, bool useAnimation = true, bool triggerEvent = true)
    {
        isOn = state;
        UpdateVisuals(useAnimation);

        if (triggerEvent)
        {
            onValueChanged?.Invoke(isOn);
        }
    }

    private void UpdateVisuals(bool useAnimation)
    {
        // 1. Cập nhật Text
        if (statusText != null) statusText.text = isOn ? "ON" : "OFF";

        float targetX = isOn ? slideDistance : 0f;

        if (useAnimation)
        {
            if (sliderHandle != null)
            {
                sliderHandle.DOKill();
                
                // Trượt Slider và cập nhật Fill theo từng khung hình
                sliderHandle.DOAnchorPosX(targetX, duration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnUpdate(() => 
                    {
                        // Tính % quãng đường Slider đã đi được để gán vào fillAmount
                        if (fillImage != null && slideDistance > 0)
                        {
                            float currentFill = sliderHandle.anchoredPosition.x / slideDistance;
                            fillImage.fillAmount = Mathf.Clamp01(currentFill);
                        }
                    });
            }
        }
        else
        {
            // Cập nhật tức thì không animation
            if (sliderHandle != null)
            {
                 sliderHandle.anchoredPosition = new Vector2(targetX, sliderHandle.anchoredPosition.y);
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = isOn ? 1f : 0f;
            }
        }
    }
}