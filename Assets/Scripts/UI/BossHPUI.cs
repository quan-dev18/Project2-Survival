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
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged += UpdateHealthUI;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged -= UpdateHealthUI;
    }

    private void Update()
    {
        if (healthFill != null)
            healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (max > 0f)
            targetFill = current / max;
    }
}
