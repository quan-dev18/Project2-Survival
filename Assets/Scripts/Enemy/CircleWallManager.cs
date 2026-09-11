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
        public EnemyController controller;
        public Rigidbody2D rb;
        public float angle;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerTransform = ObjectPooling.Instance.targetTransform;
    }

    public void TryActivate(StageSO stage, int phaseIndex)
    {
        if (stage == null) return;
        if (phaseIndex < 0 || phaseIndex >= stage.Phases.Count) return;

        StageSO.CircleWallConfig config = stage.Phases[phaseIndex].CircleWall;
        if (config == null || !config.Enabled) return;

        ActivateWall(config);
    }

    private void ActivateWall(StageSO.CircleWallConfig config)
    {
        if (playerTransform == null)
            playerTransform = ObjectPooling.Instance.targetTransform;
        if (playerTransform == null) return;

        WallEntry entry = new WallEntry
        {
            config = config,
            currentRadius = config.StartRadius,
            elapsed = 0f,
            center = playerTransform.position
        };

        float angleStep = 360f / config.WallEnemyCount;
        for (int i = 0; i < config.WallEnemyCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 pos = (Vector2)playerTransform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * config.StartRadius;

            GameObject obj = ObjectPooling.Instance.Spawn(config.WallEnemyKey, pos, Quaternion.identity);
            if (obj == null) continue;

            EnemyController ec = obj.GetComponent<EnemyController>();
            EnemyMovement em = obj.GetComponent<EnemyMovement>();
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

            if (ec != null)
            {
                ec.ResetBonuses();
                float hpBonus = (config.WallEnemyHP - ec.baseMaxHealth) / Mathf.Max(ec.baseMaxHealth, 0.01f);
                if (hpBonus > 0f)
                    ec.AddMaxHealthPercent(hpBonus);
                ec.SetCurrentHealth(ec.maxHealth);
            }

            if (em != null)
                em.enabled = false;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            WallEnemy wallEnemy = new WallEnemy
            {
                gameObject = obj,
                controller = ec,
                rb = rb,
                angle = angle
            };

            entry.enemies.Add(wallEnemy);
        }

        activeWalls.Add(entry);
    }

    private void Update()
    {
        if (activeWalls.Count == 0) return;
        if (playerTransform == null) return;

        for (int w = activeWalls.Count - 1; w >= 0; w--)
        {
            WallEntry entry = activeWalls[w];

            entry.elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(entry.elapsed / entry.config.ShrinkDuration);
            entry.currentRadius = Mathf.Lerp(entry.config.StartRadius, entry.config.EndRadius, t);

            Vector2 playerPos = playerTransform.position;

            for (int i = entry.enemies.Count - 1; i >= 0; i--)
            {
                WallEnemy enemy = entry.enemies[i];

                if (enemy.gameObject == null || !enemy.gameObject.activeInHierarchy)
                {
                    entry.enemies.RemoveAt(i);
                    continue;
                }

                EnemyHealth eh = enemy.gameObject.GetComponent<EnemyHealth>();
                if (eh != null && eh.CurrentHealth <= 0f)
                {
                    entry.enemies.RemoveAt(i);
                    continue;
                }

                Vector2 pos = entry.center + new Vector2(Mathf.Cos(enemy.angle), Mathf.Sin(enemy.angle)) * entry.currentRadius;
                enemy.rb.MovePosition(pos);

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

    public void ClearAll()
    {
        for (int w = activeWalls.Count - 1; w >= 0; w--)
        {
            for (int i = activeWalls[w].enemies.Count - 1; i >= 0; i--)
            {
                if (activeWalls[w].enemies[i].gameObject != null)
                    activeWalls[w].enemies[i].gameObject.SetActive(false);
            }
        }
        activeWalls.Clear();
    }
}
