using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text currentHPText;
    [SerializeField] private float hpLerpSpeed = 5f;

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
            UpdateHealthUI(playerStats.CurrentHealth, playerStats.MaxHealth);
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
            playerStats.OnHealthChanged -= UpdateHealthUI;
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (healthFill != null)
            healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetHPFill, Time.deltaTime * hpLerpSpeed);
        if (expFill != null)
            expFill.fillAmount = Mathf.Lerp(expFill.fillAmount, targetExpFill, Time.deltaTime * expLerpSpeed);
    }

    private void UpdateExpBar()
    {
        if (PlayerXP.Instance != null)
            targetExpFill = (float)PlayerXP.Instance.CurrentXP / PlayerXP.Instance.XPToNextLevel;
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (max > 0f)
            targetHPFill = current / max;
        if (currentHPText != null)
            currentHPText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
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