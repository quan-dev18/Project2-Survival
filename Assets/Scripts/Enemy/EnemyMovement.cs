using UnityEngine;

public class EnemyMovement : MonoBehaviour, IPoolSpawnable, IKnockbackable
{
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float separationRadius = 0.6f;
    [SerializeField] private float separationForce = 2f;

    private Transform targetTransform;
    private Vector2 knockbackVelocity;

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
    }

    public void OnSpawned()
    {
        targetTransform = ObjectPooling.Instance.targetTransform;
        knockbackVelocity = Vector2.zero;
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        knockbackVelocity = dir.normalized * force;
    }

    private void Update()
    {
        knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, 8f * Time.deltaTime);

        if (targetTransform == null) return;

        Vector2 toTarget = targetTransform.position - transform.position;
        float dist = toTarget.magnitude;

        if (dist <= enemyController.attackRange)
        {
            Attack();
            return;
        }

        Vector2 moveDir = toTarget / dist;
        Vector2 separation = GetSeparation();
        Vector2 velocity = moveDir * enemyController.movementSpeed + separation + knockbackVelocity;

        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private Vector2 GetSeparation()
    {
        Vector2 push = Vector2.zero;
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i].transform == transform) continue;

            Vector2 away = transform.position - neighbors[i].transform.position;
            float dist = away.magnitude;
            if (dist > 0.01f && dist < separationRadius)
                push += away / dist * (1f - dist / separationRadius);
        }

        return push * separationForce;
    }

    private void Attack()
    {
        return;
    }
}