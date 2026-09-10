using UnityEngine;
using UnityEngine.SceneManagement;

/// ============================================================================
/// SceneBGM - Tự động đổi nhạc nền (BGM) theo từng Scene.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR:
///   1. Chọn GameObject "AudioManager" (có sẵn script AudioManager).
///   2. Chọn Add Component -> tìm "SceneBGM" -> gắn lên cùng GameObject đó.
///      (Đặt cùng AudioManager để nó sống xuyên các scene, vì đây là chỗ
///      duy nhất chúng ta cần cấu hình BGM cho cả game.)
///   3. Trong Inspector khai báo danh sách "Entries":
///        - Scene Name : nhập CHÍNH XÁC tên scene như trong File > Build Settings.
///                       VD: "MainMenu", "GameMap1", "GameMap2", "LoadingScene"...
///                       (Nếu bạn dùng tên "MenuScene"/"GameScene" thì nhập đúng tên đó.)
///        - BGM Clip  : kéo AudioClip nhạc nền tương ứng vào.
///        - Volume    : độ lớn riêng cho clip đó.
///   4. Bấm Play để test. Mỗi khi vào scene có tên khớp, nhạc đổi mượt có fade.
///
/// ▐▌ LƯU Ý HỮU ÍCH:
///   - Nếu muốn 1 vài scene KHÔNG có nhạc (VD scene Menu), để trống BGM Clip
///     của entry đó; hệ thống sẽ tự tắt nhạc đang chạy (fade out).
///   - Nếu tới 1 scene khôngằm trong danh sách, bật "Stop BGM When No Match"
///     để tắt nhạc, hoặc để mặc định nhạc cũ phát tiếp.
/// ============================================================================
public class SceneBGM : MonoBehaviour
{
    /// <summary>
    /// 1 mục trong danh sách: gắn 1 scene với 1 bài nhạc nền.
    /// </summary>
    [System.Serializable]
    public class SceneBGMEntry
    {
        [Tooltip("Tên scene CHÍNH XÁC (giống Build Settings). VD: MainMenu, GameMap1")]
        public string sceneName;

        [Tooltip("Nhạc nền cho scene này. Để trống = scene này sẽ không có nhạc.")]
        public AudioClip bgmClip;

        [Tooltip("Độ lớn nhạc của riêng scene này (0-1).")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Lặp vô tận? Nhạc nền nên bật.")]
        public bool loop = true;
    }

    [Header("=== Danh sách BGM theo Scene ===")]
    [Tooltip("Mỗi dòng: tên scene + nhạc nền tương ứng.")]
    [SerializeField] private SceneBGMEntry[] entries = new SceneBGMEntry[0];

    [Header("=== Cài đặt ===")]
    [Tooltip("Thời gian fade nhạc khi chuyển scene (giây).")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("Vào scene KHÔNG có trong danh sách -> có tắt nhạc đang chơi không?")]
    [SerializeField] private bool stopBGMWhenNoMatch = false;

    [Tooltip("Tự phát nhạc ngay từ scene hiện tại khi Enable (không cần chờ đổi scene).")]
    [SerializeField] private bool playOnEnable = true;

    // -----------------------------------------------------------------------

    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện đổi scene của Unity.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Chạy lần đầu: phát nhạc cho scene mở trước đó.
        if (playOnEnable && AudioManager.Instance != null)
            PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        // Hủy đăng ký -> tránh gọi nhầm khi GameObject này bị tắt/xóa.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Start BẢO ĐẢM BGM của scene đầu tiên được phát.
    ///
    /// ▐▌ VÌ SAO CẦN Start()?
    ///   - Với scene MỞ ĐẦU (bấm Play), sự kiện `sceneLoaded` có thể KHÔNG
    ///     được gọi đúng, và AudioManager nếu được tự động tạo (EnsureInstance)
    ///     thì chỉ xuất hiện SAU khi sceneLoaded + OnEnable đã chạy xong.
    ///   - => Ở lần vào game đầu, chưa có chỗ nào gọi PlayBGMForScene => im lặng.
    ///   - Start() chạy TRƯỚC frame đầu tiên, khi mọi thứ đã sẵn sàng
    ///     (AudioManager.Instance tồn tại, scene đã chính thức active).
    ///   - Nếu OnEnable/OnSceneLoaded ĐÃ phát đúng nhạc rồi, PlayBGM bỏ qua
    ///     (idempotent) nên không ngắt giữa chừng fade.
    /// </summary>
    private void Start()
    {
        if (AudioManager.Instance != null)
            PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Sự kiện Unity gọi mỗi khi 1 scene được load xong.
    /// Tự kiểm tra tên scene và đổi nhạc nền cho đúng.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    /// <summary>
    /// Tìm entry khớp với sceneName và phát nhạc tương ứng.
    /// Là hàm CÔNG KHAI nên bạn có thể gọi bằng tay từ nơi khác,
    /// VD: sau khi đổi scene thủ công bằng SceneManager.LoadScene.
    /// </summary>
    /// <param name="sceneName">Tên scene muốn phát nhạc.</param>
    public void PlayBGMForScene(string sceneName)
    {
        if (AudioManager.Instance == null)
            return;

        // Duyệt danh sách, tìm entry trùng tên scene.
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                SceneBGMEntry entry = entries[i];
                if (entry == null)
                    continue;
                if (string.IsNullOrEmpty(entry.sceneName))
                    continue;
                if (entry.sceneName == sceneName)
                {
                    // Đổi nhạc (clip null => tự tắt nhạc cũ).
                    AudioManager.Instance.PlayBGM(entry.bgmClip, entry.volume, entry.loop, fadeDuration);
                    return;
                }
            }
        }

        // Không tìm thấy scene trong danh sách:
        if (stopBGMWhenNoMatch)
            AudioManager.Instance.StopBGM(fadeDuration);
    }

    /// <summary>
    /// Làm mới thủ công: phát lại nhạc cho scene ĐANG hoạt động.
    /// Hữu ích khi bạn chỉnh danh sách entries lúc runtime.
    /// </summary>
    public void Refresh()
    {
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }
}