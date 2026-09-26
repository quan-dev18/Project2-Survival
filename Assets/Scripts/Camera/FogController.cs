using UnityEngine;

public class FogController : MonoBehaviour
{
    private static readonly int FogColorID = Shader.PropertyToID("_FogColor");
    private static readonly int PlayerPosID = Shader.PropertyToID("_PlayerPos");
    private static readonly int InnerRadiusID = Shader.PropertyToID("_InnerRadius");
    private static readonly int OuterRadiusID = Shader.PropertyToID("_OuterRadius");
    private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int FlashPosID = Shader.PropertyToID("_FlashPos");
    private static readonly int FlashDirID = Shader.PropertyToID("_FlashDir");
    private static readonly int FlashRadiusID = Shader.PropertyToID("_FlashRadius");
    private static readonly int FlashAngleID = Shader.PropertyToID("_FlashAngle");
    private static readonly int FlashStrengthID = Shader.PropertyToID("_FlashStrength");

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Material fogMaterial;

    [Header("Vision")]
    [SerializeField] private Color fogColor;
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

    /// <summary>Đổi màu fog theo map.</summary>
    public void SetFogColor(Color color)
    {
        fogColor = color;
    }

    /// <summary>Eagle Eyes: widen the visible radius (stacks multiplicatively).</summary>
    public void AddVisionPercent(float pct)
    {
        float mult = 1f + pct;
        innerRadius *= mult;
        outerRadius *= mult;
    }

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
        fogMaterial.SetVector(PlayerPosID, currentPos);
        fogMaterial.SetColor(FogColorID, fogColor);

        fogMaterial.SetFloat(InnerRadiusID, innerRadius);
        fogMaterial.SetFloat(OuterRadiusID, outerRadius);
        fogMaterial.SetFloat(EdgeSoftnessID, edgeSoftness);

        if (flashStrength > 0f)
        {
            flashStrength = Mathf.MoveTowards(flashStrength, 0f, Time.deltaTime / flashDuration);
            fogMaterial.SetVector(FlashPosID, new Vector4(flashPos.x, flashPos.y, 0f, 0f));
            fogMaterial.SetVector(FlashDirID, new Vector4(flashDir.x, flashDir.y, 0f, 0f));
            fogMaterial.SetFloat(FlashRadiusID, flashRadius);
            fogMaterial.SetFloat(FlashAngleID, flashHalfAngle);
            fogMaterial.SetFloat(FlashStrengthID, flashStrength);
        }
        else if (fogMaterial.GetFloat(FlashStrengthID) > 0f)
        {
            fogMaterial.SetFloat(FlashStrengthID, 0f);
        }
    }
}
