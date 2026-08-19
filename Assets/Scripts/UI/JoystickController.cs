using UnityEngine;

public class JoystickController : MonoBehaviour
{
    public static JoystickController Instance { get; private set; }

    [SerializeField] private RectTransform baseRect;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float deadZone = 0.2f;

    public Vector2 Direction { get; private set; }
    public bool IsActive { get; private set; }

    private float radius;
    private Canvas canvas;

    private void Awake()
    {
        Instance = this;

        if (baseRect == null)
            baseRect = GetComponent<RectTransform>();
        if (handle == null)
            FindHandle();

        canvas = GetComponentInParent<Canvas>();
        radius = baseRect != null ? baseRect.sizeDelta.x * 0.5f : 0f;

        gameObject.SetActive(false);
    }

    private void FindHandle()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out RectTransform rt))
            {
                handle = rt;
                return;
            }
        }
    }

    public void ShowAt(Vector2 screenPos)
    {
        if (IsActive) return;
        if (baseRect == null || handle == null) return;

        IsActive = true;
        gameObject.SetActive(true);

        handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;

        if (ToCanvasLocal(screenPos, out Vector2 localPos))
            baseRect.anchoredPosition = localPos;
    }

    public void MoveHandle(Vector2 screenPos)
    {
        if (!IsActive || baseRect == null || handle == null) return;
        if (!ToCanvasLocal(screenPos, out Vector2 localPos)) return;

        Vector2 delta = localPos - baseRect.anchoredPosition;
        Vector2 clamped = Vector2.ClampMagnitude(delta, radius);

        handle.anchoredPosition = clamped;

        float mag = radius > 0f ? clamped.magnitude / radius : 0f;
        Direction = mag > deadZone ? clamped.normalized * Mathf.Clamp01((mag - deadZone) / (1f - deadZone)) : Vector2.zero;
    }

    public void Hide()
    {
        if (!IsActive) return;

        IsActive = false;
        Direction = Vector2.zero;
        gameObject.SetActive(false);
    }

    private bool ToCanvasLocal(Vector2 screenPos, out Vector2 localPos)
    {
        RectTransform parentRect = baseRect.parent as RectTransform;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPos, canvas != null ? canvas.worldCamera : null, out localPos);
    }
}