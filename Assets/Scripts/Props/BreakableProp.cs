using System.Collections.Generic;
using UnityEngine;

public class BreakableProp : MonoBehaviour, IDamageable, IPoolSpawnable
{
    [SerializeField] private float hp = 3f;
    [SerializeField] private List<PropDropEntry> drops;
    [SerializeField] private float dropChance = 1f;
    [SerializeField] private float spreadRadius = 0.5f;

    private float currentHp;

    private void Awake()
    {
        currentHp = hp;
    }

    public void OnSpawned()
    {
        currentHp = hp;
    }

    public void TakeDamage(float amount)
    {
        // Late hit on a despawned prop (e.g. mid-loop AOE kill): ignore.
        if (!gameObject.activeInHierarchy) return;
        currentHp -= amount;
        GetComponentInChildren<SpriteFlashEffect>()?.Flash();
        if (currentHp <= 0f)
        {
            PropDropEntry entry = GetRandomDropEntry();
            string dropType = entry != null ? entry.poolKey : "none";
            FirebaseAnalyticsHelper.LogPropBroken(gameObject.name, dropType);
            SpawnDrop();
            if (ObjectPooling.Instance != null)
                ObjectPooling.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }

    private void SpawnDrop()
    {
        if (drops == null || drops.Count == 0) return;
        if (Random.value > dropChance) return;

        PropDropEntry entry = GetRandomDropEntry();
        if (entry == null || string.IsNullOrEmpty(entry.poolKey)) return;

        int count = Mathf.Max(1, entry.quantity);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Vector3.zero;
            if (count > 1)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spreadRadius;
            }

            ObjectPooling.Instance.Spawn(entry.poolKey, transform.position + offset, Quaternion.identity);
        }
    }

    private PropDropEntry GetRandomDropEntry()
    {
        float totalWeight = 0f;
        foreach (var entry in drops)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.poolKey))
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in drops)
        {
            if (entry == null || string.IsNullOrEmpty(entry.poolKey)) continue;
            cumulative += entry.weight;
            if (random <= cumulative)
                return entry;
        }

        return null;
    }
}
