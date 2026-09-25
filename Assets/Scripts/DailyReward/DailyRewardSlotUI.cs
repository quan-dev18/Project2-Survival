using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
///        - objNotify   : Badge thông báo (✓/chấm đỏ...). ĐỂ ẨN sẵn — manager tự BẬT khi ô đang là ngày
///                        CÓ THỂ nhận quà (Claimable), TẮT ở mọi ô khác (Locked/Claimed).
///                        (Trước đây tên là objCheckmark — hệ thống vẫn nhận theo tham chiếu cũ
///                        nhờ FormerlySerializedAs, gán lại không bị mất.)
///        - objCheckmark: Dấu ✓ đã nhận quà. ĐỂ ẨN sẵn — manager tự BẬT khi ô ĐÃ nhận quà (Claimed),
///                        TẮT ở các ô khác (Locked/Claimable).
///        - objHighlight : GameObject viền/sáng highlight — ĐỂ ẨN sẵn (manager sẽ tự bật khi ô có thể nhận quà).
///        - canvasGroup (Tùy chọn): KHÔNG BẮT BUỘC. Hệ thống làm tối bằng cách đổi màu sang tối hơn,
///          nên bạn KHÔNG cần CanvasGroup. Nếu ô của bạn đang có CanvasGroup với alpha < 1, nó sẽ khiến
///          ô bị TRONG SUỐT — hãy chỉnh alpha = 1 (hoặc để trống ô này).
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
    [Tooltip("Badge thông báo (✓/dấu chấm...). Chỉ BẬT ở ngày ĐANG CÓ THỂ nhận quà (Claimable). Ô Locked/Claimed không hiện. Nên để ẨN sẵn trong Editor.")]
    [SerializeField, FormerlySerializedAs("objCheckmark")] private GameObject objNotify;
    [Tooltip("Dấu ✓ ĐÃ NHẬN quà. Chỉ BẬT ở ngày đã Claimed, ẨN ở các ô khác. (So với Obj Notify: cái này trạng thái ĐÃ nhận.)")]
    [SerializeField] private GameObject objCheckmark;
    [Tooltip("Viền/sáng highlight. Nên để ẨN sẵn trong Editor, manager sẽ bật khi ô có thể nhận quà.")]
    [SerializeField] private GameObject objHighlight;
    [Tooltip("(Tùy chọn) Để trống hoặc alpha = 1. Hệ thống không dùng alpha để làm mờ — dùng dark-tint thay thế. Nếu alpha < 1 thì ô sẽ bị trong suốt.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Hiệu ứng lắc (ô đang Claimable)")]
    [Tooltip("Thời gian 1 nhịp lắc (giây). Ô sẽ lắc liên tục khi là ngày có thể nhận quà.")]
    [SerializeField] private float shakeDuration = 1.2f;
    [Tooltip("Biên độ lắc góc (độ). Càng lớn càng lắc mạnh.")]
    [SerializeField] private float shakeStrength = 3f;

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

    // ── Bộ nhớ cache màu gốc để dark-tint không bị drift qua mỗi lần gọi ──
    private List<Graphic> cachedGraphics;
    private Color[] cachedBaseColors;

    // ── Tween lắc hiện tại của ô (chỉ chạy khi Claimable) ──
    private Tween claimableShakeTween;

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
///    Cập nhật hiển thị của ô theo trạng thái:
///   - Locked    : làm TỐI TOÀN BỘ toàn ô (dark-tint, KHÔNG trong suốt) + tắt Button + tắt Highlight
///                 + KHÔNG có notify + KHÔNG dấu ✓ (ô chưa tới ngày nhận).
///   - Claimable : ô sáng bình thường + bật Highlight + bật Button + Notify SÁNG (gợi ý nhấn).
///   - Claimed   : chỉ GIẢM ALPHA của ICON quà (khung UI giữ nguyên) + BẬT dấu ✓ + tắt Button
///                 + tắt Highlight + tắt Notify.
    /// </summary>
    public void UpdateState(RewardState state)
    {
        // ── Button: chỉ bấm được khi Claimable ──
        if (btnClaim != null)
            btnClaim.interactable = state == RewardState.Claimable;

        // ── Highlight: chỉ sáng khi Claimable ──
        if (objHighlight != null)
            objHighlight.SetActive(state == RewardState.Claimable);

        // ── Notify: chỉ BẬT ở ô đang CÓ THỂ nhận quà (Claimable). Ô chưa mở (Locked)
        //    và ô đã nhận (Claimed) đều KHÔNG hiện notify ──
        if (objNotify != null)
            objNotify.SetActive(state == RewardState.Claimable);

        // ── Dấu ✓: chỉ BẬT ở ô ĐÃ nhận quà (Claimed) ──
        if (objCheckmark != null)
            objCheckmark.SetActive(state == RewardState.Claimed);

        // ── Độ sáng UI theo từng trạng thái ──
        switch (state)
        {
            case RewardState.Locked:
                // Chưa tới ngày → làm TỐI (nhưng vẫn rõ nét, không mờ trong suốt).
                SetSlotDarkened(0.4f);
                break;

            case RewardState.Claimable:
                // Đang được nhận → sáng bình thường, giữ nguyên mọi thứ.
                SetSlotDarkened(0f);
                break;

            case RewardState.Claimed:
                // Đã nhận → khung giữ nguyên, chỉ GIẢM ALPHA của ICON quà (mờ trong, không tối màu).
                SetSlotDarkened(0f);
                SetIconAlpha(0.4f);
                break;
        }

        // ── Lắc nhẹ (DOTween) chỉ ở ô đang Claimable; ô khác dừng lắc và về vị trí gốc ──
        if (state == RewardState.Claimable)
            StartClaimableShake();
        else
            StopClaimableShake();
    }

    private void OnDisable()
    {
        // Dừng lắc khi ô bị ẩn (đóng panel) để không chạy tween thừa.
        StopClaimableShake();
    }

    /// <summary>
    /// Bật hiệu ứng lắc "lắc lắc nhẹ" liên tục cho ô (xoay qua lại quanh trục Z).
    /// Chỉ chạy ở trạng thái Claimable để người chơi để ý ngày đang được nhận.
    /// </summary>
    private void StartClaimableShake()
    {
        // Đang lắc rồi thì không tạo tween mới (tránh chồng tween).
        if (claimableShakeTween != null) return;

        claimableShakeTween = transform
            .DOShakeRotation(shakeDuration, shakeStrength, 12, 90, false) // lắc góc nhỏ quanh Z.
            .SetLoops(-1, LoopType.Restart);                              // lặp vô hạn: 1 vòng = lắc qua rồi về.
    }

    /// <summary>
    /// Dừng lắc và đưa ô về góc xoay ban đầu.
    /// </summary>
    private void StopClaimableShake()
    {
        if (claimableShakeTween == null) return;

        claimableShakeTween.Kill();
        claimableShakeTween = null;
        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    /// <summary>
    /// Lưu một lần danh sách toàn bộ Graphic (Image + Text + TMP) của ô kèm MÀU GỐC của chúng.
    /// Dùng để dark-tint nhiều lần mà không bị lũy tiến sai màu.
    /// </summary>
    private void CacheSlotGraphics()
    {
        if (cachedGraphics != null) return;

        cachedGraphics = new List<Graphic>(GetComponentsInChildren<Graphic>(true));
        cachedBaseColors = new Color[cachedGraphics.Count];
        for (int i = 0; i < cachedGraphics.Count; i++)
            cachedBaseColors[i] = cachedGraphics[i].color;
    }

    /// <summary>
    /// Làm TỐI toàn bộ ô (icon + khung + chữ) bằng cách pha màu về ĐEN theo <paramref name="darkAmount"/>.
    /// darkAmount = 0 → giữ nguyên màu gốc; càng lớn (max 1) càng tối sấm.
    /// KHÔNG đụng tới alpha → ô vẫn rõ nét, KHÔNG bị trong suốt.
    /// </summary>
    private void SetSlotDarkened(float darkAmount)
    {
        CacheSlotGraphics();

        // Nếu có CanvasGroup thì giữ alpha = 1 để không làm ô trong suốt.
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        for (int i = 0; i < cachedGraphics.Count; i++)
        {
            if (cachedGraphics[i] == null) continue;
            cachedGraphics[i].color = Color.Lerp(cachedBaseColors[i], Color.black, darkAmount);
        }
    }

    /// <summary>
    /// Chỉ GIẢM ALPHA của riêng ICON quà (imgIcon), giữ nguyên màu sắc và khung UI.
    /// <paramref name="alpha"/> = 1 → giữ nguyên; càng nhỏ càng mờ (0 = trong suốt).
    /// </summary>
    private void SetIconAlpha(float alpha)
    {
        if (imgIcon == null) return;
        CacheSlotGraphics();

        // Lấy màu gốc của icon từ cache để tránh drift (giữ nguyên RGB, chỉ đổi alpha).
        Color baseColor = imgIcon.color;
        int index = cachedGraphics.IndexOf(imgIcon);
        if (index >= 0)
            baseColor = cachedBaseColors[index];

        baseColor.a = alpha;
        imgIcon.color = baseColor;
    }
}