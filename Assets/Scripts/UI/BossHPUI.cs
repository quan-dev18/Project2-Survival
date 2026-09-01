using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    public static BossHPUI Instance { get; private set; }

    [SerializeField] private GameObject bossHPPanel;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text currentHPText;

    private EnemyHealth currentBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (bossHPPanel != null)
            bossHPPanel.SetActive(false);
    }

    public void Show(EnemyHealth boss)
    {
        if (boss == null) return;

        if (currentBoss != null)
            Hide();

        currentBoss = boss;
        currentBoss.OnHealthChanged += UpdateHealthUI;
        currentBoss.OnDeath += Hide;

        if (bossHPPanel != null)
            bossHPPanel.SetActive(true);

        UpdateHealthUI(currentBoss.CurrentHealth, currentBoss.MaxHealth);
    }

    public void Hide()
    {
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateHealthUI;
            currentBoss.OnDeath -= Hide;
            currentBoss = null;
        }

        if (bossHPPanel != null)
            bossHPPanel.SetActive(false);
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthFill != null)
            healthFill.fillAmount = current / max;
        if (currentHPText != null)
            currentHPText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void OnDestroy()
    {
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateHealthUI;
            currentBoss.OnDeath -= Hide;
        }
    }
}
