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
///        - 3 nút chất lượng "Low/Medium/High": kéo vào các ô qualityLowButton /
///          qualityMediumButton / qualityHighButton. Low = mức 0, Medium = mức
///          giữa, High = mức cao nhất. Nút đang chọn giữ màu gốc; nút chưa
///          chọn bị nền xám tối + chữ sáng (màu chỉnh được trong Inspector).
///        - Toggle "Adaptive Toggle"
///        - Toggle "VSync Toggle"
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

    [Header("Chất lượng đồ họa (3 nút)")]
    [Tooltip("Nút Low: ép chất lượng về mức 0.")]
    [SerializeField] private Button qualityLowButton;
    [Tooltip("Nút Medium: chất lượng trung bình (mức giữa theo QualitySettings).")]
    [SerializeField] private Button qualityMediumButton;
    [Tooltip("Nút High: ép chất lượng lên mức cao nhất.")]
    [SerializeField] private Button qualityHighButton;

    [Tooltip("Màu nền cho nút KHÔNG được chọn (xám tối). Nút đang chọn giữ màu gốc.")]
    [SerializeField] private Color unselectedBgColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    [Tooltip("Màu chữ cho nút KHÔNG được chọn (sáng).")]
    [SerializeField] private Color unselectedTextColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("Adaptive Throttling")]
    [Tooltip("Toggle bật/tắt tự điều chỉnh hiệu năng (chống nóng máy, tiết kiệm pin).")]
    [SerializeField] private Toggle adaptiveToggle;

    [Header("VSync")]
    [Tooltip("Toggle bật/tắt VSync (đồng bộ với PerformanceManager).")]
    [SerializeField] private Toggle vSyncToggle;

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
        if (qualityLowButton != null)
            qualityLowButton.onClick.AddListener(OnQualityLowClicked);
        if (qualityMediumButton != null)
            qualityMediumButton.onClick.AddListener(OnQualityMediumClicked);
        if (qualityHighButton != null)
            qualityHighButton.onClick.AddListener(OnQualityHighClicked);
        if (adaptiveToggle != null)
            adaptiveToggle.onValueChanged.AddListener(OnAdaptiveToggleChanged);
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(OnVSyncToggleChanged);

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
    /// Đăng ký toàn bộ event và đồng bộ UI 1 lần.
    /// </summary>
    private void Bind()
    {
        performance.OnFPSUpdated += OnFPSUpdated;
        performance.OnFrameRateModeChanged += OnFrameRateModeChanged;
        performance.OnQualityLevelChanged += OnQualityLevelChanged;
        performance.OnAdaptiveEnabledChanged += OnAdaptiveEnabledChanged;
        performance.OnVSyncChanged += OnVSyncChanged;
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
            performance.OnVSyncChanged -= OnVSyncChanged;
            performance.OnAdaptiveStepChanged -= OnAdaptiveStepChanged;
            performance.OnBatteryWarning -= OnBatteryWarning;
        }

        if (frameRateDropdown != null)
            frameRateDropdown.onValueChanged.RemoveListener(OnFrameRateDropdownChanged);
        if (qualityLowButton != null)
            qualityLowButton.onClick.RemoveListener(OnQualityLowClicked);
        if (qualityMediumButton != null)
            qualityMediumButton.onClick.RemoveListener(OnQualityMediumClicked);
        if (qualityHighButton != null)
            qualityHighButton.onClick.RemoveListener(OnQualityHighClicked);
        if (adaptiveToggle != null)
            adaptiveToggle.onValueChanged.RemoveListener(OnAdaptiveToggleChanged);
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.RemoveListener(OnVSyncToggleChanged);
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
    /// Người chơi bấm nút chất lượng: đẩy thẳng xuống PerformanceManager.
    /// Low = mức 0, Medium = mức giữa, High = mức cao nhất (tự tính theo số mức
    /// trong QualitySettings để không cứng nhắc với số mức thực tế).
    /// </summary>
    private void OnQualityLowClicked() => OnQualityButtonClicked(0);
    private void OnQualityMediumClicked() => OnQualityButtonClicked(PerformanceManager.QualityLevelCount / 2);
    private void OnQualityHighClicked() => OnQualityButtonClicked(PerformanceManager.QualityLevelCount - 1);

    private void OnQualityButtonClicked(int level)
    {
        if (performance == null) return;
        performance.SetQualityLevel(level);
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

    /// <summary>
    /// Người chơi bật/tắt VSync: đẩy thẳng xuống PerformanceManager.
    /// </summary>
    private void OnVSyncToggleChanged(bool enabled)
    {
        if (syncingUI || performance == null) return;
        performance.SetVSyncEnabled(enabled);
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
        RefreshQualityButtons(level);
    }

    /// <summary>
    /// Đồng bộ toggle adaptive theo manager.
    /// </summary>
    private void OnAdaptiveEnabledChanged(bool enabled)
    {
        RefreshAdaptiveToggle(enabled);
    }

    /// <summary>
    /// Đồng bộ toggle VSync theo manager.
    /// </summary>
    private void OnVSyncChanged(bool enabled)
    {
        if (vSyncToggle != null && vSyncToggle.isOn != enabled)
            vSyncToggle.isOn = enabled;
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
    /// Đồng bộ toàn bộ UI với trạng thái hiện tại của manager.
    /// </summary>
    private void RefreshAllUI()
    {
        syncingUI = true;

        RefreshFrameRateDropdown((int)performance.CurrentMode);
        RefreshQualityButtons(performance.AppliedQualityLevel);
        RefreshAdaptiveToggle(performance.AdaptiveEnabled);
        if (vSyncToggle != null && vSyncToggle.isOn != performance.VSyncEnabled)
            vSyncToggle.isOn = performance.VSyncEnabled;
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
    /// Bộ nhớ màu gốc của 1 nút chất lượng để có thể phục hồi khi được chọn.
    /// </summary>
    private struct QualityButtonStyle
    {
        public Button button;
        public Image bg;
        public TMP_Text label;
        public Color bgOriginal;
        public Color labelOriginal;
    }
    private QualityButtonStyle lowStyle, midStyle, highStyle;

    /// <summary>
    /// Đồng bộ 3 nút chất lượng theo mức đang áp dụng (kể cả khi adaptive/cấu
    /// hình thấp đang làm lệch mức): nút chọn giữ màu gốc, các nút còn lại
    /// nền xám tối + chữ sáng.
    /// </summary>
    private void RefreshQualityButtons(int level)
    {
        int low = 0;
        int mid = PerformanceManager.QualityLevelCount / 2;
        int high = PerformanceManager.QualityLevelCount - 1;

        bool isLow = Mathf.Abs(level - low) <= Mathf.Abs(level - mid);
        bool isHigh = !isLow && Mathf.Abs(level - high) < Mathf.Abs(level - mid);
        bool isMid = !isLow && !isHigh;

        // Cache màu gốc chỉ 1 lần — trước khi bắt đầu đổi màu nút
        if (lowStyle.button == null && qualityLowButton != null) lowStyle = CacheButtonStyle(qualityLowButton);
        if (midStyle.button == null && qualityMediumButton != null) midStyle = CacheButtonStyle(qualityMediumButton);
        if (highStyle.button == null && qualityHighButton != null) highStyle = CacheButtonStyle(qualityHighButton);

        ApplyButtonStyle(lowStyle, isLow);
        ApplyButtonStyle(midStyle, isMid);
        ApplyButtonStyle(highStyle, isHigh);
    }

    /// <summary>
    /// Lưu lại màu nền + chữ gốc của một nút (chỉ gọi 1 lần ở lần highlight đầu).
    /// </summary>
    private QualityButtonStyle CacheButtonStyle(Button b)
    {
        QualityButtonStyle s;
        s.button = b;
        s.bg = b.targetGraphic as Image;
        s.label = b.GetComponentInChildren<TMP_Text>(true);
        s.bgOriginal = s.bg != null ? s.bg.color : Color.white;
        s.labelOriginal = s.label != null ? s.label.color : Color.white;
        return s;
    }

    /// <summary>
    /// Áp màu cho 1 nút: được chọn = phục hồi màu gốc, không chọn = nền xám
    /// tối + chữ sáng.
    /// </summary>
    private void ApplyButtonStyle(QualityButtonStyle s, bool selected)
    {
        if (s.button == null) return;
        if (s.bg != null) s.bg.color = selected ? s.bgOriginal : unselectedBgColor;
        if (s.label != null) s.label.color = selected ? s.labelOriginal : unselectedTextColor;
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