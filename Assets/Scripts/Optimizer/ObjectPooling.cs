using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling Instance { get; private set; }


    public Transform targetTransform;
    [System.Serializable]
    public class PoolSetup
    {
        public string key;
        public GameObject prefab;
        public int count = 10;
    }

    [SerializeField] private PoolSetup[] setups;
    [SerializeField] private bool autoGrow = true;

    private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (setups != null)
        {
            foreach (var setup in setups)
            {
                if (setup.prefab == null) continue;
                string key = string.IsNullOrEmpty(setup.key) ? setup.prefab.name : setup.key;
                prefabMap[key] = setup.prefab;
                for (int i = 0; i < setup.count; i++)
                    Despawn(CreateNew(key, setup.prefab));
            }
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot)
    {
        if (!prefabMap.TryGetValue(key, out var prefab))
        {
            Debug.LogWarning($"ObjectPooling: no prefab registered for key '{key}'");
            return null;
        }
        return Spawn(key, prefab, pos, rot);
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        return Spawn(prefab.name, prefab, pos, rot);
    }

    public GameObject Spawn(string key, GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!pools.TryGetValue(key, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[key] = queue;
        }
        if (!prefabMap.ContainsKey(key))
            prefabMap[key] = prefab;

        GameObject obj;
        if (queue.Count > 0)
            obj = queue.Dequeue();
        else if (autoGrow)
            obj = CreateNew(key, prefab);
        else
            return null;

        if (obj.TryGetComponent(out PooledObject pooledObj))
            pooledObj.InPool = false;

        obj.SetActive(true);
        obj.transform.SetPositionAndRotation(pos, rot);
        if (obj.TryGetComponent(out IPoolSpawnable spawnable))
            spawnable.OnSpawned();
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        if (!obj.TryGetComponent(out PooledObject pooled))
        {
            Destroy(obj);
            return;
        }

        // Already returned to this pool: skip to avoid enqueueing the same
        // instance twice (which makes two Spawns hand out one GameObject and
        // makes bullets "vanish" mid-flight).
        if (pooled.InPool) return;

        if (!pools.TryGetValue(pooled.Key, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[pooled.Key] = queue;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform, false);
        pooled.InPool = true;
        queue.Enqueue(obj);
    }

    private GameObject CreateNew(string key, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        if (!obj.TryGetComponent(out PooledObject pooled))
            pooled = obj.AddComponent<PooledObject>();
        pooled.Key = key;
        return obj;
    }
}

public class PooledObject : MonoBehaviour
{
    public string Key;
    public bool InPool;
}
public interface IPoolSpawnable
{
    void OnSpawned();
}