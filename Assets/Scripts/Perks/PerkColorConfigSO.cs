using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một mốc màu: khi Perk đạt đến minLevel (VD: 12, 18, 24, 36...)
/// thì Slot sẽ đổi sang màu này. Admin cấu hình thoải mái, danh sách mở rộng được.
/// </summary>
[Serializable]
public class PerkLevelColor
{
    [Tooltip("Ngưỡng level. Level >= minLevel thì áp màu này. Nên có 1 entry minLevel = 0 làm màu gốc.")]
    [SerializeField] private int minLevel = 0;

    [Tooltip("Màu nền Slot ở ngưỡng này.")]
    [SerializeField] private Color backgroundColor = Color.gray;

    [Tooltip("Màu viền khi Slot được chọn ở ngưỡng này.")]
    [SerializeField] private Color selectionColor = Color.yellow;

    public int MinLevel => minLevel;
    public Color BackgroundColor => backgroundColor;
    public Color SelectionColor => selectionColor;
}

/// <summary>
/// ScriptableObject chứa Bảng màu theo mốc level (thay cho khái niệm Tier cũ).
/// Màu nền Slot thay đổi khi level Perk vượt qua các ngưỡng 12, 18, 24, 36...
/// </summary>
[CreateAssetMenu(fileName = "PerkColorConfig", menuName = "Perks/PerkColorConfig")]
public class PerkColorConfigSO : ScriptableObject
{
    [Tooltip("Danh sách mốc màu tăng dần theo MinLevel. (0 = màu gốc, 12, 18, 24, 36...).")]
    [SerializeField] private List<PerkLevelColor> levelColors = new List<PerkLevelColor>();

    /// <summary>
    /// Lấy màu tương ứng cho 1 level: chọn mốc có MinLevel lớn nhất nhưng &lt;= level.
    /// Nếu level = 0 sẽ chọn entry MinLevel = 0 (màu gốc).
    /// </summary>
    public PerkLevelColor GetColorForLevel(int level)
    {
        if (levelColors == null || levelColors.Count == 0) return null;

        PerkLevelColor best = null;
        foreach (PerkLevelColor entry in levelColors)
        {
            if (entry == null) continue;
            if (level >= entry.MinLevel && (best == null || entry.MinLevel > best.MinLevel))
                best = entry;
        }
        return best;
    }

#if UNITY_EDITOR
    /// <summary>Đảm bảo danh sách có entry minLevel = 0 làm màu gốc khi chưa có.</summary>
    private void OnValidate()
    {
        if (levelColors == null) return;
        bool hasDefault = false;
        foreach (PerkLevelColor entry in levelColors)
        {
            if (entry != null && entry.MinLevel == 0) hasDefault = true;
        }
        if (!hasDefault)
            Debug.LogWarning("[PerkColorConfig] Nên có 1 entry MinLevel = 0 làm màu gốc cho Slot.", this);
    }
#endif
}