using UnityEngine;

/// ============================================================================
/// ExplosionSFX - Phát âm thanh VỤ NỔ theo không gian 3D (spatial audio).
///
/// ▐▌ VÌ SAO DÙNG 3D CHO VỤ NỔ?
///   - Vụ nổ xảy ra ở 1 vị trí trên bản đồ (lựu đạn, bom, xe nổ...).
///   - Dùng AudioSource 3D: xa thì nghe nhỏ, gần thì nghe to, nổ sau lưng
///     thì nghe sau lưng => cảm giác chân thật, có chiều sâu không gian.
///
/// ▐▌ CÁCH DÙNG (chỉ thêm 1 DÒNG trong code của bạn):
///   Cách 1 - nổ đơn lẻ:
///     ExplosionSFX.Play(clip, transform.position);
///
///   Cách 2 - có tùy chỉnh khoảng cách nghe:
///     ExplosionSFX.Play(clip, transform.position, volume: 1f,
///                       minDistance: 2f, maxDistance: 40f);
///
///   Ví dụ TÍCH HỢP NGAY vào script BombActive.cs có sẵn trong dự án
///   (trong hàm Explode(), ngay sau CameraShake.Shake(...)):
///       ExplosionSFX.Play(explosionSound, transform.position);
///   (nhớ khai báo thêm 1 ô [SerializeField] private AudioClip explosionSound;)
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR:
///   - Đây là script DẠNG STATIC: không cần kéo vào bất kỳ GameObject nào.
///   - Chỉ cần đảm bảo có 1 AudioManager (hoặc hệ thống tự tạo) trong game.
///   - Nhớ kéo AudioClip tiếng nổ của bạn vào biến clip khi gọi hàm.
///
/// ▐▌ LƯU Ý HIỆU NĂNG:
///   - Bên trong chỉ gọi AudioManager.PlaySFX3D - sử dụng POOL nên hầu như
///     không tạo Garbage, không Instantiate/Destroy, không lo bị leak.
/// ============================================================================
public static class ExplosionSFX
{
    // Khoảng cách nghe mặc định hợp lý cho game 2.5D / top-down.
    private const float DEF_MIN_DISTANCE = 1f;
    private const float DEF_MAX_DISTANCE = 30f;

    /// <summary>
    /// Phát tiếng vụ nổ 3D tại vị trí mong muốn.
    /// Là hàm duy nhất bạn cần gọi.
    /// </summary>
    /// <param name="clip">AudioClip tiếng nổ.</param>
    /// <param name="position">Vị trí nổ trong world coordinates.</param>
    /// <param name="volume">Độ lớn (0-1), mặc định 1.</param>
    /// <param name="minDistance">Khoảng cách gần nhất nghe to nhất (mặc định 1m).</param>
    /// <param name="maxDistance">Khoảng cách xa nhất còn nghe thấy (mặc định 30m).</param>
    /// <param name="randomPitchMin">Pitch tối thiểu ngẫu nhiên (VD 0.9).</param>
    /// <param name="randomPitchMax">Pitch tối đa ngẫu nhiên (VD 1.1).</param>
    public static void Play(AudioClip clip, Vector3 position,
        float volume = 1f,
        float minDistance = DEF_MIN_DISTANCE,
        float maxDistance = DEF_MAX_DISTANCE,
        float randomPitchMin = 1f,
        float randomPitchMax = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[ExplosionSFX] Không có AudioClip => bỏ qua phát âm thanh nổ.");
            return;
        }

        if (AudioManager.Instance == null)
        {
            // AudioManager chưa được tạo -> không có điểm phát -> bỏ qua.
            return;
        }

        float center = (randomPitchMin + randomPitchMax) * 0.5f;
        float variation = Mathf.Max(0f, (randomPitchMax - randomPitchMin) * 0.5f);

        AudioManager.Instance.PlaySFX3D(
            clip,               // tiếng nổ
            position,           // vị trí nổ
            volume,             // độ lớn
            1f,                 // spatialBlend = 1 => âm thanh theo khoảng cách thật
            minDistance,
            maxDistance,
            center,             // pitch gốc
            variation);         // ngẫu nhiên hóa pitch để các tiếng nổ đỡ trùng lặp
    }

    /// <summary>
    /// Phiên bản tiện lợi: ngẫu nhiên hóa pitch mặc định ±10% để nhiều quả nổ
    /// cùng lúc không nghe y chan nhau (đỡ đau tai).
    /// </summary>
    /// <param name="clip">AudioClip tiếng nổ.</param>
    /// <param name="position">Vị trí nổ.</param>
    /// <param name="volume">Độ lớn (0-1).</param>
    /// <param name="minDistance">Khoảng cách nghe gần nhất.</param>
    /// <param name="maxDistance">Khoảng cách nghe xa nhất.</param>
    public static void PlayRandomized(AudioClip clip, Vector3 position,
        float volume = 1f,
        float minDistance = DEF_MIN_DISTANCE,
        float maxDistance = DEF_MAX_DISTANCE)
    {
        Play(clip, position, volume, minDistance, maxDistance, 0.9f, 1.1f);
    }

    // Cached player position (để tránh FindGameObjectWithTag mỗi lần gọi).
    private static Transform cachedPlayer;

    /// <summary>
    /// PHÁT TIẾNG NỔ CHO GAME 2D TOP-DOWN.
    ///
    /// ▐▌ VÌ SAO CẦN HÀM NÀY THAY VÌ Play() (3D)?
    ///   - Ở game 2D top-down, camera nhìn thẳng xuống trục -Z nên audio 3D
    ///     (dựa theo khoảng cách dọc trục Z tới AudioListener) bị phẳng, dễ
    ///     KHÔNG NGHE THẤY tiếng nổ.
    ///   - Hàm này phát kiểu 2D one-shot nhưng TỰ TÍNH khoảng cách thật (X,Y)
    ///     từ vị trí nổ tới player, rồi GIẢM VOLUME khi càng xa => vẫn có cảm
    ///     giác gần/xa chân thật, mà tiếng luôn NGHE ĐƯỢC.
    /// </summary>
    /// <param name="clip">AudioClip tiếng nổ.</param>
    /// <param name="position">Vị trí nổ (world).</param>
    /// <param name="volume">Độ lớn tối đa khi nổ SAT ngay cạnh player (0-1).</param>
    /// <param name="maxDistance">Khoảng cách xa nhất còn nghe thấy (mặc định 20m).</param>
    /// <param name="pitchVariation">Mức random pitch (±), VD 0.1 = ±10%.</param>
    public static void Play2D(AudioClip clip, Vector3 position,
        float volume = 1f,
        float maxDistance = 20f,
        float pitchVariation = 0.1f)
    {
        if (clip == null)
            return;

        if (AudioManager.Instance == null)
            return;

        // Tìm player một lần, cache lại để lần sau không tốn chi phí.
        if (cachedPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                cachedPlayer = player.transform;
        }

        // Không tìm thấy player => phát to đều (2D) để đảm bảo nghe thấy.
        if (cachedPlayer == null)
        {
            AudioManager.Instance.PlaySFX(clip, volume, 1f, pitchVariation);
            return;
        }

        // Tính khoảng cách X,Y thật và suy ra hệ số volume.
        float dist = Vector3.Distance(position, cachedPlayer.position);
        float t = Mathf.InverseLerp(0f, maxDistance, dist); // 0 = sát, 1 = tối đa
        float scale = 1f - Mathf.Clamp01(t);                // 1 = sát, ~0 = xa tối đa
        float finalVolume = Mathf.Clamp01(volume) * scale;

        if (finalVolume <= 0.001f)
            return; // quá xa, bỏ qua để tiết kiệm.

        AudioManager.Instance.PlaySFX(clip, finalVolume, 1f, pitchVariation);
    }
}