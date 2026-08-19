using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkTrigger : MonoBehaviour
{
    MapController mapController;
    [SerializeField] public GameObject TargetMap;
    void Start()
    {
        mapController = FindObjectOfType<MapController>();
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            mapController.currentChunk = TargetMap;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {   
            if(mapController.currentChunk == TargetMap)
            {
                mapController.currentChunk = null;
            }
        }
    }

}
