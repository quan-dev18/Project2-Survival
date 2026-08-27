using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private Camera targetCamera;
    [SerializeField] private StageSO stage;
    [SerializeField] private float spawnMargin = 2f;
    [SerializeField] private int maxSpawned = 50;

    [Header("UI")]
    [SerializeField] private TMP_Text phaseTimerText;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<GameObject> spawnedBosses = new List<GameObject>();
    private readonly List<EntryState> entryStates = new List<EntryState>();
    private readonly List<int> pendingBosses = new List<int>();
    private int phaseIndex;
    private float phaseTimer;
    private bool phaseHasBoss;
    private int bossSpawnedCount;
    private float bossRetryTimer;
    private bool bossRetryWarningShown;
    
    // Global stat scaling (linear: adds flat % per interval)
    private float globalStatTimer;
    private float globalHealthBonusPercent = 0f;
    private float globalSpeedBonusPercent = 0f;
    private float globalAttackBonusPercent = 0f;

    private class EntryState
    {
        public float spawnTimer;
        public readonly List<GameObject> spawned = new List<GameObject>();
    }

    private StageSO.Phase CurrentPhase => stage.Phases[phaseIndex];
    private bool IsFinalPhase => phaseIndex >= stage.Phases.Count - 1;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Start()
    {
        if (stage == null || stage.Phases == null || stage.Phases.Count == 0)
        {
            enabled = false;
            Debug.LogWarning("EnemySpawner: StageSO has no phases, spawning disabled");
            return;
        }
        StartPhase(0);
    }

    private void StartPhase(int index)
    {
        phaseIndex = index;
        phaseTimer = 0f;
        phaseHasBoss = false;
        bossSpawnedCount = 0;
        bossRetryTimer = 0f;
        bossRetryWarningShown = false;
        spawnedBosses.Clear();
        entryStates.Clear();
        pendingBosses.Clear();

        StageSO.Phase phase = CurrentPhase;
        for (int i = 0; i < phase.Enemies.Count; i++)
            entryStates.Add(new EntryState());

        for (int i = 0; i < phase.Enemies.Count; i++)
        {
            StageSO.PhaseEnemy entry = phase.Enemies[i];
            if (!entry.IsBoss) continue;

            phaseHasBoss = true;
            TrySpawnBoss(i);
        }
    }

    private void TrySpawnBoss(int entryIndex)
    {
        StageSO.PhaseEnemy entry = CurrentPhase.Enemies[entryIndex];
        GameObject boss = SpawnEnemy(entry.MobKey, entryStates[entryIndex]);
        if (boss != null)
        {
            spawnedBosses.Add(boss);
            bossSpawnedCount++;
            pendingBosses.Remove(entryIndex);
        }
        else
        {
            if (!pendingBosses.Contains(entryIndex))
                pendingBosses.Add(entryIndex);
            if (!bossRetryWarningShown)
            {
                bossRetryWarningShown = true;
                Debug.LogWarning($"EnemySpawner: boss '{entry.MobKey}' failed to spawn — check it is registered in ObjectPooling with that key");
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        CleanupActiveEnemies();
        UpdatePhaseTimerText();
        phaseTimer += Time.deltaTime;

        // Global stat scaling (linear: adds flat % per interval)
        if (stage != null)
        {
            globalStatTimer += Time.deltaTime;
            float interval = stage.StatsIncrementInterval;
            if (interval > 0f && globalStatTimer >= interval)
            {
                globalStatTimer = 0f;
                globalHealthBonusPercent += stage.HealthIncrementPercent;
                globalSpeedBonusPercent += stage.SpeedIncrementPercent;
                globalAttackBonusPercent += stage.AttackIncrementPercent;
                
                Debug.Log($"[EnemySpawner] Global stats increased — Health: +{stage.HealthIncrementPercent}%, Speed: +{stage.SpeedIncrementPercent}%, Attack: +{stage.AttackIncrementPercent}% | Total: Health {globalHealthBonusPercent}%, Speed {globalSpeedBonusPercent}%, Attack {globalAttackBonusPercent}%");
            }
        }

        if (pendingBosses.Count > 0)
            TryRetryPendingBosses();

        if (CurrentPhase.Duration > 0f && phaseTimer >= CurrentPhase.Duration)
        {
            if (IsFinalPhase)
            {
                Lose();
                return;
            }

            StartPhase(phaseIndex + 1);
            return;
        }

        if (activeEnemies.Count < maxSpawned)
            UpdateSpawning();

        CheckEndCondition();
    }

    private void CleanupActiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null || !activeEnemies[i].activeInHierarchy)
                activeEnemies.RemoveAt(i);
        }

        for (int i = 0; i < entryStates.Count; i++)
        {
            List<GameObject> spawned = entryStates[i].spawned;
            for (int j = spawned.Count - 1; j >= 0; j--)
            {
                if (spawned[j] == null || !spawned[j].activeInHierarchy)
                    spawned.RemoveAt(j);
            }
        }
    }

    private void UpdatePhaseTimerText()
    {
        if (phaseTimerText == null) return;

        float remaining = Mathf.Max(0f, CurrentPhase.Duration - phaseTimer);
        phaseTimerText.text = $"Wave {phaseIndex + 1}  {remaining:0}s";
    }

    private void UpdateSpawning()
    {
        StageSO.Phase phase = CurrentPhase;
        for (int i = 0; i < phase.Enemies.Count && i < entryStates.Count; i++)
        {
            StageSO.PhaseEnemy entry = phase.Enemies[i];
            if (entry.IsBoss) continue;

            EntryState state = entryStates[i];
            if (entry.SpawnLimit > 0 && state.spawned.Count >= entry.SpawnLimit)
                continue;

            state.spawnTimer += entry.SpawnPerSecond * Time.deltaTime;
            while (state.spawnTimer >= 1f && (entry.SpawnLimit == 0 || state.spawned.Count < entry.SpawnLimit))
            {
                state.spawnTimer -= 1f;
                if (activeEnemies.Count >= maxSpawned) return;

                SpawnEnemy(entry.MobKey, state);
            }
        }
    }

    private void TryRetryPendingBosses()
    {
        bossRetryTimer += Time.deltaTime;
        if (bossRetryTimer < 1f) return;

        bossRetryTimer = 0f;
        for (int i = pendingBosses.Count - 1; i >= 0; i--)
            TrySpawnBoss(pendingBosses[i]);
    }

    private void CheckEndCondition()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
        if (!IsFinalPhase) return;

        for (int i = spawnedBosses.Count - 1; i >= 0; i--)
        {
            if (spawnedBosses[i] == null || !spawnedBosses[i].activeInHierarchy)
                spawnedBosses.RemoveAt(i);
        }

        bool bossesCleared = spawnedBosses.Count == 0;
        bool allEnemiesCleared = activeEnemies.Count == 0;

        bool bossPhaseActive = phaseHasBoss && bossSpawnedCount > 0;

        if (bossPhaseActive ? bossesCleared : allEnemiesCleared)
        {
            Debug.Log("Stage cleared!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetIsWin(true);
                GameManager.Instance.SetState(GameState.GameOver);
            }
        }
    }

    private void Lose()
    {
        Debug.Log("Timer ran out!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetIsWin(false);
            GameManager.Instance.SetState(GameState.GameOver);
        }
    }

    private GameObject SpawnEnemy(string mobKey, EntryState state)
    {
        Vector2 spawnPos = GetSpawnPosition();
        GameObject enemy = ObjectPooling.Instance.Spawn(mobKey, spawnPos, Quaternion.identity);
        if (enemy != null)
        {
            // Apply global stat scaling (linear: flat % bonus)
            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                // Reset bonuses from previous spawn, then apply current global values
                ec.ResetBonuses();
                if (globalHealthBonusPercent > 0f)
                    ec.AddMaxHealthPercent(globalHealthBonusPercent / 100f);
                if (globalSpeedBonusPercent > 0f)
                    ec.AddMovementSpeedPercent(globalSpeedBonusPercent / 100f);
                if (globalAttackBonusPercent > 0f)
                    ec.AddAttackDamagePercent(globalAttackBonusPercent / 100f);

                enemy.GetComponent<EnemyColorVariant>()?.ApplyRandomColor();
                
                Debug.Log($"[EnemySpawner] Spawned {mobKey} with global buffs — Health: {ec.maxHealth:0}, Speed: {ec.movementSpeed:0.00}, Attack: {ec.attackDamage:0} (Total buffs: Health +{globalHealthBonusPercent}%, Speed +{globalSpeedBonusPercent}%, Attack +{globalAttackBonusPercent}%)");
            }

            activeEnemies.Add(enemy);
            state.spawned.Add(enemy);
        }
        return enemy;
    }

    private Vector2 GetSpawnPosition()
    {
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;
        float radius = Mathf.Max(halfW, halfH) + spawnMargin;

        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        Vector3 camPos = targetCamera.transform.position;
        return new Vector2(camPos.x + offset.x, camPos.y + offset.y);
    }
}