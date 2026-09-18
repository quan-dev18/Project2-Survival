using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển 1 ô quà (D1 → D7) trong panel Daily Reward.
/// Đây là lớp UI Controller thuần túy: chỉ lo HIỂN THỊ dữ liệu + trạng thái của 1 ô,
/// KHÔNG chứa bất kỳ logic game nào (logic thời gian/thưởng nằm ở DailyRewardManager).
///
/// CÁC BƯỚC GÁN TRÊN EDITOR (cho TỪNG ô D1 → D7):
///   1) Chọn GameObject D1 trong Hierarchy → Add Component → DailyRewardSlotUI.
///   2) Kéo thả các thành phần con của ô vào component tương ứng:
///        - txtDay      : TextMeshPro hiển thị "Ngày X".
///        - txtAmount   : TextMeshPro hiển thị số lượng quà (vd: 100).
///        - imgIcon     : Image hiển thị icon quà.
///        - btnClaim    : Button của ô (nút chính để bấm nhận quà).
///        - objCheckmark: GameObject dấu tích "✓" — ĐỂ ẨN sẵn (manager sẽ tự bật khi ô đã nhận quà).
///        - objHighlight : GameObject viền/sáng highlight — ĐỂ ẨN sẵn (manager sẽ tự bật khi ô có thể nhận quà).
///        - canvasGroup (Tùy chọn): kéo CanvasGroup của ô vào để làm mờ TOÀN Ô khi Locked.
///          Nếu bỏ trống, hệ thống chỉ làm mờ imgIcon.
///   3) Lặp lại cho 7 ô D1 → D7.
///   KHÔNG cần gán sự kiện hay gõ mã thủ công: DailyRewardManager sẽ gọi SetupSlot() tự động.
/// </summary>
public class DailyRewardSlotUI : MonoBehaviour
{
    [Header("Tham chiếu UI (kéo thả từ Hierarchy)")]
    [SerializeField] private TextMeshProUGUI txtDay;
    [SerializeField] private TextMeshProUGUI txtAmount;
    [SerializeField] private Image imgIcon;
    [SerializeField] private Button btnClaim;
    [Tooltip("Dấu tích ✓. Nên để ẨN sẵn trong Editor, manager sẽ bật khi ô đã nhận quà.")]
    [SerializeField] private GameObject objCheckmark;
    [Tooltip("Viền/sáng highlight. Nên để ẨN sẵn trong Editor, manager sẽ bật khi ô có thể nhận quà.")]
    [SerializeField] private GameObject objHighlight;
    [Tooltip("(Tùy chọn) Kéo CanvasGroup của ô vào để làm mờ cả ô khi Locked. Nếu bỏ trống thì chỉ làm mờ imgIcon.")]
    [SerializeField] private CanvasGroup canvasGroup;

    /// <summary>Ba trạng thái hiển thị của 1 ô quà.</summary>
    public enum RewardState
    {
        Locked,     // Chưa tới lượt nhận (hoặc đã nhận hôm nay, phải chờ hết 24h).
        Claimable,  // Đủ điều kiện → có thể bấm nhận ngay bây giờ.
        Claimed     // Đã nhận quà của ngày này.
    }

    private RewardItem currentData;
    private Action<int> onClaimCallback;

    /// <summary>Dữ liệu quà đang giữ trong ô này.</summary>
    public RewardItem CurrentData => currentData;

    /// <summary>
    /// Gán dữ liệu quà + gán sự kiện bấm nút cho ô.
    /// </summary>
    /// <param name="data">Dữ liệu RewardItem của ngày này.</param>
    /// <param name="onClaimCallback">Hàm được gọi khi bấm nút, kèm dayIndex (1..7).</param>
    public void SetupSlot(RewardItem data, Action<int> onClaimCallback)
    {
        currentData = data;
        this.onClaimCallback = onClaimCallback;

        // ── Gán dữ liệu hiển thị ──
        if (txtDay != null)
            txtDay.text = $"Ngày {data.dayIndex}";

        if (txtAmount != null)
            txtAmount.text = data.amount.ToString();

        if (imgIcon != null)
            imgIcon.sprite = data.rewardIcon;

        // ── Gán sự kiện click cho Button, truyền dayIndex ra ngoài ──
        if (btnClaim != null)
        {
            btnClaim.onClick.RemoveAllListeners(); // Xóa sự kiện cũ để tránh bấm nhiều lần.
            btnClaim.onClick.AddListener(() => onClaimCallback?.Invoke(data.dayIndex));
        }
    }

    /// <summary>
    /// Cập nhật hiển thị của ô theo trạng thái:
    ///   - Locked    : làm mờ + tắt Button.
    ///   - Claimable : bật highlight + bật Button.
    ///   - Claimed   : tắt highlight + hiện dấu ✓ + tắt Button.
    /// </summary>
    public void UpdateState(RewardState state)
    {
        // ── Button: chỉ bấm được khi Claimable ──
        if (btnClaim != null)
            btnClaim.interactable = state == RewardState.Claimable;

        // ── Highlight: chỉ sáng khi Claimable ──
        if (objHighlight != null)
            objHighlight.SetActive(state == RewardState.Claimable);

        // ── Dấu ✓: chỉ hiện khi đã Claimed ──
        if (objCheckmark != null)
            objCheckmark.SetActive(state == RewardState.Claimed);

        // ── Làm mờ toàn ô khi Locked ──
        SetDimmed(state == RewardState.Locked);
    }

    /// <summary>
    /// Làm mờ / hồi lại độ sáng cho ô.
    /// Dùng CanvasGroup nếu có (làm mờ cả ô), ngược lại làm mờ imgIcon.
    /// </summary>
    private void SetDimmed(bool dimmed)
    {
        float targetAlpha = dimmed ? 0.45f : 1f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else if (imgIcon != null)
        {
            Color c = imgIcon.color;
            c.a = targetAlpha;
            imgIcon.color = c;
        }
    }
}