using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HeroSelectManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<HeroSelectSO> heroList;

    [Header("UI Containers")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Preview Parent")]
    [SerializeField] private Transform previewParent;

    [Header("Select Button")]
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI selectButtonText;

    [Header("Outside Equipment UI")]
    [SerializeField] private Image outsideHeroIcon;

    [Header("Tween Animation")]
    [SerializeField] private UISlideTween slideTween;

    private GameObject currentPreviewInstance;
    private HeroSelectSO currentSelectedHero;
    private HeroSlotUI currentSelectedSlot;

    private void OnEnable()
    {
        if (PlayerPrefs.HasKey("SelectedHeroIndex"))
            PlayerEquipment.SelectedHeroIndex = PlayerPrefs.GetInt("SelectedHeroIndex");

        RestoreSelectedHeroIcon();
        GenerateListUI();
    }

    private void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnConfirmSelect);
        }
    }

    private void RestoreSelectedHeroIcon()
    {
        int savedIndex = PlayerPrefs.GetInt("SelectedHeroIndex", PlayerEquipment.SelectedHeroIndex);
        if (savedIndex >= 0 && savedIndex < heroList.Count && heroList[savedIndex] != null)
        {
            if (outsideHeroIcon != null) outsideHeroIcon.sprite = heroList[savedIndex].heroIcon;
        }
    }

    private void GenerateListUI()
    {
        foreach (Transform child in slotContainer) DestroyImmediate(child.gameObject);

        int savedIndex = PlayerPrefs.GetInt("SelectedHeroIndex", PlayerEquipment.SelectedHeroIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, heroList.Count - 1));

        currentSelectedHero = null;
        currentSelectedSlot = null;

        for (int i = 0; i < heroList.Count; i++)
        {
            var hero = heroList[i];
            if (hero == null) continue;

            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            HeroSlotUI slotScript = slotObj.GetComponent<HeroSlotUI>();
            if (slotScript != null)
            {
                slotScript.Setup(hero, OnSelectHero);

                bool isSavedSelected = (i == savedIndex);
                slotScript.SetSelected(isSavedSelected);
                slotScript.SetIconAlpha(isSavedSelected ? 0.45f : 1f);

                if (isSavedSelected) currentSelectedSlot = slotScript;
            }

            slotObj.transform.localScale = Vector3.one;
            slotObj.transform.localPosition = Vector3.zero;
        }

        ResetToSavedHero();
    }

    // Hàm trả giao diện về nhân vật đã lưu gần nhất
    public void ResetToSavedHero()
    {
        int savedIndex = PlayerPrefs.GetInt("SelectedHeroIndex", PlayerEquipment.SelectedHeroIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, heroList.Count - 1));

        if (savedIndex >= 0 && savedIndex < heroList.Count && heroList[savedIndex] != null)
        {
            OnSelectHero(heroList[savedIndex]);
        }
    }

    private void OnSelectHero(HeroSelectSO data)
    {
        currentSelectedHero = data;

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
            currentSelectedSlot.SetIconAlpha(1f);
        }

        int index = heroList.IndexOf(data);
        if (index >= 0 && index < slotContainer.childCount)
        {
            Transform slotTransform = slotContainer.GetChild(index);
            HeroSlotUI slotScript = slotTransform.GetComponent<HeroSlotUI>();
            if (slotScript != null)
            {
                currentSelectedSlot = slotScript;
                if (data.isUnlocked)
                {
                    slotScript.SetSelected(true);
                    slotScript.SetIconAlpha(0.69f);
                }
            }
        }

        foreach (Transform child in previewParent) DestroyImmediate(child.gameObject);
        if (data.previewPrefab != null)
        {
            currentPreviewInstance = Instantiate(data.previewPrefab, previewParent);
        }

        if (data.isUnlocked)
        {
            int heroIndex = heroList.IndexOf(data);
            bool isAlreadySelected = (heroIndex == PlayerEquipment.SelectedHeroIndex);

            selectButton.interactable = !isAlreadySelected;
            if (selectButtonText != null)
                selectButtonText.text = isAlreadySelected ? "Đã chọn" : "Chọn";
        }
        else
        {
            selectButton.interactable = false;
            if (selectButtonText != null) selectButtonText.text = "Đã khóa";
        }
    }

    private void OnConfirmSelect()
    {
        if (currentSelectedHero != null && currentSelectedHero.isUnlocked)
        {
            if (outsideHeroIcon != null)
                outsideHeroIcon.sprite = currentSelectedHero.heroIcon;

            if (selectButton != null) selectButton.interactable = false;
            if (selectButtonText != null) selectButtonText.text = "Đã chọn";

            int heroIndex = heroList.IndexOf(currentSelectedHero);
            PlayerEquipment.SelectedHeroIndex = heroIndex;
            PlayerPrefs.SetInt("SelectedHeroIndex", heroIndex);
            PlayerPrefs.Save();

            Debug.Log("Đã chọn nhân vật: " + currentSelectedHero.name);
        }
    }

    public void ClosePanel()
    {
        // Khôi phục lại nhân vật đã lưu trước khi đóng Panel
        ResetToSavedHero();

        if (slideTween != null)
            slideTween.Hide();
        else
            gameObject.SetActive(false);
    }
}