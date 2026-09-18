using System.Collections;
using UnityEngine;

public class BombActive : MonoBehaviour, IDamageable, IPoolSpawnable
{
    [Header("Fuse")]
    [SerializeField] private float fuseTime = 3f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 10f;
    [SerializeField] private GameObject explosionVFX;
    [Tooltip("Tiếng nổ 3D (theo khoảng cách). Để trống thì không phát tiếng.")]
    [SerializeField] private AudioClip explosionSound;
    [Header("Flash Colors")]
    [SerializeField] private Color flashRed = Color.red;
    [SerializeField] private Color flashWhite = Color.white;

    private float timer;
    private bool exploding;
    private SpriteRenderer[] renderers;
    private MaterialPropertyBlock mpb;
    private Coroutine flashRoutine;

    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        StartFuse();
    }

    private void OnDisable()
    {
        StopFlash();
        ResetFlash();
    }

    public void OnSpawned()
    {
    }

    private void StartFuse()
    {
        timer = fuseTime;
        exploding = false;
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private void Update()
    {
        if (exploding) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
            Explode();
    }

    public void TakeDamage(float amount)
    {
        Explode();
    }

    private void Explode()
    {
        if (exploding) return;
        exploding = true;

        StopFlash();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Bomb] Explode at {transform.position}, radius={explosionRadius}, hits={hits.Length}");
#endif

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            IDamageable dmg = hit.GetComponentInChildren<IDamageable>();
            if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();

            if (dmg != null)
                dmg.TakeDamage(explosionDamage);
        }

        CameraShake.Shake(0.6f, 0.35f);

        // Phát tiếng nổ 2D phù hợp game top-down; volume giảm dần theo
        // khoảng cách thật (X,Y) từ player tới vị trí nổ => gần to, xa nhỏ.
        ExplosionSFX.Play2D(explosionSound, transform.position);

        if (explosionVFX != null &&
            (GameSettingsManager.Instance == null || GameSettingsManager.Instance.ShowVFX))
            ObjectPooling.Instance.Spawn(explosionVFX, transform.position, Quaternion.identity);

        ObjectPooling.Instance.Despawn(gameObject);
    }

    private IEnumerator FlashRoutine()
    {
        while (timer > 0f && !exploding)
        {
            float progress = 1f - (timer / fuseTime);
            float flashInterval = Mathf.Lerp(0.5f, 0.05f, progress);

            SetFlash(flashRed);
            yield return new WaitForSeconds(flashInterval * 0.5f);
            SetFlash(flashWhite);
            yield return new WaitForSeconds(flashInterval * 0.5f);
        }
    }

    private void SetFlash(Color color)
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(FlashColorID, color);
            mpb.SetFloat(FlashAmountID, 1f);
            r.SetPropertyBlock(mpb);
        }
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }

    private void ResetFlash()
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetFloat(FlashAmountID, 0f);
            r.SetPropertyBlock(mpb);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
