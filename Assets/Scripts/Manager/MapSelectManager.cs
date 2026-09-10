using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using DG.Tweening; // Import DOTween (๑•̀ㅂ•́)و✧

public class MapSelectionManager : MonoBehaviour
{
    [Header("--- MAP DATABASE ---")]
    public List<PreMapSO> mapList = new List<PreMapSO>();
    private int currentIndex = 0;

    [Header("--- CONTAINER & ANIMATION ---")]
    public RectTransform mapContainer; // Khung chứa Map (Ví dụ Tab_StageView)
    public float transitionDuration = 0.35f;
    public float minScale = 0.6f;
    public Ease transitionEase = Ease.OutQuad;

    [Header("--- UI BUTTON ---")]
    public Button startButton; // Nút "Bắt đầu" ngoài màn hình main menu

    [Header("--- SWIPE SETTINGS ---")]
    public float swipeThreshold = 50f;

    private GameObject currentMapInstance;
    private bool isTransitioning = false;
    private Vector2 dragStartPos;
    private bool isDragging = false;

    private void Start()
    {
        // Gán sự kiện cho Nút Bắt đầu
        if (startButton != null) 
            startButton.onClick.AddListener(OnStartButtonClicked);

        // Load Map đã chọn gần nhất (mặc định 0)
        currentIndex = Mathf.Clamp(PlayerPrefs.GetInt("SelectedMapIndex", 0), 0, mapList.Count - 1);
        
        SpawnInitialMap();
    }

    private void Update()
    {
        if (isTransitioning || mapList.Count == 0) return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            float deltaX = Input.mousePosition.x - dragStartPos.x;

            if (Mathf.Abs(deltaX) > swipeThreshold)
            {
                if (deltaX < 0 && currentIndex < mapList.Count - 1)
                {
                    ChangeMap(1);
                }
                else if (deltaX > 0 && currentIndex > 0)
                {
                    ChangeMap(-1);
                }
            }
        }
    }

    private void SpawnInitialMap()
    {
        if (mapList.Count == 0 || mapList[currentIndex]?.mapPreviewPrefab == null) return;

        currentMapInstance = Instantiate(mapList[currentIndex].mapPreviewPrefab, mapContainer);
        ResetRectTransform(currentMapInstance.GetComponent<RectTransform>());

        ShowCurrentMapProgress();
    }

    private void ChangeMap(int direction)
    {
        int targetIndex = currentIndex + direction;
        if (targetIndex < 0 || targetIndex >= mapList.Count) return;

        isTransitioning = true;
        GameObject oldMap = currentMapInstance;

        // Tính toán khoảng cách trượt
        float width = mapContainer.rect.width > 0 ? mapContainer.rect.width : 800f;
        Vector2 oldTargetPos = new Vector2(direction > 0 ? -width : width, 0);
        Vector2 newStartPos = new Vector2(direction > 0 ? width : -width, 0);

        // Tạo Map mới
        GameObject newMap = Instantiate(mapList[targetIndex].mapPreviewPrefab, mapContainer);
        RectTransform newRect = newMap.GetComponent<RectTransform>();
        ResetRectTransform(newRect);

        newRect.anchoredPosition = newStartPos;
        newMap.transform.localScale = Vector3.one * minScale;

        CanvasGroup newCG = GetOrAddCanvasGroup(newMap);
        CanvasGroup oldCG = GetOrAddCanvasGroup(oldMap);
        newCG.alpha = 0f;

        // Animation chuyển map bằng DOTween Sequence
        Sequence seq = DOTween.Sequence();

        if (oldMap != null)
        {
            RectTransform oldRect = oldMap.GetComponent<RectTransform>();
            seq.Join(oldRect.DOAnchorPos(oldTargetPos, transitionDuration).SetEase(transitionEase));
            seq.Join(oldMap.transform.DOScale(minScale, transitionDuration).SetEase(transitionEase));
            seq.Join(oldCG.DOFade(0f, transitionDuration).SetEase(transitionEase));
        }

        seq.Join(newRect.DOAnchorPos(Vector2.zero, transitionDuration).SetEase(transitionEase));
        seq.Join(newMap.transform.DOScale(1f, transitionDuration).SetEase(transitionEase));
        seq.Join(newCG.DOFade(1f, transitionDuration).SetEase(transitionEase));

        seq.OnComplete(() =>
        {
            if (oldMap != null) Destroy(oldMap); // Xóa map cũ giải phóng RAM

            currentMapInstance = newMap;
            currentIndex = targetIndex;

            PlayerPrefs.SetInt("SelectedMapIndex", currentIndex);
            PlayerPrefs.Save();

            ShowCurrentMapProgress();

            isTransitioning = false;
        });
    }

    /// <summary>
    /// Hiển thị kỷ lục tiến trình của map đang hiển thị lên thanh progress.
    /// Thanh progress (StageProgressBarUI) nằm BÊN TRONG prefab map preview,
    /// nên được tự động tìm qua GetComponentInChildren trên map instance.
    /// </summary>
    private void ShowCurrentMapProgress()
    {
        if (mapList.Count == 0 || mapList[currentIndex] == null) return;
        if (currentMapInstance == null) return;

        StageProgressBarUI progressUI = currentMapInstance.GetComponentInChildren<StageProgressBarUI>();
        if (progressUI == null) return;

        StageSO stage = mapList[currentIndex].stageData;
        progressUI.DisplayStageProgress(stage);
    }

    private void ResetRectTransform(RectTransform rt)
    {
    if (rt == null) return;
    
    // Chỉ đưa về vị trí chính giữa và giữ nguyên Size/Anchors gốc của Prefab
    rt.anchoredPosition = Vector2.zero;
    rt.localScale = Vector3.one;
    rt.localRotation = Quaternion.identity;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (!go.TryGetComponent<CanvasGroup>(out var cg))
        {
            cg = go.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    // Ấn nút "Bắt đầu" -> Vào thẳng Scene Game
    private void OnStartButtonClicked()
    {
        if (mapList.Count == 0 || mapList[currentIndex] == null) return;

        // Lưu thông tin Map đã chọn
        PlayerPrefs.SetInt("SelectedMapIndex", currentIndex);
        PlayerPrefs.Save();

        // Chuyển Scene
        string sceneName = mapList[currentIndex].sceneToLoad;
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}