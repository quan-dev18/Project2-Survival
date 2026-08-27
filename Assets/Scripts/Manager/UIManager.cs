using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text currentHPText;

    [Header("EXP")]
    [SerializeField] private Image expFill;

    [Header("Level")]
    [SerializeField] private TMP_Text currentLevelText;

    [Header("Kills")]
    [SerializeField] private TMP_Text killCountText;

    [Header("Pause")]
    [SerializeField] private Button pauseBtn;
    [SerializeField] private PausePanel pausePanel;

    private PlayerStats playerStats;

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

        if (PlayerXP.Instance != null)
        {
            PlayerXP.Instance.OnLevelUp += OnLevelUp;
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
        if (PlayerXP.Instance != null)
            PlayerXP.Instance.OnLevelUp -= OnLevelUp;
        if (playerStats != null)
            playerStats.OnHealthChanged -= UpdateHealthUI;
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        UpdateExpBar();
    }

    private void UpdateExpBar()
    {
        if (expFill != null && PlayerXP.Instance != null)
            expFill.fillAmount = (float)PlayerXP.Instance.CurrentXP / PlayerXP.Instance.XPToNextLevel;
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthFill != null)
            healthFill.fillAmount = current / max;
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