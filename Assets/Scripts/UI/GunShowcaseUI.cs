using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pop-up hiển thị chi tiết thông số của một Vũ khí (WeaponSO).
/// <para>Gắn script này lên GameObject `GunShowcase` (panel Pop-up).</para>
/// <para>Hiệu ứng xuất hiện dùng DOTween (nảy scale).</para>
/// <para>Pop-up nằm CÙNG NHÁNH với panel chọn vũ khí: khi tắt panel chọn súng
/// thì nó sẽ tự ẩn theo, nên KHÔNG cần nút Close.</para>
///
/// CÁCH GỌI TỪ DANH SÁCH VŨ KHÍ:
/// <code>
/// // 1. Trong WeaponSelectManager (hoặc slot), khai báo & gán qua Inspector:
/// //    [SerializeField] private GunShowcaseUI gunShowcase;
///
/// // 2. Khi chọn 1 súng trong danh sách (truyền thêm trạng thái mở khóa):
/// private void OnSelectWeapon(WeaponSO weapon, bool unlocked)
/// {
///     gunShowcase.Show(weapon, unlocked);  // mở pop-up + load thông số
///     // Súng LOCKED sẽ hiển thị các stat dưới dạng "?"
/// }
/// </code>
/// </summary>
public class GunShowcaseUI : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Ảnh đại diện của súng (icon).")]
    [SerializeField] private Image gunIcon;
    [Tooltip("Tên súng.")]
    [SerializeField] private TextMeshProUGUI gunNameText;

    [Header("Cột chỉ số P1 (Bên trái)")]
    [Tooltip("Sát thương (ATK). Lấy trực tiếp từ BulletSO bên trong WeaponSO.")]
    [SerializeField] private TextMeshProUGUI atkText;
    [Tooltip("Tốc độ bắn (đạn/giây).")]
    [SerializeField] private TextMeshProUGUI fireRateText;
    [Tooltip("Tầm bắn.")]
    [SerializeField] private TextMeshProUGUI fireRangeText;
    [Tooltip("Thời gian nạp đạn (giây).")]
    [SerializeField] private TextMeshProUGUI reloadTimeText;

    [Header("Cột chỉ số P2 (Bên phải)")]
    [Tooltip("Sức chứa băng đạn.")]
    [SerializeField] private TextMeshProUGUI magazineSizeText;
    [Tooltip("Số đạn bắn ra mỗi lần (bulletCount).")]
    [SerializeField] private TextMeshProUGUI bulletCountText;
    [Tooltip("Độ phân tán đạn (spread).")]
    [SerializeField] private TextMeshProUGUI spreadText;

    /// <summary>Tween đang chạy để kịp kill khi ẩn/hiện lặp lại (tránh đè animation).</summary>
    private Tween _activeTween;

    private void OnDisable()
    {
        // Pop-up bị tắt theo nhánh panel chọn súng -> hủy tween & reset scale
        // để lần mở lại không hiện nhảy giữa chừng animation cũ.
        _activeTween?.Kill();
        transform.DOKill();
        transform.localScale = Vector3.zero;
    }

    private void OnDestroy()
    {
        // Kill tween còn sót để tránh callback gọi trên object đã hủy.
        _activeTween?.Kill();
    }

    /// <summary>
    /// Mở Pop-up và nạp toàn bộ thông số của vũ khí.
    /// </summary>
    /// <param name="weaponData">WeaponSO cần hiển thị.</param>
    /// <param name="isUnlocked">Súng đã mở khóa hay chưa. Nếu LOCKED, các stat sẽ hiện "?".</param>
    public void Show(WeaponSO weaponData, bool isUnlocked)
    {
        // Kill tween đang chạy trên transform để tránh lỗi đè animation.
        _activeTween?.Kill();
        transform.DOKill();

        // Nạp dữ liệu trước khi hiện để tránh nhấp nháy nội dung cũ.
        SetupData(weaponData, isUnlocked);

        gameObject.SetActive(true);

        // Reset scale về 0 rồi chạy animation nảy xuất hiện.
        transform.localScale = Vector3.zero;
        _activeTween = transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true); // Chạy cả khi game đang Pause
    }

    /// <summary>Đóng Pop-up với hiệu ứng thu nhỏ, sau khi xong mới ẩn hẳn.</summary>
    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        _activeTween?.Kill();
        transform.DOKill();

        _activeTween = transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// Nạp các chỉ số súng từ WeaponSO vào các text tương ứng.
    /// ATK được lấy trực tiếp từ BulletSO nằm bên trong WeaponSO (weaponData.BulletSO.Damage).
    /// Súng chưa mở khóa (locked) sẽ hiển thị toàn bộ stat dưới dạng "?".
    /// </summary>
    private void SetupData(WeaponSO weaponData, bool isUnlocked)
    {
        if (weaponData == null)
        {
            gunNameText.text = string.Empty;
            return;
        }

        // --- Visual chính ---
        if (gunIcon != null)
            gunIcon.sprite = weaponData.WeaponIcon;
        if (gunNameText != null)
            gunNameText.text = weaponData.WeaponName;

        // --- Súng LOCKED: che toàn bộ chỉ số bằng "?" ---
        if (!isUnlocked)
        {
            SetText(atkText, "?");
            SetText(fireRateText, "?");
            SetText(fireRangeText, "?");
            SetText(reloadTimeText, "?");
            SetText(magazineSizeText, "?");
            SetText(bulletCountText, "?");
            SetText(spreadText, "?");
            return;
        }

        // --- Cột P1 (Bên trái) ---
        // ATK đọc từ BulletSO gắn trong WeaponSO (đã được nâng cấp damage bonus?
        // Ở đây chỉ hiển thị giá trị gốc từ asset).
        float atk = weaponData.BulletSO != null ? weaponData.BulletSO.Damage : 0f;
        SetText(atkText, atk.ToString("F1"));

        SetText(fireRateText, weaponData.FireRate.ToString("F1"));
        SetText(fireRangeText, weaponData.FireRange.ToString("F1"));
        SetText(reloadTimeText, weaponData.ReloadTime.ToString("F1"));

        // --- Cột P2 (Bên phải) ---
        SetText(magazineSizeText, weaponData.MagazineSize.ToString());
        SetText(bulletCountText, weaponData.BulletCount.ToString());
        SetText(spreadText, weaponData.Spread.ToString());
    }

    /// <summary>Gán text an toàn (bỏ qua nếu field null).</summary>
    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }
}