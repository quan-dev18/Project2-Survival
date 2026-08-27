using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private List<GameObject> terrainChunks;
    [SerializeField] private GameObject player;
    [SerializeField] private float checkerRadius;
    public GameObject currentChunk;
    Vector3 noTerrainChunk;
    public LayerMask terrainLayer;
    PlayerMovement playerMovement;

    [Header("Optimization")]
    public List<GameObject> SpawnedChunks;
    GameObject lastChunk;
    public float maxDistace; //must be greater than the length of the chunk
    float OpDistance;
    float OptimizerCooldown;
    public float OptimizerCooldownDuration;

    void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        ChunkChecker();
        ChunkOptimizer();
    }
    void ChunkChecker()
    {
        if(!currentChunk)
        {
            return;
        }

        Vector2 input = playerMovement.movementInput;
        if (input.sqrMagnitude < 0.01f) return;

        string direction = GetChunkDirection(input);
        Transform spawnPoint = currentChunk.transform.Find(direction);
        if (spawnPoint != null &&
            !Physics2D.OverlapCircle(spawnPoint.position, checkerRadius, terrainLayer))
        {
            noTerrainChunk = spawnPoint.position;
            SpawnChunk();
        }
    }

    string GetChunkDirection(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        if (angle < 22.5f || angle >= 337.5f) return "Right";
        if (angle < 67.5f)  return "RightUp";
        if (angle < 112.5f) return "Up";
        if (angle < 157.5f) return "LeftUp";
        if (angle < 202.5f) return "Left";
        if (angle < 247.5f) return "LeftDown";
        if (angle < 292.5f) return "Down";
        return "RightDown";
    }
    void SpawnChunk()
    {
        int randChunk = Random.Range(0, terrainChunks.Count);
        lastChunk = Instantiate(terrainChunks[randChunk], noTerrainChunk, Quaternion.identity);
        lastChunk.transform.parent = transform;
    }
    void ChunkOptimizer()
    {
        OptimizerCooldown -= Time.deltaTime;
        if(OptimizerCooldown <= 0f)
        {
            OptimizerCooldown = OptimizerCooldownDuration;
        }
        else
        {
            return;
        }
        foreach(GameObject chunk in SpawnedChunks)
        {
            OpDistance = Vector3.Distance(player.transform.position, chunk.transform.position);
            if(OpDistance > maxDistace)
            {
                chunk.SetActive(false);
            }
            else
            {
                chunk.SetActive(true);
            }
        }
    }
}
