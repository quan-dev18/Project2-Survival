using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private Camera targetCamera;
    [SerializeField] private string enemyKey = "Enemy";
    [SerializeField] private float spawnMargin = 2f;
    [SerializeField] private float spawnInterval = 0.2f;
    [SerializeField] private int maxSpawned = 50;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private float timer;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null || !activeEnemies[i].activeInHierarchy)
                activeEnemies.RemoveAt(i);
        }

        if (activeEnemies.Count >= maxSpawned) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetSpawnPosition();
        GameObject enemy = ObjectPooling.Instance.Spawn(enemyKey, spawnPos, Quaternion.identity);
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