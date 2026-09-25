using UnityEngine;

/// <summary>
/// Điều khiển badge notify của ICON DẪN VÀO panel Daily Reward (nằm ngoài menu).
///
/// QUY TẮC ĐƠN GIẢN:
///   - Daily đang có ngày được nhận (Claimable) → BẬT notify.
///   - Daily đã nhận xong / chưa tới ngày (Claimed/Locked) → KHÔNG bật notify.
///
/// CÁCH HOẠT ĐỘNG:
///   - Không cần kéo tham chiếu manager nào cả: cứ mỗi chu kỳ (mặc định 0.5s)
///     tự hỏi `DailyRewardManager.IsClaimableNow()` — hàm này đọc THẲNG PlayerPrefs,
///     nên luôn thấy dữ liệu mới nhất (kể cả sau khi nhận quà / reset).
///   - Kiểm tra đơn giản như vậy sẽ KHÔNG BAO GIỜ bị kẹt notify do dữ liệu cũ.
///
/// CÁC BƯỚC GÁN TRÊN EDITOR:
///   1) Chọn icon/bút ngoài menu.
///   2) Add Component → DailyRewardMenuNotify.
///   3) Kéo badge notify của icon vào "Obj Notify".
///   4) (Tùy chọn) Chỉnh "Refresh Interval" — chu kỳ kiểm tra lại.
/// </summary>
public class DailyRewardMenuNotify : MonoBehaviour
{
    [Header("Tham chiếu UI")]
    [Tooltip("Badge notify của icon. Nên để ẨN sẵn, hệ thống tự bật khi có quà để nhận.")]
    [SerializeField] private GameObject objNotify;

    [Header("Tùy chỉnh")]
    [Tooltip("Chu kỳ kiểm tra lại (giây). Nhỏ hơn = notify bật/tắt nhanh hơn.")]
    [SerializeField] private float refreshInterval = 0.5f;

    private float nextRefreshTime;

    private void OnEnable()
    {
        RefreshNotify();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + refreshInterval;
        RefreshNotify();
    }

    /// <summary>
    /// Bật notify nếu hệ thống đang có ngày được nhận, ngược lại tắt.
    /// Đọc trực tiếp PlayerPrefs qua hàm static nên trạng thái luôn mới nhất.
    /// </summary>
    private void RefreshNotify()
    {
        if (objNotify == null) return;
        objNotify.SetActive(DailyRewardManager.IsClaimableNow());
    }
}