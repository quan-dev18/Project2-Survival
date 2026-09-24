using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialController : MonoBehaviour
{
    public const string KEY_TUTORIAL_COMPLETED = "TutorialCompleted";

    [Header("Dependencies")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject tutorialUIRoot;
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private TMP_Text stepContentText;
    [SerializeField] private TMP_Text stepIndicatorText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private Button prevButton;

    [Header("Video Display Components")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private RenderTexture renderTexture;

    [Header("Danh sách Video cho từng bước (Kéo thả VideoClip vào đây)")]
    [Tooltip("Element 0: Bước 1 (Di chuyển)\nElement 1: Bước 2 (Bắn quái)\nElement 2: Bước 3 (EXP & Nâng cấp)\nElement 3: Bước 4 (Phá hòm)\nElement 4: Bước 5 (Bắt đầu)")]
    [SerializeField] private List<VideoClip> stepVideos = new List<VideoClip>();

    [Header("Optional Banner")]
    [SerializeField] private GameObject waveStartBanner;
    [SerializeField] private TMP_Text bannerText;

    private readonly List<string> stepTitles = new List<string>
    {
        "1. CÁCH DI CHUYỂN",
        "2. CHIẾN ĐẤU TỰ ĐỘNG",
        "3. THU THẬP EXP & NÂNG CẤP",
        "4. PHÁ VẬY CẢN NHẬN PHẦN THƯỞNG(PROPS)",
        "5. SẴN SÀNG CHIẾN ĐẤU!"
    };

    private readonly List<string> stepDescriptions = new List<string>
    {
        "Sử dụng <b>Cần điều khiển (Joystick)</b> ở góc trái màn hình (hoặc các phím <b>W, A, S, D / Mũi tên</b> trên bàn phím) để điều khiển nhân vật né đòn của quái vật.",
        "Nhân vật của bạn sẽ <b>tự động nhắm và bắn</b> quái vật gần nhất khi chúng tiến lại gần. Hãy luôn giữ khoảng cách an toàn và liên tục di chuyển!",
        "Khi quái vật bị tiêu diệt, chúng sẽ rơi ra <b>Ngọc Kinh Nghiệm (Exp Gem)</b>. Nhặt đủ ngọc để lên cấp và chọn các kỹ năng nâng cấp (Perk) mạnh mẽ.",
        "Trên đường đi có nhiều <b>vật cản có thể bắn vỡ</b>. Bạn có thể bắn vỡ chúng để nhặt thêm <b>phần thưởng</b> hoặc không có gì.",
        "Bạn đã nắm rõ các kỹ năng cơ bản! Hãy chuẩn bị tinh thần sống sót qua các đợt sóng quái vật trong màn chơi hướng dẫn này."
    };

    private int currentStepIndex = 0;
    private bool tutorialGuidanceFinished = false;

    public static bool IsTutorialCompleted
    {
        get
        {
            if (UserData.Instance != null)
                return UserData.Instance.TutorialCompleted;
            return PlayerPrefs.GetInt(KEY_TUTORIAL_COMPLETED, 0) == 1;
        }
    }

    public static void SetTutorialCompleted(bool completed = true)
    {
        if (UserData.Instance != null)
        {
            UserData.Instance.TutorialCompleted = completed;
        }
        else
        {
            PlayerPrefs.SetInt(KEY_TUTORIAL_COMPLETED, completed ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    [ContextMenu("Reset Tutorial Status")]
    public static void ResetTutorialStatus()
    {
        SetTutorialCompleted(false);
    }

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }

        if (enemySpawner != null)
        {
            // Ngăn không cho quái tự động sinh khi mới vào scene
            enemySpawner.AutoStart = false;
        }

        SetupVideoComponents();
        EnsureStepVideosCount();
    }

    private void OnValidate()
    {
        EnsureStepVideosCount();
    }

    private void SetupVideoComponents()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        }

        if (videoDisplay == null)
        {
            videoDisplay = GetComponentInChildren<RawImage>(true);
        }

        if (videoDisplay != null)
        {
            // Đảm bảo GameObject của videoDisplay luôn Active
            videoDisplay.gameObject.SetActive(true);
            videoDisplay.transform.SetAsLastSibling();
        }

        // Tự động tạo RenderTexture độ phân giải chuẩn nếu chưa có
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(860, 480, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = "TutorialVideoRT";
            renderTexture.Create();
        }

        if (videoPlayer != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // Tắt âm thanh UI video
        }

        if (videoDisplay != null)
        {
            videoDisplay.texture = renderTexture;
            videoDisplay.color = Color.white;
            videoDisplay.enabled = true;
        }
    }

    private void EnsureStepVideosCount()
    {
        if (stepVideos == null)
        {
            stepVideos = new List<VideoClip>();
        }

        // Đảm bảo có đủ 5 slots tương ứng 5 bước
        while (stepVideos.Count < 5)
        {
            stepVideos.Add(null);
        }

        // Nếu slot 0 chưa có video mà videoPlayer đã gán sẵn clip mẫu thì gán vào
        if (stepVideos[0] == null && videoPlayer != null && videoPlayer.clip != null)
        {
            stepVideos[0] = videoPlayer.clip;
        }
    }

    private void Start()
    {
        GameManager.OnStateChanged += HandleGameStateChanged;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrevButtonClicked);
        }

        if (tutorialUIRoot != null)
            tutorialUIRoot.SetActive(true);

        if (waveStartBanner != null)
            waveStartBanner.SetActive(false);

        ShowStep(0);
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleGameStateChanged;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    public void OnNextButtonClicked()
    {
        if (currentStepIndex < stepTitles.Count - 1)
        {
            // Log tutorial step completed
            FirebaseAnalyticsHelper.LogTutorialStepCompleted(currentStepIndex + 1, stepTitles[currentStepIndex]);
            ShowStep(currentStepIndex + 1);
        }
        else
        {
            // Log tutorial step completed for last step
            FirebaseAnalyticsHelper.LogTutorialStepCompleted(currentStepIndex + 1, stepTitles[currentStepIndex]);
            FinishGuidanceAndStartWaves();
        }
    }

    public void OnPrevButtonClicked()
    {
        if (currentStepIndex > 0)
        {
            ShowStep(currentStepIndex - 1);
        }
    }

    private void ShowStep(int index)
    {
        currentStepIndex = Mathf.Clamp(index, 0, stepTitles.Count - 1);

        if (stepTitleText != null)
            stepTitleText.text = stepTitles[currentStepIndex];

        if (stepContentText != null)
            stepContentText.text = stepDescriptions[currentStepIndex];

        if (stepIndicatorText != null)
            stepIndicatorText.text = $"Bước {currentStepIndex + 1} / {stepTitles.Count}";

        if (prevButton != null)
            prevButton.gameObject.SetActive(currentStepIndex > 0);

        if (nextButtonText != null)
        {
            nextButtonText.text = (currentStepIndex == stepTitles.Count - 1) ? "BẮT ĐẦU CHIẾN ĐẤU" : "TIẾP TỤC";
        }

        PlayVideoForCurrentStep();
    }

    private void PlayVideoForCurrentStep()
    {
        if (videoPlayer == null) return;

        VideoClip clipToPlay = null;
        if (stepVideos != null && currentStepIndex < stepVideos.Count)
        {
            clipToPlay = stepVideos[currentStepIndex];
        }

        if (clipToPlay != null)
        {
            if (videoDisplay != null)
            {
                if (!videoDisplay.gameObject.activeSelf)
                    videoDisplay.gameObject.SetActive(true);

                videoDisplay.transform.SetAsLastSibling();
                if (videoDisplay.texture != renderTexture)
                    videoDisplay.texture = renderTexture;
                videoDisplay.color = Color.white;
                videoDisplay.enabled = true;
            }

            // Xóa khung hình cũ trên RenderTexture để tránh nháy hình video trước
            if (renderTexture != null && renderTexture.IsCreated())
            {
                RenderTexture prevRT = RenderTexture.active;
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = prevRT;
            }

            videoPlayer.Stop();
            videoPlayer.clip = clipToPlay;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Stop();
            if (videoDisplay != null)
            {
                videoDisplay.enabled = false;
            }
        }
    }

    private void FinishGuidanceAndStartWaves()
    {
        if (tutorialGuidanceFinished) return;
        tutorialGuidanceFinished = true;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (tutorialUIRoot != null)
            tutorialUIRoot.SetActive(false);

        StartCoroutine(ShowWaveAnnouncementAndStart());
    }

    private IEnumerator ShowWaveAnnouncementAndStart()
    {
        if (waveStartBanner != null)
        {
            waveStartBanner.SetActive(true);
            if (bannerText != null)
                bannerText.text = "LÀN SÓNG QUÁI VẬT BẮT ĐẦU!";
            yield return new WaitForSeconds(1.5f);
            waveStartBanner.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // Chuyển GameManager sang trạng thái Playing
        if (GameManager.Instance != null)
            GameManager.Instance.CompleteTutorial();

        // Xử lý pending level-ups (nếu player đã tích đủ XP trong tutorial)
        PlayerXP.Instance?.ProcessPendingLevelUps();

        // Bắt đầu chạy stage quái vật (StageTut_SO)
        if (enemySpawner != null)
        {
            enemySpawner.BeginSpawning();
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsWin)
            {
                SetTutorialCompleted(true);
                FirebaseAnalyticsHelper.LogTutorialCompleted();
            }
        }
    }
}
