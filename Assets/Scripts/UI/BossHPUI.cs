using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private float lerpSpeed = 5f;

    private EnemyHealth enemyHealth;
    private float targetFill;

    private void Awake()
    {
        ResolveHealth();
        // Also listen for newly spawned bosses (pooled bosses spawn after this Awake)
        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.OnBossSpawned += BindBoss;
    }

    private void OnDestroy()
    {
        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.OnBossSpawned -= BindBoss;
        if (enemyHealth != null) enemyHealth.OnHealthChanged -= UpdateHealthUI;
    }

    private void ResolveHealth()
    {
        if (enemyHealth != null) return;
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = GetComponentInChildren<EnemyHealth>(true);
        if (enemyHealth == null && transform.parent != null) enemyHealth = transform.parent.GetComponentInChildren<EnemyHealth>(true);
        if (enemyHealth == null) enemyHealth = FindObjectOfType<EnemyHealth>();
    }

    private void BindBoss(EnemyHealth boss)
    {
        if (enemyHealth != null) enemyHealth.OnHealthChanged -= UpdateHealthUI;
        enemyHealth = boss;
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
        }
    }

    private void OnEnable()
    {
        ResolveHealth();
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged -= UpdateHealthUI;
    }

    private void Update()
    {
        if (enemyHealth == null) ResolveHealth();
        // Fallback poll in case event missed (pooled boss, sibling hierarchy, etc.)
        if (enemyHealth != null && enemyHealth.MaxHealth > 0f)
            targetFill = enemyHealth.CurrentHealth / enemyHealth.MaxHealth;

        if (healthFill != null)
            healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (max > 0f)
            targetFill = current / max;
    }
}
