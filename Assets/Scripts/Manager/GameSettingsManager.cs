using System;
using UnityEngine;

/// ============================================================================
/// GameSettingsManager - Core Manager chứa các cờ cài đặt hiển thị của game.
///
/// ▐▌ CHỨC NĂNG CHÍNH
///   1. Hiện FPS (ShowFPS)      : bật/tắt overlay FPS toàn màn hình.
///   2. Hiển thị sát thương      : bật/tắt popup số sát thương khi đánh trúng.
///   3. Hiển thị VFX             : bật/tắt hiệu ứng hạt (nổ, cháy, va chạm).
///   (Riêng "Cấu hình thấp" là cài đặt hiệu năng, được xử lý trong
///   PerformanceManager.SetLowGraphics — Core Manager của hiệu năng.)
///
/// ▐▌ KIẾN TRÚC
///   - Singleton DontDestroyOnLoad, tự sinh runtime nếu chưa có (xem EnsureInstance).
///   - KHÔNG đụng UI. UI giao tiếp qua:
///       + Setter công khai: SetShowFPS / SetShowDamage / SetShowVFX
///       + event Action&lt;bool&gt;: OnShowFPSChanged / OnShowDamageChanged / OnShowVFXChanged
///   - Giá trị lưu PlayerPrefs (khóa const tập trung), gọi Save() ngay mỗi lần đổi.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR
///   Không cần gắn: nếu scene thiếu, hệ thống tự tạo một GameObject
///   "GameSettingsManager" đúng lúc game chạy.
/// ============================================================================
public sealed class GameSettingsManager : MonoBehaviour
{
    // =============================================================
    //  SINGLETON - con trỏ dùng chung cho toàn game
    // =============================================================
    public static GameSettingsManager Instance { get; private set; }

    // =============================================================
    //  CONSTANT PLAYERPREFS KEYS - quản lý tập trung
    // =============================================================
    private const string KEY_SHOW_FPS = "Setting_ShowFPS";
    private const string KEY_SHOW_DAMAGE = "Setting_ShowDamage";
    private const string KEY_SHOW_VFX = "Setting_ShowVFX";

    // =============================================================
    //  SERIALIZE FIELDS - giá trị mặc định cho lần chạy đầu tiên
    // =============================================================
    [Header("=== Giá trị mặc định (chỉ áp dụng lần chạy đầu) ===")]
    [Tooltip("Mặc định có hiển thị overlay FPS không (thường tắt).")]
    [SerializeField] private bool showFPSByDefault = false;

    [Tooltip("Mặc định có hiển thị số sát thương không.")]
    [SerializeField] private bool showDamageByDefault = true;

    [Tooltip("Mặc định có hiển thị hiệu ứng VFX không.")]
    [SerializeField] private bool showVFXByDefault = true;

    // =============================================================
    //  RUNTIME STATE
    // =============================================================
    private bool showFPS;
    private bool showDamage;
    private bool showVFX;

    // =============================================================
    //  PUBLIC PROPERTIES (dữ liệu thuần, không đụng UI)
    // =============================================================

    /// <summary>Có hiển thị overlay FPS toàn màn hình hay không.</summary>
    public bool ShowFPS => showFPS;

    /// <summary>Có hiển thị số sát thương (popup) hay không.</summary>
    public bool ShowDamage => showDamage;

    /// <summary>Có hiển thị hiệu ứng VFX hay không.</summary>
    public bool ShowVFX => showVFX;

    // =============================================================
    //  PUBLIC EVENTS - UI đăng ký lắng nghe
    // =============================================================

    /// <summary>Khi cờ hiển thị FPS thay đổi.</summary>
    public event Action<bool> OnShowFPSChanged;

    /// <summary>Khi cờ hiển thị sát thương thay đổi.</summary>
    public event Action<bool> OnShowDamageChanged;

    /// <summary>Khi cờ hiển thị VFX thay đổi.</summary>
    public event Action<bool> OnShowVFXChanged;

    // =============================================================
    //  PHẦN 1: KHỞI TẠO
    // =============================================================

    /// <summary>
    /// Awake: ép Singleton duy nhất, sống xuyên scene, đọc cài đặt đã lưu.
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

        showFPS = PlayerPrefs.GetInt(KEY_SHOW_FPS, showFPSByDefault ? 1 : 0) == 1;
        showDamage = PlayerPrefs.GetInt(KEY_SHOW_DAMAGE, showDamageByDefault ? 1 : 0) == 1;
        showVFX = PlayerPrefs.GetInt(KEY_SHOW_VFX, showVFXByDefault ? 1 : 0) == 1;

        Debug.Log($"[GameSettingsManager] 🚀 Khởi tạo: ShowFPS={showFPS}, ShowDamage={showDamage}, ShowVFX={showVFX}");
    }

    /// <summary>
    /// [TỰ ĐỘNG] Nếu scene chưa có GameSettingsManager, tự tạo 1 cái ngay sau khi scene đầu tiên load.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null || FindObjectOfType<GameSettingsManager>() != null)
            return;

        GameObject go = new GameObject("GameSettingsManager");
        go.AddComponent<GameSettingsManager>();
    }

    /// <summary>
    /// Giải phóng Singleton khi object bị hủy.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =============================================================
    //  PHẦN 2: PUBLIC API - UI gọi xuống để thay đổi cờ
    // =============================================================

    /// <summary>
    /// Bật/tắt hiển thị overlay FPS và lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="enabled">true = hiện FPS.</param>
    public void SetShowFPS(bool enabled)
    {
        if (showFPS == enabled) return;

        showFPS = enabled;
        PlayerPrefs.SetInt(KEY_SHOW_FPS, showFPS ? 1 : 0);
        PlayerPrefs.Save();
        OnShowFPSChanged?.Invoke(showFPS);
        FirebaseAnalyticsHelper.LogSettingChanged("show_fps", showFPS);
        Debug.Log($"[GameSettingsManager] 🚀 Hiện FPS: {(showFPS ? "BẬT" : "TẮT")}");
    }

    /// <summary>
    /// Bật/tắt hiển thị số sát thương và lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="enabled">true = hiện số sát thương.</param>
    public void SetShowDamage(bool enabled)
    {
        if (showDamage == enabled) return;

        showDamage = enabled;
        PlayerPrefs.SetInt(KEY_SHOW_DAMAGE, showDamage ? 1 : 0);
        PlayerPrefs.Save();
        OnShowDamageChanged?.Invoke(showDamage);
        FirebaseAnalyticsHelper.LogSettingChanged("show_damage", showDamage);
        Debug.Log($"[GameSettingsManager] 🚀 Hiển thị sát thương: {(showDamage ? "BẬT" : "TẮT")}");
    }

    /// <summary>
    /// Bật/tắt hiển thị hiệu ứng VFX và lưu ngay vào PlayerPrefs.
    /// </summary>
    /// <param name="enabled">true = hiện VFX.</param>
    public void SetShowVFX(bool enabled)
    {
        if (showVFX == enabled) return;

        showVFX = enabled;
        PlayerPrefs.SetInt(KEY_SHOW_VFX, showVFX ? 1 : 0);
        PlayerPrefs.Save();
        OnShowVFXChanged?.Invoke(showVFX);
        FirebaseAnalyticsHelper.LogSettingChanged("show_vfx", showVFX);
        Debug.Log($"[GameSettingsManager] 🚀 Hiển thị VFX: {(showVFX ? "BẬT" : "TẮT")}");
    }
}