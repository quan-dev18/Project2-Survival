using System.Collections.Generic;
using UnityEngine;

public class BreakableProp : MonoBehaviour, IDamageable
{
    [SerializeField] private float hp = 3f;
    [SerializeField] private List<PropDropEntry> drops;
    [SerializeField] private float dropChance = 1f;
    [SerializeField] private float spreadRadius = 0.5f;

    public void TakeDamage(float amount)
    {
        hp -= amount;
        GetComponentInChildren<SpriteFlashEffect>()?.Flash();
        if (hp <= 0f)
        {
            SpawnDrop();
            ObjectPooling.Instance.Despawn(gameObject);
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
