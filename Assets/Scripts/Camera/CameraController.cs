using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private Vector3 velocity;
    private Vector3 shakeOffsetAccumulator;

    public void ApplyShakeOffset(Vector3 amount)
    {
        shakeOffsetAccumulator += amount;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset + shakeOffsetAccumulator;
        targetPos.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );

        shakeOffsetAccumulator = Vector3.zero;
    }
}