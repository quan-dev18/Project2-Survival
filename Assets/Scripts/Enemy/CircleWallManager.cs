using System.Collections.Generic;
using UnityEngine;

public class CircleWallManager : MonoBehaviour
{
    public static CircleWallManager Instance { get; private set; }

    [SerializeField] private float wallAttackRange = 1.5f;

    private Transform playerTransform;

    private readonly List<WallEntry> activeWalls = new List<WallEntry>();

    private class WallEntry
    {
        public StageSO.CircleWallConfig config;
        public float currentRadius;
        public float elapsed;
        public Vector2 center;
        public readonly List<WallEnemy> enemies = new List<WallEnemy>();
    }

    private class WallEnemy
    {
        public GameObject gameObject;
        public Transform transform;
        public EnemyController controller;
        public EnemyMovement[] movements;
        public BossController[] bossControllers;
        public Rigidbody2D[] rigidbodies;
        public float angle;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerTransform = ObjectPooling.Instance != null ? ObjectPooling.Instance.targetTransform : null;
        GameManager.OnStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
        if (Instance == this) Instance = null;
        ClearAll();
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            ClearAll();
        }
    }

    public void TryActivate(StageSO stage, int phaseIndex)
    {
        if (stage == null) return;
        if (phaseIndex < 0 || phaseIndex >= stage.Phases.Count) return;

        StageSO.CircleWallConfig config = stage.Phases[phaseIndex].CircleWall;
        if (config == null || !config.Enabled)
        {
            ClearAll();
            return;
        }

        ActivateWall(config);
    }

    private void ActivateWall(StageSO.CircleWallConfig config)
    {
        if (playerTransform == null && ObjectPooling.Instance != null)
            playerTransform = ObjectPooling.Instance.targetTransform;
        if (playerTransform == null) return;

        // Clear any previous active wall to prevent duplicate overlapping walls
        ClearAll();

        WallEntry entry = new WallEntry
        {
            config = config,
            currentRadius = config.StartRadius,
            elapsed = 0f,
            center = playerTransform.position
        };

        float angleStep = config.WallEnemyCount > 0 ? 360f / config.WallEnemyCount : 0f;
        for (int i = 0; i < config.WallEnemyCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 pos = (Vector2)playerTransform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * config.StartRadius;

            GameObject obj = ObjectPooling.Instance.Spawn(config.WallEnemyKey, pos, Quaternion.identity);
            if (obj == null) continue;

            // Find all components across root and children to support any enemy prefab hierarchy
            EnemyController ec = obj.GetComponentInChildren<EnemyController>(true);
            if (ec == null) ec = obj.GetComponentInParent<EnemyController>();

            EnemyMovement[] movements = obj.GetComponentsInChildren<EnemyMovement>(true);
            foreach (var em in movements)
            {
                if (em != null)
                {
                    em.enabled = false;
                    em.ResetKnockback(); // frozen enemies must not bank knockback
                }
            }

            BossController[] bosses = obj.GetComponentsInChildren<BossController>(true);
            foreach (var bc in bosses)
            {
                if (bc != null) bc.enabled = false;
            }

            Rigidbody2D[] rbs = obj.GetComponentsInChildren<Rigidbody2D>(true);
            foreach (var rb in rbs)
            {
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }

            if (ec != null)
            {
                ec.ResetBonuses();
                float hpBonus = (config.WallEnemyHP - ec.baseMaxHealth) / Mathf.Max(ec.baseMaxHealth, 0.01f);
                if (hpBonus > 0f)
                    ec.AddMaxHealthPercent(hpBonus);
                ec.SetCurrentHealth(ec.maxHealth);
            }

            // Directly enforce initial position
            obj.transform.position = pos;
            foreach (var rb in rbs)
            {
                if (rb != null) rb.position = pos;
            }

            WallEnemy wallEnemy = new WallEnemy
            {
                gameObject = obj,
                transform = obj.transform,
                controller = ec,
                movements = movements,
                bossControllers = bosses,
                rigidbodies = rbs,
                angle = angle
            };

            entry.enemies.Add(wallEnemy);
        }

        activeWalls.Add(entry);
    }

    private void Update()
    {
        if (activeWalls.Count == 0) return;
        if (playerTransform == null)
        {
            if (ObjectPooling.Instance != null)
                playerTransform = ObjectPooling.Instance.targetTransform;
            if (playerTransform == null) return;
        }

        Vector2 playerPos = playerTransform.position;

        for (int w = activeWalls.Count - 1; w >= 0; w--)
        {
            WallEntry entry = activeWalls[w];

            entry.elapsed += Time.deltaTime;
            float duration = Mathf.Max(entry.config.ShrinkDuration, 0.001f);
            float t = Mathf.Clamp01(entry.elapsed / duration);
            entry.currentRadius = Mathf.Lerp(entry.config.StartRadius, entry.config.EndRadius, t);

            for (int i = entry.enemies.Count - 1; i >= 0; i--)
            {
                WallEnemy enemy = entry.enemies[i];

                if (enemy.gameObject == null || !enemy.gameObject.activeInHierarchy)
                {
                    entry.enemies.RemoveAt(i);
                    continue;
                }

                EnemyHealth eh = enemy.gameObject.GetComponentInChildren<EnemyHealth>(true);
                if (eh == null) eh = enemy.gameObject.GetComponentInParent<EnemyHealth>();
                if (eh != null && eh.CurrentHealth <= 0f)
                {
                    RestoreEnemyState(enemy);
                    entry.enemies.RemoveAt(i);
                    continue;
                }

                // Continuously enforce that movement scripts remain disabled
                if (enemy.movements != null)
                {
                    for (int m = 0; m < enemy.movements.Length; m++)
                    {
                        if (enemy.movements[m] != null && enemy.movements[m].enabled)
                            enemy.movements[m].enabled = false;
                    }
                }
                if (enemy.bossControllers != null)
                {
                    for (int b = 0; b < enemy.bossControllers.Length; b++)
                    {
                        if (enemy.bossControllers[b] != null && enemy.bossControllers[b].enabled)
                            enemy.bossControllers[b].enabled = false;
                    }
                }

                // Calculate target position on the circle
                Vector2 pos = entry.center + new Vector2(Mathf.Cos(enemy.angle), Mathf.Sin(enemy.angle)) * entry.currentRadius;

                // Lock position directly onto transform & rigidbodies
                enemy.transform.position = pos;
                if (enemy.rigidbodies != null)
                {
                    for (int r = 0; r < enemy.rigidbodies.Length; r++)
                    {
                        if (enemy.rigidbodies[r] != null)
                        {
                            enemy.rigidbodies[r].velocity = Vector2.zero;
                            enemy.rigidbodies[r].position = pos;
                        }
                    }
                }

                // Attack check
                if (enemy.controller != null && enemy.controller.CanAttack)
                {
                    float dist = Vector2.Distance(pos, playerPos);
                    if (dist <= wallAttackRange)
                        enemy.controller.Attack();
                }
            }

            if (entry.enemies.Count == 0)
            {
                activeWalls.RemoveAt(w);
            }
        }
    }

    private void RestoreEnemyState(WallEnemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;

        if (enemy.rigidbodies != null)
        {
            for (int r = 0; r < enemy.rigidbodies.Length; r++)
            {
                if (enemy.rigidbodies[r] != null)
                {
                    // All enemy prefabs are authored Kinematic (transform-driven,
                    // top-down). Restoring Dynamic + prefab gravityScale 1 made
                    // released wall enemies accelerate downward forever.
                    enemy.rigidbodies[r].bodyType = RigidbodyType2D.Kinematic;
                    enemy.rigidbodies[r].velocity = Vector2.zero;
                }
            }
        }

        if (enemy.movements != null)
        {
            for (int m = 0; m < enemy.movements.Length; m++)
            {
                if (enemy.movements[m] != null)
                {
                    enemy.movements[m].ResetKnockback(); // never launch on stale hits
                    enemy.movements[m].enabled = true;
                }
            }
        }

        if (enemy.bossControllers != null)
        {
            for (int b = 0; b < enemy.bossControllers.Length; b++)
            {
                if (enemy.bossControllers[b] != null)
                    enemy.bossControllers[b].enabled = true;
            }
        }
    }

    public void ClearAll()
    {
        for (int w = activeWalls.Count - 1; w >= 0; w--)
        {
            for (int i = activeWalls[w].enemies.Count - 1; i >= 0; i--)
            {
                WallEnemy enemy = activeWalls[w].enemies[i];
                if (enemy.gameObject != null)
                {
                    RestoreEnemyState(enemy);
                    if (ObjectPooling.Instance != null)
                        ObjectPooling.Instance.Despawn(enemy.gameObject);
                    else
                        enemy.gameObject.SetActive(false);
                }
            }
        }
        activeWalls.Clear();
    }
}
