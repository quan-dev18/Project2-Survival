using UnityEngine;

/// ============================================================================
/// GunAudio - Quản lý âm thanh riêng cho từng loại súng (bắn + lên đạn).
///
/// ▐▌ MÔ HÌNH HOẠT ĐỘNG:
///   - Mỗi súng có 2 AudioClip riêng: shootSFX (tiếng bắn), reloadSFX (tiếng lên đạn).
///   - Có 2 cách cấp AudioClip:
///       [Cách 1 - ƯU TIÊN] Gán ngay trong WeaponSO (ScriptableObject). Vì mỗi
///         loại súng là 1 WeaponSO, đây là nơi gọn nhất để "mỗi súng tự quản lý
///         2 clip riêng". GunAudio tự đọc từ WeaponController.WeaponStats.
///       [Cách 2 - Dự phòng] Gán trực tiếp 2 ô shootSFX / reloadSFX trên component
///         GunAudio (nếu súng chưa có WeaponSO, hoặc muốn ghi đè).
///   - Script TỰ bắt sự kiện của WeaponController (OnFire, OnReloadStart) nên
///     bạn KHÔNG cần viết thêm 1 dòng code nào để nó kêu.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR:
///   1. Chọn GameObject súng (object có chứa script WeaponController).
///   2. Add Component > tìm "GunAudio".
///   3. Kéo WeaponController (nếu là con của prefab, để trống cũng được -
///      script tự tìm trong cùng object hoặc cha con).
///   4. Nếu dùng WeaponSO: mở file Weapon.asset đó, kéo shootSFX/reloadSFX vào.
///   5. Nếu không dùng SO: kéo 2 clip vào ô "Override" trên GunAudio.
///   6. Tinh chỉnh Volume / Min Pitch / Max Pitch cho tiếng súng varied tự nhiên.
///
/// ▐▌ NẾU VÌ LÝ DO NÀO ĐÓ BẠN MUỐN TỰ GỌI BẰNG TAY:
///     gunAudio.PlayShootSFX();   // gọi khi súng bắn
///     gunAudio.PlayReloadSFX();  // gọi khi súng lên đạn
/// ============================================================================
public class GunAudio : MonoBehaviour
{
    [Header("=== Tham chiếu (để trống tự tìm) ===")]
    [Tooltip("WeaponController của súng. Để trống, script tự tìm trên object này.")]
    [SerializeField] private WeaponController weaponController;

    [Header("=== AudioClip (Override - ghi đè lên WeaponSO) ===")]
    [Tooltip("Tiếng bắn. Nếu để trống, dùng shootSFX trong WeaponSO.")]
    [SerializeField] private AudioClip shootSFX;

    [Tooltip("Tiếng lên đạn. Nếu để trống, dùng reloadSFX trong WeaponSO.")]
    [SerializeField] private AudioClip reloadSFX;

    [Header("=== Cài đặt phát ===")]
    [Tooltip("Độ lớn tiếng súng (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Tooltip("Pitch tối thiểu. Mỗi phát bắn pitch ngẫu nhiên trong khoảng này để tiếng nghe đỡ 'máy móc'.")]
    [SerializeField] private float minPitch = 0.95f;

    [Tooltip("Pitch tối đa.")]
    [SerializeField] private float maxPitch = 1.05f;

    [Tooltip("Có phát thêm tiếng lên đạn 1 lần ở sau khi reload XONG không (tùy chọn).")]
    [SerializeField] private bool playReloadEndClick = false;

    // -----------------------------------------------------------------------
    //  ĐĂNG KÝ / HỦY SỰ KIỆN
    // -----------------------------------------------------------------------

    private void OnEnable()
    {
        ResolveWeaponController();

        if (weaponController == null)
            return;

        // Bắt sự kiện chính của WeaponController: bắn 1 phát, bắt đầu reload.
        weaponController.OnFire += HandleFire;
        weaponController.OnReloadStart += HandleReloadStart;

        if (playReloadEndClick)
            weaponController.OnReloadEnd += HandleReloadEnd;
    }

    private void OnDisable()
    {
        // Luôn hủy đăng ký khi tắt để không giữ tham chiếu -> tránh leak.
        if (weaponController == null)
            return;

        weaponController.OnFire -= HandleFire;
        weaponController.OnReloadStart -= HandleReloadStart;
        weaponController.OnReloadEnd -= HandleReloadEnd;
    }

    /// <summary>
    /// Tự tìm WeaponController trên object này (hoặc cha/con).
    /// Ưu tiên để tránh quên kéo tay.
    /// </summary>
    private void ResolveWeaponController()
    {
        if (weaponController != null)
            return;

        weaponController = GetComponent<WeaponController>();
        if (weaponController == null)
            weaponController = GetComponentInParent<WeaponController>();
        if (weaponController == null)
            weaponController = GetComponentInChildren<WeaponController>();
    }

    // -----------------------------------------------------------------------
    //  HÀM KÍCH HOẠT ÂM THANH (công khai - gọi được từ nơi khác)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Kích hoạt TIẾNG BẮN. Tự chọn clip: ghi đè cục bộ > WeaponSO.
    /// Pitch được ngẫu nhiên hóa theo minPitch/maxPitch.
    /// </summary>
    public void PlayShootSFX()
    {
        AudioClip clip = ResolveShootClip();
        if (clip == null)
            return;

        AudioManager.Instance?.PlaySFX(clip, volume, 1f, PitchRandomRange());
    }

    /// <summary>
    /// Kích hoạt TIẾNG LÊN ĐẠN. Tự chọn clip: ghi đè cục bộ > WeaponSO.
    /// </summary>
    public void PlayReloadSFX()
    {
        AudioClip clip = ResolveReloadClip();
        if (clip == null)
            return;

        AudioManager.Instance?.PlaySFX(clip, volume, 1f, PitchRandomRange());
    }

    // -----------------------------------------------------------------------
    //  XỬ LÝ SỰ KIỆN TỪ WEAPONCONTROLLER
    // -----------------------------------------------------------------------

    /// <summary>Sự kiện WeaponController vừa bắn -> phát tiếng bắn.</summary>
    private void HandleFire()
    {
        PlayShootSFX();
    }

    /// <summary>Sự kiện WeaponController bắt đầu reload -> phát tiếng lên đạn.</summary>
    private void HandleReloadStart()
    {
        PlayReloadSFX();
    }

    /// <summary>Sự kiện WeaponController reload xong -> phát thêm tiếng 'cạch' (tùy chọn).</summary>
    private void HandleReloadEnd()
    {
        PlayReloadSFX();
    }

    // -----------------------------------------------------------------------
    //  CHỌN CLIP & CÀI ĐẶT PITCH
    // -----------------------------------------------------------------------

    /// <summary>
    /// Chọn tiếng bắn: ưu tiên clip gán trên GunAudio; không có thì lấy từ WeaponSO.
    /// </summary>
    private AudioClip ResolveShootClip()
    {
        if (shootSFX != null)
            return shootSFX;

        if (weaponController != null && weaponController.WeaponStats != null)
            return weaponController.WeaponStats.ShootSFX;

        return null;
    }

    /// <summary>
    /// Chọn tiếng lên đạn: ưu tiên clip gán trên GunAudio; không có thì lấy từ WeaponSO.
    /// </summary>
    private AudioClip ResolveReloadClip()
    {
        if (reloadSFX != null)
            return reloadSFX;

        if (weaponController != null && weaponController.WeaponStats != null)
            return weaponController.WeaponStats.ReloadSFX;

        return null;
    }

    /// <summary>
    /// Trả về mức ngẫu nhiên hóa pitch (±) dựa trên minPitch ~ maxPitch.
    /// VD min=0.95, max=1.05 => trả về phạm vi ±0.05 quanh pitch gốc 1.
    /// </summary>
    private float PitchRandomRange()
    {
        float center = (minPitch + maxPitch) * 0.5f;
        float half = Mathf.Max(0f, (maxPitch - minPitch) * 0.5f);
        return Mathf.Max(0f, half / Mathf.Max(0.01f, center));
    }
}