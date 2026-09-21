using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Tương thích nhiều phiên bản Unity: từ 2023.2 các API pin/nhiệt độ
// được chuyển sang namespace UnityEngine.Device (không còn deprecated).
#if UNITY_2023_2_OR_NEWER
using BatteryInfo = UnityEngine.Device.SystemInfo;
using BatteryStatusType = UnityEngine.Device.BatteryStatus;
#else
using BatteryInfo = UnityEngine.SystemInfo;
using BatteryStatusType = UnityEngine.BatteryStatus;
#endif

/// <summary>
/// Chế độ khung hình mục tiêu mà người chơi chọn trong Settings.
/// </summary>
public enum FrameRateMode
{
    Auto = 0,
    FPS30 = 1,
    FPS60 = 2,
    FPS90 = 3,
    FPS120 = 4
}

/// <summary>
/// Mức hiệu năng tổng thể hiện tại do Adaptive Throttling tự duy trì.
/// </summary>
public enum PerformanceLevel
{
    High = 0,
    Medium = 1,
    Low = 2
}

/// <summary>
/// Bộ cài đặt GPU cho một nấc chất lượng (Low/Medium/High).
/// Chỉ dùng đúng các property URP set được lúc RUNTIME (Unity 2022.3 / URP 14):
///   - renderScale
///   - msaaSampleCount
///   - shadowDistance (0 = tắt hẳn bóng đổ, tiết kiệm nhất)
///   - shadowCascadeCount (1..4)
/// Các cờ như supportsSoftShadows / mainLightShadowmapResolution là internal set
/// trong URP 14 nên không đụng đến ở runtime; muốn tinh chỉnh thì sửa trực tiếp
/// trên URP asset trong Inspector (hoặc nâng cấp sang nhiều URP asset/nấc).
/// </summary>
[Serializable]
public struct RenderProfile
{
    [Tooltip("Render Scale: tỷ lệ độ phân giải GPU (0.1..2). Càng thấp càng nhanh.")]
    [Range(0.1f, 2f)]
    public float renderScale;

    [Tooltip("MSAA chống răng cưa: 0 = tắt, 2/4/8 = số mẫu.")]
    public int msaaSampleCount;

    [Tooltip("Khoảng cách vẽ bóng (0 = tắt hẳn, tiết kiệm nhất).")]
    public float shadowDistance;

    [Tooltip("Số cascade của bóng (1..4). Càng nhiều bóng càng nét nhưng đắt hơn.")]
    [Range(1, 4)]
    public int shadowCascadeCount;

    public static RenderProfile HighPreset =>
        new RenderProfile { renderScale = 1f, msaaSampleCount = 4, shadowDistance = 50f, shadowCascadeCount = 4 };

    public static RenderProfile MediumPreset =>
        new RenderProfile { renderScale = 0.875f, msaaSampleCount = 2, shadowDistance = 30f, shadowCascadeCount = 2 };

    public static RenderProfile LowPreset =>
        new RenderProfile { renderScale = 0.7f, msaaSampleCount = 0, shadowDistance = 0f, shadowCascadeCount = 1 };

    /// <summary>MSAA hợp lệ cho URP: chỉ nhận 0, 2, 4 hoặc 8 mẫu.</summary>
    public static int ValidateMsaa(int samples)
    {
        if (samples == 0 || samples == 2 || samples == 4 || samples == 8) return samples;
        return samples < 4 ? 0 : 4;
    }

    /// <summary>Số cascade hợp lệ: luôn nằm trong 1..4.</summary>
    public static int ValidateCascades(int count) => Mathf.Clamp(count, 1, 4);
}

/// <summary>
/// [PerformanceManager] Core Manager chịu trách nhiệm tối ưu hiệu năng toàn game:
/// giám sát FPS (không cấp phát GC), kiểm soát Frame Budget theo FPS mục tiêu
/// (60FPS <= 16.67ms, 90FPS <= 11.11ms, 120FPS <= 8.33ms) và Tự điều chỉnh thích ứng
/// (Adaptive Throttling) hạ chất lượng / FPS khi máy yếu, pin cạn hoặc bộ nhớ thấp.
///
/// ▐▌ KIẾN TRÚC
///   - Là Singleton sống xuyên scene (DontDestroyOnLoad), tự động được tạo nếu
///     chưa có trong scene (xem EnsureInstance).
///   - KHÔNG chứa bất kỳ logic UI nào. Toàn bộ UI giao tiếp qua:
///       + Các Setter công khai (Push):  SetFrameRateMode / SetQualityLevel / ...
///       + Các event Action (Push):      OnFPSUpdated / OnFrameRateModeChanged / ...
///   - Dữ liệu cài đặt lưu PlayerPrefs với khóa hằng const quản lý tập trung,
///     mỗi lần thay đổi đều gọi PlayerPrefs.Save() ngay.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR
///   1. Tạo GameObject rỗng tên "PerformanceManager".
///   2. Kéo script này lên. Script tự gọi DontDestroyOnLoad.
///   3. Không cần làm gì thêm — nếu quên gắn vào scene, hệ thống tự tạo runtime.
/// </summary>
public sealed class PerformanceManager : MonoBehaviour
{
    // =============================================================
    //  SINGLETON - con trỏ dùng chung cho toàn game
    // =============================================================
    public static PerformanceManager Instance { get; private set; }

    // =============================================================
    //  CONSTANT PLAYERPREFS KEYS - quản lý tập trung
    // =============================================================
    private const string KEY_FRAME_RATE_MODE = "Performance_FrameRate_Mode";
    private const string KEY_QUALITY_LEVEL = "Performance_Quality_Level";
    private const string KEY_ADAPTIVE_ENABLED = "Performance_Adaptive_Enabled";
    private const string KEY_LOW_GRAPHICS = "Performance_LowGraphics";
    private const string KEY_VSYNC = "Performance_VSync";

    // =============================================================
    //  SERIALIZE FIELDS - thiết lập trong Inspector
    // =============================================================
    [Header("=== Giám sát FPS ===")]
    [Tooltip("Chu kỳ lấy mẫu và tính FPS trung bình (giây).")]
    [SerializeField] private float fpsSampleInterval = 0.5f;

    [Tooltip("FPS mục tiêu khi chế độ Auto và màn hình không đạt 90/120Hz.")]
    [SerializeField] private int autoTargetFPS = 60;

    [Header("=== Adaptive Throttling ===")]
    [Tooltip("Bật Adaptive Throttling mặc định cho lần chạy đầu tiên.")]
    [SerializeField] private bool adaptiveEnabledByDefault = true;

    [Tooltip("Chu kỳ kiểm tra hiệu năng để quyết định hạ/nâng mức (giây).")]
    [SerializeField] private float adaptiveCheckInterval = 2f;

    [Tooltip("Số lần kiểm tra liên tiếp dưới ngưỡng mới hạ 1 mức.")]
    [SerializeField] private int requiredBadChecks = 3;

    [Tooltip("Số lần kiểm tra liên tiếp đạt mục tiêu mới nâng 1 mức.")]
    [SerializeField] private int requiredGoodChecks = 5;

    [Tooltip("Tỷ lệ FPS thực so với mục tiêu để coi là 'đang tụt hiệu năng' (vd 0.85).")]
    [Range(0.5f, 1f)]
    [SerializeField] private float underPerformanceRatio = 0.85f;

    [Tooltip("Tỷ lệ FPS thực so với mục tiêu để coi là 'đã hồi phục' (vd 0.98).")]
    [Range(0.9f, 1f)]
    [SerializeField] private float performanceRecoveryRatio = 0.98f;

    [Tooltip("Số bước tối đa hệ thống có thể tự hạ xuống.")]
    [Range(0, 4)]
    [SerializeField] private int maxAdaptiveStep = 2;

    [Tooltip("Số FPS bị giảm mỗi bước adaptive (nếu FPS đang cao).")]
    [SerializeField] private int frameRateDropPerStep = 15;

    [Tooltip("Trần FPS tối thiểu cho phép khi adaptive đang hạ mức.")]
    [SerializeField] private int minimumTargetFPS = 30;

    [Header("=== Pin & nhiệt (mobile) ===")]
    [Tooltip("Ngưỡng % pin coi là yếu để kích hoạt tiết kiệm điện (mobile only).")]
    [Range(0f, 50f)]
    [SerializeField] private float lowBatteryThresholdPercent = 20f;

    [Header("=== Test pin (dev) ===")]
    [Tooltip("-1 (mặc định) = dùng % pin thật của thiết bị.\n0-100 = mô phỏng mức pin giả để test cảnh báo tiết kiệm năng lượng (chạy được trên mọi nền tảng).\nMô phỏng coi như máy ĐANG KHÔNG SẠC nên sẽ kích hoạt đúng cảnh báo.")]
    [Range(-1f, 100f)]
    [SerializeField] private float simulateBatteryPercent = -1f;

    [Tooltip("Có đọc % pin thật trên desktop (Windows/Mac) hay không.\nMặc định bỏ qua vì nhiều máy báo sai (vd: luôn ra 1%). Trên mobile luôn đọc pin thật.")]
    [SerializeField] private bool checkBatteryOnDesktop = false;

    [Header("=== Cấu hình thấp ===")]
    [Tooltip("Bật 'Cấu hình thấp' mặc định cho lần chạy đầu tiên (thường để tắt).")]
    [SerializeField] private bool lowGraphicsEnabledByDefault = false;

    [Header("=== Render Profile (GPU) — 1 bộ cài đặt cho mỗi nấc ===")]
    [Tooltip("Nấc High: render scale 1.0, MSAA 4x, bóng nét nhiều cascade.")]
    [SerializeField] private RenderProfile renderProfileHigh = RenderProfile.HighPreset;

    [Tooltip("Nấc Medium: cân bằng hình ảnh/hiệu năng.")]
    [SerializeField] private RenderProfile renderProfileMedium = RenderProfile.MediumPreset;

    [Tooltip("Nấc Low hoặc bật Cấu hình thấp: ưu tiên FPS (bóng tắt, MSAA 0).")]
    [SerializeField] private RenderProfile renderProfileLow = RenderProfile.LowPreset;

    // =============================================================
    //  RUNTIME STATE
    // =============================================================
    private FrameRateMode frameRateMode = FrameRateMode.Auto;
    private bool adaptiveEnabled;
    private int baseQualityLevel;
    private bool lowGraphicsEnabled;
    private bool vSyncEnabled;

    private float fpsAccumulator;        // thời gian tích lũy trong cửa sổ mẫu
    private float frameTimeAccumulator;  // tổng frame time trong cửa sổ mẫu (ms)
    private int fpsFrameCount;           // số frame đã qua trong cửa sổ mẫu
    private float minFrameTimeMs;        // frame time nhỏ nhất cửa sổ hiện tại
    private float maxFrameTimeMs;        // frame time lớn nhất cửa sổ hiện tại
    private float lastMinFrameTimeMs;    // frame time nhỏ nhất cửa sổ vừa xong
    private float lastMaxFrameTimeMs;    // frame time lớn nhất cửa sổ vừa xong
    private float smoothedFPS;
    private float smoothedFrameTimeMs;

    private int adaptiveStep;            // 0 = cao nhất
    private float adaptiveTimer;
    private int consecutiveBadChecks;
    private int consecutiveGoodChecks;
    private bool batteryWarningRaised;

    // =============================================================
    //  PUBLIC PROPERTIES (mọi thứ dưới đây là dữ liệu thuần, không đụng UI)
    // =============================================================

    /// <summary>Chế độ frame rate người chơi đang chọn.</summary>
    public FrameRateMode CurrentMode => frameRateMode;

    /// <summary>FPS mục tiêu gốc theo chế độ đã chọn (trước khi adaptive giảm).</summary>
    public int RawTargetFPS => GetTargetFPS(frameRateMode);

    /// <summary>
    /// FPS mục tiêu đang thực sự áp dụng: mục tiêu gốc trừ bước hạ của adaptive.
    /// KHÔNG clamp theo tần số quét hiện tại của màn hình vì:
    ///   1. Screen.currentResolution.refreshRateRatio (Android) trả tần số ĐANG
    ///      chạy (thường 60) chứ không phải tần số tối đa → clamp sai sẽ chặn luôn
    ///      việc đạt 90/120 trên màn hình 90/120Hz.
    ///   2. Làm nút VSync "vô hình": tắt VSync thì target cũng bị ép bằng khi bật.
    ///   Việc không vượt quá Hz của panel do VSync / phần cứng tự xử lý.
    /// </summary>
    public int AppliedTargetFPS => Mathf.Max(minimumTargetFPS, RawTargetFPS - adaptiveStep * frameRateDropPerStep);

    /// <summary>FPS trung bình (làm mượt) của cửa sổ mẫu vừa qua.</summary>
    public float SmoothedFPS => smoothedFPS;

    /// <summary>Frame time trung bình (ms) của cửa sổ mẫu vừa qua.</summary>
    public float SmoothedFrameTimeMs => smoothedFrameTimeMs;

    /// <summary>Frame time hiển thị (ms) ngưỡng cho phép theo FPS mục tiêu hiện tại.</summary>
    public float CurrentFrameBudgetMs => GetFrameBudgetMilliseconds(AppliedTargetFPS);

    /// <summary>Có đang vượt frame budget của FPS mục tiêu ở cửa sổ vừa qua hay không.</summary>
    public bool IsExceedingFrameBudget => lastMaxFrameTimeMs > CurrentFrameBudgetMs;

    /// <summary>Adaptive Throttling có đang bật hay không.</summary>
    public bool AdaptiveEnabled => adaptiveEnabled;

    /// <summary>Mức chất lượng đồ họa đang thực sự áp dụng (0..QualitySettings.names.Length-1).</summary>
    public int AppliedQualityLevel => lowGraphicsEnabled
        ? 0
        : Mathf.Clamp(baseQualityLevel - adaptiveStep, 0, QualitySettings.names.Length - 1);

    /// <summary>Chất lượng đồ họa gốc người chơi chọn (trước adaptive và trước cấu hình thấp).</summary>
    public int BaseQualityLevel => baseQualityLevel;

    /// <summary>Chế độ cấu hình thấp: ép chất lượng về mức 0 để ưu tiên hiệu năng.</summary>
    public bool LowGraphicsEnabled => lowGraphicsEnabled;

    /// <summary>VSync thủ công đang bật hay không (bật = khung hình đồng bộ màn hình).</summary>
    public bool VSyncEnabled => vSyncEnabled;

    /// <summary>Render Scale URP đang áp dụng — đi theo MỨC CHẤT LƯỢNG người chơi chọn
    /// (High=1.0 / Medium=0.875 / Low=0.7 theo RenderProfile), chứ không theo adaptive.
    /// Adaptive/low-graphics chỉ làm giảm mức áp dụng (AppliedQualityLevel).</summary>
    public float CurrentRenderScale =>
        GetRenderScaleForLevel(GetPerformanceLevelForQuality(AppliedQualityLevel));

    /// <summary>Mức render scale đang áp dụng (Low/Medium/High), xác định từ
    /// CurrentRenderScale so với 3 mức định sẵn. Nút chất lượng UI dựa vào
    /// đây để highlight nút đang hoạt động.</summary>
    public PerformanceLevel CurrentRenderScaleLevel
    {
        get
        {
            float current = CurrentRenderScale;
            float lowScale = GetRenderScaleForLevel(PerformanceLevel.Low);
            float midScale = GetRenderScaleForLevel(PerformanceLevel.Medium);
            float highScale = GetRenderScaleForLevel(PerformanceLevel.High);

            bool isLow = Mathf.Abs(current - lowScale) <= Mathf.Abs(current - midScale);
            if (isLow) return PerformanceLevel.Low;
            bool isHigh = Mathf.Abs(current - highScale) < Mathf.Abs(current - midScale);
            return isHigh ? PerformanceLevel.High : PerformanceLevel.Medium;
        }
    }

    /// <summary>Mức hiệu năng tổng thể hiện tại do adaptive duy trì.</summary>
    public PerformanceLevel CurrentPerformanceLevel =>
        adaptiveStep <= 0 ? PerformanceLevel.High
        : adaptiveStep == 1 ? PerformanceLevel.Medium
        : PerformanceLevel.Low;

    /// <summary>Số mức chất lượng đồ họa có sẵn của dự án.</summary>
    public static int QualityLevelCount => Mathf.Max(1, QualitySettings.names.Length);

    // =============================================================
    //  PUBLIC EVENTS - UI đăng ký lắng nghe, KHÔNG được gọi logic lên manager
    // =============================================================

    /// <summary>Được kích hoạt mỗi khi bám mẫu FPS mới (kèm FPS làm mượt).</summary>
    public event Action<float> OnFPSUpdated;

    /// <summary>Khi chế độ frame rate được thay đổi.</summary>
    public event Action<FrameRateMode> OnFrameRateModeChanged;

    /// <summary>Khi chất lượng đồ họa áp dụng thay đổi (kèm mức áp dụng).</summary>
    public event Action<int> OnQualityLevelChanged;

    /// <summary>Khi adaptive throttling bật/tắt.</summary>
    public event Action<bool> OnAdaptiveEnabledChanged;

    /// <summary>Khi adaptive tự hạ/nâng mức hiệu năng.</summary>
    public event Action<PerformanceLevel> OnAdaptiveStepChanged;

    /// <summary>Khi phát hiện pin yếu (mobile) — kèm % pin còn lại.</summary>
    public event Action<float> OnBatteryWarning;

    /// <summary>Khi chế độ cấu hình thấp được bật/tắt.</summary>
    public event Action<bool> OnLowGraphicsChanged;

    /// <summary>Khi bật/tắt VSync thủ công.</summary>
    public event Action<bool> OnVSyncChanged;

    // =============================================================
    //  PHẦN 1: KHỞI TẠO (Singleton + đọc cài đặt đã lưu)
    // =============================================================

    /// <summary>
    /// Awake: ép Singleton duy nhất, sống xuyên scene, đọc cài đặt PlayerPrefs và áp dụng.
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

        minFrameTimeMs = float.MaxValue;
        frameRateMode = (FrameRateMode)Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_FRAME_RATE_MODE, (int)FrameRateMode.Auto),
            (int)FrameRateMode.Auto, (int)FrameRateMode.FPS120);

        adaptiveEnabled = PlayerPrefs.GetInt(KEY_ADAPTIVE_ENABLED, adaptiveEnabledByDefault ? 1 : 0) == 1;
        lowGraphicsEnabled = PlayerPrefs.GetInt(KEY_LOW_GRAPHICS, lowGraphicsEnabledByDefault ? 1 : 0) == 1;
        vSyncEnabled = PlayerPrefs.GetInt(KEY_VSYNC, 0) == 1;
        baseQualityLevel = Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_QUALITY_LEVEL, QualitySettings.GetQualityLevel()),
            0, QualityLevelCount - 1);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        // Bản Release: luôn dùng % pin thật của máy. Nếu trong Editor từng gán
        // simulateBatteryPercent (vd 5) để test, giá trị đó bị nướng vào build
        // và sẽ khóa cảnh báo yếu pin lại mãi -> buộc về -1 khi build ra ngoài.
        simulateBatteryPercent = -1f;
#endif

        ApplyAll();

        // Bộ nhớ thấp: hạ mức tối đa ngay để cứu game khỏi súp sệt / bị kill.
        Application.lowMemory += OnLowMemoryWarning;

        Debug.Log($"[PerformanceManager] 🚀 Khởi tạo: Mode={frameRateMode}, Target={AppliedTargetFPS} FPS, Quality={AppliedQualityLevel}, Adaptive={adaptiveEnabled}");
    }

    /// <summary>
    /// Lặp lại ApplyFrameRate sau khi toàn bộ scene đã Awake xong. Vài nền tảng
    /// (nhất là mobile) cài đặt vSyncCount rất sớm trong Awake thì không ăn —
    /// Start là lúc graphics đã sẵn sàng nên cài lại chắc chắn hơn.
    /// </summary>
    private void Start()
    {
        ApplyFrameRate();
        GameManager.OnStateChanged += OnGameStateChanged;
    }

    /// <summary>
    /// [TỰ ĐỘNG] Nếu scene chưa có PerformanceManager, tự tạo 1 cái ngay sau khi scene đầu tiên load.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null || FindObjectOfType<PerformanceManager>() != null)
            return;

        GameObject go = new GameObject("PerformanceManager");
        go.AddComponent<PerformanceManager>();
    }

    /// <summary>
    /// Hủy đăng ký event của UnityEngine để tránh leak reference.
    /// </summary>
    private void OnDestroy()
    {
        Application.lowMemory -= OnLowMemoryWarning;
        GameManager.OnStateChanged -= OnGameStateChanged;
        if (Instance == this)
            Instance = null;
    }

    private void OnGameStateChanged(GameState state)
    {
        // Khi chuyển state -> re-apply render profile theo mức chất lượng đã chọn
        // (render scale áp dụng đồng nhất cả menu lẫn gameplay).
        ApplyRenderProfile();
    }

    // =============================================================
    //  PHẦN 2: UPDATE LOOP - chỉ phép toán số học, KHÔNG cấp phát
    // =============================================================

    /// <summary>
    /// Update: tích lũy frame time bằng Time.unscaledDeltaTime (vẫn chạy khi tạm dừng),
    /// bám mẫu theo cửa sổ thời gian, gọi event FPS. Kiểm tra adaptive theo chu kỳ riêng để
    /// không đọc batteryStatus (tốn chi phí trên mobile) mỗi frame.
    /// </summary>
    private void Update()
    {
        float frameTime = Time.unscaledDeltaTime;
        fpsAccumulator += frameTime;
        frameTimeAccumulator += frameTime;
        fpsFrameCount++;
        minFrameTimeMs = Mathf.Min(minFrameTimeMs, frameTime * 1000f);
        maxFrameTimeMs = Mathf.Max(maxFrameTimeMs, frameTime * 1000f);

        if (fpsAccumulator >= fpsSampleInterval)
        {
            if (fpsFrameCount > 0)
            {
                smoothedFrameTimeMs = frameTimeAccumulator / fpsFrameCount * 1000f;
                smoothedFPS = fpsFrameCount / fpsAccumulator;
            }
            lastMinFrameTimeMs = minFrameTimeMs;
            lastMaxFrameTimeMs = maxFrameTimeMs;
            fpsAccumulator = 0f;
            frameTimeAccumulator = 0f;
            fpsFrameCount = 0;
            minFrameTimeMs = float.MaxValue;
            maxFrameTimeMs = 0f;

            OnFPSUpdated?.Invoke(smoothedFPS);
        }

        if (adaptiveEnabled)
        {
            adaptiveTimer += frameTime;
            if (adaptiveTimer >= adaptiveCheckInterval)
            {
                adaptiveTimer = 0f;
                CheckAdaptiveState();
            }
        }
        else if (adaptiveStep != 0)
        {
            // Tắt adaptive: khôi phục ngay mức hiệu năng cao nhất.
            adaptiveStep = 0;
            consecutiveBadChecks = 0;
            consecutiveGoodChecks = 0;
            ApplyAll();
        }
    }

    // =============================================================
    //  PHẦN 3: ADAPTIVE THROTTLING - tự hạ/nâng mức hiệu năng
    // =============================================================

    /// <summary>
    /// Kiểm tra chu kỳ: nếu FPS tụt dưới ngưỡng liên tục đủ số lần thì hạ 1 mức
    /// (giảm chất lượng + giảm FPS mục tiêu); nếu hồi phục bền vững thì nâng lại.
    /// </summary>
    private void CheckAdaptiveState()
    {
        CheckBattery();

        if (smoothedFPS <= 0f) return; // chưa đủ mẫu, bỏ qua

        float target = AppliedTargetFPS;
        float underThreshold = target * underPerformanceRatio;
        float goodThreshold = target * performanceRecoveryRatio;

        if (smoothedFPS < underThreshold)
        {
            consecutiveBadChecks++;
            consecutiveGoodChecks = 0;
            if (consecutiveBadChecks >= requiredBadChecks && adaptiveStep < maxAdaptiveStep)
            {
                consecutiveBadChecks = 0;
                AdaptiveStepDown();
            }
        }
        else if (smoothedFPS >= goodThreshold)
        {
            consecutiveGoodChecks++;
            consecutiveBadChecks = 0;
            if (consecutiveGoodChecks >= requiredGoodChecks && adaptiveStep > 0)
            {
                consecutiveGoodChecks = 0;
                AdaptiveStepUp();
            }
        }
        else
        {
            consecutiveBadChecks = 0;
            consecutiveGoodChecks = 0;
        }
    }

    /// <summary>
    /// Hạ 1 bước: giảm chất lượng và FPS mục tiêu, thông báo qua event.
    /// </summary>
    private void AdaptiveStepDown()
    {
        adaptiveStep = Mathf.Min(maxAdaptiveStep, adaptiveStep + 1);
        Debug.Log($"[PerformanceManager] ⚠️ Hiệu năng tụt, hạ xuống mức {CurrentPerformanceLevel} (Target={AppliedTargetFPS} FPS, Quality={AppliedQualityLevel})");
        ApplyAdaptiveStep();
    }

    /// <summary>
    /// Nâng 1 bước khi hiệu năng đã hồi phục bền vững.
    /// </summary>
    private void AdaptiveStepUp()
    {
        adaptiveStep = Mathf.Max(0, adaptiveStep - 1);
        Debug.Log($"[PerformanceManager] ✅ Hiệu năng hồi phục, nâng lên mức {CurrentPerformanceLevel} (Target={AppliedTargetFPS} FPS, Quality={AppliedQualityLevel})");
        ApplyAdaptiveStep();
    }

    /// <summary>
    /// Ghi đè % pin giả để test cảnh báo yếu pin ở nhiều mức (0-100).
    /// Truyền -1 để quay lại dùng % pin thật của thiết bị.
    /// </summary>
    /// <param name="percent">% pin mô phỏng (0-100) hoặc -1 = tắt mô phỏng.</param>
    public void SetSimulatedBatteryPercent(float percent)
    {
        simulateBatteryPercent = Mathf.Clamp(percent, -1f, 100f);
        batteryWarningRaised = false;
        Debug.Log($"[PerformanceManager] 🔋 Mô phỏng pin: {(simulateBatteryPercent < 0f ? "TẮT (dùng pin thật)" : simulateBatteryPercent + "%")}");
    }

    /// <summary>
    /// Kiểm tra pin: nếu pin yếu và không sạc, hạ mức tiết kiệm tối đa 1 lần.
    /// - Mobile: luôn đọc % pin thật của thiết bị.
    /// - Desktop: bỏ qua pin thật trừ khi bật checkBatteryOnDesktop (tránh báo sai).
    /// - simulateBatteryPercent ≥ 0: dùng pin giả để test cảnh báo ở mọi nền tảng.
    /// </summary>
    private void CheckBattery()
    {
        float battery;
        bool charging;

        bool simulating = simulateBatteryPercent >= 0f;
        if (simulating)
        {
            battery = simulateBatteryPercent;
            charging = false; // mô phỏng coi như đang KHÔNG SẠC để tái hiện đúng cảnh báo
        }
        else
        {
            if (!Application.isMobilePlatform && !checkBatteryOnDesktop)
                return;

            float level = BatteryInfo.batteryLevel;
            if (level < 0f) return; // không hỗ trợ đọc pin
            // Unity trả 0-1, nhưng một số device/editor trả thẳng 0-100 -> chuẩn hóa.
            battery = level > 1f ? level : level * 100f;
            charging = BatteryInfo.batteryStatus == BatteryStatusType.Charging;
        }

        bool lowBattery = !charging && battery <= lowBatteryThresholdPercent;

        if (lowBattery)
        {
            if (!batteryWarningRaised)
            {
                batteryWarningRaised = true;
                Debug.LogWarning($"[PerformanceManager] 🔋 Pin yếu ({battery:0}%), kích hoạt tiết kiệm điện.");
                OnBatteryWarning?.Invoke(battery);
            }
            if (adaptiveStep < maxAdaptiveStep)
            {
                adaptiveStep = maxAdaptiveStep;
                ApplyAdaptiveStep();
            }
        }
        else
        {
            batteryWarningRaised = false;
        }
    }

    /// <summary>
    /// Cảnh báo bộ nhớ thấp từ UnityEngine: hạ mức hiệu năng thấp nhất để cứu frame.
    /// </summary>
    private void OnLowMemoryWarning()
    {
        if (adaptiveStep < maxAdaptiveStep)
        {
            adaptiveStep = maxAdaptiveStep;
            Debug.LogWarning("[PerformanceManager] 🧠 Bộ nhớ thấp, hạ chất lượng xuống tối đa.");
            ApplyAdaptiveStep();
        }
    }

    /// <summary>
    /// Áp dụng bước adaptive hiện tại lên FPS + chất lượng + render profile, phát event.
    /// </summary>
    private void ApplyAdaptiveStep()
    {
        ApplyQuality();
        ApplyRenderProfile();
        ApplyFrameRate();
        OnAdaptiveStepChanged?.Invoke(CurrentPerformanceLevel);
    }

    // =============================================================
    //  PHẦN 4: PUBLIC API - UI gọi xuống để thay đổi cài đặt
    // =============================================================

    /// <summary>
    /// Đổi chế độ frame rate (Auto/30/60/90/120) và lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="mode">Chế độ frame rate mới.</param>
    public void SetFrameRateMode(FrameRateMode mode)
    {
        if (frameRateMode == mode) return;

        frameRateMode = mode;
        ApplyFrameRate();
        SavePreferences();
        OnFrameRateModeChanged?.Invoke(frameRateMode);
        //Debug.Log($"[PerformanceManager] 🚀 Đổi chế độ frame rate: {mode} (Target={AppliedTargetFPS} FPS)");
    }

    /// <summary>
    /// Đổi chất lượng đồ họa gốc (0..QualityLevelCount-1) và lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="level">Mức chất lượng mới.</param>
    public void SetQualityLevel(int level)
    {
        int clamped = Mathf.Clamp(level, 0, QualityLevelCount - 1);
        if (baseQualityLevel == clamped) return;

        baseQualityLevel = clamped;
        ApplyQuality();
        ApplyRenderProfile(); // render profile giờ theo chất lượng
        ApplyFrameRate(); // quality mới có thể bật lại VSync -> áp lại target
        SavePreferences();
        OnQualityLevelChanged?.Invoke(AppliedQualityLevel);
        //Debug.Log($"[PerformanceManager] 🚀 Đổi chất lượng đồ họa: {clamped} (áp dụng {AppliedQualityLevel})");
    }

    /// <summary>
    /// Đổi chất lượng đồ họa theo 1 trong 3 nấc hiệu năng — khớp đúng với
    /// Render Profile GPU đã định sẵn trong Inspector (Low → renderProfileLow,
    /// Medium → renderProfileMedium, High → renderProfileHigh). Dùng cho nút
    /// chất lượng trong Settings.
    /// </summary>
    /// <param name="level">Nấc hiệu năng/profile cần áp dụng.</param>
    public void SetQualityPreset(PerformanceLevel level)
    {
        SetQualityLevel(GetQualityLevelForPerformanceLevel(level));
    }

    /// <summary>
    /// Bật/tắt Adaptive Throttling và lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="enabled">true = bật tự điều chỉnh hiệu năng.</param>
    public void SetAdaptiveEnabled(bool enabled)
    {
        if (adaptiveEnabled == enabled) return;

        adaptiveEnabled = enabled;
        if (!adaptiveEnabled)
        {
            adaptiveStep = 0;
            consecutiveBadChecks = 0;
            consecutiveGoodChecks = 0;
        }
        ApplyAll();
        SavePreferences();
        OnAdaptiveEnabledChanged?.Invoke(adaptiveEnabled);
        Debug.Log($"[PerformanceManager] 🚀 Adaptive Throttling: {(adaptiveEnabled ? "BẬT" : "TẮT")}");
    }

    /// <summary>
    /// Bật/tắt chế độ cấu hình thấp: ép chất lượng về mức 0 + render scale thấp nhất
    /// (không đổi chất lượng gốc đã lưu, nên khi tắt sẽ khôi phục nguyên trạng).
    /// Lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="enabled">true = bật cấu hình thấp.</param>
    public void SetLowGraphics(bool enabled)
    {
        if (lowGraphicsEnabled == enabled) return;

        lowGraphicsEnabled = enabled;
        ApplyQuality();
        ApplyRenderProfile();
        ApplyFrameRate(); // quality ép về 0 có thể đổi VSync -> áp lại target
        SavePreferences();
        OnLowGraphicsChanged?.Invoke(lowGraphicsEnabled);
        OnQualityLevelChanged?.Invoke(AppliedQualityLevel);
        //Debug.Log($"[PerformanceManager] 🚀 Cấu hình thấp: {(lowGraphicsEnabled ? "BẬT" : "TẮT")} (Quality={AppliedQualityLevel}, RenderScale={CurrentRenderScale:0.00})");
    }

    /// <summary>
    /// Bật/tắt VSync thủ công và lưu ngay vào PlayerPrefs.
    /// - BẬT:   dùng VSync (khung hình đồng bộ màn hình), FPS do màn hình quyết định
    ///          (60/120...), không tổn hao GPU phế phẩm; thích hợp khoe nét mượt.
    /// - TẮT:   trả về chế độ targetFrameRate theo frame rate đã chọn (Auto/30/60/90/120).
    /// Lưu ý: nếu không dùng nút này thì tất cả do ApplyFrameRate tự quyết (mặc định TẮT).
    /// </summary>
    /// <param name="enabled">true = bật VSync.</param>
    public void SetVSyncEnabled(bool enabled)
    {
        if (vSyncEnabled == enabled) return;

        vSyncEnabled = enabled;
        ApplyFrameRate();
        SavePreferences();
        OnVSyncChanged?.Invoke(vSyncEnabled);
        Debug.Log($"[PerformanceManager] 🎮 VSync: {(vSyncEnabled ? "BẬT" : "TẮT")} (Target={AppliedTargetFPS} FPS)");
    }

    // =============================================================
    //  PHẦN 5: HELPERS - áp dụng cài đặt và tính frame budget
    // =============================================================

    /// <summary>
    /// Trả về FPS mục tiêu gốc của một chế độ. Chế độ Auto chọn theo tần số quét màn hình
    /// (ưu tiên 120/90/60) và rơi về autoTargetFPS cho màn hình thường.
    /// </summary>
    /// <param name="mode">Chế độ cần tra FPS mục tiêu.</param>
    /// <returns>FPS mục tiêu gốc (&gt; 0).</returns>
    public int GetTargetFPS(FrameRateMode mode)
    {
        switch (mode)
        {
            case FrameRateMode.FPS30: return 30;
            case FrameRateMode.FPS60: return 60;
            case FrameRateMode.FPS90: return 90;
            case FrameRateMode.FPS120: return 120;
            default:
#if UNITY_2022_2_OR_NEWER
                double refresh = System.Math.Max(1.0, Screen.currentResolution.refreshRateRatio.value);
#else
                double refresh = System.Math.Max(1, Screen.currentResolution.refreshRate);
#endif
                if (refresh >= 120.0) return 120;
                if (refresh >= 90.0) return 90;
                if (refresh >= 60.0) return 60;
                return Mathf.Clamp(autoTargetFPS, 30, 60);
        }
    }

    /// <summary>
    /// Hàm tĩnh: trả về thời gian tối đa cho phép của 1 frame (ms) theo FPS mục tiêu.
    /// 60FPS => 16.67ms, 90FPS => 11.11ms, 120FPS => 8.33ms.
    /// </summary>
    /// <param name="fps">FPS mục tiêu.</param>
    /// <returns>Frame budget tính bằng mili-giây.</returns>
    public static float GetFrameBudgetMilliseconds(int fps)
    {
        if (fps <= 0) return float.PositiveInfinity;
        return 1000f / fps;
    }

    /// <summary>
    /// Áp dụng toàn bộ cài đặt (gọi khi khởi tạo hoặc bật/tắt adaptive).
    /// </summary>
    private void ApplyAll()
    {
        ApplyQuality();
        ApplyRenderProfile();
        ApplyFrameRate();
        OnFrameRateModeChanged?.Invoke(frameRateMode);
        OnQualityLevelChanged?.Invoke(AppliedQualityLevel);
        OnAdaptiveEnabledChanged?.Invoke(adaptiveEnabled);
    }

/// <summary>
/// Lấy URP asset đang dùng, tương thích nhiều phiên bản Unity
/// (defaultRenderPipelineAsset chỉ có từ 2023.1; 2022.x dùng renderPipelineAsset).
/// </summary>
/// <returns>UniversalRenderPipelineAsset hoặc null nếu không dùng URP.</returns>
private static UniversalRenderPipelineAsset GetURPAsset()
{
    RenderPipelineAsset pipeline =
#if UNITY_2023_1_OR_NEWER
        GraphicsSettings.defaultRenderPipelineAsset;
#else
        GraphicsSettings.renderPipelineAsset;
#endif
    return pipeline as UniversalRenderPipelineAsset;
}

/// <summary>
    /// Trả về RenderProfile của 1 nấc hiệu năng (đã cấu hình trong Inspector).
    /// </summary>
    /// <param name="level">Mức hiệu năng cần tra.</param>
    /// <returns>RenderProfile tương ứng.</returns>
    public RenderProfile GetRenderProfile(PerformanceLevel level)
    {
        switch (level)
        {
            case PerformanceLevel.High: return renderProfileHigh;
            case PerformanceLevel.Medium: return renderProfileMedium;
            default: return renderProfileLow;
        }
    }

    /// <summary>
    /// Trả về render scale tương ứng 1 nấc hiệu năng — đọc từ RenderProfile
    /// (renderScale trong profile là giá trị TUYỆT ĐỐI, không nhân thêm gì).
    /// </summary>
    /// <param name="level">Mức hiệu năng cần tra.</param>
    /// <returns>Render scale đã clamp trong khoảng hợp lệ.</returns>
    public float GetRenderScaleForLevel(PerformanceLevel level)
    {
        return Mathf.Clamp(GetRenderProfile(level).renderScale, 0.1f, 1f);
    }

    /// <summary>
    /// Quy mức chất lượng đồ họa (0..QualityLevelCount-1) về 1 trong 3 nấc hiệu
    /// năng để chọn render profile: Low = mức 0, Medium = mức giữa, High = mức cao
    /// nhất. Chọn nấc gần nhất (cùng khoảng cách thì về nấc thấp hơn).
    /// </summary>
    public static PerformanceLevel GetPerformanceLevelForQuality(int qualityLevel)
    {
        int low = 0;
        int mid = QualityLevelCount / 2;
        int high = QualityLevelCount - 1;

        bool isLow = Mathf.Abs(qualityLevel - low) <= Mathf.Abs(qualityLevel - mid);
        if (isLow) return PerformanceLevel.Low;
        bool isHigh = Mathf.Abs(qualityLevel - high) < Mathf.Abs(qualityLevel - mid);
        return isHigh ? PerformanceLevel.High : PerformanceLevel.Medium;
    }

    /// <summary>
    /// Quy 1 mức hiệu năng (Low/Medium/High) về mức chất lượng đồ họa tương ứng:
    /// Low = mức 0, Medium = mức giữa, High = mức cao nhất. Ngược với
    /// GetPerformanceLevelForQuality — dùng để khớp nút chất lượng với
    /// Render Profile GPU đã định sẵn.
    /// </summary>
    public static int GetQualityLevelForPerformanceLevel(PerformanceLevel level)
    {
        switch (level)
        {
            case PerformanceLevel.High: return QualityLevelCount - 1;
            case PerformanceLevel.Medium: return QualityLevelCount / 2;
            default: return 0;
        }
    }

    /// <summary>
    /// Áp dụng RenderProfile của mức đang áp dụng xuống URP asset (chỉ khi đang dùng URP).
    /// Gồm: render scale, MSAA, shadow distance (0 = tắt bóng), số cascade.
    /// Áp dụng ĐỒNG NHẤT cả menu lẫn gameplay theo chất lượng người chơi chọn —
    /// trước đây gameplay bị ÉP render scale = 1.0 (bất chấp mức chất lượng) nên
    /// máy yếu/chọn Low vẫn chạy full resolution → lag trận đấu.
    /// Tránh ghi lại khi giá trị chưa đổi để không gây re-alloc render target.
    /// </summary>
    private void ApplyRenderProfile()
    {
        UniversalRenderPipelineAsset urp = GetURPAsset();
        if (urp == null)
            return;

        PerformanceLevel tier = GetPerformanceLevelForQuality(AppliedQualityLevel);
        RenderProfile profile = GetRenderProfile(tier);

        bool changed = false;

        float scale = Mathf.Clamp(profile.renderScale, UniversalRenderPipeline.minRenderScale, 1f);
        if (Mathf.Abs(urp.renderScale - scale) > 0.001f)
        {
            urp.renderScale = scale;
            changed = true;
        }

        int msaa = RenderProfile.ValidateMsaa(profile.msaaSampleCount);
        if (urp.msaaSampleCount != msaa)
        {
            urp.msaaSampleCount = msaa;
            changed = true;
        }

        float shadowDistance = Mathf.Max(0f, profile.shadowDistance);
        if (Mathf.Abs(urp.shadowDistance - shadowDistance) > 0.01f)
        {
            urp.shadowDistance = shadowDistance;
            changed = true;
        }

        int cascades = RenderProfile.ValidateCascades(profile.shadowCascadeCount);
        if (urp.shadowCascadeCount != cascades)
        {
            urp.shadowCascadeCount = cascades;
            changed = true;
        }

        if (changed)
            Debug.Log($"[PerformanceManager] Render Profile {tier} | Scale={scale:0.00} MSAA={msaa}x Shadows={shadowDistance} Cascades={cascades}");
    }

    /// <summary>
    /// Áp dụng chế độ đồng bộ hiện tại:
    /// - Nếu bật VSync thủ công: vSyncCount = 1 và bỏ targetFrameRate (màn hình tự đồng bộ).
    /// - Ngược lại: tắt VSync mọi mức (vì VSync đè lên targetFrameRate làm chọn
    ///   FPS 120/60/30 vô dụng) rồi áp targetFrameRate theo chế độ người chơi chọn.
    /// </summary>
    private void ApplyFrameRate()
    {
        if (vSyncEnabled)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            return;
        }

        if (QualitySettings.vSyncCount > 0)
            QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = AppliedTargetFPS;
    }

    /// <summary>
    /// Áp dụng chất lượng đồ họa (đã trừ bước adaptive; ép về 0 nếu đang bật cấu hình thấp).
    /// </summary>
    private void ApplyQuality()
    {
        int effective = lowGraphicsEnabled
            ? 0
            : Mathf.Clamp(baseQualityLevel - adaptiveStep, 0, QualityLevelCount - 1);
        if (QualitySettings.GetQualityLevel() != effective)
            QualitySettings.SetQualityLevel(effective, false);

        // QualitySettings.SetQualityLevel (kể cả applyExpensiveSettings=false) có thể
        // reset vSyncCount về giá trị mặc định trong QualitySettings.asset — nên
        // nếu đang bật VSync thủ công thì phải set lại ngay.
        if (vSyncEnabled)
            QualitySettings.vSyncCount = 1;
    }

    /// <summary>
    /// Lưu toàn bộ cài đặt hiện tại vào PlayerPrefs và gọi Save() ngay.
    /// </summary>
    private void SavePreferences()
    {
        PlayerPrefs.SetInt(KEY_FRAME_RATE_MODE, (int)frameRateMode);
        PlayerPrefs.SetInt(KEY_QUALITY_LEVEL, baseQualityLevel);
        PlayerPrefs.SetInt(KEY_ADAPTIVE_ENABLED, adaptiveEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_LOW_GRAPHICS, lowGraphicsEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_VSYNC, vSyncEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}