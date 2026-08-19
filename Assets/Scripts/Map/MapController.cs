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
        if(playerMovement.movementInput.x > 0 && playerMovement.movementInput.y == 0) //right
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("Right").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("Right").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x < 0 && playerMovement.movementInput.y == 0) //left
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("Left").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("Left").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x == 0 && playerMovement.movementInput.y > 0) //up
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("Up").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("Up").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x == 0 && playerMovement.movementInput.y < 0) //down
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("Down").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("Down").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x > 0 && playerMovement.movementInput.y > 0) //right up
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("RightUp").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("RightUp").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x > 0 && playerMovement.movementInput.y < 0) //right down
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("RightDown").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("RightDown").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x < 0 && playerMovement.movementInput.y > 0) //left up
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("LeftUp").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("LeftUp").position;
                SpawnChunk();
            }
        }
        else if(playerMovement.movementInput.x < 0 && playerMovement.movementInput.y < 0) //left down
        {
            if(!Physics2D.OverlapCircle(currentChunk.transform.Find("LeftDown").position, checkerRadius, terrainLayer))
            {
                noTerrainChunk = currentChunk.transform.Find("LeftDown").position;
                SpawnChunk();
            }
        }
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
