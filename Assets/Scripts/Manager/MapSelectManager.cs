using System.Collections.Generic;
using TMPro;
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

    [Header("--- LOCKED MAP ---")]
    [Tooltip("Text của nút Bắt đầu (tự tìm nếu để trống). Bị xóa khi map bị khóa.")]
    public TextMeshProUGUI startButtonText;
    [Tooltip("Màu của nút Bắt đầu khi map bị khóa (xám).")]
    public Color lockedButtonColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("--- SWIPE SETTINGS ---")]
    public float swipeThreshold = 50f;

    private GameObject currentMapInstance;
    private bool isTransitioning = false;
    private Vector2 dragStartPos;
    private bool isDragging = false;

    private string defaultStartText;      // Text gốc của nút để khôi phục lại
    private ColorBlock defaultButtonColors; // Màu gốc của nút để khôi phục lại

    private void Start()
    {
        // Lưu trạng thái gốc của nút Bắt đầu (trước khi bị đổi do khóa map).
        if (startButton != null)
        {
            defaultButtonColors = startButton.colors;
            if (startButtonText == null)
                startButtonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (startButtonText != null)
                defaultStartText = startButtonText.text;

            // Gán sự kiện cho Nút Bắt đầu
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        // Load Map đã chọn gần nhất (mặc định 0), nhưng không vượt quá map hiển thị được.
        if (mapList.Count > 0)
        {
            currentIndex = Mathf.Clamp(PlayerPrefs.GetInt("SelectedMapIndex", 0), 0, GetMaxVisibleIndex());
        }

        SpawnInitialMap();
        RefreshStartButton();
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

            int maxVisible = GetMaxVisibleIndex();
            if (Mathf.Abs(deltaX) > swipeThreshold)
            {
                if (deltaX < 0 && currentIndex < maxVisible)
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
        // Không cho swipe tới các map nằm sau map khóa đầu tiên.
        if (targetIndex < 0 || targetIndex > GetMaxVisibleIndex()) return;

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
            RefreshStartButton(); // Cập nhật trạng thái khóa/trống của nút theo map mới

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

    /// <summary>
    /// Kiểm tra map có được mở khóa hay không.
    /// Map đầu tiên (index 0) luôn mở. Map sau chỉ mở khi map trước đó
    /// hoàn thành 100% (best progress >= MaxProgress).
    /// </summary>
    private bool IsMapUnlocked(int index)
    {
        if (mapList == null || mapList.Count == 0 || index < 0 || index >= mapList.Count) return false;
        if (index == 0) return true; // Map đầu tiên luôn mở.

        // Map trước đó không có StageData => không có điều kiện khóa, xem như mở.
        PreMapSO prev = mapList[index - 1];
        if (prev == null || prev.stageData == null) return true;

        float best = UserData.Instance != null
            ? UserData.Instance.GetStageBestProgress(prev.stageData.StageID)
            : 0f;

        return best >= prev.stageData.MaxProgress; // 100% hoặc hơn.
    }

    /// <summary>
    /// Trả về index cao nhất được phép xem/chọn.
    /// Chỉ hiển thị: toàn bộ map đã mở + MAP KHÓA ĐẦU TIÊN (i+1) làm gợi ý.
    /// Các map nằm sau map khóa đầu tiên sẽ không được swipe tới (bị tắt).
    /// </summary>
    private int GetMaxVisibleIndex()
    {
        if (mapList == null || mapList.Count == 0) return 0;

        int lastUnlocked = -1;
        for (int i = 0; i < mapList.Count; i++)
        {
            if (IsMapUnlocked(i))
                lastUnlocked = i;
            else
                break; // Gặp map khóa đầu tiên thì dừng lại.
        }

        // maxVisible = map khóa đầu tiên (lastUnlocked + 1), nhưng không vượt quá danh sách.
        return Mathf.Clamp(lastUnlocked + 1, 0, mapList.Count - 1);
    }

    /// <summary>
    /// Cập nhật trạng thái nút Bắt đầu theo map đang hiển thị:
    /// - Map KHÓA  : nút màu xám + xóa text + không bấm được.
    /// - Map MỞ    : khôi phục màu + text gốc.
    /// </summary>
    private void RefreshStartButton()
    {
        if (startButton == null) return;

        bool unlocked = IsMapUnlocked(currentIndex);

        // Chặn/bật bấm nút.
        startButton.interactable = unlocked;

        // Đổi màu nút theo trạng thái.
        ColorBlock cb = startButton.colors;
        if (unlocked)
        {
            startButton.colors = defaultButtonColors;
        }
        else
        {
            cb.normalColor = lockedButtonColor;
            cb.highlightedColor = lockedButtonColor;
            cb.pressedColor = lockedButtonColor;
            cb.selectedColor = lockedButtonColor;
            cb.disabledColor = lockedButtonColor;
            startButton.colors = cb;
        }

        // Xóa / khôi phục text trên nút.
        if (startButtonText != null)
            startButtonText.text = unlocked ? defaultStartText : string.Empty;
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

    //  Ấn nút "Bắt đầu" -> Vào thẳng Scene Game
    private void OnStartButtonClicked()
    {
        if (mapList.Count == 0 || mapList[currentIndex] == null) return;

        // Map đang bị khóa thì không cho vào (phòng trường hợp nút vẫn bị bấm).
        if (!IsMapUnlocked(currentIndex))
        {
            Debug.Log($"[MapSelection] Map '{currentIndex}' đang khóa - cần hoàn thành 100% map trước.");
            return;
        }

        // Lưu thông tin Map đã chọn
        PlayerPrefs.SetInt("SelectedMapIndex", currentIndex);
        PlayerPrefs.Save();

        // Log map selected event
        PreMapSO map = mapList[currentIndex];
        string stageId = map.stageData != null ? map.stageData.StageID : "unknown";
        FirebaseAnalyticsHelper.LogMapSelected(map.name, stageId, currentIndex);

        // Chuyển Scene
        string sceneName = mapList[currentIndex].sceneToLoad;
        if (!string.IsNullOrEmpty(sceneName))
        {
            LoadingSceneController.targetScene = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }
    }
}