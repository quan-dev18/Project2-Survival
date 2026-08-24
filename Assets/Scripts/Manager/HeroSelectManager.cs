using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroSelectManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<HeroSelectSO> heroList; // Kéo các file Data vào đây

    [Header("UI Containers")]
    [SerializeField] private Transform slotContainer; // Kéo GameObject 'Heroes' vào đây
    [SerializeField] private GameObject slotPrefab;   // Prefab ô chọn tướng

    [Header("Preview Parent")]
    [SerializeField] private Transform previewParent; // Kéo 'HeroPreview' vào đây

    [Header("Select Button")]
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI selectButtonText;

    private GameObject currentPreviewInstance;

    private void Start()
    {
        GenerateListUI();
    }

    private void GenerateListUI()
    {
        // 1. Dọn dẹp danh sách cũ
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        // 2. Sinh các ô nút bấm từ Data
        foreach (var hero in heroList)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            HeroSlotUI slotScript = slotObj.GetComponent<HeroSlotUI>();
            slotScript.Setup(hero, OnSelectHero);

            slotObj.transform.localScale = Vector3.one;
            slotObj.transform.localPosition = Vector3.zero;
        }

        // 3. Hiển thị tướng đầu tiên mặc định
        if (heroList.Count > 0) OnSelectHero(heroList[0]);
    }

    private void OnSelectHero(HeroSelectSO data)
    {
        // 1. Xóa Prefab cũ, spawn Prefab nhân vật mới (chứa cả Sprite + Des) vào HeroPreview
        if (currentPreviewInstance != null) Destroy(currentPreviewInstance);
        if (data.previewPrefab != null)
        {
            currentPreviewInstance = Instantiate(data.previewPrefab, previewParent);
        }

        // 2. Cập nhật trạng thái nút Chọn
        if (data.isUnlocked)
        {
            selectButton.interactable = true;
            if (selectButtonText != null) selectButtonText.text = "Đã chọn";
        }
        else
        {
            selectButton.interactable = false;
            if (selectButtonText != null) selectButtonText.text = "Đã khóa";
        }
    }
}