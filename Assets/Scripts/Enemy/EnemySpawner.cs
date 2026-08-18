using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private Camera targetCamera;
    [SerializeField] private StageSO stage;
    [SerializeField] private float spawnMargin = 2f;
    [SerializeField] private int maxSpawned = 50;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private int stageIndex;
    private float stageTimer;
    private float entryElapsed;
    private float totalElapsed;
    private float spawnTimer;
    private float currentSpawnRate;

    private StageSO.StageEntry CurrentEntry => stage.Stages[stageIndex];

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Start()
    {
        if (stage == null || stage.Stages == null || stage.Stages.Count == 0)
        {
            enabled = false;
            Debug.LogWarning("EnemySpawner: StageSO has no stages, spawning disabled");
            return;
        }
        ResetStageEntry();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null || !activeEnemies[i].activeInHierarchy)
                activeEnemies.RemoveAt(i);
        }

        if (activeEnemies.Count >= maxSpawned) return;

        stageTimer += Time.deltaTime;
        entryElapsed += Time.deltaTime;
        totalElapsed += Time.deltaTime;

        UpdateSpawnRate();
        TryAdvanceEntry();

        spawnTimer += currentSpawnRate * Time.deltaTime;
        int spawnCount = Mathf.FloorToInt(spawnTimer);
        if (spawnCount > 0)
        {
            spawnTimer -= spawnCount;
            for (int i = 0; i < spawnCount; i++)
                SpawnEnemy();
        }
    }

    private void ResetStageEntry()
    {
        stageTimer = 0f;
        entryElapsed = 0f;
        spawnTimer = 0f;
    }

    private void UpdateSpawnRate()
    {
        StageSO.StageEntry entry = CurrentEntry;
        if (entry.SpawnIncrementInterval <= 0f) return;

        float increments = Mathf.Floor(totalElapsed / entry.SpawnIncrementInterval);
        currentSpawnRate = entry.SpawnPerSecond + increments * entry.SpawnIncrementAmount;
    }

    private void TryAdvanceEntry()
    {
        StageSO.StageEntry entry = CurrentEntry;
        if (entry.Duration > 0f && stageTimer >= entry.Duration)
        {
            stageIndex = (stageIndex + 1) % stage.Stages.Count;
            ResetStageEntry();
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetSpawnPosition();
        GameObject enemy = ObjectPooling.Instance.Spawn(CurrentEntry.MobKey, spawnPos, Quaternion.identity);
        if (enemy != null)
            activeEnemies.Add(enemy);
    }

    private Vector2 GetSpawnPosition()
    {
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;
        float radius = Mathf.Max(halfW, halfH) + spawnMargin;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        Vector3 camPos = targetCamera.transform.position;
        return new Vector2(camPos.x + offset.x, camPos.y + offset.y);
    }
}
