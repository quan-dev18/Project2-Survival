using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image healthFillSlow;
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private float slowLerpSpeed = 2.5f;
    [SerializeField] private float hpSlowDelay = 0.5f;

    public Image HealthFillSlow => healthFillSlow;
    public Image healthFillslow => healthFillSlow;

    private EnemyHealth enemyHealth;
    private float targetFill;
    private float hpSlowDelayTimer;

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
            if (enemyHealth.MaxHealth > 0f)
                ApplyHealthChange(Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth), isInit: true);
        }
    }

    private void OnEnable()
    {
        ResolveHealth();
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += UpdateHealthUI;
            if (enemyHealth.MaxHealth > 0f)
                ApplyHealthChange(Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth), isInit: true);
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged -= UpdateHealthUI;
    }

    private void Update()
    {
        if (enemyHealth == null)
        {
            ResolveHealth();
            if (enemyHealth != null && enemyHealth.MaxHealth > 0f)
            {
                enemyHealth.OnHealthChanged -= UpdateHealthUI;
                enemyHealth.OnHealthChanged += UpdateHealthUI;
                ApplyHealthChange(Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth), isInit: true);
            }
        }
        else if (enemyHealth.MaxHealth > 0f)
        {
            // Fallback poll in case event missed (pooled boss, sibling hierarchy, etc.)
            float currentFill = Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth);
            ApplyHealthChange(currentFill, isInit: false);
        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillSlow != null)
        {
            // Primary bar: lerps up if healing
            if (healthFill != null && healthFill.fillAmount < targetFill)
            {
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
            }

            // Slow bar: wait for delay after damage, then lerp down
            if (hpSlowDelayTimer > 0f)
            {
                hpSlowDelayTimer -= Time.deltaTime;
            }
            else
            {
                if (healthFillSlow.fillAmount > targetFill)
                {
                    healthFillSlow.fillAmount = Mathf.Lerp(healthFillSlow.fillAmount, targetFill, Time.deltaTime * slowLerpSpeed);
                    if (Mathf.Abs(healthFillSlow.fillAmount - targetFill) < 0.001f)
                    {
                        healthFillSlow.fillAmount = targetFill;
                    }
                }
                else if (healthFillSlow.fillAmount < targetFill)
                {
                    healthFillSlow.fillAmount = targetFill;
                }
            }

            // Ensure slow bar never falls below primary bar
            if (healthFill != null && healthFillSlow.fillAmount < healthFill.fillAmount)
            {
                healthFillSlow.fillAmount = healthFill.fillAmount;
            }
        }
        else
        {
            // Fallback when healthFillSlow is not assigned
            if (healthFill != null)
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
        }
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (max > 0f)
            ApplyHealthChange(Mathf.Clamp01(current / max), isInit: false);
    }

    private void ApplyHealthChange(float newFill, bool isInit = false)
    {
        if (isInit)
        {
            targetFill = newFill;
            if (healthFill != null)
                healthFill.fillAmount = newFill;
            if (healthFillSlow != null)
                healthFillSlow.fillAmount = newFill;
            hpSlowDelayTimer = 0f;
            return;
        }

        if (Mathf.Abs(newFill - targetFill) > 0.0001f)
        {
            if (newFill < targetFill)
            {
                // Taking damage: primary bar drops directly
                if (healthFillSlow != null)
                {
                    if (healthFill != null)
                        healthFill.fillAmount = newFill;
                    hpSlowDelayTimer = hpSlowDelay;
                }
            }
            else if (newFill > targetFill)
            {
                // Healing
                hpSlowDelayTimer = 0f;
                if (healthFillSlow != null && healthFillSlow.fillAmount < newFill)
                    healthFillSlow.fillAmount = newFill;
            }

            targetFill = newFill;
        }
    }
}
