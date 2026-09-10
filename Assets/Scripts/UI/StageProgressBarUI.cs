using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// ============================================================================
/// StageProgressBarUI - Hiển thị kỷ lục tiến độ cao nhất (Best Progress)
/// của một Stage trên UI chọn Map, tích hợp trực tiếp với UserData.Instance.
///
/// ▐▌ CÁCH GẮN TRONG UNITY EDITOR
///   1. Gắn script này lên GameObject chứa thanh tiến trình (map preview...).
///   2. Kéo Image thuộc loại Filled (Fill Method = Horizontal) vào "Progress Bar".
///      (Nếu dùng Slider thì sửa field theo hướng dẫn trong mã nguồn).
///   3. Kéo TextMeshPro (tùy chọn) vào "Progress Percent Text" để hiển thị %.
///   4. Gọi từ MapSelectionManager hoặc StageSlot khi cần hiển thị:
///        progressUI.DisplayStageProgress(stageData);
///
/// ▐▌ GHÉP VỚI MAP SELECT MANAGER (ví dụ):
///   // Trong PreMapSO.Ngoài việc có sceneToLoad, thêm 1 ô StageSO;
///   // rồi trong MapSelectionManager.SpawnInitialMap()/ChangeMap() gọi:
///   //   stageProgressUI.DisplayStageProgress(mapList[currentIndex].stageData);
/// ============================================================================
public class StageProgressBarUI : MonoBehaviour
{
    [Header("UI Progress Bar")]
    [Tooltip("Thanh tiến trình: dùng Image loại Filled (Fill Method = Horizontal) hoặc Slider.")]
    [SerializeField] private Image progressBar;

    [Tooltip("Text hiển thị phần trăm kỷ lục (vd: '80%'). Để trống nếu không cần.")]
    [SerializeField] private TextMeshProUGUI progressPercentText;

    [Header("Animation (DOTween)")]
    [Tooltip("Thời gian tween thanh từ 0 → % kỷ lục. = 0 để hiện ngay lập tức.")]
    [SerializeField] private float fillDuration = 0.4f;

    // Tween đang chạy để kill khi hiển thị lặp lại (tránh đè animation).
    private Tween _activeTween;

    /// <summary>
    /// Hiển thị kỷ lục tiến độ cao nhất của Stage lên thanh progress.
    /// Tỷ lệ % = Clamp01(bestProgress / maxProgress).
    /// </summary>
    /// <param name="stageData">Dữ liệu Stage cần hiển thị.</param>
    public void DisplayStageProgress(StageSO stageData)
    {
        // --- Chuẩn hóa dữ liệu đầu vào ---
        if (stageData == null)
        {
            Debug.LogWarning("[StageProgressBarUI] stageData == null => reset thanh về 0.", this);
            SetDisplay(0f, true);
            return;
        }

        // Tránh chia cho 0 nếu maxProgress chưa được cấu hình (sẽ hiểu là 100% đạt được).
        if (stageData.MaxProgress <= 0f)
        {
            Debug.LogWarning($"[StageProgressBarUI] Stage '{stageData.StageID}' có MaxProgress <= 0 => thanh hiện 100%.", this);
            SetDisplay(1f, true);
            return;
        }

        // --- Lấy kỷ lục từ UserData ---
        float highestProgress = UserData.Instance != null
            ? UserData.Instance.GetStageBestProgress(stageData.StageID)
            : 0f;

        // --- Tính tỷ lệ hiển thị (0..1) ---
        float fillRatio = Mathf.Clamp01(highestProgress / stageData.MaxProgress);

        SetDisplay(fillRatio, false);
    }

    /// <summary>
    /// Lưu kỷ lục tiến độ mới nếu cao hơn kỷ lục cũ, rồi cập nhật thanh hiển thị.
    /// Gọi khi kết thúc màn chơi (win/lose đều có thể ghi nhận tiến độ đạt được).
    /// </summary>
    /// <param name="stageID">ID duy nhất của Stage (StageSO.StageID).</param>
    /// <param name="newProgress">Tiến trình mới đạt được trong lượt chơi vừa rồi.</param>
    public void UpdateAndSaveProgress(string stageID, float newProgress)
    {
        if (string.IsNullOrEmpty(stageID))
        {
            Debug.LogWarning("[StageProgressBarUI] stageID rỗng => bỏ qua.", this);
            return;
        }

        if (UserData.Instance == null)
        {
            Debug.LogWarning("[StageProgressBarUI] UserData.Instance == null => không lưu được.", this);
            return;
        }

        // Đọc kỷ lục cũ và so sánh: chỉ ghi nhận khi cao hơn.
        float oldBest = UserData.Instance.GetStageBestProgress(stageID);
        if (newProgress > oldBest)
            UserData.Instance.SetStageBestProgress(stageID, newProgress);
    }

    // ──────────────────── Helper ──────────────────────────

    /// <summary>
    /// Gán % hiển thị (0..1) vào thanh progress và text. Có tween mượt nếu bật.
    /// </summary>
    /// <param name="fillRatio">Giá trị chuẩn hóa 0..1.</param>
    /// <param name="instant">True = gán thẳng không tween (dùng cho trường hợp lỗi/reset).</param>
    private void SetDisplay(float fillRatio, bool instant)
    {
        // Cập nhật text phần trăm trước, hiển thị ngay giá trị cuối.
        if (progressPercentText != null)
            progressPercentText.text = Mathf.RoundToInt(fillRatio * 100f) + "%";

        if (instant || fillDuration <= 0f)
        {
            ApplyFillAmount(fillRatio);
            return;
        }

        // Kill tween cũ để tween mới chạy từ 0 → target (not đè lẫn nhau).
        _activeTween?.Kill();
        ApplyFillAmount(0f);

        _activeTween = DOTween.To(
                GetFillAmount,
                ApplyFillAmount,
                fillRatio,
                fillDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    /// <summary>Đọc giá trị fill hiện tại của thanh (hỗ trợ Image + Slider).</summary>
    private float GetFillAmount()
    {
        if (progressBar != null) return progressBar.fillAmount;
        return 0f;
    }

    /// <summary>Gán giá trị fill vào thanh (hỗ trợ Image + Slider).</summary>
    private void ApplyFillAmount(float value)
    {
        if (progressBar != null)
            progressBar.fillAmount = value;
    }
}