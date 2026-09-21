using UnityEngine;

public enum BossState { Idle, Chase, Attack, Skill, Dead }

public class BossController : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyMovement enemyMovement;

    private BossState currentState = BossState.Idle;
    private Transform targetTransform;
    private float stateTimer;
    private float attackCooldown;

    public BossState CurrentState => currentState;
    public event System.Action<BossState> OnStateChanged;

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();
    }

    private void OnEnable()
    {
        currentState = BossState.Idle;
        stateTimer = 0f;
        attackCooldown = 0f;
    }

    public void OnSpawned()
    {
        targetTransform = ObjectPooling.Instance.targetTransform;
        currentState = BossState.Chase;
        stateTimer = 0f;
        attackCooldown = 0f;
        OnStateChanged?.Invoke(currentState);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (currentState == BossState.Dead) return;

        stateTimer += Time.deltaTime;
        attackCooldown -= Time.deltaTime;

        switch (currentState)
        {
            case BossState.Idle:
                UpdateIdle();
                break;
            case BossState.Chase:
                UpdateChase();
                break;
            case BossState.Attack:
                UpdateAttack();
                break;
            case BossState.Skill:
                UpdateSkill();
                break;
        }
    }

    private void UpdateIdle()
    {
        if (stateTimer >= 0.5f)
            ChangeState(BossState.Chase);
    }

    private void UpdateChase()
    {
        if (targetTransform == null) return;

        float dist = Vector2.Distance(transform.position, targetTransform.position);
        if (dist <= enemyController.attackRange && attackCooldown <= 0f)
        {
            ChangeState(BossState.Attack);
            return;
        }
    }

    private void UpdateAttack()
    {
        if (attackCooldown <= 0f)
        {
            enemyController.Attack();
            attackCooldown = 1f / enemyController.attackSpeed;
            ChangeState(BossState.Chase);
        }
    }

    private void UpdateSkill()
    {
        // Placeholder for future skills
        if (stateTimer >= 1f)
            ChangeState(BossState.Chase);
    }

    public void ChangeState(BossState newState)
    {
        if (currentState == BossState.Dead) return;

        currentState = newState;
        stateTimer = 0f;
        OnStateChanged?.Invoke(currentState);
    }

    public void OnBossDied()
    {
        currentState = BossState.Dead;
        OnStateChanged?.Invoke(currentState);

        // Log boss killed event
        string bossType = gameObject.name;
        float timeAlive = GameManager.Instance != null ? GameManager.Instance.TotalElapsedTime : 0f;
        int playerLevel = PlayerXP.Instance != null ? PlayerXP.Instance.CurrentLevel : 1;
        int killCount = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
        FirebaseAnalyticsHelper.LogBossKilled(bossType, timeAlive, playerLevel, killCount);
    }
}
