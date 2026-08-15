using UnityEngine;

public class FogController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Material fogMaterial;

    [Header("Vision")]
    [SerializeField] private float innerRadius = 4f;
    [SerializeField] private float outerRadius = 7f;
    [SerializeField] private float edgeSoftness = 1.5f;
    [SerializeField] private float smoothTime = 0.15f;
    private Vector3 currentPos;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponentInParent<Camera>();

        UpdateFogSize();
    }

    private void LateUpdate()
    {
        UpdateFogSize();
        UpdateShader();
    }

    private void UpdateFogSize()
    {
        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;

        transform.localScale = new Vector3(width, height, 1f);
    }

    private void UpdateShader()
    {
        if (player == null || fogMaterial == null)
            return;

        currentPos = Vector3.SmoothDamp(currentPos, player.position, ref velocity, smoothTime);
        fogMaterial.SetVector("_PlayerPos", currentPos);

        fogMaterial.SetFloat("_InnerRadius", innerRadius);
        fogMaterial.SetFloat("_OuterRadius", outerRadius);
        fogMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
    }
}
