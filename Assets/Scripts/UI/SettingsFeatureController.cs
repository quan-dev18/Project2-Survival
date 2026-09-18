using UnityEngine;

/// ============================================================================
/// SettingsFeatureController - UI View Controller tách bạch UI vs Logic.
///
/// ▐▌ CHỨC NĂNG CHÍNH
///   Điều khiển 5 nút gạt (CustomSwitchUI) trong panel Cài đặt:
///     + "Cấu hình thấp"        -> PerformanceManager.SetLowGraphics (Core hiệu năng)
///     + "Hiện FPS"             -> GameSettingsManager.SetShowFPS     (Core cài đặt)
///     + "Hiển thị sát thương"   -> GameSettingsManager.SetShowDamage (Core cài đặt)
///     + "Hiển thị VFX"          -> GameSettingsManager.SetShowVFX    (Core cài đặt)
///     + "VSync"                 -> PerformanceManager.SetVSyncEnabled (Core hiệu năng)
///
/// ▐▌ QUY TẮC
///   - Không chứa logic quyết định: mọi tương tác chỉ "đẩy xuống" Core qua Setter
///     và "nhận lên" để cập nhật UI qua event Action&lt;bool&gt;.
///   - Mỗi lần panel bật (OnEnable) đều đồng bộ lại trạng thái switch theo Core.
///   - Đăng ký/hủy đăng ký event chuẩn; dùng cờ _syncingUI chống hồi tiếp.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR
///   1. Gắn script lên GameObject panel Cài đặt (cạnh SettingsUI / PauseAudioSettings).
///   2. Kéo 5 CustomSwitchUI vào 5 ô dưới đây (tên trùng khớp sẽ tự tìm nếu bỏ trống).
/// ============================================================================
public class SettingsFeatureController : MonoBehaviour
{
    [Header("Các nút gạt (bỏ trống sẽ tự tìm theo tên)")]
    [Tooltip("Nút 'Cấu hình thấp' — tìm theo tên chứa 'thap'/'low'.")]
    [SerializeField] private CustomSwitchUI lowGraphicsSwitch;

    [Tooltip("Nút 'Hiện FPS' — tìm theo tên chứa 'fps'.")]
    [SerializeField] private CustomSwitchUI showFPSSwitch;

    [Tooltip("Nút 'Hiển thị sát thương' — tìm theo tên chứa 'sát'/'damage'.")]
    [SerializeField] private CustomSwitchUI showDamageSwitch;

    [Tooltip("Nút 'Hiển thị VFX' — tìm theo tên chứa 'vfx'/'hieuung'.")]
    [SerializeField] private CustomSwitchUI showVFXSwitch;

    [Tooltip("Nút 'VSync' — tìm theo tên chứa 'vsync'.")]
    [SerializeField] private CustomSwitchUI vSyncSwitch;

    private PerformanceManager performance;
    private GameSettingsManager settings;
    private bool syncingUI;      // chặn hồi tiếp khi tự set trạng thái switch
    private bool subscribed;

    // ──────────────────── Unity Callbacks ─────────────────

    /// <summary>
    /// Tự tìm các switch theo tên nếu Inspector bỏ trống, rồi gắn callback onValueChanged.
    /// </summary>
    private void Awake()
    {
        FindSwitches();

        if (lowGraphicsSwitch != null) lowGraphicsSwitch.onValueChanged.AddListener(OnLowGraphicsChanged);
        if (showFPSSwitch != null) showFPSSwitch.onValueChanged.AddListener(OnShowFPSChanged);
        if (showDamageSwitch != null) showDamageSwitch.onValueChanged.AddListener(OnShowDamageChanged);
        if (showVFXSwitch != null) showVFXSwitch.onValueChanged.AddListener(OnShowVFXChanged);
        if (vSyncSwitch != null) vSyncSwitch.onValueChanged.AddListener(OnVSyncChanged);
    }

    /// <summary>
    /// Tìm 2 Core Manager, đăng ký event và đồng bộ UI 1 lần.
    /// Nếu manager auto-create hơi trễ, thử lại sau 1 frame.
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
    /// Đăng ký toàn bộ event của 2 Core Manager và đồng bộ UI theo trạng thái hiện tại.
    /// </summary>
    private void Bind()
    {
        performance.OnLowGraphicsChanged += OnLowGraphicsCoreChanged;
        settings.OnShowFPSChanged += OnShowFPSCoreChanged;
        settings.OnShowDamageChanged += OnShowDamageCoreChanged;
        settings.OnShowVFXChanged += OnShowVFXCoreChanged;
        performance.OnVSyncChanged += OnVSyncCoreChanged;
        subscribed = true;

        SyncAllSwitches();
    }

    /// <summary>
    /// Mỗi lần panel bật lại (mở settings), đồng bộ lại trạng thái 4 switch theo Core.
    /// </summary>
    private void OnEnable()
    {
        if (!subscribed) return;
        SyncAllSwitches();
    }

    /// <summary>
    /// Hủy đăng ký tất cả event (cả switch lẫn Core Manager) để tránh leak.
    /// </summary>
    private void OnDestroy()
    {
        if (performance != null)
            performance.OnLowGraphicsChanged -= OnLowGraphicsCoreChanged;
        if (settings != null)
        {
            settings.OnShowFPSChanged -= OnShowFPSCoreChanged;
            settings.OnShowDamageChanged -= OnShowDamageCoreChanged;
            settings.OnShowVFXChanged -= OnShowVFXCoreChanged;
        }
        if (performance != null)
            performance.OnVSyncChanged -= OnVSyncCoreChanged;

        if (lowGraphicsSwitch != null) lowGraphicsSwitch.onValueChanged.RemoveListener(OnLowGraphicsChanged);
        if (showFPSSwitch != null) showFPSSwitch.onValueChanged.RemoveListener(OnShowFPSChanged);
        if (showDamageSwitch != null) showDamageSwitch.onValueChanged.RemoveListener(OnShowDamageChanged);
        if (showVFXSwitch != null) showVFXSwitch.onValueChanged.RemoveListener(OnShowVFXChanged);
        if (vSyncSwitch != null) vSyncSwitch.onValueChanged.RemoveListener(OnVSyncChanged);
    }

    // ──────────────────── UI -> Core (chỉ đẩy xuống, không logic) ─

    private void OnLowGraphicsChanged(bool enabled)
    {
        if (syncingUI || performance == null) return;
        performance.SetLowGraphics(enabled);
    }

    private void OnShowFPSChanged(bool enabled)
    {
        if (syncingUI || settings == null) return;
        settings.SetShowFPS(enabled);
    }

    private void OnShowDamageChanged(bool enabled)
    {
        if (syncingUI || settings == null) return;
        settings.SetShowDamage(enabled);
    }

    private void OnShowVFXChanged(bool enabled)
    {
        if (syncingUI || settings == null) return;
        settings.SetShowVFX(enabled);
    }

    private void OnVSyncChanged(bool enabled)
    {
        if (syncingUI || performance == null) return;
        // Đảo trạng thái: switch ON -> VSync OFF, switch OFF -> VSync ON.
        performance.SetVSyncEnabled(!enabled);
    }

    // ──────────────────── Core -> UI (chỉ cập nhật giao diện) ─

    private void OnLowGraphicsCoreChanged(bool enabled)
    {
        SyncSwitch(lowGraphicsSwitch, enabled);
    }

    private void OnShowFPSCoreChanged(bool enabled)
    {
        SyncSwitch(showFPSSwitch, enabled);
    }

    private void OnShowDamageCoreChanged(bool enabled)
    {
        SyncSwitch(showDamageSwitch, enabled);
    }

    private void OnShowVFXCoreChanged(bool enabled)
    {
        SyncSwitch(showVFXSwitch, enabled);
    }

    private void OnVSyncCoreChanged(bool enabled)
    {
        // Đảo trạng thái: VSync bật -> switch hiện OFF, VSync tắt -> switch hiện ON.
        SyncSwitch(vSyncSwitch, !enabled);
    }

    // ──────────────────── Helpers / Data binding ─────────────

    /// <summary>
    /// Đồng bộ cả 5 switch theo trạng thái hiện tại của 2 Core Manager.
    /// </summary>
    private void SyncAllSwitches()
    {
        syncingUI = true;

        SyncSwitch(lowGraphicsSwitch, performance.LowGraphicsEnabled);
        SyncSwitch(showFPSSwitch, settings.ShowFPS);
        SyncSwitch(showDamageSwitch, settings.ShowDamage);
        SyncSwitch(showVFXSwitch, settings.ShowVFX);
        // Đảo trạng thái: switch ON = VSync OFF, switch OFF = VSync ON.
        SyncSwitch(vSyncSwitch, !performance.VSyncEnabled);

        syncingUI = false;
    }

    /// <summary>
    /// Set trạng thái switch (không animation, không kích hoạt onValueChanged).
    /// </summary>
    private static void SyncSwitch(CustomSwitchUI sw, bool enabled)
    {
        if (sw != null && sw.isOn != enabled)
            sw.SetState(enabled, false, false);
    }

    /// <summary>
    /// Tự tìm 4 switch theo tên GameObject (bỏ qua nếu đã gán tay trong Inspector).
    /// </summary>
    private void FindSwitches()
    {
        CustomSwitchUI[] list = GetComponentsInChildren<CustomSwitchUI>(true);

        if (lowGraphicsSwitch == null) lowGraphicsSwitch = FindByName(list, "thap", "low");
        if (showFPSSwitch == null) showFPSSwitch = FindByName(list, "fps");
        if (showDamageSwitch == null) showDamageSwitch = FindByName(list, "sát", "sat", "damage");
        if (showVFXSwitch == null) showVFXSwitch = FindByName(list, "vfx", "hieuung");
        if (vSyncSwitch == null) vSyncSwitch = FindByName(list, "vsync");
    }

    /// <summary>
    /// Tìm phần tử đầu tiên có tên (hoặc tên object cha nếu switch con tên chung chung như
    /// "Toggle") chứa một trong các từ khóa — so sánh không phân biệt hoa thường.
    /// </summary>
    private static CustomSwitchUI FindByName(CustomSwitchUI[] list, params string[] keywords)
    {
        foreach (CustomSwitchUI item in list)
        {
            if (item == null) continue;
            string name = item.name.ToLowerInvariant();
            string parentName = item.transform.parent != null
                ? item.transform.parent.name.ToLowerInvariant()
                : string.Empty;
            foreach (string kw in keywords)
            {
                string key = kw.ToLowerInvariant();
                if (name.Contains(key) || parentName.Contains(key))
                    return item;
            }
        }
        return null;
    }
}