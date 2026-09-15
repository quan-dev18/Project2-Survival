using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// ============================================================================
/// PerformanceSettingUIController - UI View Controller cho phần hiệu năng.
///
/// ▐▌ VAI TRÒ
///   - Cập nhật UI (Text FPS, Dropdown frame rate / chất lượng, Toggle adaptive)
///     theo đúng dữ liệu của PerformanceManager.
///   - KHÔNG chứa logic quyết định: mọi thay đổi chỉ đẩy xuống Core Manager qua
///     SetFrameRateMode / SetQualityLevel / SetAdaptiveEnabled, và chỉ update UI
///     khi nhận sự kiện (OnFPSUpdated / OnFrameRateModeChanged / ...).
///   - Tách bạch UI vs Logic theo đúng pattern của dự án.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR
///   1. Gắn script lên GameObject panel Settings (PanelPerformance).
///   2. Kéo thả:
///        - TMP_Text "Fps Text"      (text xanh 1 dòng, dòng 1: FPS, dòng 2: budget)
///        - TMP_Dropdown "Frame Rate Dropdown"   (mục 'Auto')
///        - TMP_Dropdown "Quality Dropdown"
///        - Toggle "Adaptive Toggle"
///        - GameObject "Low Power Warning"  (panel cảnh báo pin yếu, mặc định tắt)
///        - TMP_Text "Performance Level Text" (tùy chọn: High/Medium/Low)
///   3. PerformanceManager tự sinh runtime, KHÔNG cần kéo.
/// ============================================================================
public class PerformanceSettingUIController : MonoBehaviour
{
    [Header("FPS Overlay (có thể để trống)")]
    [Tooltip("Text hiển thị FPS và frame budget hiện tại. Để trống nếu không cần.")]
    [SerializeField] private TMP_Text fpsText;

    [Tooltip("Text hiển thị mức hiệu năng (High/Medium/Low). Để trống nếu không cần.")]
    [SerializeField] private TMP_Text performanceLevelText;

    [Header("Frame Rate")]
    [Tooltip("Dropdown chọn chế độ frame rate (Auto/30/60/90/120).")]
    [SerializeField] private TMP_Dropdown frameRateDropdown;

    [Header("Chất lượng đồ họa")]
    [Tooltip("Dropdown chọn chất lượng đồ họa được đổ từ QualitySettings.")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Adaptive Throttling")]
    [Tooltip("Toggle bật/tắt tự điều chỉnh hiệu năng (chống nóng máy, tiết kiệm pin).")]
    [SerializeField] private Toggle adaptiveToggle;

    [Header("Cảnh báo pin yếu")]
    [Tooltip("Root panel cảnh báo pin yếu, hiện/ẩn theo event OnBatteryWarning. Có thể để trống.")]
    [SerializeField] private GameObject lowPowerWarningRoot;

    private PerformanceManager performance;
    private bool syncingUI;              // chặn hồi tiếp khi tự set giá trị dropdown/toggle
    private bool subscribed;

    // ──────────────────── Unity Callbacks ─────────────────

    /// <summary>
    /// Gắn sự kiện UI (dropdown, toggle) một lần.
    /// </summary>
    private void Awake()
    {
        if (frameRateDropdown != null)
            frameRateDropdown.onValueChanged.AddListener(OnFrameRateDropdownChanged);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
        if (adaptiveToggle != null)
            adaptiveToggle.onValueChanged.AddListener(OnAdaptiveToggleChanged);

        BuildFrameRateOptions();
    }

    /// <summary>
    /// Tìm PerformanceManager, điền options chất lượng một lần và đăng ký event.
    /// Nếu manager được auto-create hơi trễ (AfterSceneLoad), thử lại sau 1 frame.
    /// </summary>
    private void Start()
    {
        if (TryResolvePerformance())
            Bind();
        else
            Invoke(nameof(TryBindDelayed), 0.1f);
    }

    /// <summary>
    /// Thử tìm lại manager sau khi auto-create đã chạy xong.
    /// </summary>
    private void TryBindDelayed()
    {
        if (TryResolvePerformance())
            Bind();
        else
            Debug.LogWarning("[PerformanceSettingUIController] Không tìm thấy PerformanceManager! (<i>Script tự tạo khi scene load</i>)");
    }

    /// <summary>
    /// Tìm PerformanceManager qua Instance hoặc FindObjectOfType.
    /// </summary>
    private bool TryResolvePerformance()
    {
        performance = PerformanceManager.Instance != null
            ? PerformanceManager.Instance
            : FindObjectOfType<PerformanceManager>();
        return performance != null;
    }

    /// <summary>
    /// Điền options chất lượng, đăng ký toàn bộ event và đồng bộ UI 1 lần.
    /// </summary>
    private void Bind()
    {
        BuildQualityOptions();

        performance.OnFPSUpdated += OnFPSUpdated;
        performance.OnFrameRateModeChanged += OnFrameRateModeChanged;
        performance.OnQualityLevelChanged += OnQualityLevelChanged;
        performance.OnAdaptiveEnabledChanged += OnAdaptiveEnabledChanged;
        performance.OnAdaptiveStepChanged += OnAdaptiveStepChanged;
        performance.OnBatteryWarning += OnBatteryWarning;
        subscribed = true;

        RefreshAllUI();
    }

    /// <summary>
    /// Khi panel bật lại, đồng bộ lại toàn bộ UI theo trạng thái manager hiện tại.
    /// </summary>
    private void OnEnable()
    {
        if (performance == null || !subscribed) return;
        RefreshAllUI();
    }

    /// <summary>
    /// Hủy đăng ký event (cả callback UI lẫn manager) để tránh leak.
    /// </summary>
    private void OnDestroy()
    {
        if (performance != null)
        {
            performance.OnFPSUpdated -= OnFPSUpdated;
            performance.OnFrameRateModeChanged -= OnFrameRateModeChanged;
            performance.OnQualityLevelChanged -= OnQualityLevelChanged;
            performance.OnAdaptiveEnabledChanged -= OnAdaptiveEnabledChanged;
            performance.OnAdaptiveStepChanged -= OnAdaptiveStepChanged;
            performance.OnBatteryWarning -= OnBatteryWarning;
        }

        if (frameRateDropdown != null)
            frameRateDropdown.onValueChanged.RemoveListener(OnFrameRateDropdownChanged);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.RemoveListener(OnQualityDropdownChanged);
        if (adaptiveToggle != null)
            adaptiveToggle.onValueChanged.RemoveListener(OnAdaptiveToggleChanged);
    }

    // ──────────────────── UI -> Core (không logic ở đây) ─────

    /// <summary>
    /// Người chơi chọn chế độ frame rate: đẩy thẳng xuống PerformanceManager.
    /// </summary>
    private void OnFrameRateDropdownChanged(int index)
    {
        if (syncingUI || performance == null) return;
        performance.SetFrameRateMode((FrameRateMode)index);
    }

    /// <summary>
    /// Người chơi chọn chất lượng đồ họa: đẩy thẳng xuống PerformanceManager.
    /// </summary>
    private void OnQualityDropdownChanged(int index)
    {
        if (syncingUI || performance == null) return;
        performance.SetQualityLevel(index);
    }

    /// <summary>
    /// Người chơi bật/tắt adaptive: đẩy thẳng xuống PerformanceManager.
    /// Lưu ý: Offline sẽ không show panel cảnh báo pin nữa vì logic nằm ở Core.
    /// </summary>
    private void OnAdaptiveToggleChanged(bool enabled)
    {
        if (syncingUI || performance == null) return;
        performance.SetAdaptiveEnabled(enabled);
    }

    // ──────────────────── Core -> UI (chỉ update giao diện) ─

    /// <summary>
    /// Cập nhật text FPS + frame budget mỗi khi manager bám mẫu FPS mới.
    /// </summary>
    private void OnFPSUpdated(float fps)
    {
        if (fpsText == null || performance == null) return;

        float frameTimeMs = performance.SmoothedFrameTimeMs;
        float budgetMs = performance.CurrentFrameBudgetMs;
        fpsText.text = $"{fps:0} FPS";
        if (frameTimeMs > 0f)
            fpsText.text += $"\n{frameTimeMs:0.0}ms / {budgetMs:0.0}ms";
    }

    /// <summary>
    /// Đồng bộ dropdown frame rate theo manager (chống hồi tiếp bằng syncingUI).
    /// </summary>
    private void OnFrameRateModeChanged(FrameRateMode mode)
    {
        RefreshFrameRateDropdown((int)mode);
    }

    /// <summary>
    /// Đồng bộ dropdown chất lượng theo mức áp dụng của manager.
    /// </summary>
    private void OnQualityLevelChanged(int level)
    {
        RefreshQualityDropdown(level);
    }

    /// <summary>
    /// Đồng bộ toggle adaptive theo manager.
    /// </summary>
    private void OnAdaptiveEnabledChanged(bool enabled)
    {
        RefreshAdaptiveToggle(enabled);
    }

    /// <summary>
    /// Cập nhật text mức hiệu năng khi adaptive hạ/nâng bước.
    /// </summary>
    private void OnAdaptiveStepChanged(PerformanceLevel level)
    {
        if (performanceLevelText != null)
            performanceLevelText.text = level.ToString();
    }

    /// <summary>
    /// Hiện cảnh báo pin yếu theo event từ manager.
    /// </summary>
    private void OnBatteryWarning(float batteryPercent)
    {
        if (lowPowerWarningRoot != null)
            lowPowerWarningRoot.SetActive(true);
    }

    // ──────────────────── Helpers / Data binding ─────────────

    /// <summary>
    /// Tạo danh sách option cho dropdown frame rate từ enum FrameRateMode.
    /// </summary>
    private void BuildFrameRateOptions()
    {
        if (frameRateDropdown == null) return;

        string[] names = System.Enum.GetNames(typeof(FrameRateMode));
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>(names.Length);
        for (int i = 0; i < names.Length; i++)
            options.Add(new TMP_Dropdown.OptionData(names[i].Replace("FPS", "FPS ")));
        frameRateDropdown.ClearOptions();
        frameRateDropdown.AddOptions(options);
    }

    /// <summary>
    /// Đổ option chất lượng từ QualitySettings.names vào dropdown.
    /// </summary>
    private void BuildQualityOptions()
    {
        if (qualityDropdown == null || performance == null) return;

        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>(PerformanceManager.QualityLevelCount);
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            string name = QualitySettings.names[i];
            name = string.IsNullOrEmpty(name) ? $"Level {i}" : char.ToUpper(name[0]) + name.Substring(1);
            options.Add(new TMP_Dropdown.OptionData(name));
        }
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(options);
    }

    /// <summary>
    /// Đồng bộ toàn bộ UI với trạng thái hiện tại của manager.
    /// </summary>
    private void RefreshAllUI()
    {
        syncingUI = true;

        RefreshFrameRateDropdown((int)performance.CurrentMode);
        RefreshQualityDropdown(performance.AppliedQualityLevel);
        RefreshAdaptiveToggle(performance.AdaptiveEnabled);
        if (performanceLevelText != null)
            performanceLevelText.text = performance.CurrentPerformanceLevel.ToString();

        syncingUI = false;
    }

    /// <summary>
    /// Set giá trị dropdown frame rate mà không kích hoạt onValueChanged.
    /// </summary>
    private void RefreshFrameRateDropdown(int modeIndex)
    {
        if (frameRateDropdown == null) return;
        if (frameRateDropdown.value != modeIndex)
            frameRateDropdown.value = Mathf.Clamp(modeIndex, 0, frameRateDropdown.options.Count - 1);
    }

    /// <summary>
    /// Set giá trị dropdown chất lượng mà không kích hoạt onValueChanged.
    /// </summary>
    private void RefreshQualityDropdown(int level)
    {
        if (qualityDropdown == null) return;
        if (qualityDropdown.value != level)
            qualityDropdown.value = Mathf.Clamp(level, 0, qualityDropdown.options.Count - 1);
    }

    /// <summary>
    /// Set giá trị toggle adaptive mà không kích hoạt onValueChanged.
    /// </summary>
    private void RefreshAdaptiveToggle(bool enabled)
    {
        if (adaptiveToggle != null && adaptiveToggle.isOn != enabled)
            adaptiveToggle.isOn = enabled;
    }
}