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
        Debug.Log($"[Bomb] Explode at {transform.position}, radius={explosionRadius}, hits={hits.Length}");

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            IDamageable dmg = hit.GetComponentInChildren<IDamageable>();
            if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();

            Debug.Log($"[Bomb] Hit: {hit.name}, layer={LayerMask.LayerToName(hit.gameObject.layer)}, hasCollider={hit != null}, hasIDamageable={dmg != null}");

            if (dmg != null)
                dmg.TakeDamage(explosionDamage);
        }

        CameraShake.Shake(0.6f, 0.35f);

        if (explosionVFX != null)
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
