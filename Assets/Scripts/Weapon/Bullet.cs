using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private BulletSO bulletStats;
    [SerializeField] private float hitRadius = 0.15f;
    [SerializeField] private float knockbackForce = 3f;

    private Vector2 direction;
    private float age;
    private Transform owner;
    private int pierceRemaining;
    private float speedMultiplier = 1f;
    private float damageMultiplier = 1f;
    private float executePercent;
    private float knockbackMultiplier = 1f;
    private float sizeMultiplier = 1f;
    private Vector3 baseScale = Vector3.one;
    private bool infinitePierceOnKill = false;
    private float explosionDamagePercent = 0f;
    private int bounceRemaining = 0;
    private System.Action onKillCallback;
    private Collider2D lastHit;
    private float lastHitTime;
    private TrailRenderer trail;
    private bool trailReady;
    private float trailOriginalTime;
    private static readonly Collider2D[] s_BounceOverlapBuffer = new Collider2D[32];
    private static readonly Collider2D[] s_ExplosionOverlapBuffer = new Collider2D[64];
    private static readonly List<Transform> s_BounceValidTargets = new List<Transform>(16);

    public void Init(Vector2 dir, Transform owner, int pierce = 0, float speedMultiplier = 1f, float damageMultiplier = 1f, float executePercent = 0f, float knockbackMultiplier = 1f, float sizeMultiplier = 1f, bool infinitePierceOnKill = false, float explosionDamagePercent = 0f, int bounceCount = 0, System.Action onKillCallback = null)
    {
        direction = dir.normalized;
        this.owner = owner;
        pierceRemaining = pierce;
        this.speedMultiplier = speedMultiplier;
        this.damageMultiplier = damageMultiplier;
        this.executePercent = executePercent;
        this.knockbackMultiplier = knockbackMultiplier;
        this.sizeMultiplier = sizeMultiplier;
        this.infinitePierceOnKill = infinitePierceOnKill;
        this.explosionDamagePercent = explosionDamagePercent;
        this.bounceRemaining = bounceCount;
        this.onKillCallback = onKillCallback;
        transform.localScale = baseScale * sizeMultiplier;
        age = 0f;
        lastHit = null;
        lastHitTime = 0f;
    }

    private void Awake()
    {
        EnsurePhysics();
        baseScale = transform.localScale;
        trail = GetComponent<TrailRenderer>();
        if (trail != null)
            trailOriginalTime = trail.time;
    }

    public void OnSpawned()
    {
        age = 0f;
        lastHit = null;
        lastHitTime = 0f;
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }
        trailReady = false;
    }

    private void EnsurePhysics()
    {
        if (!TryGetComponent(out Rigidbody2D rb))
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
        rb.gravityScale = 0f;

        if (!TryGetComponent(out CircleCollider2D col))
            col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = hitRadius;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (!trailReady && trail != null)
        {
            trail.time = trailOriginalTime;
            trail.emitting = true;
            trailReady = true;
        }

        if (age >= bulletStats.LifeTime)
        {
            DespawnSelf();
            return;
        }

        float t = age / bulletStats.LifeTime;
        float speed = bulletStats.Speed * bulletStats.SpeedCurve.Evaluate(t) * speedMultiplier;

        float step = speed * Time.deltaTime;

        transform.position += (Vector3)(direction * step);
        transform.up = direction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Already despawned earlier in this physics step: skip so a queued
        // second callback cannot deal damage again or despawn twice.
        if (!gameObject.activeSelf)
            return;

        // Ignore any hit on owner hierarchy (player, weapons, etc.) or any WeaponController
        if (owner != null && (other.transform.IsChildOf(owner) || other.transform == owner || other.transform.root == owner || other.GetComponentInParent<WeaponController>() != null))
            return;
        if (other.TryGetComponent(out Bullet _))
            return;

        IDamageable damageable;
        if (!other.TryGetComponent(out damageable))
        {
            damageable = other.GetComponentInChildren<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
            return;

        // Never damage the player (including from summons)
        if (other.GetComponentInParent<PlayerStats>() != null)
            return;

        if (lastHit == other && Time.time - lastHitTime < 0.1f)
            return;

        float finalDamage = bulletStats.Damage * damageMultiplier;
        bool wouldKill = false;
        if (damageable is EnemyHealth enemyHealthForKillCheck)
        {
            wouldKill = enemyHealthForKillCheck.CurrentHealth <= finalDamage;
        }
        damageable.TakeDamage(finalDamage);

        if (executePercent > 0f && damageable is EnemyHealth enemyHealth
            && enemyHealth.CurrentHealth > 0f && enemyHealth.CurrentHealth <= enemyHealth.MaxHealth * executePercent)
        {
            // Fixed execute damage: flat value, never scaled by damage multipliers.
            damageable.TakeDamage(999f);
        }

        IKnockbackable knockbackable;
        if (!other.TryGetComponent(out knockbackable))
            knockbackable = other.GetComponentInParent<IKnockbackable>();

        if (knockbackable != null)
        {
            knockbackable.ApplyKnockback(direction, knockbackForce * knockbackMultiplier);
        }

        bool infinitePierced = false;
        bool consumedPierce = false;
        if (infinitePierceOnKill && wouldKill)
        {
            infinitePierced = true; // pierce for free, don't consume count
        }
        else if (pierceRemaining > 0)
        {
            pierceRemaining--;
            consumedPierce = true;
        }

        // Explosion on kill - skip owner/player
        if (wouldKill && explosionDamagePercent > 0f && bulletStats.ExplosionRadius > 0f && UnityEngine.Random.value < 0.25f)
        {
            float explosionDamage = finalDamage * explosionDamagePercent;
            float explosionRadius = bulletStats.ExplosionRadius * 0.5f;
            int hitCount = Physics2D.OverlapCircleNonAlloc(other.transform.position, explosionRadius, s_ExplosionOverlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = s_ExplosionOverlapBuffer[i];
                if (hit == other) continue;
                if (hit == null || !hit.gameObject.activeInHierarchy) continue; // killed earlier in this blast
                if (owner != null && (hit.transform == owner || hit.transform.IsChildOf(owner) || hit.transform.root == owner)) continue;
                if (hit.GetComponentInParent<PlayerStats>() != null) continue;
                IDamageable dmg;
                if (!hit.TryGetComponent(out dmg))
                {
                    dmg = hit.GetComponentInChildren<IDamageable>();
                    if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
                }
                if (dmg != null && dmg != damageable)
                    dmg.TakeDamage(explosionDamage);
            }
            PlayPooledOneShotVFXScaled("VFX_NO2", other.transform.position, 0.5f);
        }

        // Multiple damage: AOE on every hit
        if (bulletStats.IsMultipleDamage)
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(other.transform.position, bulletStats.ExplosionRadius, s_ExplosionOverlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = s_ExplosionOverlapBuffer[i];
                if (hit == other) continue;
                if (hit == null || !hit.gameObject.activeInHierarchy) continue; // killed earlier in this blast
                if (owner != null && hit.transform.IsChildOf(owner)) continue;
                IDamageable dmg;
                if (!hit.TryGetComponent(out dmg))
                {
                    dmg = hit.GetComponentInChildren<IDamageable>();
                    if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
                }
                if (dmg != null)
                    dmg.TakeDamage(finalDamage);
            }
            PlayPooledOneShotVFX("VFX_NO2", other.transform.position);
        }

        if (infinitePierced)
        {
            lastHit = other;
            lastHitTime = Time.time;
            // piercing for free - don't bounce, just continue
        }
        else if (!consumedPierce && pierceRemaining <= 0)
        {
            // Try bounce if no pierce left
            if (bounceRemaining > 0)
            {
                BounceToNewTarget(other.transform);
                return;
            }
            DespawnSelf();
            return;
        }
        else if (consumedPierce)
        {
            lastHit = other;
            lastHitTime = Time.time;
        }

        // Notify kill callback
        if (wouldKill && onKillCallback != null)
        {
            onKillCallback.Invoke();
        }
    }

    private void BounceToNewTarget(Transform excludeTarget)
    {
        // Resolve exclude root to avoid matching child colliders of same enemy
        IDamageable excludeDmg;
        if (!excludeTarget.TryGetComponent(out excludeDmg))
        {
            excludeDmg = excludeTarget.GetComponentInChildren<IDamageable>();
            if (excludeDmg == null)
                excludeDmg = excludeTarget.GetComponentInParent<IDamageable>();
        }
        Transform excludeRoot = excludeDmg != null ? (excludeDmg as Component)?.transform : excludeTarget;

        // Find all valid enemies in range (no GC alloc - uses static buffer)
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 12f, s_BounceOverlapBuffer);
        s_BounceValidTargets.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = s_BounceOverlapBuffer[i];
            if (hit == null) continue;
            if (hit.transform.IsChildOf(owner)) continue;
            IDamageable dmg;
            if (!hit.TryGetComponent(out dmg))
            {
                dmg = hit.GetComponentInChildren<IDamageable>();
                if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
            }
            if (dmg == null) continue;
            if (dmg == excludeDmg) continue;
            Transform hitRoot = (dmg as Component)?.transform;
            if (hitRoot != null && excludeRoot != null && (hitRoot == excludeRoot || hitRoot.IsChildOf(excludeRoot) || excludeRoot.IsChildOf(hitRoot))) continue;

            s_BounceValidTargets.Add(hit.transform);
        }

        if (s_BounceValidTargets.Count > 0)
        {
            Transform newTarget = s_BounceValidTargets[UnityEngine.Random.Range(0, s_BounceValidTargets.Count)];
            direction = (newTarget.position - transform.position).normalized;
            transform.up = direction;
            bounceRemaining--;
            lastHit = null;
            lastHitTime = 0f;
            transform.position += (Vector3)(direction * 0.3f);
            return;
        }

        DespawnSelf();
    }

    private void PlayPooledOneShotVFX(string key, Vector3 pos)
    {
        // Tôn trọng cờ "Hiển thị VFX": tắt thì bỏ qua spawn để tiết kiệm hiệu năng.
        if (GameSettingsManager.Instance != null && !GameSettingsManager.Instance.ShowVFX)
            return;
        if (ObjectPooling.Instance == null) return;
        GameObject vfx = ObjectPooling.Instance.Spawn(key, pos, Quaternion.identity);
        if (vfx == null) return;
        // ParticleSystem: must Play() after SetActive (animations auto-play via Animator)
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
        // Auto-despawn after longest particle lifetime (or fixed 1s fallback for animations)
        float lifetime = 1f;
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            float d = main.duration + main.startLifetime.constantMax;
            if (d > lifetime) lifetime = d;
        }
        var animator = vfx.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            float animLen = 0f;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.length > animLen) animLen = clip.length;
            if (animLen > lifetime) lifetime = animLen;
        }
        // Chạy coroutine trên ObjectPooling.Instance (luôn active) thay vì bullet:
        // bullet có thể đã bị despawn (inactive) trong cùng callback vật lý,
        // StartCoroutine trên gameObject inactive sẽ ném lỗi.
        ObjectPooling.Instance.StartCoroutine(DespawnVFXAfter(vfx, lifetime + 0.1f));
    }

    private void PlayPooledOneShotVFXScaled(string key, Vector3 pos, float scale)
    {
        if (GameSettingsManager.Instance != null && !GameSettingsManager.Instance.ShowVFX)
            return;
        if (ObjectPooling.Instance == null) return;
        GameObject vfx = ObjectPooling.Instance.Spawn(key, pos, Quaternion.identity);
        if (vfx == null) return;
        vfx.transform.localScale = Vector3.one * scale;
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
        float lifetime = 1f;
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            float d = main.duration + main.startLifetime.constantMax;
            if (d > lifetime) lifetime = d;
        }
        var animator = vfx.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            float animLen = 0f;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.length > animLen) animLen = clip.length;
            if (animLen > lifetime) lifetime = animLen;
        }
        ObjectPooling.Instance.StartCoroutine(DespawnVFXAfter(vfx, lifetime + 0.1f));
    }

    private System.Collections.IEnumerator DespawnVFXAfter(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfx == null) yield break;
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(vfx);
        else
            Destroy(vfx);
    }

    private void DespawnSelf()
    {
        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }
}

public interface IDamageable
{
    void TakeDamage(float amount);
}

public interface IKnockbackable
{
    void ApplyKnockback(Vector2 dir, float force);
}
