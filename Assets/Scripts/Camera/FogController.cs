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

    [Header("Muzzle Flash")]
    [SerializeField] private float flashRadius = 3f;
    [SerializeField] private float flashHalfAngle = 25f;
    [SerializeField] private float flashDuration = 0.25f;
    private float flashStrength;
    private Vector2 flashPos;
    private Vector2 flashDir;

    public void RevealAt(Vector2 worldPos, Vector2 direction)
    {
        flashPos = worldPos;
        flashDir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        flashStrength = 1f;
    }

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

        if (flashStrength > 0f)
        {
            flashStrength = Mathf.MoveTowards(flashStrength, 0f, Time.deltaTime / flashDuration);
            fogMaterial.SetVector("_FlashPos", new Vector4(flashPos.x, flashPos.y, 0f, 0f));
            fogMaterial.SetVector("_FlashDir", new Vector4(flashDir.x, flashDir.y, 0f, 0f));
            fogMaterial.SetFloat("_FlashRadius", flashRadius);
            fogMaterial.SetFloat("_FlashAngle", flashHalfAngle);
            fogMaterial.SetFloat("_FlashStrength", flashStrength);
        }
        else if (fogMaterial.GetFloat("_FlashStrength") > 0f)
        {
            fogMaterial.SetFloat("_FlashStrength", 0f);
        }
    }
}
