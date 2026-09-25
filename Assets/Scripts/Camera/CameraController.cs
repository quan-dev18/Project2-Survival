using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private Vector3 velocity;

    public Vector3 ShakeOffset { get; set; }

    /// <summary>
    /// Eagle Eyes: zoom the camera out so a wider area is visible,
    /// and widen fog vision so the new area isn't hidden in darkness.
    /// </summary>
    public static void ApplyVisionBonus(float pct)
    {
        float mult = 1f + pct;
        Camera cam = Camera.main;
        if (cam != null)
            cam.orthographicSize *= mult;
        FogController[] fogs = Object.FindObjectsOfType<FogController>(true);
        for (int i = 0; i < fogs.Length; i++)
            if (fogs[i] != null) fogs[i].AddVisionPercent(pct);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset + ShakeOffset;
        targetPos.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}