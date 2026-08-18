using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyController enemyController;

    public float CurrentHealth => enemyController.currentHealth;

    public event System.Action<float, float> OnHealthChanged;

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
    }

    private void OnEnable()
    {
        enemyController.ResetHealth();
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0f) return;

        float health = Mathf.Max(0f, CurrentHealth - amount);
        enemyController.SetCurrentHealth(health);
        OnHealthChanged?.Invoke(health, enemyController.maxHealth);

        if (health <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        float health = Mathf.Min(CurrentHealth + amount, enemyController.maxHealth);
        enemyController.SetCurrentHealth(health);
        OnHealthChanged?.Invoke(health, enemyController.maxHealth);
    }

    private void Die()
    {
        DropXP();
        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    private void DropXP()
    {
        if (ObjectPooling.Instance == null || enemyController == null) return;

        GameObject gem = ObjectPooling.Instance.Spawn("XPGem", transform.position, Quaternion.identity);
        if (gem != null && gem.TryGetComponent(out XPGem xpGem))
            xpGem.SetAmount(enemyController.xpReward);
    }
}