using UnityEngine;

public class ChunkTrigger : MonoBehaviour
{
    private MapController mapController;
    [SerializeField] public GameObject TargetMap;

    private void Start()
    {
        mapController = FindObjectOfType<MapController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && mapController != null)
        {
            mapController.currentChunk = TargetMap;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && mapController != null)
        {   
            if (mapController.currentChunk == TargetMap)
            {
                mapController.currentChunk = null;
            }
        }
    }
}
