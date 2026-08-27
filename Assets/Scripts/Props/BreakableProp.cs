using System.Collections.Generic;
using UnityEngine;

public class BreakableProp : MonoBehaviour, IDamageable
{
    [SerializeField] private float hp = 3f;
    [SerializeField] private List<PropDropEntry> drops;
    [SerializeField] private float dropChance = 1f;

    public void TakeDamage(float amount)
    {
        hp -= amount;
        GetComponentInChildren<SpriteFlashEffect>()?.Flash();
        if (hp <= 0f)
        {
            SpawnDrop();
            Destroy(gameObject);
        }
    }

    private void SpawnDrop()
    {
        if (drops == null || drops.Count == 0) return;
        if (Random.value > dropChance) return;

        GameObject prefab = GetRandomDrop();
        if (prefab != null)
            Instantiate(prefab, transform.position, Quaternion.identity);
    }

    private GameObject GetRandomDrop()
    {
        float totalWeight = 0f;
        foreach (var entry in drops)
        {
            if (entry != null && entry.prefab != null)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in drops)
        {
            if (entry == null || entry.prefab == null) continue;
            cumulative += entry.weight;
            if (random <= cumulative)
                return entry.prefab;
        }

        return null;
    }
}
