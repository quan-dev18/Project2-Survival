using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private Animator _animator;

    public float CurrentHealth => enemyController.currentHealth;

    public event System.Action<float, float> OnHealthChanged;

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
        if(_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        enemyController.ResetHealth();
        if (TryGetComponent(out Collider2D col)) col.enabled = true;
        enemyMovement.enabled = true;
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0f) return;

        float health = Mathf.Max(0f, CurrentHealth - amount);
        enemyController.SetCurrentHealth(health);
        OnHealthChanged?.Invoke(health, enemyController.maxHealth);

        if (PopUpManager.Instance != null)
            PopUpManager.Instance.Show(transform.position, amount, PopupType.Damage);

        if (health <= 0f)
        {
            Die();
            _animator.SetBool("isDead",true);
            
        }
            
    }

    public void Heal(float amount)
    {
        float health = Mathf.Min(CurrentHealth + amount, enemyController.maxHealth);
        enemyController.SetCurrentHealth(health);
        OnHealthChanged?.Invoke(health, enemyController.maxHealth);
    }

    private void Die()
    {
        enemyMovement.enabled = false;
        if (TryGetComponent(out Collider2D col)) col.enabled = false; 

    }
    public void OnDeathAnimationEnd()
    {
        DropXP();
        _animator.SetBool("isDead",false);
        if (ObjectPooling.Instance != null)
        {
            ObjectPooling.Instance.Despawn(gameObject);
        }
    }


    private void DropXP()
    {
        if (ObjectPooling.Instance == null || enemyController == null) return;

        GameObject gem = ObjectPooling.Instance.Spawn("XPGem", transform.position, Quaternion.identity);
        if (gem != null && gem.TryGetComponent(out XPGem xpGem))
            xpGem.SetAmount(enemyController.xpReward);
    }
}