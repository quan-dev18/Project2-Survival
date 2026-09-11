using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// ============================================================================
/// SettingsUI - Panel cài đặt: điều khiển âm lượng (BGM / SFX) bằng Slider.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR
///   1. Gắn script này lên GameObject panel Settings.
///   2. Kéo 2 Slider vào "BGM Slider" và "SFX Slider".
///      (Slider nên cài: Min Value = 0, Max Value = 1, Whole Numbers = false)
///   3. Mở AudioManager.cs: tham chiếu Instance => script tự tìm, không cần kéo.
///   4. Khi kéo Slider, volume được áp ngay & tự lưu vào PlayerPrefs
///      (xử lý trong AudioManager.BGMVolume / SFXVolume).
/// ============================================================================
public class SettingsUI : MonoBehaviour
{
    [Header("Volume Slider")]
    [Tooltip("Slider chỉnh nhạc nền (BGM).")]
    [SerializeField] private Slider bgmSlider;
    [Tooltip("Slider chỉnh hiệu ứng âm thanh (SFX).")]
    [SerializeField] private Slider sfxSlider;

    [Header("Text hiển thị % (tùy chọn, có thể bỏ trống)")]
    [Tooltip("Text hiển thị % âm lượng BGM hiện tại.")]
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [Tooltip("Text hiển thị % âm lượng SFX hiện tại.")]
    [SerializeField] private TextMeshProUGUI sfxValueText;

    private bool _initialized;

    // ──────────────────── Unity Callbacks ─────────────────
    private void Awake()
    {
        // Gắn sự kiện một lần: khi kéo Slider → cập nhật volume AudioManager.
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBGMValueChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXValueChanged);
    }

    private void Start()
    {
        // Đồng bộ giá trị Slider theo volume đã lưu trong PlayerPrefs.
        SyncSlidersWithAudio();
        _initialized = true;
    }

    private void OnEnable()
    {
        // Nếu panel tắt/mở nhiều lần, vẫn phải đồng bộ lại slider
        // (phòng khi volume bị đổi ở chỗ khác hoặc lần đầu vào game).
        if (_initialized)
            SyncSlidersWithAudio();
    }

    // ──────────────────── Event Handlers ──────────────────
    private void OnBGMValueChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = value;

        UpdateBGMText(value);
    }

    private void OnSFXValueChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = value;

        UpdateSFXText(value);
    }

    /// <summary>
    /// Đưa Slider về đúng vị trí theo volume đang lưu trong AudioManager.
    /// </summary>
    private void SyncSlidersWithAudio()
    {
        if (AudioManager.Instance == null)
            return;

        if (bgmSlider != null)
            bgmSlider.value = AudioManager.Instance.BGMVolume;
        if (sfxSlider != null)
            sfxSlider.value = AudioManager.Instance.SFXVolume;
    }

    private void UpdateBGMText(float value)
    {
        if (bgmValueText != null)
            bgmValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateSFXText(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}