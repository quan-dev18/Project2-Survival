using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum FPSDisplayCorner
{
    BottomLeft = 0,
    BottomRight = 1,
    TopLeft = 2,
    TopRight = 3
}

/// ============================================================================
/// FPSDisplay - Overlay FPS toàn màn hình, điều khiển bởi cờ "Hiện FPS".
///
/// ▐▌ HAI CHẾ ĐỘ (tuỳ bạn chọn 1)
///   1) TỰ ĐỘNG: không gắn gì vào scene. Script tự tạo Canvas + text TMP góc
///      dưới - trái mỗi lần load scene (EnsureInstance).
///   2) GẮN TAY (khuyên dùng khi muốn chỉnh vị trí trong Edit mode):
///      Menu GameObject > FPS Display để tạo sẵn object "FPS Display" trong scene,
///      rồi kéo thẳng "FPS Label" (scene hoặc Inspector). Vị trí LƯU vào scene —
///      thoát Play vào lại vẫn nguyên vị trí (khác hẳn chế độ tự động, thay đổi
///      lúc chạy Play không bao giờ lưu).
///
/// ▐▌ VAI TRÒ
///   - Nghe PerformanceManager.OnFPSUpdated (bám mẫu 2 lần/giây, không cấp phát
///     trong Update) để hiển thị FPS + frame time + frame budget hiện tại.
///   - Bật/tắt theo GameSettingsManager.OnShowFPSChanged.
///   - Di chuyển overlay: F8/F9 (lúc chơi) hoặc FPSDisplay.Instance.SetScreenCorner(...).
///
/// ▐▌ LƯU Ý QUAN TRỌNG (đã sửa)
///   - KHÔNG được SetActive(false) cả GameObject ngay trong Awake: làm vậy thì
///     Start() (nơi đăng ký event) sẽ không bao giờ được gọi → overlay không bao
///     giờ hiện. Thay vào đó root luôn active, chỉ bật/tắt object chữ con
///     (label.gameObject.SetActive) trong SetVisible.
///   - Nếu font TMP mặc định null (thiếu TMP Essentials), chế độ tự động fallback
///     qua Resources.Load("LiberationSans SDF").
/// ============================================================================
public class FPSDisplay : MonoBehaviour
{
    /// <summary>Con trỏ Singleton phục vụ cho các UI khác cần gọi SetVisible trực tiếp.</summary>
    public static FPSDisplay Instance { get; private set; }

    [Header("Giao diện (tốt nhất để mặc định)")]
    [Tooltip("Màu chữ khi frame time đang nằm trong frame budget.")]
    [SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.35f);

    [Tooltip("Màu chữ khi frame time vượt frame budget của FPS mục tiêu.")]
    [SerializeField] private Color badColor = new Color(1f, 0.3f, 0.3f);

    [Header("Gắn tay (để trống = chế độ tự động)")]
    [Tooltip("Text TMP đặt sẵn trong scene. Để trống: chế độ tự động tự dựng canvas.\nGắn vào: kéo 'FPS Label' trong Edit mode để định vị lưu vĩnh viễn vào scene.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Vị trí mặc định khi overlay được tạo (đổi lúc runtime qua SetScreenCorner).")]
    [SerializeField] private FPSDisplayCorner corner = FPSDisplayCorner.BottomLeft;

    private RectTransform textHolder;
    private PerformanceManager performance;
    private GameSettingsManager settings;

    // ──────────────────── Unity Callbacks ─────────────────

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Phím tắt test vị trí (chỉ Editor / Development Build):
    ///   [F8]  Xoay vòng 4 góc màn hình.
    ///   [F9]  Về góc dưới - trái (mặc định).
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            corner = (FPSDisplayCorner)(((int)corner + 1) % 4);
            ApplyCorner();
            Debug.Log($"[FPSDisplay] 🎮 Đổi góc sang {corner}");
        }
        else if (Input.GetKeyDown(KeyCode.F9))
        {
            corner = FPSDisplayCorner.BottomLeft;
            ApplyCorner();
            Debug.Log("[FPSDisplay] 🎮 Về góc dưới trái");
        }
    }
#endif

    /// <summary>
    /// [TỰ ĐỘNG] Tạo overlay ngay sau khi scene đầu tiên load, sống xuyên scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null || FindObjectOfType<FPSDisplay>() != null)
            return;

        GameObject go = new GameObject("FPSDisplay");
        go.AddComponent<FPSDisplay>();
    }

    /// <summary>
    /// Awake: ép Singleton duy nhất.
    /// - Chế độ tự động (label == null): tự dựng canvas trên GO này.
    /// - Chế độ gắn tay (label != null): mượn text đã đặt trong scene, TÔN TRỌNG
    ///   vị trí đã kéo trong Edit mode (không gọi ApplyCorner để không ghi đè).
    /// Root luôn GIỮ active để Start chạy được.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Chú ý: không đặt gameObject.SetActive(false) ở đây — sẽ làm hỏng luồng Start().
        if (label == null)
        {
            if (!BuildCanvas())
            {
                Debug.LogWarning("[FPSDisplay] ⚠️ Không có font TMP (thiếu TMP Essential Assets). Overlay FPS bị tắt.");
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            // Gắn tay: dùng text người dùng đặt sẵn, vị trí do scene quyết định.
            textHolder = label.rectTransform;
        }
    }

    /// <summary>
    /// Bắt đầu: resolve 2 Core Manager, đăng ký event và áp ngay trạng thái đã lưu.
    /// Nếu manager được auto-create hơi trễ, thử lại sau 1 frame.
    /// </summary>
    private void Start()
    {
        performance = PerformanceManager.Instance;
        settings = GameSettingsManager.Instance;

        if (performance == null || settings == null)
        {
            Invoke(nameof(TryBindDelayed), 0.1f);
            return;
        }

        Bind();
    }

    /// <summary>
    /// Thử resolve lại manager sau khi auto-create đã chạy xong.
    /// </summary>
    private void TryBindDelayed()
    {
        performance = PerformanceManager.Instance;
        settings = GameSettingsManager.Instance;
        if (performance != null && settings != null)
            Bind();
    }

    /// <summary>
    /// Đăng ký event và đồng bộ trạng thái hiển thị theo cờ đã lưu.
    /// </summary>
    private void Bind()
    {
        performance.OnFPSUpdated += OnFPSUpdated;
        settings.OnShowFPSChanged += OnShowFPSChanged;
        SetVisible(settings.ShowFPS);
    }

    /// <summary>
    /// Hủy đăng ký event để tránh leak reference.
    /// </summary>
    private void OnDestroy()
    {
        if (performance != null)
            performance.OnFPSUpdated -= OnFPSUpdated;
        if (settings != null)
            settings.OnShowFPSChanged -= OnShowFPSChanged;
        if (Instance == this)
            Instance = null;
    }

    // ──────────────────── Core -> UI (chỉ phản ánh dữ liệu) ─

    /// <summary>
    /// Cập nhật text FPS + frame time/budget mỗi lần manager bám mẫu FPS.
    /// </summary>
    private void OnFPSUpdated(float fps)
    {
        if (label == null || performance == null) return;
        if (!label.gameObject.activeSelf) return;

        label.text = $"{fps:0} FPS";
        label.color = performance.IsExceedingFrameBudget ? badColor : goodColor;
    }

    /// <summary>
    /// Hiện/ẩn overlay theo cờ từ GameSettingsManager.
    /// </summary>
    private void OnShowFPSChanged(bool show)
    {
        SetVisible(show);
    }

    // ──────────────────── Helpers ─────────────────────────

    /// <summary>
    /// Bật/tắt overlay: chỉ bật/tắt object chữ con, root vẫn active để Start/event còn sống.
    /// </summary>
    /// <param name="visible">true = hiện overlay.</param>
    public void SetVisible(bool visible)
    {
        if (label == null) return;
        label.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Dựng Canvas ScreenSpaceOverlay + Text TMP góc dưới - trái.
    /// </summary>
    private bool BuildCanvas()
    {
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        if (defaultFont == null) return false;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        GameObject textGo = new GameObject("FPS Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(transform, false);

        label = textGo.GetComponent<TextMeshProUGUI>();
        label.font = defaultFont;
        label.fontSize = 32f;
        label.color = goodColor;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.text = "";

        textHolder = textGo.GetComponent<RectTransform>();
        textHolder.sizeDelta = new Vector2(300f, 80f);
        ApplyCorner();

        // Ẩn mặc định cho tới khi cờ ShowFPS bật — chỉ ẩn chữ con, không ẩn root.
        textGo.SetActive(false);
        Debug.Log("[FPSDisplay] 🚀 Đã tạo overlay FPS (đang ẩn, bật theo setting).");
        return true;
    }

    /// <summary>
    /// Di chuyển overlay sang 1 góc màn hình (góc dưới - trái mặc định).
    /// Gọi từ bất kỳ đâu: FPSDisplay.Instance.SetScreenCorner(FPSDisplayCorner.TopRight);
    /// </summary>
    /// <param name="newCorner">Góc màn hình muốn đặt overlay.</param>
    public void SetScreenCorner(FPSDisplayCorner newCorner)
    {
        corner = newCorner;
        ApplyCorner();
    }

    /// <summary>Áp dụng anchor/pivot/vị trí cho góc hiện tại.</summary>
    private void ApplyCorner()
    {
        if (textHolder == null || label == null) return;

        const float pad = 12f;
        bool right = corner == FPSDisplayCorner.BottomRight || corner == FPSDisplayCorner.TopRight;
        bool top = corner == FPSDisplayCorner.TopLeft || corner == FPSDisplayCorner.TopRight;

        textHolder.anchorMin = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
        textHolder.anchorMax = textHolder.anchorMin;
        textHolder.pivot = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
        textHolder.anchoredPosition = new Vector2(right ? -pad : pad, top ? -pad : pad);

        // Căn chữ theo hướng chữ "mọc" từ anchor: xuống dưới nếu ở góc trên.
        label.alignment = top ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.TopLeft;
    }

#if UNITY_EDITOR
    /// <summary>
    /// [EDITOR] Menu GameObject > FPS Display: tạo sẵn 1 Canvas + Text TMP trong scene.
    /// Kéo "FPS Label" để định vị — vị trí sẽ được lưu vào scene (khác chế độ tự động).
    /// </summary>
    [UnityEditor.MenuItem("GameObject/FPS Display", false, 10)]
    private static void CreateFPSDisplayInScene()
    {
        GameObject root = new GameObject("FPS Display");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        GameObject textGo = new GameObject("FPS Label", typeof(RectTransform));
        textGo.transform.SetParent(root.transform, false);

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null)
            text.font = defaultFont;
        text.fontSize = 32f;
        text.color = new Color(0.2f, 1f, 0.35f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = "";

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 80f);
        rt.pivot = Vector2.zero;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = new Vector2(12f, 12f);

        // Gắn sẵn label để FPSDisplay dùng chế độ "gắn tay".
        FPSDisplay fps = root.AddComponent<FPSDisplay>();
        fps.label = text;

        UnityEditor.Selection.activeGameObject = root;
        UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Create FPS Display");
    }
#endif
}