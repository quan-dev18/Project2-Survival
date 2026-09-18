using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý LOGIC hệ thống Daily Reward (điểm danh 7 ngày).
/// Lớp này tách biệt hoàn toàn Game Logic khỏi UI: nó chỉ "lái" các ô DailyRewardSlotUI
/// đã được kéo thả sẵn từ Hierarchy (KHÔNG dùng Instantiate).
///
/// Dữ liệu lưu vào PlayerPrefs:
///   - "DailyReward_LastClaimTime_Ticks": thời điểm nhận quà gần nhất (DateTime.UtcNow.Ticks).
///   - "DailyReward_StreakCount"        : số ngày liên tiếp đã điểm danh (0..7).
///
/// LOGIC THỜI GIAN (theo DateTime.UtcNow):
///   - Chưa từng nhận quà                       → nhận được ngay NGÀY 1.
///   - Cách lần nhận gần nhất < 24h             → phải chờ (chưa tới lượt).
///   - Cách từ 24h cho tới missResetHours (48h) → nhận NGÀY TIẾP THEO (ngày gọi quà hằng ngày).
///   - Cách quá missResetHours (48h) = BỎ LỠ 1 NGÀY → reset StreakCount về 0, quay lại NGÀY 1.
///   - Đã nhận hết NGÀY 7                        → sau đủ 24h quay VÒNG MỚI từ ngày 1.
///
/// Tóm tắt luật chơi:
///   * Mỗi ngày vào game → có 1 ngày để nhận (ngày tiếp theo).
///   * Đủ 7 ngày liên tiếp → reset về ngày 1 để nhận tiếp.
///   * Bỏ lỡ 1 ngày không nhận (quá 48h) → reset về ngày 1.
///
/// CÁC BƯỚC GÁN TRÊN EDITOR (sau khi đã gán DailyRewardSlotUI cho 7 ô D1 → D7):
///   1) Thêm component DailyRewardManager vào GameObject bất kỳ (Panel cha hoặc 1 object rỗng trong scene).
///   2) Kéo asset DailyRewardDataSO (đã tạo ở Create → Daily Reward → DailyRewardData)
///      vào ô "Reward Data".
///   3) Mở rộng ô "Reward Slots" và kéo 7 ô D1 → D7 từ Hierarchy vào theo đúng thứ tự:
///        - Phần tử [0] = D1, [1] = D2, ... [6] = D7.
///      (Dùng danh sách kéo thả này — hệ thống SẼ KHÔNG generate/Instantiate ô nào cả.)
///   4) Bấm Play: các ô sẽ tự hiển thị đúng trạng thái Locked / Claimable / Claimed.
///   5) Muốn thử lại từ đầu: click chuột phải vào component → "Reset Daily Reward (test)".
///
/// LƯU Ý:
///   - Quà loại Coin sẽ được cộng qua UserData.Instance.AddGold() (nếu singleton UserData có trong scene).
///   - Quà loại Item sẽ phát sự kiện OnItemReward — bạn ghi lại sự kiện này từ hệ thống
///     inventory/popup của game để xử lý cộng vật phẩm.
/// </summary>
public class DailyRewardManager : MonoBehaviour
{
    // ── Khóa lưu trữ PlayerPrefs (đặt cố định để tránh nhầm key) ──
    private const string KEY_LAST_CLAIM = "DailyReward_LastClaimTime_Ticks";
    private const string KEY_STREAK    = "DailyReward_StreakCount";
    private const int MaxRewardDays = 7; // Chu kỳ 7 ngày.

    [Header("Dữ liệu cấu hình 7 ngày quà")]
    [Tooltip("Kéo asset DailyRewardDataSO đã tạo: Create → Daily Reward → DailyRewardData.")]
    [SerializeField] private DailyRewardDataSO rewardData;

    [Header("Danh sách 7 ô quà (D1 → D7)")]
    [Tooltip("Kéo thả 7 GameObject D1 → D7 từ Hierarchy vào đây, đúng thứ tự D1 trước, D7 cuối.")]
    [SerializeField] private List<DailyRewardSlotUI> rewardSlots = new List<DailyRewardSlotUI>();

    [Header("Cài đặt luật chơi")]
    [Tooltip("Quá bao nhiêu giờ kể từ lần nhận cuối thì coi là BỎ LỠ 1 NGÀY → reset về ngày 1?\n- 48h (mặc định) = bỏ lỡ đúng 1 ngày thì reset.\n- PHẢI LỚN HƠN 24h (vì cần 24h để qua ngày mới).")]
    [Range(25f, 120f)]
    [SerializeField] private float missResetHours = 48f;

    /// <summary>
    /// Sự kiện phát ra khi nhận quà loại Item.
    /// Đăng ký lắng nghe từ hệ thống khác (inventory, popup nhận quà...) để cộng vật phẩm:
    ///   DailyRewardManager.OnItemReward += (item) => InventorySystem.Instance.AddItem(item.rewardName, item.amount);
    /// </summary>
    public event Action<RewardItem> OnItemReward;

    private bool isInitialized;
    private int streakCount;          // Số ngày liên tiếp đã nhận trong chu kỳ hiện tại (0..7).
    private DateTime lastClaimTime;   // Thời điểm nhận quà gần nhất (theo UtcNow).
    private bool hasClaimedOnce;      // Đã từng nhận quà lần nào chưa.

    /// <summary>Số streak hiện tại (0..7), dùng để hiển thị lên UI khác nếu cần.</summary>
    public int StreakCount => streakCount;

    /// <summary>
    /// Đang có ít nhất 1 ngày có thể nhận quà (Claimable) hay không.
    /// Dùng để bật/tắt notify ở ICON DẪN VÀO panel Daily Reward trên menu chính.
    /// </summary>
    public bool HasRewardToClaim => GetNextClaimableDayIndex() >= 0;

    /// <summary>
    /// Kiểm tra TRỰC TIẾP (đọc PlayerPrefs ngay lúc gọi) hiện có ngày được nhận quà hay không.
    /// KHÔNG phụ thuộc vào instance nào — luôn đọc dữ liệu mới nhất vừa được lưu.
    /// Dùng cho notify của icon ngoài menu để tránh kẹt notify khi có nhiều manager / sai tham chiếu.
    /// Luật đơn giản: chưa nhận lần nào HOẶC đã cách lần nhận >= 24h → có quà để nhận (true).
    /// </summary>
    public static bool IsClaimableNow()
    {
        string raw = PlayerPrefs.GetString(KEY_LAST_CLAIM, "");
        if (string.IsNullOrEmpty(raw))
            return true; // Chưa từng nhận quà → ngày 1 đang chờ nhận.

        if (!TryParseTime(raw, out DateTime lastClaim))
            return true; // Dữ liệu hỏng → coi như có quà để nhận.

        // Quá 48h sẽ tự reset streak và nhận lại → claimable.
        // Nên chỉ cần so 24h: cách lần nhận gần nhất >= 24h là được nhận.
        return (DateTime.UtcNow - lastClaim).TotalHours >= 24f;
    }

    /// <summary>
    /// Sự kiện phát ra mỗi khi trạng thái điểm danh thay đổi
    /// (nhận quà, mở panel, reset...) — để notify icon menu tự cập nhật.
    /// </summary>
    public event Action OnRewardStateChanged;

    // Đăng ký trong Awake để hệ thống khác có thể gọi trước khi scene chạy.
    private void Awake()
    {
        LoadProgress();
    }

    private void OnEnable()
    {
        // Nếu đã khởi tạo xong (lần mở panel thứ 2 trở đi) thì làm mới lại trạng thái.
        if (isInitialized) RefreshAllSlots();
    }

    private void Start()
    {
        InitializeSlots();
    }

    // ──────────────────── Đọc/Lưu PlayerPrefs ────────────────────

    /// <summary>Đọc dữ liệu streak + thời gian nhận quà gần nhất từ PlayerPrefs.</summary>
    private void LoadProgress()
    {
        streakCount = Mathf.Clamp(PlayerPrefs.GetInt(KEY_STREAK, 0), 0, MaxRewardDays);

        string raw = PlayerPrefs.GetString(KEY_LAST_CLAIM, "");
        if (string.IsNullOrEmpty(raw))
        {
            hasClaimedOnce = false;
            streakCount = 0;
            return;
        }

        hasClaimedOnce = true;

        // Hỗ trợ cả 2 định dạng: Ticks (long) hoặc ISO 8601 ("o").
        if (!TryParseTime(raw, out lastClaimTime))
        {
            // Dữ liệu bị hỏng → coi như chưa từng nhận quà.
            hasClaimedOnce = false;
            streakCount = 0;
            Debug.LogWarning("[DailyReward] Dữ liệu thời gian điểm danh bị hỏng, đã đặt lại về đầu.");
        }
    }

    private static bool TryParseTime(string raw, out DateTime result)
    {
        // Cách 1: lưu dạng Ticks (long).
        if (long.TryParse(raw, out long ticks))
        {
            try
            {
                result = new DateTime(ticks, DateTimeKind.Utc);
                return true;
            }
            catch (ArgumentException)
            {
                // rớt xuống cách 2
            }
        }

        // Cách 2: lưu dạng ISO 8601 (vd: tostring "o").
        return DateTime.TryParse(raw, out result);
    }

    /// <summary>Ghi streak + thời điểm nhận quà hiện tại xuống PlayerPrefs.</summary>
    private void SaveProgress()
    {
        PlayerPrefs.SetString(KEY_LAST_CLAIM, lastClaimTime.Ticks.ToString());
        PlayerPrefs.SetInt(KEY_STREAK, streakCount);
        PlayerPrefs.Save();
    }

    // ──────────────────── Khởi tạo các ô ────────────────────

    /// <summary>
    /// Duyệt qua rewardSlots theo đúng thứ tự kéo thả, gọi SetupSlot() cho từng ô
    /// với dữ liệu quà tương ứng (ngày i+1 = D(i+1)). Không tạo mới GameObject nào.
    /// </summary>
    private void InitializeSlots()
    {
        if (rewardSlots == null || rewardSlots.Count == 0)
        {
            Debug.LogWarning("[DailyReward] Chưa kéo ô D1 → D7 nào vào Reward Slots. Hãy gán trong Inspector.");
            return;
        }

        int count = Mathf.Min(rewardSlots.Count, MaxRewardDays);

        for (int i = 0; i < count; i++)
        {
            DailyRewardSlotUI slot = rewardSlots[i];
            if (slot == null) continue;

            int day = i + 1;
            RewardItem item = GetItemForDay(day);
            // Gán dữ liệu + sự kiện bấm (callback truyền dayIndex vào ClaimReward).
            slot.SetupSlot(item, ClaimReward);
        }

        if (rewardSlots.Count > MaxRewardDays)
            Debug.LogWarning($"[DailyReward] Reward Slots có {rewardSlots.Count} ô, nhưng chỉ dùng tối đa {MaxRewardDays} ngày.");

        isInitialized = true;
        RefreshAllSlots();
    }

    /// <summary>
    /// Lấy dữ liệu quà cho 1 ngày:
    /// ưu tiên tìm theo dayIndex trong rewardData; nếu không có thì dùng theo thứ tự danh sách.
    /// </summary>
    private RewardItem GetItemForDay(int day)
    {
        // Ưu tiên tìm theo dayIndex được điền trong asset.
        if (rewardData != null && rewardData.TryGetItem(day, out RewardItem found))
            return found;

        // Fallback: lấy theo vị trí day-1 trong danh sách (dữ liệu điền đúng thứ tự).
        if (rewardData != null && rewardData.Items != null && (day - 1) < rewardData.Items.Count)
            return rewardData.Items[day - 1];

        return default;
    }

    // ──────────────────── Logic thời gian ────────────────────

    /// <summary>
    /// Xác định ngày hiện có thể nhận quà (dayIndex 1..7).
    /// Trả về -1 khi chưa đủ 24h (phải chờ).
    /// Nếu quá missResetHours → tự reset streak về 0 và lưu lại.
    /// </summary>
    private int GetNextClaimableDayIndex()
    {
        // Chưa từng nhận quà → nhận được ngay ngày 1.
        if (!hasClaimedOnce) return 1;

        TimeSpan elapsed = DateTime.UtcNow - lastClaimTime;

        // Quá missResetHours (mặc định 48h) → bỏ lỡ 1 ngày → reset streak về 0.
        if (elapsed.TotalHours >= missResetHours)
        {
            if (streakCount != 0)
            {
                streakCount = 0;
                SaveProgress();
                Debug.Log($"[DailyReward] Bỏ lỡ {Mathf.FloorToInt((float)elapsed.TotalHours)}h (> {missResetHours}h) → Streak đã reset về 0.");
            }
            return 1;
        }

        // Chưa đủ 24h → phải chờ hết hôm nay.
        if (elapsed.TotalHours < 24f)
            return -1;

        // Đủ 24h nhưng chưa quá missResetHours → nhận ngày tiếp theo trong chu kỳ.
        int next = streakCount + 1;
        if (next > MaxRewardDays) next = 1;
        return next;
    }

    // ──────────────────── Cập nhật toàn bộ ô ────────────────────

    /// <summary>
    /// Tính lại trạng thái cho cả 7 ô dựa trên streak hiện tại và ngày có thể nhận.
    /// Gọi lại mỗi khi mở panel / nhận quà / reset để đồng bộ UI.
    /// </summary>
    public void RefreshAllSlots()
    {
        if (!isInitialized || rewardSlots == null) return;

        int nextClaimable = GetNextClaimableDayIndex();

        // Khi đã nhận đủ 7 ngày và sang vòng mới (ngày 1 claimable), tạm ẩn dấu ✓ của vòng cũ.
        int displayClaimed = streakCount;
        if (streakCount >= MaxRewardDays && nextClaimable == 1)
            displayClaimed = 0;

        for (int i = 0; i < rewardSlots.Count; i++)
        {
            DailyRewardSlotUI slot = rewardSlots[i];
            if (slot == null) continue;

            int day = i + 1;
            DailyRewardSlotUI.RewardState state;

            if (day <= displayClaimed)
                state = DailyRewardSlotUI.RewardState.Claimed;
            else if (day == nextClaimable)
                state = DailyRewardSlotUI.RewardState.Claimable;
            else
                state = DailyRewardSlotUI.RewardState.Locked;

            slot.UpdateState(state);
        }

        // Báo cho notify icon ngoài menu biết để ẩn/bật theo trạng thái mới nhất.
        OnRewardStateChanged?.Invoke();
    }

    // ──────────────────── Nhận quà ────────────────────

    /// <summary>
    /// Xử lý khi người chơi bấm nhận quà ở ngày <paramref name="dayIndex"/>.
    /// Chỉ cho nhận nếu ngày đó ĐANG claimable (chống spam / bấm khi chưa tới lượt).
    /// Sau khi nhận: cộng quà → lưu thời gian hiện tại + tăng streak → làm mới lại UI.
    /// </summary>
    public void ClaimReward(int dayIndex)
    {
        // Kiểm tra ngày này có đang được phép nhận không.
        int allowedDay = GetNextClaimableDayIndex();
        if (allowedDay < 0 || dayIndex != allowedDay)
        {
            Debug.LogWarning($"[DailyReward] Không thể nhận quà ngày {dayIndex} (ngày hợp lệ hiện tại: {allowedDay}).");
            return;
        }

        RewardItem item = GetItemForDay(dayIndex);

        // ── Bước 1: cộng phần thưởng cho người chơi ──
        GrantReward(item);

        // ── Bước 2: ghi nhận đã nhận quà (thời gian hiện tại + cập nhật streak) ──
        lastClaimTime = DateTime.UtcNow;
        streakCount = dayIndex; // Sau ngày 7, GetNextClaimableDayIndex() sẽ tự quay vòng về 1.
        SaveProgress();

        // ── Bước 3: cập nhật lại trạng thái tất cả các ô ──
        RefreshAllSlots();
    }

    /// <summary>
    /// Cộng quà vào tài khoản người chơi theo loại:
    ///   - Coin: cộng qua UserData.Instance.AddGold() (singleton có sẵn trong scene).
    ///   - Item: phát sự kiện OnItemReward để hệ thống khác tự xử lý.
    /// </summary>
    private void GrantReward(RewardItem item)
    {
        switch (item.type)
        {
            case RewardType.Coin:
                if (UserData.Instance != null)
                {
                    UserData.Instance.AddGold(item.amount);
                    Debug.Log($"[DailyReward] +{item.amount} Vàng (ngày {item.dayIndex}).");
                }
                else
                {
                    Debug.LogWarning("[DailyReward] Không tìm thấy UserData để cộng vàng. Hãy đảm bảo singleton UserData tồn tại trong scene.");
                }
                break;

            case RewardType.Item:
                // Để hệ thống inventory/popup của bạn lắng nghe sự kiện này để cộng vật phẩm.
                OnItemReward?.Invoke(item);
                Debug.Log($"[DailyReward] Nhận vật phẩm '{item.rewardName}' x{item.amount} (ngày {item.dayIndex}).");
                break;
        }
    }

    // ──────────────────── Tiện ích test ────────────────────

    /// <summary>
    /// Xóa toàn bộ tiến trình điểm danh (test). Kích hoạt từ Editor:
    /// click chuột phải component → "Reset Daily Reward (test)".
    /// </summary>
    [ContextMenu("Reset Daily Reward (test)")]
    private void ResetProgressForTesting()
    {
        PlayerPrefs.DeleteKey(KEY_LAST_CLAIM);
        PlayerPrefs.DeleteKey(KEY_STREAK);
        PlayerPrefs.Save();

        hasClaimedOnce = false;
        streakCount = 0;
        lastClaimTime = DateTime.MinValue;

        RefreshAllSlots();
        Debug.Log("[DailyReward] Đã reset toàn bộ tiến trình điểm danh (thử lại từ ngày 1).");
    }
}