using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private BulletSO bulletStats;
    [SerializeField] private float hitRadius = 0.15f;
    [SerializeField] private float knockbackForce = 3f;

    private Vector2 direction;
    private float age;
    private float maxDistance;
    private float travelledDistance;
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
    private float explosionRadius = 0f;
    private int bounceRemaining = 0;
    private System.Action onKillCallback;
    private Collider2D lastHit;
    private float lastHitTime;

    public void Init(Vector2 dir, Transform owner, float maxDistance, int pierce = 0, float speedMultiplier = 1f, float damageMultiplier = 1f, float executePercent = 0f, float knockbackMultiplier = 1f, float sizeMultiplier = 1f, bool infinitePierceOnKill = false, float explosionDamagePercent = 0f, float explosionRadius = 0f, int bounceCount = 0, System.Action onKillCallback = null)
    {
        direction = dir.normalized;
        this.owner = owner;
        this.maxDistance = maxDistance;
        pierceRemaining = pierce;
        this.speedMultiplier = speedMultiplier;
        this.damageMultiplier = damageMultiplier;
        this.executePercent = executePercent;
        this.knockbackMultiplier = knockbackMultiplier;
        this.sizeMultiplier = sizeMultiplier;
        this.infinitePierceOnKill = infinitePierceOnKill;
        this.explosionDamagePercent = explosionDamagePercent;
        this.explosionRadius = explosionRadius;
        this.bounceRemaining = bounceCount;
        this.onKillCallback = onKillCallback;
        transform.localScale = baseScale * sizeMultiplier;
        age = 0f;
        travelledDistance = 0f;
        lastHit = null;
        lastHitTime = 0f;
    }

    private void Awake()
    {
        EnsurePhysics();
        baseScale = transform.localScale;
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
        if (age >= bulletStats.LifeTime)
        {
            DespawnSelf();
            return;
        }

        float t = age / bulletStats.LifeTime;
        float speed = bulletStats.Speed * bulletStats.SpeedCurve.Evaluate(t) * speedMultiplier;

        float step = speed * Time.deltaTime;
        travelledDistance += step;
        if (travelledDistance >= maxDistance)
        {
            DespawnSelf();
            return;
        }

        transform.position += (Vector3)(direction * step);
        transform.up = direction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.IsChildOf(owner))
            return;
        if (other.TryGetComponent(out Bullet _))
            return;

        IDamageable damageable = other.GetComponentInChildren<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
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
            && enemyHealth.CurrentHealth <= enemyHealth.MaxHealth * executePercent)
        {
            damageable.TakeDamage(float.MaxValue);
        }

        IKnockbackable knockbackable = other.GetComponent<IKnockbackable>();
        if (knockbackable == null)
            knockbackable = other.GetComponentInParent<IKnockbackable>();

        if (knockbackable != null)
        {
            knockbackable.ApplyKnockback(direction, knockbackForce * knockbackMultiplier);
        }

        bool consumedPierce = false;
        if (infinitePierceOnKill && wouldKill)
        {
            // Don't consume pierce if the hit kills
        }
        else if (pierceRemaining > 0)
        {
            pierceRemaining--;
            consumedPierce = true;
        }

        // Explosion on kill
        if (wouldKill && explosionDamagePercent > 0f && explosionRadius > 0f)
        {
            float explosionDamage = finalDamage * explosionDamagePercent;
            Collider2D[] hits = Physics2D.OverlapCircleAll(other.transform.position, explosionRadius);
            foreach (Collider2D hit in hits)
            {
                if (hit == other) continue;
                IDamageable dmg = hit.GetComponentInChildren<IDamageable>();
                if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg != damageable)
                    dmg.TakeDamage(explosionDamage);
            }
        }

        if (!consumedPierce && pierceRemaining <= 0)
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

        if (consumedPierce)
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
        // Find all valid enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 10f);
        List<Transform> validTargets = new List<Transform>();

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == excludeTarget) continue;
            if (hit.transform.IsChildOf(owner)) continue;
            IDamageable dmg = hit.GetComponentInChildren<IDamageable>();
            if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            validTargets.Add(hit.transform);
        }

        if (validTargets.Count > 0)
        {
            Transform newTarget = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
            direction = (newTarget.position - transform.position).normalized;
            transform.up = direction;
            bounceRemaining--;
            lastHit = null;
            lastHitTime = 0f;
            return;
        }

        DespawnSelf();
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
