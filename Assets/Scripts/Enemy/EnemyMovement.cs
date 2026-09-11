using UnityEngine;

public class EnemyMovement : MonoBehaviour, IPoolSpawnable, IKnockbackable
{
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Transform mesh;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float separationRadius = 0.6f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private Animator _animator;

    private Transform targetTransform;
    private Vector2 knockbackVelocity;
    private bool hasAttackParam;

    private Vector2 wanderDir;
    private float wanderTimer;
    private static readonly Collider2D[] s_SeparationBuffer = new Collider2D[16];

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        hasAttackParam = HasAnimatorParam(_animator, "isAttack");
    }

    public void OnSpawned()
    {
        targetTransform = ObjectPooling.Instance.targetTransform;
        knockbackVelocity = Vector2.zero;
        if (hasAttackParam)
            _animator.SetBool("isAttack", false);
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        knockbackVelocity = dir.normalized * force;
    }

    private void Update()
    {
        knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, 8f * Time.deltaTime);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
        {
            Wander();
            return;
        }

        if (targetTransform == null) return;

        Vector2 toTarget = targetTransform.position - transform.position;
        float dist = toTarget.magnitude;

        if (dist <= enemyController.attackRange)
        {
            if (enemyController.CanAttack)
                Attack();
            return;
        }

        Vector2 moveDir = toTarget / dist;
        Flip(moveDir.x);
        Vector2 separation = GetSeparation();
        Vector2 velocity = moveDir * enemyController.movementSpeed + separation + knockbackVelocity;

        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private Vector2 GetSeparation()
    {
        Vector2 push = Vector2.zero;
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, s_SeparationBuffer, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            if (s_SeparationBuffer[i].transform == transform) continue;

            Vector2 away = transform.position - s_SeparationBuffer[i].transform.position;
            float dist = away.magnitude;
            if (dist > 0.01f && dist < separationRadius)
                push += away / dist * (1f - dist / separationRadius);
        }

        return push * separationForce;
    }

    private void Attack()
    {
        if (hasAttackParam)
            _animator.SetBool("isAttack", true);
        enemyController.Attack();
    }

    public void ResetAttack()
    {
        if (hasAttackParam)
            _animator.SetBool("isAttack", false);
    }

    private void Wander()
    {
        wanderTimer -= Time.unscaledDeltaTime;
        if (wanderTimer <= 0f)
        {
            wanderDir = Random.insideUnitCircle.normalized;
            wanderTimer = Random.Range(2f, 3f);
        }

        if (wanderDir != Vector2.zero)
            Flip(wanderDir.x);

        Vector2 separation = GetSeparation();
        Vector2 velocity = wanderDir * enemyController.movementSpeed * 0.5f + separation;
        transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
    }
    private void Flip(float x)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (x < 0f ? 1f : -1f);
        if(mesh != null)
        {
            mesh.localScale = scale;
        }
    }

    private static bool HasAnimatorParam(Animator animator, string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
}