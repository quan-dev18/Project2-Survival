using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> propsLocation;
    [SerializeField]
    private List<GameObject> props;

    private readonly List<GameObject> spawnedProps = new List<GameObject>();

    private void Start()
    {
        SpawnProps();
    }

    public void SpawnProps()
    {
        if (props == null || props.Count == 0 || propsLocation == null) return;

        foreach (GameObject spawnLocation in propsLocation)
        {
            if (spawnLocation == null) continue;
            // Tránh spawn đè nếu vị trí đã có prop
            if (spawnLocation.transform.childCount > 0) continue;

            int randomIndex = Random.Range(0, props.Count);
            GameObject propPrefab = props[randomIndex];
            if (propPrefab == null) continue;

            GameObject prop;
            if (ObjectPooling.Instance != null)
            {
                prop = ObjectPooling.Instance.Spawn(propPrefab, spawnLocation.transform.position, Quaternion.identity);
            }
            else
            {
                prop = Instantiate(propPrefab, spawnLocation.transform.position, Quaternion.identity);
            }

            if (prop != null)
            {
                prop.transform.SetParent(spawnLocation.transform, true);
                spawnedProps.Add(prop);
            }
        }
    }

    private void OnDestroy()
    {
        if (ObjectPooling.Instance != null)
        {
            foreach (GameObject prop in spawnedProps)
            {
                if (prop != null && prop.transform.IsChildOf(transform))
                {
                    ObjectPooling.Instance.Despawn(prop);
                }
            }
        }
        spawnedProps.Clear();
    }
}
