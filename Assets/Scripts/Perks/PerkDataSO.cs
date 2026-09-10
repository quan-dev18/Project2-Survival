using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa dữ liệu tĩnh của một Perk/Nâng cấp.
/// Tách biệt hoàn toàn Data (SO) khỏi Logic UI (View/Controller).
/// </summary>
[CreateAssetMenu(fileName = "PerkData", menuName = "Perks/PerkData")]
public class PerkDataSO : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    [SerializeField] private string perkID;
    [SerializeField] private string perkName;
    [SerializeField] private Sprite icon;

    [Tooltip("Mô tả tác dụng của Perk.")]
    [TextArea]
    [SerializeField] private string description;

    [Header("Cấp độ & Giá tiền")]
    [Tooltip("Số cấp tối đa Perk có thể nâng.")]
    [SerializeField] private int maxLevel;

    [Tooltip("Giá tiền gốc để nâng cấp cấp đầu tiên.")]
    [SerializeField] private int baseCost;

    [Tooltip("Hệ số tăng giá theo cấp (cấp càng cao, giá càng đắt).")]
    [SerializeField] private float costMultiplier = 1f;

    [Header("Buff")]
    [Tooltip("Các chỉ số được tăng. AmountPerLevel là mức tăng MỖI CẤP (percent: 5 = +5%).")]
    [SerializeField] private List<PerkStatMod> statMods = new List<PerkStatMod>();

    public string PerkID => perkID;
    public string PerkName => perkName;
    public Sprite Icon => icon;
    public string Description => description;
    public int MaxLevel => maxLevel;
    public int BaseCost => baseCost;
    public float CostMultiplier => costMultiplier;
    public List<PerkStatMod> StatMods => statMods;

    /// <summary>
    /// Tổng buff (%) tại 1 cấp độ: sum(AmountPerLevel) * level.
    /// Giả định các mod đều theo quy ước percent (5 = +5%).
    /// </summary>
    public float GetBuffPercentAtLevel(int currentLevel)
    {
        if (currentLevel < 0) currentLevel = 0;
        if (statMods == null || statMods.Count == 0) return 0f;

        float perLevel = 0f;
        foreach (PerkStatMod mod in statMods)
        {
            if (mod != null) perLevel += mod.AmountPerLevel;
        }
        return perLevel * currentLevel;
    }

    /// <summary>
    /// Tính giá tiền để nâng cấp Perk từ <paramref name="currentLevel"/> lên cấp tiếp theo.
    /// Công thức dạng cấp số nhân: cost = round(baseCost * multiplier^currentLevel)
    /// </summary>
    public int GetCostForLevel(int currentLevel)
    {
        if (currentLevel < 0) currentLevel = 0;
        if (currentLevel >= maxLevel) return -1; // Đã đạt Max Level

        float raw = baseCost * Mathf.Pow(costMultiplier, currentLevel);
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    /// <summary>
    /// Một viên đá buff: chỉ số mục tiêu + lượng tăng MỖI CẤP.
    /// </summary>
    [System.Serializable]
    public class PerkStatMod
    {
        [Tooltip("Loại chỉ số cần tăng (dùng chung enum UpgradeType với hệ thống Upgrade cũ).")]
        [SerializeField] private UpgradeType stat;
        [Tooltip("Giá trị tăng mỗi cấp. Xu hướng dùng percent: 5 = +5%. Với count (đạn, xuyên...) dùng số nguyên.")]
        [SerializeField] private float amountPerLevel;

        public UpgradeType Stat => stat;
        public float AmountPerLevel => amountPerLevel;
    }
}
