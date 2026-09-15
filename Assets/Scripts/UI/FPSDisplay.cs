using TMPro;
using UnityEngine;

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
/// ▐▌ VAI TRÒ
///   - Tự tạo 1 Canvas + Text TMP ở góc dưới - trái (không cần gắn trong scene).
///   - Nghe PerformanceManager.OnFPSUpdated (bám mẫu 2 lần/giây, không cấp phát
///     trong Update) để hiển thị FPS + frame time + frame budget hiện tại.
///   - Bật/tắt theo GameSettingsManager.OnShowFPSChanged.
///   - Di chuyển overlay sang góc khác: FPSDisplay.Instance.SetScreenCorner(...).
///
/// ▐▌ LƯU Ý QUAN TRỌNG (đã sửa)
///   - KHÔNG được SetActive(false) cả GameObject ngay trong Awake: làm vậy thì
///     Start() (nơi đăng ký event) sẽ không bao giờ được gọi → overlay không bao
///     giờ hiện. Thay vào đó root luôn active, chỉ bật/tắt object chữ con
///     (label.gameObject.SetActive) trong SetVisible.
///   - Nếu font TMP mặc định null (thiếu TMP Essentials), fallback qua
///     Resources.Load("LiberationSans SDF").
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

    [Tooltip("Vị trí mặc định khi overlay được tạo (đổi lúc runtime qua SetScreenCorner).")]
    [SerializeField] private FPSDisplayCorner corner = FPSDisplayCorner.BottomLeft;

    private RectTransform textHolder;
    private TMP_Text label;
    private PerformanceManager performance;
    private GameSettingsManager settings;

    // ──────────────────── Unity Callbacks ─────────────────

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
    /// Awake: ép Singleton duy nhất và dựng canvas (root GIỮ active để Start chạy được).
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Chú ý: không đặt gameObject.SetActive(false) ở đây — sẽ làm hỏng luồng Start().
        if (!BuildCanvas())
        {
            Debug.LogWarning("[FPSDisplay] ⚠️ Không có font TMP (thiếu TMP Essential Assets). Overlay FPS bị tắt.");
            Destroy(gameObject);
            return;
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
}