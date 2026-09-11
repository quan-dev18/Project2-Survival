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
    [SerializeField] private LayerMask terrainLayer;
    PlayerMovement playerMovement;

    [Header("Optimization")]
    [SerializeField] private List<GameObject> SpawnedChunks;
    GameObject lastChunk;
    [SerializeField] private float maxDistace; //must be greater than the length of the chunk
    float OpDistance;
    float OptimizerCooldown;
    [SerializeField] private float OptimizerCooldownDuration;

    private static readonly string[] s_DirRight = { "Right", "RightUp", "RightDown" };
    private static readonly string[] s_DirRightUp = { "RightUp", "Right", "Up" };
    private static readonly string[] s_DirUp = { "Up", "RightUp", "LeftUp" };
    private static readonly string[] s_DirLeftUp = { "LeftUp", "Up", "Left" };
    private static readonly string[] s_DirLeft = { "Left", "LeftUp", "LeftDown" };
    private static readonly string[] s_DirLeftDown = { "LeftDown", "Left", "Down" };
    private static readonly string[] s_DirDown = { "Down", "LeftDown", "RightDown" };
    private static readonly string[] s_DirRightDown = { "RightDown", "Down", "Right" };

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

        string[] directions = GetChunkDirections(input);
        for (int d = 0; d < directions.Length; d++)
        {
            Transform spawnPoint = currentChunk.transform.Find(directions[d]);
            if (spawnPoint != null &&
                !Physics2D.OverlapCircle(spawnPoint.position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = spawnPoint.position;
                SpawnChunk();
            }
        }
    }

    string[] GetChunkDirections(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        if (angle < 22.5f || angle >= 337.5f) return s_DirRight;
        if (angle < 67.5f) return s_DirRightUp;
        if (angle < 112.5f) return s_DirUp;
        if (angle < 157.5f) return s_DirLeftUp;
        if (angle < 202.5f) return s_DirLeft;
        if (angle < 247.5f) return s_DirLeftDown;
        if (angle < 292.5f) return s_DirDown;
        return s_DirRightDown;
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
