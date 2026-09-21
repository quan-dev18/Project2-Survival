using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ============================================================================
/// AudioManager - Hệ thống quản lý âm thanh toàn cục (Singleton).
///
/// ▐▌ CHỨC NĂNG CHÍNH
///   1. Quản lý BGM (Background Music): chỉ 1 nhạc nền tại 1 thời điểm,
///      hỗ trợ fade in/out mượt khi đổi scene hay đổi nhạc.
///   2. Quản lý SFX dạng OneShot 2D (tiếng click UI, tiếng súng...).
///   3. Quản lý SFX 3D (khoảng cách thật) dùng cho vụ nổ, vật rơi...
///   4. Âm thanh UI: play click, play toggle on/off.
///   5. Lưu/đọc Volume BGM & SFX qua PlayerPrefs (game thoát vẫn nhớ).
///
/// ▐▌ VÌ SAO KHÔNG BỊ LEAK / KHÔNG GIẬT (LAG)?
///   - AudioSource được NHỒI SẴN vào 2 cái POOL (2D và 3D) từ Awake.
///   - Khi phát SFX ta chỉ "tái sử dụng" AudioSource có sẵn thay vì
///     Instantiate/Destroy GameObject liên tục => không tạo Garbage nhiều
///     (Garbage chính là thủ phạm gây giật frame).
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR (nhanh gọn):
///   1. Tạo 1 GameObject RỖNG tên là "AudioManager".
///      (Nhấp chuột phải trong Hierarchy -> Create Empty).
///   2. Kéo script AudioManager.cs lên GameObject đó.
///   3. Tích vào ô "Is Global" KHÔNG CẦN - làm theo bước 4 để đổi scene không mất:
///      Trong script, AudioManager TỰ GỌI DontDestroyOnLoad nên Object sẽ sống
///      xuyên qua các scene. KHÔNG cần làm gì thêm.
///   4. Kéo AudioClip tiếng click / toggle on / toggle off vào 3 ô
///      "UI Click SFX", "UI Toggle On SFX", "UI Toggle Off SFX" (nếu có).
///   5. Đưa GameObject "AudioManager" này vào scene ĐẦU TIÊN của game
///      (LoadScene đầu tiên chạy), ví dụ scene Menu. Nó sẽ tự tồn tại mãi mãi.
///   6. Nhớ: mỗi scene phải có ĐÚNG 1 AudioListener (thường để trên Main Camera).
///      Đừng gắn AudioListener lên AudioManager để tránh cảnh báo "2 listeners".
///
///   Nếu quên đặt AudioManager vào scene nào đó, hệ thống sẽ TỰ ĐỘNG tạo ra
///   một bản đúng lúc game chạy (xem EnsureInstance/Hàm auto-create bên dưới).
/// ============================================================================
public sealed class AudioManager : MonoBehaviour
{
    // =============================================================
    //  SIGNLETON - con trỏ dùng chung cho toàn game
    // =============================================================
    public static AudioManager Instance { get; private set; }

    // =============================================================
    //  SERIALIZE FIELDS - thiết lập ngay trong Inspector của Unity
    // =============================================================
    [Header("=== Audio Sources (để trống là tự tạo) ===")]
    [Tooltip("Nguồn phát nhạc nền BGM. Để trống, script tự tạo.")]
    [SerializeField] private AudioSource bgmSource;

    [Header("=== Pool kích thước ===")]
    [Tooltip("Số AudioSource 2D dùng làm SFX one-shot. 8-16 là cân bằng tốt.")]
    [SerializeField] private int sfx2DPoolSize = 12;

    [Tooltip("Số AudioSource 3D cho âm thanh theo vị trí (vụ nổ...).")]
    [SerializeField] private int sfx3DPoolSize = 8;

    [Tooltip("Số AudioSource dùng cho SFX dạng LOOP (tiếng lửa flamethrower...).")]
    [SerializeField] private int loopSFXPoolSize = 3;

    [Header("=== Âm thanh UI ===")]
    [Tooltip("Tiếng click nút chung cho toàn game.")]
    [SerializeField] private AudioClip uiClickSFX;

    [Tooltip("Tiếng khi BẬT (on) một Toggle/switch trong UI.")]
    [SerializeField] private AudioClip uiToggleOnSFX;

    [Tooltip("Tiếng khi TẮT (off) một Toggle/switch trong UI.")]
    [SerializeField] private AudioClip uiToggleOffSFX;

    [Tooltip("Tiếng khi NHẬN ĐỒ (claim) — dùng cho Daily Reward, nhận quà hằng ngày, nhận thưởng...")]
    [SerializeField] private AudioClip uiClaimSFX;

    [Header("=== Âm thanh thu thập ===")]
    [Tooltip("Tiếng khi nhặt viên kinh nghiệm (EXP).")]
    [SerializeField] private AudioClip xpCollectSFX;

    [Tooltip("Tiếng khi nhặt vàng (Gold).")]
    [SerializeField] private AudioClip goldCollectSFX;

    [Tooltip("Tiếng khi lên cấp (Level Up).")]
    [SerializeField] private AudioClip levelUpSFX;

    [Header("=== Cài đặt mặc định ===")]
    [Tooltip("Thời gian fade in/out BGM khi đổi nhạc hoặc đổi scene (giây).")]
    [SerializeField] private float bgmFadeDuration = 0.5f;

    [Tooltip("Volume BGM mặc định lần đầu chạy game.")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultBGMVolume = 0.6f;

    [Tooltip("Volume SFX mặc định lần đầu chạy game.")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 1f;

    // =============================================================
    //  RUNTIME - danh sách các AudioSource được nhồi sẵn (POOL)
    // =============================================================
    private readonly List<AudioSource> sfx2DPool = new List<AudioSource>();
    private readonly List<AudioSource> sfx3DPool = new List<AudioSource>();
    private int sfx2DCursor;   // con trỏ vòng tròn cho pool 2D
    private int sfx3DCursor;   // con trỏ vòng tròn cho pool 3D

    private readonly List<AudioSource> loopSFXPool = new List<AudioSource>();
    private readonly HashSet<AudioClip> activeLoopClips = new HashSet<AudioClip>();
    private readonly Dictionary<AudioSource, float> loopBaseVolumes = new Dictionary<AudioSource, float>();
    private int loopSFXCursor; // con trỏ vòng tròn cho pool loop

    private AudioClip currentBGM;          // clip BGM đang phát
    private float currentBGMVolume = 1f;   // hệ số volume riêng của clip đang phát
    private Coroutine bgmFadeRoutine;      // coroutine fade đang chạy

    private float bgmVolume;   // volume BGM do người chơi chỉnh (lưu PlayerPrefs)
    private float sfxVolume;   // volume SFX do người chơi chỉnh (lưu PlayerPrefs)

    // Khóa lưu giá trị volume trong PlayerPrefs
    private const string KEY_BGM_VOLUME = "Audio_BGM_Volume";
    private const string KEY_SFX_VOLUME = "Audio_SFX_Volume";

    // =============================================================
    //  PHẦN 1: KHỞI TẠO
    // =============================================================

    /// <summary>
    /// Awake: ép Singleton duy nhất, sống xuyên scene, tạo AudioSource + pool.
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

        // Tạo nguồn BGM nếu Inspector chưa gán.
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureBGMSource(bgmSource);
        }

        // Nhồi sẵn các AudioSource dùng 1 lần.
        InitializePools();

        // Đọc volume lưu từ lần chơi trước (nếu chưa từng lưu thì dùng mặc định).
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, defaultBGMVolume);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, defaultSFXVolume);
        ApplyVolumes();
    }

    /// <summary>
    /// [TỰ ĐỘNG] Nếu trong scene không có AudioManager nào,
    /// game sẽ tự tạo 1 cái khi scene đầu tiên chạy xong.
    /// => bạn có thể quên việc kéo object vào scene nhưng vẫn có âm thanh.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null || FindObjectOfType<AudioManager>() != null)
            return;

        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    /// <summary>
    /// Cấu hình AudioSource chuyên phát BGM (2D, lặp vô tận).
    /// </summary>
    private void ConfigureBGMSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    /// <summary>
    /// Tạo pool AudioSource 2D (one-shot) và 3D (âm thanh theo vị trí).
    /// Tất cả đều là con của AudioManager để xếp gọn trong Hierarchy.
    /// </summary>
    private void InitializePools()
    {
        int size2D = Mathf.Max(1, sfx2DPoolSize);
        int size3D = Mathf.Max(1, sfx3DPoolSize);

        for (int i = 0; i < size2D; i++)
        {
            GameObject go = new GameObject($"SFX2D_{i}");
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            Configure2DSource(src);
            sfx2DPool.Add(src);
        }

        for (int i = 0; i < size3D; i++)
        {
            GameObject go = new GameObject($"SFX3D_{i}");
            go.transform.SetParent(transform, false);
            go.SetActive(false); // chưa dùng thì tắt để không tốn công xử lý
            AudioSource src = go.AddComponent<AudioSource>();
            Configure3DSource(src);
            sfx3DPool.Add(src);
        }

        int loopSize = Mathf.Max(1, loopSFXPoolSize);
        for (int i = 0; i < loopSize; i++)
        {
            GameObject go = new GameObject($"SFXLoop_{i}");
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            ConfigureLoopSource(src);
            loopSFXPool.Add(src);
        }
    }

    /// <summary>
    /// Cấu hình AudioSource 2D: không lặp, không âm thanh không gian.
    /// </summary>
    private void Configure2DSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.pitch = 1f;
    }

    /// <summary>
    /// Cấu hình AudioSource dành riêng SFX dạng LOOP (tiếng lửa, quạt...).
    /// Không âm thanh không gian, lặp vô tận cho tới khi được dừng.
    /// </summary>
    private void ConfigureLoopSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.pitch = 1f;
    }

    /// <summary>
    /// Cấu hình AudioSource 3D: âm thanh không gian (log), dùng cho vụ nổ...
    /// </summary>
    private void Configure3DSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1f;
        source.maxDistance = 30f;
        source.dopplerLevel = 0f;
        source.volume = 1f;
        source.pitch = 1f;
    }

    /// <summary>
    /// Áp volume đã đọc được lên các nguồn AudioSource.
    /// </summary>
    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmVolume * currentBGMVolume;
    }

    // =============================================================
    //  PHẦN 2: BGM (Nhạc nền)
    // =============================================================

    /// <summary>
    /// Phát nhạc nền mới (có fade). Nếu clip == null => đây là lệnh tắt nhạc.
    /// Nếu clip giống BGM đang chạy => giữ nguyên, không phát lại từ đầu.
    /// </summary>
    /// <param name="clip">Clip nhạc nền muốn phát.</param>
    /// <param name="volume">Volume riêng của clip này (0-1).</param>
    /// <param name="loop">Có lặp vô tận không (mặc định có).</param>
    public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true)
    {
        PlayBGM(clip, volume, loop, bgmFadeDuration);
    }

    /// <summary>
    /// Phát BGM với thời gian fade tùy chỉnh. Dùng khi đổi scene code thủ công.
    /// </summary>
    public void PlayBGM(AudioClip clip, float volume, bool loop, float fadeDuration)
    {
        // Đang phát chính clip này rồi => KHÔNG làm gì, giữ nguyên nhạc
        // (không cắt ngang fade đang chạy, không khởi động lại). Nhờ vậy các
        // lần gọi LẶP LẠI (SceneBGM gọi ở cả OnEnable + Start, hoặc 2 scene
        // vẫn dùng chung 1 nhạc) là vô hại, nhạc phát liền mạch.
        if (clip != null && currentBGM == clip && bgmSource.isPlaying)
            return;

        StopCurrentFade();
        currentBGMVolume = Mathf.Clamp01(volume);

        // Clip rỗng => tắt nhạc từ từ.
        if (clip == null)
        {
            bgmFadeRoutine = StartCoroutine(FadeOutRoutine(Mathf.Max(0.01f, fadeDuration)));
            return;
        }

        currentBGM = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();
        bgmFadeRoutine = StartCoroutine(FadeInRoutine(bgmVolume * currentBGMVolume, Mathf.Max(0.01f, fadeDuration)));
    }

    /// <summary>
    /// Tắt nhạc nền hiện tại (fade ra rồi Stop).
    /// </summary>
    public void StopBGM()
    {
        StopBGM(bgmFadeDuration);
    }

    /// <summary>
    /// Tắt nhạc nền với thời gian fade tùy chỉnh.
    /// </summary>
    public void StopBGM(float fadeDuration)
    {
        StopCurrentFade();
        bgmFadeRoutine = StartCoroutine(FadeOutRoutine(Mathf.Max(0.01f, fadeDuration)));
    }

    /// <summary>
    /// Dừng coroutine fade đang chạy (nếu có) để tránh 2 coroutine đấu nhau.
    /// </summary>
    private void StopCurrentFade()
    {
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }
    }

    /// <summary>
    /// Tăng dần volume BGM tới targetVolume trong fadeDuration giây.
    /// </summary>
    private IEnumerator FadeInRoutine(float targetVolume, float fadeDuration)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (bgmSource != null)
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        if (bgmSource != null)
            bgmSource.volume = targetVolume;
        bgmFadeRoutine = null;
    }

    /// <summary>
    /// Giảm dần volume BGM về 0 rồi Stop. Được gọi khi đổi nhạc/tắt nhạc.
    /// </summary>
    private IEnumerator FadeOutRoutine(float fadeDuration)
    {
        float elapsed = 0f;
        float startVolume = bgmSource != null ? bgmSource.volume : 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (bgmSource != null)
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        if (bgmSource != null)
        {
            bgmSource.volume = 0f;
            bgmSource.Stop();
            bgmSource.clip = null;
        }
        currentBGM = null;
        bgmFadeRoutine = null;
    }

    // =============================================================
    //  PHẦN 3: SFX 2D (âm thanh thường, không theo khoảng cách)
    // =============================================================

    /// <summary>
    /// HÀM SFX TỔNG QUÁT: phát 1 hiệu ứng âm thanh 2D kiểu one-shot.
    /// Dùng cho tiếng súng, tiếng nhặt vật phẩm, tiếng click UI...
    /// </summary>
    /// <param name="clip">AudioClip cần phát.</param>
    /// <param name="volume">Độ lớn (0-1), sau đó nhân thêm volume SFX toàn cục.</param>
    /// <param name="pitch">Độ cao giọng gốc (1 = bình thường).</param>
    /// <param name="pitchRandom">Mức ngẫu nhiên hóa pitch (± bao nhiêu). VD: 0.1 => cao/thấp hơn tối đa 10%, tao cảm giác tự nhiên.</param>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchRandom = 0f)
    {
        if (clip == null) return;

        AudioSource src = GetNext2DSource();
        if (src == null) return;

        float finalPitch = pitch;
        if (pitchRandom > 0f)
            finalPitch = pitch * UnityEngine.Random.Range(1f - pitchRandom, 1f + pitchRandom);

        src.pitch = Mathf.Max(0.01f, finalPitch);
        src.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
    }

    /// <summary>
    /// Lấy 1 AudioSource 2D còn trống (hoặc đã phát lâu nhất) từ pool.
    /// Dùng con trỏ vòng tròn nên không tốn chi phí tìm kiếm.
    /// </summary>
    private AudioSource GetNext2DSource()
    {
        if (sfx2DPool.Count == 0)
            InitializePools();
        if (sfx2DPool.Count == 0)
            return null;

        AudioSource src = sfx2DPool[sfx2DCursor];
        sfx2DCursor = (sfx2DCursor + 1) % sfx2DPool.Count;
        return src;
    }

    // =============================================================
    //  PHẦN 4: SFX 3D (âm thanh theo vị trí - khoảng cách thật)
    // =============================================================

    /// <summary>
    /// Phát âm thanh 3D TẠI 1 VỊ TRÍ trên thế giới (vd: vụ nổ).
    /// AudioSource được lấy từ pool 3D nên không Instantiate/Destroy liên tục.
    /// Sau khi clip chạy xong, game đưa nó trở lại trạng thái "ngủ" (inactive).
    /// </summary>
    /// <param name="clip">AudioClip cần phát.</param>
    /// <param name="position">Vị trí trong world xảy ra âm thanh.</param>
    /// <param name="volume">Độ lớn (0-1).</param>
    /// <param name="spatialBlend">1 = nghe theo khoảng cách, 0 = nghe đều mọi chỗ.</param>
    /// <param name="minDistance">Khoảng cách gần nhất: âm thanh không to thêm nữa.</param>
    /// <param name="maxDistance">Khoảng cách xa nhất: ra ngoài là không nghe thấy.</param>
    /// <param name="pitch">Độ cao giọng gốc.</param>
    /// <param name="pitchRandom">Mức ngẫu nhiên hóa pitch (±%).</param>
    public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f,
        float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 30f,
        float pitch = 1f, float pitchRandom = 0f)
    {
        if (clip == null) return;

        AudioSource src = GetNext3DSource();
        if (src == null) return;

        float finalPitch = pitch;
        if (pitchRandom > 0f)
            finalPitch = pitch * UnityEngine.Random.Range(1f - pitchRandom, 1f + pitchRandom);

        // Đánh thức source, đưa tới vị trí cần phát.
        src.gameObject.SetActive(true);
        src.transform.position = position;
        src.spatialBlend = spatialBlend;
        src.minDistance = Mathf.Max(0.01f, minDistance);
        src.maxDistance = Mathf.Max(minDistance, maxDistance);
        src.pitch = Mathf.Max(0.01f, finalPitch);
        src.volume = Mathf.Clamp01(volume) * sfxVolume;
        src.clip = clip;
        src.Play();

        // Đặt hẹn giờ trả source về pool khi clip phát xong.
        StartCoroutine(Release3DSourceWhenDone(src, clip));
    }

    /// <summary>
    /// Lấy 1 AudioSource 3D từ pool (con trỏ vòng tròn).
    /// </summary>
    private AudioSource GetNext3DSource()
    {
        if (sfx3DPool.Count == 0)
            InitializePools();
        if (sfx3DPool.Count == 0)
            return null;

        AudioSource src = sfx3DPool[sfx3DCursor];
        sfx3DCursor = (sfx3DCursor + 1) % sfx3DPool.Count;
        return src;
    }

    /// <summary>
    /// Chờ clip phát xong rồi tắt GameObject => về lại pool.
    /// Kiểm tra src.clip == clip để chắc chắn không tắt nhầm 1 âm thanh
    /// mới đang phát trên chính source này (tránh bị cắt ngang).
    /// </summary>
    private IEnumerator Release3DSourceWhenDone(AudioSource src, AudioClip clip)
    {
        yield return new WaitForSeconds(clip.length + 0.1f);

        if (src != null && src.clip == clip)
        {
            src.Stop();
            src.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Tắt toàn bộ SFX đang kêu (2D + 3D + loop). Hay dùng khi vào màn hình tạm dừng.
    /// </summary>
    public void StopAllSFX()
    {
        for (int i = 0; i < sfx2DPool.Count; i++)
        {
            if (sfx2DPool[i] != null)
                sfx2DPool[i].Stop();
        }
        for (int i = 0; i < sfx3DPool.Count; i++)
        {
            if (sfx3DPool[i] != null)
            {
                sfx3DPool[i].Stop();
                sfx3DPool[i].gameObject.SetActive(false);
            }
        }
        for (int i = 0; i < loopSFXPool.Count; i++)
        {
            if (loopSFXPool[i] != null)
                loopSFXPool[i].Stop();
        }
        // Xóa trạng thái loop để phiên phát tiếp theo tự khởi động lại.
        activeLoopClips.Clear();
        loopBaseVolumes.Clear();
    }

    // =============================================================
    //  PHẦN 4b: SFX LOOP (âm thanh lặp liên tục - tiếng lửa flamethrower...)
    // =============================================================

    /// <summary>
    /// Bắt đầu phát SFX dạng LOOP (lặp vô tận cho tới khi StopLoopSFX/StopAllSFX).
    /// Dùng riêng cho tiếng liên tục như tiếng lửa của flamethrower.
    /// Mỗi clip loop được gọi nhiều lần đều an toàn: nếu đang chạy rồi thì giữ nguyên.
    /// </summary>
    /// <param name="clip">AudioClip cần lặp.</param>
    /// <param name="volume">Độ lớn (0-1), sau đó nhân thêm volume SFX toàn cục.</param>
    /// <param name="pitch">Độ cao giọng (1 = bình thường).</param>
    public void PlayLoopSFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        if (activeLoopClips.Contains(clip)) return;

        AudioSource src = GetFreeLoopSource();
        if (src == null) return;

        src.clip = clip;
        src.pitch = Mathf.Max(0.01f, pitch);
        loopBaseVolumes[src] = Mathf.Clamp01(volume);
        src.volume = Mathf.Clamp01(volume) * sfxVolume;
        src.Play();
        activeLoopClips.Add(clip);
    }

    /// <summary>
    /// Dừng 1 SFX loop theo clip (idempotent - gọi nhiều lần vẫn an toàn).
    /// Các loop khác vẫn tiếp tục chạy.
    /// </summary>
    public void StopLoopSFX(AudioClip clip)
    {
        if (clip == null) return;

        for (int i = 0; i < loopSFXPool.Count; i++)
        {
            AudioSource src = loopSFXPool[i];
            if (src != null && src.clip == clip)
            {
                src.Stop();
                src.clip = null;
                loopBaseVolumes.Remove(src);
            }
        }
        activeLoopClips.Remove(clip);
    }

    /// <summary>
    /// Đổi volume 1 SFX loop đang chạy (dùng để fade in/out nhịp nhàng).
    /// Idempotent: nếu clip không đang loop thì không làm gì.
    /// </summary>
    public void SetLoopVolume(AudioClip clip, float volume)
    {
        if (clip == null) return;

        for (int i = 0; i < loopSFXPool.Count; i++)
        {
            AudioSource src = loopSFXPool[i];
            if (src != null && src.clip == clip)
            {
                loopBaseVolumes[src] = Mathf.Clamp01(volume);
                src.volume = Mathf.Clamp01(volume) * sfxVolume;
            }
        }
    }

    /// <summary>
    /// Kiểm tra clip loop có đang được phát không (để nguồn khác tự phục hồi
    /// khi audio bị tắt bởi StopAllSFX như lúc pause game).
    /// </summary>
    public bool IsLoopActive(AudioClip clip)
    {
        return clip != null && activeLoopClips.Contains(clip);
    }

    /// <summary>
    /// Lấy 1 AudioSource loop rảnh. Nếu còn trống thì dùng source đó; nếu hết
    /// thì tái dùng source phát lâu nhất (con trỏ vòng tròn).
    /// </summary>
    private AudioSource GetFreeLoopSource()
    {
        if (loopSFXPool.Count == 0)
            InitializePools();
        if (loopSFXPool.Count == 0)
            return null;

        for (int i = 0; i < loopSFXPool.Count; i++)
        {
            AudioSource src = loopSFXPool[i];
            if (!src.isPlaying)
                return src;
        }

        AudioSource oldest = loopSFXPool[loopSFXCursor % loopSFXPool.Count];
        loopSFXCursor = (loopSFXCursor + 1) % loopSFXPool.Count;
        if (oldest.clip != null)
            activeLoopClips.Remove(oldest.clip);
        loopBaseVolumes.Remove(oldest);
        return oldest;
    }

    // =============================================================
    //  PHẦN 5: ÂM THANH UI (click / toggle)
    // =============================================================

    /// <summary>
    /// Phát tiếng click chung của UI. Gắn được ngay trong Unity:
    /// EventSystem/Button -> OnClick -> kéo AudioManager vào -> chọn PlayUIClick.
    /// </summary>
    public void PlayUIClick(float volume = 1f)
    {
        PlaySFX(uiClickSFX, volume);
    }

    /// <summary>
    /// Phát tiếng click bằng clip tự truyền (không cần cấu hình trong Inspector).
    /// </summary>
    public void PlayUIClick(AudioClip clip, float volume = 1f)
    {
        PlaySFX(clip, volume);
    }

    /// <summary>
    /// Phát tiếng khi NHẬN ĐỒ (claim) — gắn được ngay trong Unity:
    /// EventSystem/Button -> OnClick -> kéo AudioManager vào -> chọn PlayUIClaim.
    /// </summary>
    public void PlayUIClaim(float volume = 1f)
    {
        PlaySFX(uiClaimSFX, volume);
    }

    /// <summary>
    /// Phiên bản PlayUIClaim có clip tự truyền từ bên ngoài (không cần cấu hình Inspector).
    /// </summary>
    public void PlayUIClaim(AudioClip clip, float volume = 1f)
    {
        PlaySFX(clip, volume);
    }

    /// <summary>
    /// HÀM HỖ TRỢ UI TOGGLE: phát tiếng theo trạng thái BẬT/TẮT.
    /// Cách dùng trong code:
    ///   myToggle.onValueChanged.AddListener(isOn => AudioManager.Instance.PlayUIToggle(isOn));
    /// </summary>
    /// <param name="isOn">true = mới BẬT, false = mới TẮT.</param>
    public void PlayUIToggle(bool isOn, float volume = 1f)
    {
        AudioClip clip = isOn ? uiToggleOnSFX : uiToggleOffSFX;
        if (clip == null)
            clip = uiClickSFX; // nếu không có clip riêng on/off thì dùng tiếng click chung

        PlaySFX(clip, volume, 1f, 0.05f);
    }

    /// <summary>
    /// Phiên bản PlayUIToggle có clip tự truyền từ bên ngoài.
    /// </summary>
    public void PlayUIToggle(AudioClip clip, bool isOn, float volume = 1f)
    {
        AudioClip final = clip != null ? clip : (isOn ? uiToggleOnSFX : uiToggleOffSFX);
        if (final == null)
            final = uiClickSFX;

        PlaySFX(final, volume, 1f, 0.05f);
    }

    // =============================================================
    //  PHẦN 5.5: ÂM THANH THU THẬP (EXP / GOLD)
    // =============================================================

    /// <summary>
    /// Phát tiếng nhặt kinh nghiệm (EXP). Pitch random nhẹ để đỡ đơn điệu
    /// khi nhặt nhiều viên liên tiếp.
    /// </summary>
    public void PlayXPCollect(float volume = 1f)
    {
        PlaySFX(xpCollectSFX, volume, 1f, 0.08f);
    }

    /// <summary>
    /// Phát tiếng nhặt vàng (Gold).
    /// </summary>
    public void PlayGoldCollect(float volume = 1f)
    {
        PlaySFX(goldCollectSFX, volume, 1f, 0.08f);
    }

    /// <summary>
    /// Phát tiếng lên cấp (Level Up). Không random pitch (giữ nguyên tiếng
    /// oanh tạc/grand cho đúng cảm giác trọng đại).
    /// </summary>
    public void PlayLevelUp(float volume = 1f)
    {
        PlaySFX(levelUpSFX, volume, 1f, 0f);
    }

    // =============================================================
    //  PHẦN 6: ĐIỀU KHIỂN VOLUME (tự lưu vào PlayerPrefs)
    // =============================================================

    /// <summary>
    /// Volume BGM hiện tại (0-1). Gán giá trị mới sẽ tự lưu lên PlayerPrefs.
    /// </summary>
    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_BGM_VOLUME, bgmVolume);
            ApplyVolumes();
        }
    }

    /// <summary>
    /// Volume SFX hiện tại (0-1). Gán giá trị mới sẽ tự lưu lên PlayerPrefs.
    /// </summary>
    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
            ApplyLoopVolumes();
        }
    }

    /// <summary>
    /// Cập nhật volume theo volume SFX toàn cục cho các SFX loop đang chạy.
    /// </summary>
    private void ApplyLoopVolumes()
    {
        for (int i = 0; i < loopSFXPool.Count; i++)
        {
            AudioSource src = loopSFXPool[i];
            if (src == null) continue;
            if (loopBaseVolumes.TryGetValue(src, out float baseVolume))
                src.volume = baseVolume * sfxVolume;
        }
    }

    /// <summary>
    /// Clip BGM đang phát (để SceneBGM biết mà tránh phát lại).
    /// </summary>
    public AudioClip CurrentBGM => currentBGM;

    // =============================================================
    //  KHU VỰC DEBUG - Chỉ tồn tại khi chạy trong Editor.
    //  Giúp bạn bấm thử âm thanh không cần vào game:
    //  Chọn GO manager -> dấu 3 chấm (⋮) góc phải component AudioManager
    //  -> chọn "Test Play ...".
    // =============================================================
#if UNITY_EDITOR
    [ContextMenu("Test Play Gold Collect")]
    private void TestPlayGoldCollect()
    {
        Debug.Log($"[AudioManager] Test Gold: clip={(goldCollectSFX != null ? goldCollectSFX.name : "(TRONG !)")}, SFXVolume={sfxVolume}");
        PlayGoldCollect();
    }

    [ContextMenu("Test Play XP Collect")]
    private void TestPlayXPCollect()
    {
        Debug.Log($"[AudioManager] Test XP: clip={(xpCollectSFX != null ? xpCollectSFX.name : "(TRONG !)")}, SFXVolume={sfxVolume}");
        PlayXPCollect();
    }

    [ContextMenu("Test Play Level Up")]
    private void TestPlayLevelUp()
    {
        Debug.Log($"[AudioManager] Test LevelUp: clip={(levelUpSFX != null ? levelUpSFX.name : "(TRONG !)")}, SFXVolume={sfxVolume}");
        PlayLevelUp();
    }

    [ContextMenu("Test Play UI Toggle On")]
    private void TestPlayUIToggleOn()
    {
        Debug.Log($"[AudioManager] Test Toggle-On: clip={(uiToggleOnSFX != null ? uiToggleOnSFX.name : "(TRONG !)")}");
        PlayUIToggle(true);
    }

    [ContextMenu("Test Play UI Toggle Off")]
    private void TestPlayUIToggleOff()
    {
        Debug.Log($"[AudioManager] Test Toggle-Off: clip={(uiToggleOffSFX != null ? uiToggleOffSFX.name : "(TRONG !)")}");
        PlayUIToggle(false);
    }

    [ContextMenu("Test Play UI Claim")]
    private void TestPlayUIClaim()
    {
        Debug.Log($"[AudioManager] Test Claim: clip={(uiClaimSFX != null ? uiClaimSFX.name : "(TRONG !)")}, SFXVolume={sfxVolume}");
        PlayUIClaim();
    }
#endif
}