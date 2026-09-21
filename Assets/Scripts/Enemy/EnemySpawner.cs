using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private Camera targetCamera;
    [SerializeField] private StageSO stage;
    [SerializeField] private float spawnMargin = 2f;
    [SerializeField] private int maxSpawned = 50;
    [SerializeField] private bool autoStart = true;
    public bool AutoStart { get => autoStart; set => autoStart = value; }

    [Header("UI")]
    [SerializeField] private TMP_Text phaseTimerText;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<GameObject> spawnedBosses = new List<GameObject>();
    private readonly List<GameObject> spawnedElites = new List<GameObject>();
    private readonly List<EntryState> entryStates = new List<EntryState>();
    private readonly List<int> pendingBosses = new List<int>();
    private readonly List<int> pendingElites = new List<int>();
    private int phaseIndex;
    private float phaseTimer;
    private float totalTime; // Total elapsed time for the entire run
    private bool phaseHasBoss;
    private int bossSpawnedCount;
    private float bossRetryTimer;
    private float eliteRetryTimer;
    private bool bossRetryWarningShown;
    private bool eliteRetryWarningShown;
    private int lastDisplayedMinutes = -1;
    private int lastDisplayedSeconds = -1;

    public event System.Action<EnemyHealth> OnBossSpawned;
    public event System.Action<int> OnInfiniteLoop;

    // Infinite mode: loop counter + spawn rate multiplier
    private int infiniteLoopCount;
    private float infiniteLoopTimer;
    private float infiniteSpawnRateMult = 1f;

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

        // Nếu là scene GameTutorial, không tự động sinh quái để chờ giai đoạn hướng dẫn hoàn tất
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameTutorial")
        {
            autoStart = false;
        }
    }

    private void Start()
    {
        if (!autoStart) return;
        BeginSpawning();
    }

    public void BeginSpawning()
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
        // Only reset totalTime at the very start of a run (first phase), not on phase transitions
        if (index == 0) totalTime = 0f;
        phaseHasBoss = false;
        bossSpawnedCount = 0;
        bossRetryTimer = 0f;
        eliteRetryTimer = 0f;
        bossRetryWarningShown = false;
        eliteRetryWarningShown = false;
        spawnedBosses.Clear();
        spawnedElites.Clear();
        entryStates.Clear();
        pendingBosses.Clear();
        pendingElites.Clear();

        StageSO.Phase phase = CurrentPhase;
        for (int i = 0; i < phase.Enemies.Count; i++)
            entryStates.Add(new EntryState());

        for (int i = 0; i < phase.Enemies.Count; i++)
        {
            StageSO.PhaseEnemy entry = phase.Enemies[i];
            if (entry.IsBoss)
            {
                phaseHasBoss = true;
                TrySpawnBoss(i);
            }
            else if (entry.IsElite)
            {
                TrySpawnElite(i);
            }
        }

        if (CircleWallManager.Instance != null)
            CircleWallManager.Instance.TryActivate(stage, index);
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
            var bh = boss.GetComponentInChildren<EnemyHealth>(true);
            if (bh == null) bh = boss.GetComponent<EnemyHealth>();
            if (bh != null) OnBossSpawned?.Invoke(bh);
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

    private void TrySpawnElite(int entryIndex)
    {
        StageSO.PhaseEnemy entry = CurrentPhase.Enemies[entryIndex];
        GameObject elite = SpawnEnemy(entry.MobKey, entryStates[entryIndex]);
        if (elite != null)
        {
            spawnedElites.Add(elite);
            pendingElites.Remove(entryIndex);
        }
        else
        {
            if (!pendingElites.Contains(entryIndex))
                pendingElites.Add(entryIndex);
            if (!eliteRetryWarningShown)
            {
                eliteRetryWarningShown = true;
                Debug.LogWarning($"EnemySpawner: elite '{entry.MobKey}' failed to spawn — check it is registered in ObjectPooling with that key");
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        CleanupActiveEnemies();
        UpdatePhaseTimerText();
        totalTime += Time.deltaTime;
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
            }
        }

        if (pendingBosses.Count > 0)
            TryRetryPendingBosses();
        if (pendingElites.Count > 0)
            TryRetryPendingElites();

        if (CurrentPhase.Duration > 0f && phaseTimer >= CurrentPhase.Duration)
        {
            if (IsFinalPhase)
            {
                if (stage != null && stage.IsInfinite)
                {
                    LoopInfinitePhase();
                    return;
                }
                Lose();
                return;
            }

            // Đã vượt qua xong wave hiện tại (index phaseIndex, tính từ 0)
            // => cập nhật kỷ lục: số wave người chơi đã qua = phaseIndex + 1.
            SaveBestProgress(phaseIndex + 1);

            StartPhase(phaseIndex + 1);
            return;
        }

        if (activeEnemies.Count < maxSpawned)
            UpdateSpawning();

        CheckEndCondition();
    }

    private void LoopInfinitePhase()
    {
        infiniteLoopCount++;
        float step = stage != null ? stage.InfiniteDifficultyStep : 10f;
        float frac = step / 100f;
        globalHealthBonusPercent += step;
        globalSpeedBonusPercent += step;
        globalAttackBonusPercent += step;
        infiniteSpawnRateMult *= (1f + frac);
        phaseTimer = 0f;
        // Refresh entry spawn timers so loop doesn't burst
        foreach (var s in entryStates) s.spawnTimer = 0f;
        OnInfiniteLoop?.Invoke(infiniteLoopCount);
        SaveBestProgress(stage != null ? stage.Phases.Count + infiniteLoopCount : infiniteLoopCount);
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

        int minutes = Mathf.FloorToInt(totalTime / 60f);
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        if (minutes != lastDisplayedMinutes || seconds != lastDisplayedSeconds)
        {
            lastDisplayedMinutes = minutes;
            lastDisplayedSeconds = seconds;
            phaseTimerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void UpdateSpawning()
    {
        StageSO.Phase phase = CurrentPhase;
        for (int i = 0; i < phase.Enemies.Count && i < entryStates.Count; i++)
        {
            StageSO.PhaseEnemy entry = phase.Enemies[i];
            if (entry.IsBoss || entry.IsElite) continue;

            EntryState state = entryStates[i];
            if (entry.SpawnLimit > 0 && state.spawned.Count >= entry.SpawnLimit)
                continue;

            state.spawnTimer += entry.SpawnPerSecond * infiniteSpawnRateMult * Time.deltaTime;
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

    private void TryRetryPendingElites()
    {
        eliteRetryTimer += Time.deltaTime;
        if (eliteRetryTimer < 1f) return;

        eliteRetryTimer = 0f;
        for (int i = pendingElites.Count - 1; i >= 0; i--)
            TrySpawnElite(pendingElites[i]);
    }

    private void CheckEndCondition()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
        if (!IsFinalPhase) return;
        // Infinite stages never win - loop handles difficulty
        if (stage != null && stage.IsInfinite) return;

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Stage cleared!");
#endif
            if (GameManager.Instance != null)
            {
                // Thắng stage (boss đã bị tiêu diệt) => đạt 100% tiến trình.
                SaveBestProgress(stage.MaxProgress);

                GameManager.Instance.SetIsWin(true);
                GameManager.Instance.SetState(GameState.GameOver);
            }
        }
    }

    /// <summary>
    /// Ghi nhận kỷ lục tiến trình cao nhất cho Stage hiện tại.
    /// Chỉ ghi nếu cao hơn kỷ lục cũ (logic nằm trong UserData.SetStageBestProgress).
    /// </summary>
    /// <param name="progress">Giá trị tiến trình đạt được (vd: số wave đã qua, hoặc MaxProgress khi thắng boss).</param>
    private void SaveBestProgress(float progress)
    {
        if (stage == null || string.IsNullOrEmpty(stage.StageID)) return;
        if (UserData.Instance == null) return;

        UserData.Instance.SetStageBestProgress(stage.StageID, progress);
    }

    private void Lose()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Timer ran out!");
#endif
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