using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private BulletSO bulletStats;
    [SerializeField] private float hitRadius = 0.15f;

    private Vector2 direction;
    private float age;
    private Transform owner;

    public void Init(Vector2 dir, Transform owner)
    {
        direction = dir.normalized;
        this.owner = owner;
        age = 0f;
    }

    private void Awake()
    {
        EnsurePhysics();
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
        float speed = bulletStats.Speed * bulletStats.SpeedCurve.Evaluate(t);

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.up = direction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.IsChildOf(owner))
            return;
        if (other.TryGetComponent(out Bullet _))
            return;

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(bulletStats.Damage);
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
