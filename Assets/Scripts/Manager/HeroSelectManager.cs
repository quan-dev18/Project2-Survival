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

    [Header("Gold Cost Display")]
    [SerializeField] private TextMeshProUGUI goldCostText;
    [SerializeField] private GameObject goldCostContainer;

    [Header("Outside Equipment UI")]
    [SerializeField] private Image outsideHeroIcon;

    [Header("Tween Animation")]
    [SerializeField] private UISlideTween slideTween;

    private GameObject currentPreviewInstance;
    private HeroSelectSO currentSelectedHero;
    private HeroSlotUI currentSelectedSlot;

    private void OnEnable()
    {
        if (UserData.Instance != null)
        {
            UserData.Instance.InitHeroDefaults(heroList);
            PlayerEquipment.SelectedHeroIndex = UserData.Instance.SelectedHeroIndex;
        }

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

    private bool IsHeroUnlocked(int index)
    {
        if (UserData.Instance != null)
            return UserData.Instance.IsHeroUnlocked(index);
        if (index >= 0 && index < heroList.Count && heroList[index] != null)
            return heroList[index].isUnlocked;
        return false;
    }

    private void RestoreSelectedHeroIcon()
    {
        int savedIndex = UserData.Instance != null ? UserData.Instance.SelectedHeroIndex : 0;
        if (savedIndex >= 0 && savedIndex < heroList.Count && heroList[savedIndex] != null)
        {
            if (outsideHeroIcon != null) outsideHeroIcon.sprite = heroList[savedIndex].heroIcon;
        }
    }

    private void GenerateListUI()
    {
        foreach (Transform child in slotContainer) DestroyImmediate(child.gameObject);

        int savedIndex = UserData.Instance != null ? UserData.Instance.SelectedHeroIndex : 0;
        savedIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, heroList.Count - 1));

        currentSelectedHero = null;
        currentSelectedSlot = null;

        for (int i = 0; i < heroList.Count; i++)
        {
            var hero = heroList[i];
            if (hero == null) continue;

            bool unlocked = IsHeroUnlocked(i);

            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            HeroSlotUI slotScript = slotObj.GetComponent<HeroSlotUI>();
            if (slotScript != null)
            {
                slotScript.Setup(hero, unlocked, OnSelectHero);

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

    public void ResetToSavedHero()
    {
        int savedIndex = UserData.Instance != null ? UserData.Instance.SelectedHeroIndex : 0;
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
                bool unlocked = IsHeroUnlocked(index);
                if (unlocked)
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

        bool isUnlocked = IsHeroUnlocked(index);
        if (isUnlocked)
        {
            bool isAlreadySelected = UserData.Instance != null && (index == UserData.Instance.SelectedHeroIndex);

            selectButton.interactable = !isAlreadySelected;
            if (selectButtonText != null)
                selectButtonText.text = isAlreadySelected ? "Đã chọn" : "Chọn";

            if (goldCostContainer != null)
                goldCostContainer.SetActive(false);
        }
        else
        {
            selectButton.interactable = UserData.Instance != null && UserData.Instance.HasEnoughGold(data.GoldCost);
            if (selectButtonText != null)
                selectButtonText.text = $"Mua {FormatHelper.FormatGold(data.GoldCost)}";

            if (goldCostContainer != null)
                goldCostContainer.SetActive(true);
            if (goldCostText != null)
                goldCostText.text = FormatHelper.FormatGold(data.GoldCost);
        }
    }

    private void OnConfirmSelect()
    {
        if (currentSelectedHero == null || UserData.Instance == null) return;

        int heroIndex = heroList.IndexOf(currentSelectedHero);
        bool unlocked = IsHeroUnlocked(heroIndex);

        if (!unlocked)
        {
            if (!UserData.Instance.HasEnoughGold(currentSelectedHero.GoldCost))
                return;

            UserData.Instance.UnlockHero(heroIndex, currentSelectedHero.GoldCost);

            if (currentSelectedSlot != null)
                currentSelectedSlot.SetUnlocked(true);

            if (selectButtonText != null)
                selectButtonText.text = "Đã chọn";
            selectButton.interactable = false;

            if (goldCostContainer != null)
                goldCostContainer.SetActive(false);

            if (outsideHeroIcon != null)
                outsideHeroIcon.sprite = currentSelectedHero.heroIcon;

            UserData.Instance.SelectedHeroIndex = heroIndex;
            PlayerEquipment.SelectedHeroIndex = heroIndex;
            return;
        }

        if (outsideHeroIcon != null)
            outsideHeroIcon.sprite = currentSelectedHero.heroIcon;

        if (selectButton != null) selectButton.interactable = false;
        if (selectButtonText != null) selectButtonText.text = "Đã chọn";

        UserData.Instance.SelectedHeroIndex = heroIndex;
        PlayerEquipment.SelectedHeroIndex = heroIndex;
    }

    public void ClosePanel()
    {
        ResetToSavedHero();

        if (slideTween != null)
            slideTween.Hide();
        else
            gameObject.SetActive(false);
    }
}
