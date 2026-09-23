using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using DG.Tweening; // Import DOTween (๑•̀ㅂ•́)و✧

public class MapSelectionManager : MonoBehaviour
{
    public static bool debugMode = false;
    public static MapSelectionManager Instance { get; private set; }

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
    [Tooltip("GameObject thông báo khi map ĐANG KHÓA (vd: dòng chữ 'Hoàn thành 100% map trước đó để mở khóa map này'). " +
             "Hệ thống sẽ SetActive: BẬT khi map hiện tại bị khóa, TẮT khi map mở. Nên để ẨN sẵn.")]
    public GameObject lockedMapNotifier;

    [Header("--- SWIPE SETTINGS ---")]
    public float swipeThreshold = 50f;

    private GameObject currentMapInstance;
    private bool isTransitioning = false;
    private Vector2 dragStartPos;
    private bool isDragging = false;

    // Map mới đang được tween vào giữa lúc transition (chưa phải currentMapInstance).
    // Dùng để dọn dẹp khi người chơi bấm nút map INF giữa lúc đang lướt — tránh map
    // "orphan" (tween xong OnComplete ghi đè currentIndex về map thường, còn map INF
    // thì bị bỏ rơi nằm dưới). Tween/instances rác tích tụ chính là lý do map INF
    // "bị đè dưới và không bật lên được" sau nhiều lần lặp lại.
    private Sequence mapAnimSeq;
    private GameObject pendingMapInstance;

    [Header("--- INF (ENDLESS) MAP ---")]
    [Tooltip("Map INF (endless). Đã nằm SẴN trong mapList (database) — kéo PreMapSO của nó vào đây để " +
             "hệ thống biết map nào là INF. Map INF KHÔNG nằm trong carousel chọn map thường (không swipe tới được); " +
             "chỉ hiện khi bấm nút 'ShowInfiniteMap'. Nên đặt nó ở CUỐI danh sách.")]
    [SerializeField] private PreMapSO infiniteMap;

    [Tooltip("Text của nút bật map INF (tự tìm con trẻ nếu để trống).")]
    [SerializeField] private TextMeshProUGUI infButtonText;
    [Tooltip("Chữ hiển thị khi CHƯA ở map INF (vd: \"Đấu vô tận\").")]
    [SerializeField] private string infButtonDefaultText = "Đấu vô tận";
    [Tooltip("Chữ hiển thị khi ĐANG Ở map INF — nút lúc này dùng để quay lại (vd: \"Đấu thường\").")]
    [SerializeField] private string infButtonInfText = "Đấu thường";
    [Tooltip("Icon HIỆN sẵn của nút bật map INF (icon bình thường của 'Đấu vô tận').")]
    [SerializeField] private GameObject infButtonNormalIcon;
    [Tooltip("Icon chỉ HIỆN khi đang ở map INF (icon dùng cho nút quay lại 'Đấu thường'). Nên để ẨN sẵn.")]
    [SerializeField] private GameObject infButtonInfIcon;

    private string defaultStartText;      // Text gốc của nút để khôi phục lại
    private ColorBlock defaultButtonColors; // Màu gốc của nút để khôi phục lại
    private Tween lockedNotifyTween;      // Tween hiện/ẩn thông báo "hoàn thành map trước".
    private int previousNormalIndex = 0;  // Map thường người chơi đang xem trước khi bật map INF.

    [Header("--- LOCKED NOTIFY ANIMATION ---")]
    [Tooltip("Thời gian (giây) hiện thông báo khi map bị khóa.")]
    public float lockedNotifyInDuration = 0.25f;
    [Tooltip("Thời gian (giây) ẩn thông báo khi map được mở.")]
    public float lockedNotifyOutDuration = 0.15f;

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

        // Khởi tạo trạng thái nút bật map INF (mặc định: ngoài map INF).
        RefreshInfButtonState(false);
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

        // Nhớ sequence đang chạy + map mới để hủy đúng lúc nếu người chơi bấm nút map INF
        // (ShowInfiniteMap/ShowMapAtIndex) giữa chừng — nếu không, OnComplete của tween
        // cũ vẫn chạy và ghi đè currentIndex về map thường, làm map INF bị "kẹt dưới".
        mapAnimSeq = seq;
        pendingMapInstance = newMap;

        seq.OnComplete(() =>
        {
            mapAnimSeq = null;
            pendingMapInstance = null;

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

        if (debugMode) return true;

        // Map INF: luôn mở khi được hiển thị (nút Bắt đầu chạy được ngay).
        // Nó KHÔNG nằm trong carousel chọn map thường — chỉ hiện khi bấm nút ShowInfiniteMap.
        if (infiniteMap != null && mapList[index] == infiniteMap) return true;

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
    /// Trả về index cao nhất được phép xem/chọn trong carousel map thường.
    /// Chỉ hiển thị: toàn bộ map đã mở + MAP KHÓA ĐẦU TIÊN (i+1) làm gợi ý.
    /// Map INF bị LOẠI khỏi carousel: luôn cap dưới index của nó nên không bao giờ
    /// swipe tới được — chỉ hiện khi ấn nút ShowInfiniteMap.
    /// </summary>
    private int GetMaxVisibleIndex()
    {
        if (mapList == null || mapList.Count == 0) return 0;

        // Cap cứng: mọi index trước map INF trong danh sách.
        int hardCap = mapList.Count - 1;
        int infIndex = GetInfiniteMapIndex();
        if (infIndex >= 0)
            hardCap = Mathf.Min(hardCap, infIndex - 1);
        if (hardCap < 0) return 0;

        if (debugMode) return hardCap;

        int lastUnlocked = -1;
        for (int i = 0; i <= hardCap; i++)
        {
            if (IsMapUnlocked(i))
                lastUnlocked = i;
            else
                break; // Gặp map khóa đầu tiên thì dừng lại.
        }

        // maxVisible = map khóa đầu tiên (lastUnlocked + 1), nhưng không vượt quá hardCap.
        return Mathf.Clamp(lastUnlocked + 1, 0, hardCap);
    }

    /// <summary>
    /// Cập nhật trạng thái nút Bắt đầu theo map đang hiển thị:
    /// - Map KHÓA  : nút màu xám + xóa text + không bấm được + BẬT thông báo "hoàn thành map trước".
    /// - Map MỞ    : khôi phục màu + text gốc + TẮT thông báo.
    /// </summary>
    private void RefreshStartButton()
    {
        if (startButton == null) return;

        bool unlocked = IsMapUnlocked(currentIndex);

        // Chặn/bật bấm nút.
        startButton.interactable = unlocked;

        // Bật/tắt thông báo "hoàn thành map trước đó để chơi map này" khi map đang khóa.
        SetLockedNotifier(!unlocked);

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

        // Đồng bộ trạng thái nút bật map INF theo map đang hiển thị
        // (ủng hộ cả trường hợp lướt swipe sang map thường khi đang ở map INF).
        RefreshInfButtonState(currentIndex == GetInfiniteMapIndex());
    }

    /// <summary>
    /// Hiện/ẩn thông báo "hoàn thành map trước đó" kèm hiệu ứng DOTween:
    ///   - HIỆN (map khóa) : fade in + scale nảy lên (Ease.OutBack) cho bắt mắt.
    ///   - ẨN (map mở)     : fade out rồi mới SetActive(false) — kết thúc mượt, không lag "tắt ngay".
    /// </summary>
    private void SetLockedNotifier(bool show)
    {
        if (lockedMapNotifier == null) return;

        // Hủy tween cũ để không đè 2 hiệu ứng chồng lên nhau khi lướt liên tục.
        if (lockedNotifyTween != null)
        {
            lockedNotifyTween.Kill();
            lockedNotifyTween = null;
        }

        CanvasGroup cg = GetOrAddCanvasGroup(lockedMapNotifier);

        if (show)
        {
            // Bật GameObject rồi chạy hiệu ứng từ trong suốt + nhỏ lên.
            lockedMapNotifier.SetActive(true);
            cg.alpha = 0f;
            lockedMapNotifier.transform.localScale = Vector3.one * 0.8f;

            lockedNotifyTween = DOTween.Sequence()
                .Join(cg.DOFade(1f, lockedNotifyInDuration))
                .Join(lockedMapNotifier.transform.DOScale(Vector3.one, lockedNotifyInDuration).SetEase(Ease.OutBack));
        }
        else
        {
            // Đã tắt sẵn rồi thì không cần chạy hiệu ứng fade nữa (tránh tween trên object inactive).
            if (!lockedMapNotifier.activeSelf)
            {
                lockedNotifyTween = null;
                return;
            }

            // Fade ra xong mới tắt GameObject → không bị "tắt cái rụp".
            lockedNotifyTween = cg.DOFade(0f, lockedNotifyOutDuration)
                .OnComplete(() => lockedMapNotifier.SetActive(false));
        }
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

    /// <summary>
    /// Tìm index của map INF (endless) trong danh sách.
    /// Trả về -1 nếu map INF được gán qua field "Infinite Map" nhưng không có trong mapList.
    /// </summary>
    private int GetInfiniteMapIndex()
    {
        if (mapList == null || infiniteMap == null) return -1;
        for (int i = 0; i < mapList.Count; i++)
        {
            if (mapList[i] == infiniteMap)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// NÚT BẬT/TẮT MAP INF (endless): gắn trực tiếp vào nút qua Inspector (Button -> OnClick ->
    /// kéo GameObject MapSelectionManager -> chọn ShowInfiniteMap).
    /// Map INF đã có sẵn trong mapList (database) nhưng KHÔNG hiện trong carousel chọn map thường.
    ///   - Bấm lần 1: map INF hiện ra giữa màn hình selection.
    ///   - Bấm lần 2: trở về map thường đang chọn trước đó (bỏ qua map INF).
    /// Ngoài ra có thể lướt (swipe) sang trái/phải để về map thường, nhưng swipe vào
    /// map INF bị chặn — phải bấm nút mới hiện được.
    /// </summary>
    public void ShowInfiniteMap()
    {
        int infIndex = GetInfiniteMapIndex();
        if (infIndex < 0)
        {
            Debug.LogWarning("[MapSelection] Không thấy map INF trong mapList! Hãy kéo PreMapSO của nó vào field 'Infinite Map' và đảm bảo nó nằm trong mapList.");
            return;
        }

        // Đang ở map INF rồi => toggle: quay về map thường trước đó.
        if (currentIndex == infIndex)
        {
            Debug.Log($"[MapSelection] Tắt map INF, quay về map thường index {previousNormalIndex}.");
            ShowMapAtIndex(previousNormalIndex);
            RefreshInfButtonState(false);
            return;
        }

        // Nhớ map thường đang xem để bấm nút lần 2 quay lại.
        previousNormalIndex = currentIndex;
        Debug.Log($"[MapSelection] Bật hiện map INF (index {infIndex}).");
        ShowMapAtIndex(infIndex);
        RefreshInfButtonState(true);
    }

    /// <summary>
    /// Cập nhật giao diện nút bật map INF theo trạng thái:
    ///   - isInf = false (đang ở map thường): text "Đấu vô tận" + icon bình thường.
    ///   - isInf = true  (đang ở map INF)  : text "Đấu thường" + icon quay lại (bật icon INF, tắt icon thường).
    /// </summary>
    private void RefreshInfButtonState(bool isInf)
    {
        if (infButtonText != null)
            infButtonText.text = isInf ? infButtonInfText : infButtonDefaultText;

        if (infButtonNormalIcon != null)
            infButtonNormalIcon.SetActive(!isInf);
        if (infButtonInfIcon != null)
            infButtonInfIcon.SetActive(isInf);
    }

    /// <summary>
    /// Nhảy về hiển thị map ở index cho trước (không animate lướt,
    /// dùng khi nhảy thẳng từ nút bật map INF).
    /// </summary>
    private void ShowMapAtIndex(int index)
    {
        if (mapList.Count == 0 || index < 0 || index >= mapList.Count || mapList[index] == null) return;

        // Hủy transition swipe đang dang dở (nếu có) trước khi thay map:
        // nếu không, tween cũ chạy nốt xong vẫn ghi đè currentIndex về map thường
        // → map INF vừa bật bị kẹt dưới + tích lũy instance rác trên container.
        CancelTransition();

        if (currentMapInstance != null)
            Destroy(currentMapInstance);

        currentIndex = index;
        currentMapInstance = Instantiate(mapList[currentIndex].mapPreviewPrefab, mapContainer);
        ResetRectTransform(currentMapInstance.GetComponent<RectTransform>());

        PlayerPrefs.SetInt("SelectedMapIndex", currentIndex);
        PlayerPrefs.Save();

        ShowCurrentMapProgress();
        RefreshStartButton();
    }

    /// <summary>
    /// Hủy transition chuyển map đang chạy: kill tween + xóa map mới đang bay vào,
    /// đưa isTransitioning về false. Map hiện tại (currentMapInstance) sẽ do người
    /// gọi xử lý tiếp (ShowMapAtIndex thay map mới ngay sau đó).
    /// </summary>
    private void CancelTransition()
    {
        if (mapAnimSeq != null)
        {
            mapAnimSeq.Kill();
            mapAnimSeq = null;
        }

        if (pendingMapInstance != null)
        {
            Destroy(pendingMapInstance);
            pendingMapInstance = null;
        }

        isTransitioning = false;
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