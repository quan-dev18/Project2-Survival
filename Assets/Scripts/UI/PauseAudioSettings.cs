using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Liên kết 2 Slider âm lượng (BGM/SFX) trong Pause Menu với cùng nguồn
/// AudioManager như SettingsUI ngoài main menu -> tự đồng bộ 2 chiều.
/// Không cần kéo tay: gắn lên object chứa 2 slider, script tự tìm theo tên
/// (chứa "BGM"/"Music" và "SFX"/"Sound"). Nếu chỉ có đúng 2 slider thì lấy luôn.
/// </summary>
public class PauseAudioSettings : MonoBehaviour
{
    [Tooltip("Để trống để script tự tìm trong con.")]
    [SerializeField] private Slider bgmSlider;
    [Tooltip("Để trống để script tự tìm trong con.")]
    [SerializeField] private Slider sfxSlider;

    [Tooltip("Text % BGM (tùy chọn, tự tìm theo tên).")]
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [Tooltip("Text % SFX (tùy chọn, tự tìm theo tên).")]
    [SerializeField] private TextMeshProUGUI sfxValueText;

    private void Awake()
    {
        FindSliders();
        FindTexts();

        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    // Mỗi lần mở pause menu -> đồng bộ vị trí slider theo volume đang lưu
    private void OnEnable()
    {
        SyncFromAudioManager();
    }

    // ──────────────────── Tự tìm theo tên ─────────────────────
    private void FindSliders()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);

        if (bgmSlider == null) bgmSlider = FindByName(sliders, "bgm", "music", "nhac");
        if (sfxSlider == null) sfxSlider = FindByName(sliders, "sfx", "sound");

        // Fallback: chỉ có đúng 2 slider -> gán lần lượt
        if (bgmSlider == null && sfxSlider == null && sliders.Length == 2)
        {
            bgmSlider = sliders[0];
            sfxSlider = sliders[1];
        }
    }

    private void FindTexts()
    {
        if (bgmValueText != null && sfxValueText != null) return;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (bgmValueText == null) bgmValueText = FindByName(texts, "bgm", "music", "nhac");
        if (sfxValueText == null) sfxValueText = FindByName(texts, "sfx", "sound");
    }

    private static T FindByName<T>(T[] list, params string[] keywords) where T : Object
    {
        foreach (T item in list)
        {
            if (item == null) continue;
            string name = item.name.ToLower();
            foreach (string kw in keywords)
            {
                if (name.Contains(kw)) return item;
            }
        }
        return null;
    }

    // ──────────────────── Xử lý giá trị ─────────────────────
    private void OnBGMChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = value;
        UpdateText(bgmValueText, value);
    }

    private void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = value;
        UpdateText(sfxValueText, value);
    }

    private void SyncFromAudioManager()
    {
        if (AudioManager.Instance == null) return;

        if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.BGMVolume;
        if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SFXVolume;

        UpdateText(bgmValueText, AudioManager.Instance.BGMVolume);
        UpdateText(sfxValueText, AudioManager.Instance.SFXVolume);
    }

    private static void UpdateText(TextMeshProUGUI text, float value)
    {
        if (text != null)
            text.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}