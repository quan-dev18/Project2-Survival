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
    private Collider2D lastHit;
    private float lastHitTime;

    public void Init(Vector2 dir, Transform owner, float maxDistance, int pierce = 0, float speedMultiplier = 1f, float damageMultiplier = 1f, float executePercent = 0f, float knockbackMultiplier = 1f, float sizeMultiplier = 1f)
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

        damageable.TakeDamage(bulletStats.Damage * damageMultiplier);

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

        if (pierceRemaining > 0)
        {
            pierceRemaining--;
            lastHit = other;
            lastHitTime = Time.time;
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
