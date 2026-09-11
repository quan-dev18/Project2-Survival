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

        List<string> directions = GetChunkDirections(input);
        foreach(string dir in directions)
        {
            Transform spawnPoint = currentChunk.transform.Find(dir);
            if (spawnPoint != null &&
                !Physics2D.OverlapCircle(spawnPoint.position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = spawnPoint.position;
                SpawnChunk();
            }
        }
    }

    List<string> GetChunkDirections(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        if (angle < 22.5f || angle >= 337.5f)
            return new List<string> { "Right", "RightUp", "RightDown" };
        if (angle < 67.5f)
            return new List<string> { "RightUp", "Right", "Up" };
        if (angle < 112.5f)
            return new List<string> { "Up", "RightUp", "LeftUp" };
        if (angle < 157.5f)
            return new List<string> { "LeftUp", "Up", "Left" };
        if (angle < 202.5f)
            return new List<string> { "Left", "LeftUp", "LeftDown" };
        if (angle < 247.5f)
            return new List<string> { "LeftDown", "Left", "Down" };
        if (angle < 292.5f)
            return new List<string> { "Down", "LeftDown", "RightDown" };
        return new List<string> { "RightDown", "Down", "Right" };
    }
    void SpawnChunk()
    {
        int randChunk = Random.Range(0, terrainChunks.Count);
        lastChunk = Instantiate(terrainChunks[randChunk], noTerrainChunk, Quaternion.identity);
        lastChunk.transform.parent = transform;
        SpawnedChunks.Add(lastChunk);
    }
    void ChunkOptimizer()
    {
        OptimizerCooldown -= Time.deltaTime;
        if(OptimizerCooldown > 0f) return;
        OptimizerCooldown = OptimizerCooldownDuration;

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
