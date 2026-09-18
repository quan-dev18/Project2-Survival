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

    [SerializeField] private float chunkSize = 20f;
    private readonly Dictionary<Vector2Int, GameObject> spawnedChunkMap = new Dictionary<Vector2Int, GameObject>();

    private static readonly string[] s_AllDirections = {
        "Right", "RightUp", "Up", "LeftUp", "Left", "LeftDown", "Down", "RightDown"
    };

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
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        // Đăng ký các chunk đã có sẵn trong Scene vào Grid Map
        if (SpawnedChunks != null)
        {
            foreach (GameObject chunk in SpawnedChunks)
            {
                if (chunk != null)
                {
                    Vector2Int coord = GetChunkCoord(chunk.transform.position);
                    spawnedChunkMap[coord] = chunk;
                }
            }
        }

        // Tự động gán chunk gốc nếu chưa được gán
        if (currentChunk == null && SpawnedChunks != null && SpawnedChunks.Count > 0)
        {
            currentChunk = SpawnedChunks[0];
        }

        // Tạo sẵn các chunk xung quanh người chơi ngay khi vào Scene
        PreSpawnStartingChunks();
    }

    public Vector2Int GetChunkCoord(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / chunkSize);
        int y = Mathf.RoundToInt(pos.y / chunkSize);
        return new Vector2Int(x, y);
    }

    private void PreSpawnStartingChunks()
    {
        if (currentChunk == null) return;

        for (int i = 0; i < s_AllDirections.Length; i++)
        {
            Transform spawnPoint = currentChunk.transform.Find(s_AllDirections[i]);
            if (spawnPoint != null)
            {
                Vector2Int coord = GetChunkCoord(spawnPoint.position);
                if (!spawnedChunkMap.ContainsKey(coord))
                {
                    SpawnChunkAt(spawnPoint.position, coord);
                }
            }
        }
    }

    void Update()
    {
        ChunkChecker();
        ChunkOptimizer();
    }

    void ChunkChecker()
    {
        if (!currentChunk)
        {
            return;
        }

        Vector2 input = playerMovement != null ? playerMovement.movementInput : Vector2.zero;
        if (input.sqrMagnitude < 0.01f) return;

        string[] directions = GetChunkDirections(input);
        for (int d = 0; d < directions.Length; d++)
        {
            Transform spawnPoint = currentChunk.transform.Find(directions[d]);
            if (spawnPoint != null)
            {
                Vector2Int coord = GetChunkCoord(spawnPoint.position);
                if (spawnedChunkMap.TryGetValue(coord, out GameObject existingChunk))
                {
                    if (existingChunk != null)
                    {
                        // Chunk cũ đã tồn tại (có thể bị deactive khi đi xa), chỉ cần bật lại
                        if (!existingChunk.activeSelf)
                        {
                            existingChunk.SetActive(true);
                        }
                    }
                    else
                    {
                        // Đã bị hủy ngoài ý muốn -> spawn lại
                        SpawnChunkAt(spawnPoint.position, coord);
                    }
                }
                else
                {
                    // Vị trí mới chưa có chunk -> sinh mới
                    SpawnChunkAt(spawnPoint.position, coord);
                }
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

    void SpawnChunkAt(Vector3 pos, Vector2Int coord)
    {
        if (terrainChunks == null || terrainChunks.Count == 0) return;

        int randChunk = Random.Range(0, terrainChunks.Count);
        lastChunk = Instantiate(terrainChunks[randChunk], pos, Quaternion.identity);
        lastChunk.transform.parent = transform;
        SpawnedChunks.Add(lastChunk);
        spawnedChunkMap[coord] = lastChunk;
    }

    void ChunkOptimizer()
    {
        OptimizerCooldown -= Time.deltaTime;
        if (OptimizerCooldown > 0f) return;
        OptimizerCooldown = OptimizerCooldownDuration;

        if (player == null) return;
        Vector3 playerPos = player.transform.position;

        for (int i = 0; i < SpawnedChunks.Count; i++)
        {
            GameObject chunk = SpawnedChunks[i];
            if (chunk == null) continue;

            OpDistance = Vector3.Distance(playerPos, chunk.transform.position);
            chunk.SetActive(OpDistance <= maxDistace);
        }
    }
}
