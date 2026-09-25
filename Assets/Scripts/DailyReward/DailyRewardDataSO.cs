using System.Collections.Generic;
using UnityEngine;

// ── Loại phần thưởng ─────────────────────────────────────────────────────────
public enum RewardType
{
    Coin, // Phần thưởng là tiền vàng → sẽ cộng trực tiếp vào UserData.Gold.
    Item  // Phần thưởng là vật phẩm → phát sự kiện OnItemReward để hệ thống khác xử lý.
}

// ── Định nghĩa 1 món quà cho 1 ngày cụ thể ───────────────────────────────────
[System.Serializable]
public struct RewardItem
{
    [Tooltip("Ngày của quà (1 = D1, 2 = D2, ... 7 = D7). Giá trị phải khớp với thứ tự thực tế trên UI.")]
    public int dayIndex;

    [Tooltip("Tên hiển thị của quà (ví dụ: 'Vàng', 'Hộp quà', 'Vật phẩm').")]
    public string rewardName;

    [Tooltip("Icon của quà hiển thị trên từng ô D1 → D7.")]
    public Sprite rewardIcon;

    [Tooltip("Số lượng quà (ví dụ: 100 Vàng, 5 vật phẩm).")]
    public int amount;

    [Tooltip("Loại quà: Coin (vàng) hoặc Item (vật phẩm).")]
    public RewardType type;
}

/// <summary>
/// ScriptableObject chứa cấu hình 7 ngày quà của hệ thống Daily Reward.
/// Tách hoàn toàn DỮ LIỆU (file này) khỏi LOGIC (DailyRewardManager) và HIỂN THỊ (DailyRewardSlotUI).
///
/// CÁC BƯỚC GÁN TRÊN EDITOR:
///   1) Click chuột phải trong cửa sổ Project (Assets)
///      → Create → Daily Reward → DailyRewardData (menu này có nhờ CreateAssetMenu dưới đây).
///   2) Đổi tên asset vừa tạo (vd: "DailyRewardData_SO") để dễ quản lý.
///   3) Nhấn nút "+" dưới danh sách Items để thêm đúng 7 phần tử (D1 → D7).
///   4) Với TỪNG phần tử, điền:
///        - dayIndex   : số thứ tự ngày (1, 2, 3, ... 7).
///        - rewardName : tên quà.
///        - rewardIcon : kéo icon quà vào.
///        - amount     : số lượng.
///        - type       : Coin (vàng) hoặc Item (vật phẩm).
///   5) Kéo asset này vào ô "Reward Data" của GameObject chứa DailyRewardManager.
/// </summary>
[CreateAssetMenu(fileName = "DailyRewardData", menuName = "Daily Reward/DailyRewardData")]
public class DailyRewardDataSO : ScriptableObject
{
    [Header("Danh sách 7 ngày quà (D1 → D7)")]
    [Tooltip("Mỗi phần tử là 1 món quà. Nên điền theo đúng thứ tự ngày 1 → 7 để UI hiển thị đúng.")]
    [SerializeField] private List<RewardItem> items = new List<RewardItem>();
    public List<RewardItem> Items => items;

    /// <summary>
    /// Tìm quà theo <paramref name="dayIndex"/> (giá trị 1..7).
    /// Trả về true nếu tìm thấy; nếu dữ liệu chưa điền đủ thì trả về false
    /// và <paramref name="result"/> là phần tử mặc định để UI tự xử lý.
    /// </summary>
    public bool TryGetItem(int dayIndex, out RewardItem result)
    {
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].dayIndex == dayIndex)
                {
                    result = items[i];
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}