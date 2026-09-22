using System.Collections;
using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlashEffect : MonoBehaviour
{
    [Header("Cài đặt mặc định")]
    [Tooltip("Màu flash mặc định nếu không truyền màu khi gọi Flash()")]
    [SerializeField] private Color defaultFlashColor = Color.white;

    [Tooltip("Thời gian flash mặc định (giây)")]
    [SerializeField] private float defaultDuration = 0.15f;

    [Tooltip("Đường cong độ mạnh flash theo thời gian (1 -> 0). Để trống sẽ dùng giảm tuyến tính.")]
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private SpriteRenderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Coroutine _flashRoutine;

    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        // Lấy tất cả SpriteRenderer trong object (hỗ trợ cả trường hợp nhiều sprite con)
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Kích hoạt hiệu ứng flash với màu và thời gian mặc định.
    /// </summary>
    public void Flash()
    {
        Flash(defaultFlashColor, defaultDuration);
    }

    /// <summary>
    /// Kích hoạt hiệu ứng flash với màu tùy chỉnh.
    /// </summary>
    /// <param name="color">Màu muốn chớp (ví dụ: đỏ khi trúng đòn thường, trắng khi trúng chí mạng)</param>
    /// <param name="duration">Thời gian hiệu ứng (giây)</param>
    public void Flash(Color color, float duration)
    {
        // Pooled/despawned objects can receive late hits (e.g. bazooka AOE
        // snapshots); coroutines can't start while inactive.
        if (!isActiveAndEnabled) return;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float amount = flashCurve.Evaluate(t);

            SetFlash(color, amount);
            yield return null;
        }

        SetFlash(color, 0f);
        _flashRoutine = null;
    }

    private void SetFlash(Color color, float amount)
    {
        foreach (var renderer in _renderers)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorID, color);
            _mpb.SetFloat(FlashAmountID, amount);
            renderer.SetPropertyBlock(_mpb);
        }
    }

    // Dừng flash ngay lập tức (ví dụ khi object bị destroy hoặc reset trạng thái)
    public void StopFlash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }
        SetFlash(defaultFlashColor, 0f);
    }
}