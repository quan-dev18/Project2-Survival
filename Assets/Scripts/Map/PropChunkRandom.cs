using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    [SerializeField]
    List<GameObject> propsLocation;
    [SerializeField]
    List<GameObject> props;

    void Start()
    {
        SpawnProps();
    }
    void SpawnProps()
    {
        foreach(GameObject spawnLocation in propsLocation)
        {
            int randomIndex = Random.Range(0, props.Count);
            GameObject prop = Instantiate(props[randomIndex], spawnLocation.transform.position, Quaternion.identity);
            prop.transform.parent = spawnLocation.transform;
        }
    }
}
