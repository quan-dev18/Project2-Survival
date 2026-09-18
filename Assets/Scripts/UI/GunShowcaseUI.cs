using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pop-up hiển thị chi tiết thông số của một Vũ khí (WeaponSO) dưới dạng thanh Image Fill.
/// <para>Các Image phải设为 Image Type = Filled, Fill Method = Horizontal.</para>
/// </summary>
public class GunShowcaseUI : MonoBehaviour
{
    // ─────────────────────── Visual ───────────────────────
    [Header("Visual")]
    [Tooltip("Ảnh đại diện của súng (icon).")]
    [SerializeField] private Image gunIcon;
    [Tooltip("Tên súng.")]
    [SerializeField] private TextMeshProUGUI gunNameText;

    [Header("Panel đi kèm")]
    [Tooltip("Panel bật/tắt cùng pop-up này (ví dụ: panel chọn Skin). Tự tắt khi mở màn.")]
    [SerializeField] private GameObject skinPanel;

    [Header("Auto Show")]
    [Tooltip("(Tuỳ chọn) Manager chọn súng. Mỗi lần panel này được bật sẽ tự hiện showcase súng đang chọn + panel Skin.")]
    [SerializeField] private WeaponSelectManager weaponSelect;

    // ─────────────── Thanh Parameter Chỉ Số ──────────────
    [Header("Thanh Chỉ Số (Image Fill Amount)")]
    [Tooltip("Thanh sát thương.")]
    [SerializeField] private Image atkBar;
    [Tooltip("Thanh tốc độ bắn.")]
    [SerializeField] private Image fireRateBar;
    [Tooltip("Thanh thời gian nạp đạn (đảo ngược: nhanh = đầy hơn).")]
    [SerializeField] private Image reloadTimeBar;
    [Tooltip("Thanh băng đạn.")]
    [SerializeField] private Image magazineSizeBar;
    [Tooltip("Thanh số đạn bắn ra mỗi lần (bulletCount).")]
    [SerializeField] private Image bulletCountBar;

    // ──────────── Giới Hạn Tối Đa (Max Values) ──────────
    [Header("Giới Hạn Tối Đa (100% = mốc này)")]
    [Tooltip("Mốc ATK tối đa để tính % thanh sát thương.")]
    [SerializeField] private float maxATK = 50f;
    [Tooltip("Mốc FireRate tối đa (đạn/giây).")]
    [SerializeField] private float maxFireRate = 20f;
    [Tooltip("Mốc ReloadTime tối đa (giây). Càng thấp = thanh càng đầy.")]
    [SerializeField] private float maxReloadTime = 5f;
    [Tooltip("Mốc MagazineSize tối đa.")]
    [SerializeField] private float maxMagazineSize = 30f;
    [Tooltip("Mốc BulletCount tối đa.")]
    [SerializeField] private float maxBulletCount = 10f;

    // ──────────────── Nội bộ ─────────────────────────────
    private Tween _activeTween;
    private Sequence _barSequence;
    private Material _gunIconMatInstance;

    // ──────────────────── Unity Callbacks ─────────────────
    private void Awake()
    {
        ResetAllBars();
        if (skinPanel != null) skinPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // Khi mở màn chọn súng lần đầu (hoặc mở lại) mà đã có súng được chọn/sẵn sàng
        // thì tự hiện showcase của súng đang chọn + skin đang trang bị (kèm panel Skin).
        if (weaponSelect == null) return;
        WeaponSO weapon = weaponSelect.CurrentWeapon;
        if (weapon == null) return;
        Show(weapon, weaponSelect.IsWeaponUnlocked(weapon), weaponSelect.GetEquippedDisplaySprite(weapon));
    }

    private void OnDisable()
    {
        _activeTween?.Kill();
        _barSequence?.Kill();
        transform.DOKill();
        transform.localScale = Vector3.zero;
        if (skinPanel != null) skinPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        _activeTween?.Kill();
        _barSequence?.Kill();
        if (_gunIconMatInstance != null) Destroy(_gunIconMatInstance);
    }

    // ──────────────────── Public API ──────────────────────

    /// <summary>Gán manager chọn súng để mỗi lần panel mở đều tự hiện súng/skin đang chọn.</summary>
    public void SetWeaponSelect(WeaponSelectManager manager) => weaponSelect = manager;

    public void Show(WeaponSO weaponData, bool isUnlocked)
    {
        Show(weaponData, isUnlocked, null);
    }

    /// <summary>
    /// Hiển thị pop-up với tuỳ chọn ghi đè icon (dùng khi đang xem trước 1 skin:
    /// truyền <c>skin.weaponSprite</c> để ảnh súng đổi theo skin).
    /// </summary>
    public void Show(WeaponSO weaponData, bool isUnlocked, Sprite iconOverride)
    {
        _activeTween?.Kill();
        _barSequence?.Kill();
        transform.DOKill();

        float[] targets = SetupData(weaponData, isUnlocked, iconOverride);

        gameObject.SetActive(true);
        if (skinPanel != null) skinPanel.SetActive(true);

        transform.localScale = Vector3.zero;
        _activeTween = transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        PlayBarFillAnimation(targets, isUnlocked);
    }

    /// <summary>
    /// Chỉ đổi ảnh súng của showcase đang mở (không chạy lại animation / không đổi chỉ số).
    /// Dùng khi người chơi chọn skin khác để cập nhật ảnh ngay lập tức.
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (gunIcon != null && icon != null)
            gunIcon.sprite = icon;
    }

    /// <summary>
    /// Đổi màu outline của icon showcase (property <c>_OutlineColor</c> trong material của Image)
    /// theo Tier của skin đang chọn. Dùng material instance riêng để không ảnh hưởng material gốc.
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        if (gunIcon == null || gunIcon.material == null) return;

        if (_gunIconMatInstance == null)
        {
            _gunIconMatInstance = new Material(gunIcon.material);
            gunIcon.material = _gunIconMatInstance;
        }

        if (_gunIconMatInstance.HasProperty("_OutlineColor"))
            _gunIconMatInstance.SetColor("_OutlineColor", color);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        _activeTween?.Kill();
        _barSequence?.Kill();
        transform.DOKill();

        _activeTween = transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    // ──────────────────── Logic ───────────────────────────

    /// <summary>
    /// Tính toán tỷ lệ % hiển thị cho từng thanh bar từ dữ liệu vũ khí.
    /// Trả về mảng float[5]: [ATK, FireRate, ReloadTime, MagazineSize, BulletCount].
    /// </summary>
    private float[] SetupData(WeaponSO weaponData, bool isUnlocked, Sprite iconOverride)
    {
        float[] targets = new float[5];

        if (weaponData == null)
        {
            if (gunNameText != null) gunNameText.text = string.Empty;
            return targets;
        }

        if (gunIcon != null)
            gunIcon.sprite = iconOverride != null ? iconOverride : weaponData.WeaponIcon;
        if (gunNameText != null)
            gunNameText.text = weaponData.WeaponName;

        if (!isUnlocked)
            return targets;

        // ATK: đọc từ BulletSO (Damage).
        float atk = weaponData.BulletSO != null ? weaponData.BulletSO.Damage : 0f;
        targets[0] = Mathf.Clamp01(atk / maxATK);

        // FireRate:越高越好.
        targets[1] = Mathf.Clamp01(weaponData.FireRate / maxFireRate);

        // ReloadTime: ĐẢO NGƯỢC → ngắn hơn = đầy hơn.
        float reloadNormalized = Mathf.Clamp01(weaponData.ReloadTime / maxReloadTime);
        targets[2] = 1f - reloadNormalized;

        // MagazineSize:越大越好.
        targets[3] = Mathf.Clamp01(weaponData.MagazineSize / (float)maxMagazineSize);

        // BulletCount:越大越好.
        targets[4] = Mathf.Clamp01(weaponData.BulletCount / maxBulletCount);

        return targets;
    }

    private void ResetAllBars()
    {
        SetFillAmount(atkBar, 0f);
        SetFillAmount(fireRateBar, 0f);
        SetFillAmount(reloadTimeBar, 0f);
        SetFillAmount(magazineSizeBar, 0f);
        SetFillAmount(bulletCountBar, 0f);
    }

    private void PlayBarFillAnimation(float[] targets, bool isUnlocked)
    {
        _barSequence?.Kill();
        _barSequence = DOTween.Sequence();

        _barSequence.AppendCallback(ResetAllBars);

        if (!isUnlocked)
            return;

        // Mỗi bar lấp đầy lần lượt theo thứ tự (không song song).
        // ATK → FireRate → ReloadTime → MagazineSize → BulletCount.
        AppendFillTween(_barSequence, atkBar, targets[0]);
        AppendFillTween(_barSequence, fireRateBar, targets[1]);
        AppendFillTween(_barSequence, reloadTimeBar, targets[2]);
        AppendFillTween(_barSequence, magazineSizeBar, targets[3]);
        AppendFillTween(_barSequence, bulletCountBar, targets[4]);

        _barSequence.SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private Tween CreateFillTween(Image bar, float targetValue)
    {
        if (bar == null)
            return null;

        bar.fillAmount = 0f;

        return DOTween.To(
                () => bar.fillAmount,
                x => bar.fillAmount = x,
                targetValue,
                0.4f)
            .SetEase(Ease.OutCubic);
    }

    /// <summary>Tạo fill tween rồi Append vào Sequence (chạy lần lượt, bỏ qua nếu Image null).</summary>
    private void AppendFillTween(Sequence seq, Image bar, float targetValue)
    {
        Tween tween = CreateFillTween(bar, targetValue);
        if (tween != null)
            seq.Append(tween);
    }

    private void SetFillAmount(Image bar, float value)
    {
        if (bar != null)
            bar.fillAmount = value;
    }
}
