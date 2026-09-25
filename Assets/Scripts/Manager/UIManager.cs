using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image healthFillSlow;
    [SerializeField] private TMP_Text currentHPText;
    [SerializeField] private float hpLerpSpeed = 5f;
    [SerializeField] private float hpSlowLerpSpeed = 2.5f;
    [SerializeField] private float hpSlowDelay = 0.5f;

    [Header("Armor")]
    [SerializeField] private TMP_Text currentArmorText;
    [Tooltip("Quick armor preview (e.g. HaveArmorIcon): opacity follows armor % (opaque at full, invisible at 0).")]
    [SerializeField] private Image playerArmorIcon;

    public Image HealthFillSlow => healthFillSlow;
    public Image healthFillslow => healthFillSlow;

    [Header("EXP")]
    [SerializeField] private Image expFill;
    [SerializeField] private float expLerpSpeed = 5f;

    [Header("Level")]
    [SerializeField] private TMP_Text currentLevelText;

    [Header("Kills")]
    [SerializeField] private TMP_Text killCountText;

    [Header("Pause")]
    [SerializeField] private Button pauseBtn;
    [SerializeField] private PausePanel pausePanel;

    private PlayerStats playerStats;
    private float targetHPFill;
    private float targetExpFill;
    private float hpSlowDelayTimer;
    private bool hasInitializedHealth;

    private bool hasInitializedArmor;

    private void Awake()
    {
        pauseBtn?.onClick.AddListener(OnPauseClicked);
    }

    private void Start()
    {
        if (pausePanel == null)
            pausePanel = FindObjectOfType<PausePanel>(true);

        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthUI;
            playerStats.OnArmorChanged += UpdateArmorUI;
            UpdateHealthUI(playerStats.CurrentHealth, playerStats.MaxHealth);
            UpdateArmorUI(playerStats.CurrentArmor, playerStats.MaxArmor);
        }

        var xp = PlayerXP.Instance != null ? PlayerXP.Instance : FindObjectOfType<PlayerXP>();
        if (xp != null)
        {
            xp.OnLevelUp += OnLevelUp;
            xp.OnXPChanged += UpdateExpBar;
            UpdateExpBar();
            UpdateLevelText();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillCountChanged += OnKillCountChanged;
            UpdateKillCount();
        }

        GameManager.OnStateChanged += OnGameStateChanged;
        pausePanel?.Hide();
    }

    private void OnDestroy()
    {
        var xp = PlayerXP.Instance != null ? PlayerXP.Instance : FindObjectOfType<PlayerXP>();
        if (xp != null)
        {
            xp.OnLevelUp -= OnLevelUp;
            xp.OnXPChanged -= UpdateExpBar;
        }
        if (playerStats != null)
            {
                playerStats.OnHealthChanged -= UpdateHealthUI;
                playerStats.OnArmorChanged -= UpdateArmorUI;
            }
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        UpdateHealthBar();

        if (expFill != null)
            expFill.fillAmount = Mathf.Lerp(expFill.fillAmount, targetExpFill, Time.deltaTime * expLerpSpeed);
    }

    private void UpdateHealthBar()
    {
        if (healthFillSlow != null)
        {
            // Primary bar: lerps up if healing
            if (healthFill != null && healthFill.fillAmount < targetHPFill)
            {
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetHPFill, Time.deltaTime * hpLerpSpeed);
            }

            // Slow bar: wait for delay after damage, then lerp down
            if (hpSlowDelayTimer > 0f)
            {
                hpSlowDelayTimer -= Time.deltaTime;
            }
            else
            {
                if (healthFillSlow.fillAmount > targetHPFill)
                {
                    healthFillSlow.fillAmount = Mathf.Lerp(healthFillSlow.fillAmount, targetHPFill, Time.deltaTime * hpSlowLerpSpeed);
                    if (Mathf.Abs(healthFillSlow.fillAmount - targetHPFill) < 0.001f)
                    {
                        healthFillSlow.fillAmount = targetHPFill;
                    }
                }
                else if (healthFillSlow.fillAmount < targetHPFill)
                {
                    healthFillSlow.fillAmount = targetHPFill;
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
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetHPFill, Time.deltaTime * hpLerpSpeed);
        }
    }

    private void UpdateExpBar()
    {
        if (PlayerXP.Instance != null)
            targetExpFill = (float)PlayerXP.Instance.CurrentXP / PlayerXP.Instance.XPToNextLevel;
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (max > 0f)
        {
            float newFill = Mathf.Clamp01(current / max);

            if (!hasInitializedHealth)
            {
                targetHPFill = newFill;
                if (healthFill != null)
                    healthFill.fillAmount = newFill;
                if (healthFillSlow != null)
                    healthFillSlow.fillAmount = newFill;
                hasInitializedHealth = true;
            }
            else
            {
                if (newFill < targetHPFill)
                {
                    // Taking damage: primary bar drops directly
                    if (healthFillSlow != null)
                    {
                        if (healthFill != null)
                            healthFill.fillAmount = newFill;
                        hpSlowDelayTimer = hpSlowDelay;
                    }
                }
                else if (newFill > targetHPFill)
                {
                    // Healing
                    hpSlowDelayTimer = 0f;
                    if (healthFillSlow != null && healthFillSlow.fillAmount < newFill)
                        healthFillSlow.fillAmount = newFill;
                }

                targetHPFill = newFill;
            }
        }

        if (currentHPText != null)
            currentHPText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void UpdateArmorUI(float current, float max)
    {
        if (currentArmorText != null)
            currentArmorText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        UpdateArmorIcon(current, max);
    }

    private void UpdateArmorIcon(float current, float max)
    {
        if (playerArmorIcon == null) return;
        // Smooth mapping: opaque at 100% armor, fully transparent at 0%.
        float alpha = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        Color c = playerArmorIcon.color;
        c.a = alpha;
        playerArmorIcon.color = c;
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateLevelText();
        UpdateExpBar();
    }

    private void UpdateLevelText()
    {
        if (currentLevelText != null && PlayerXP.Instance != null)
            currentLevelText.text = PlayerXP.Instance.CurrentLevel.ToString();
    }

    private void OnKillCountChanged(int count)
    {
        if (killCountText != null)
            killCountText.text = "x"+ count.ToString();
    }

    private void UpdateKillCount()
    {
        if (killCountText != null && GameManager.Instance != null)
            killCountText.text = GameManager.Instance.KillCount.ToString();
    }

    private void OnPauseClicked()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState == GameState.Playing)
            GameManager.Instance.SetState(GameState.Paused);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Paused)
        {
            if (pausePanel == null)
                pausePanel = FindObjectOfType<PausePanel>(true);

            if (pausePanel != null)
                pausePanel.Show();
            else
                Debug.LogWarning("ExperienceUI: no PausePanel found in scene!");
        }
        else
        {
            pausePanel?.Hide();
        }
    }
}