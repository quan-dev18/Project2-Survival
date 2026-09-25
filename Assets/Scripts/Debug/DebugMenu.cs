using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    public static DebugMenu Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button toggleButton; // UI button to toggle debug panel (must be OUTSIDE debugPanel)
    [SerializeField] private Button closeButton; // UI button to close debug panel (inside debugPanel)
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("Upgrade List")]
    [SerializeField] private Transform upgradeListContainer;
    [SerializeField] private GameObject upgradeButtonPrefab;
    [SerializeField] private List<UpgradeSO> allAvailableUpgrades; // Drag all upgrade SOs here

    [Header("Health Controls")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_InputField healthInputField;
    [SerializeField] private Button setHealthButton;
    [SerializeField] private Button healFullButton;
    [SerializeField] private Button killButton;

    [Header("Tier Colors")]
    [SerializeField] private Color tier1Color = new Color(0.2f, 0.45f, 1f);
    [SerializeField] private Color tier2Color = new Color(0.2f, 0.8f, 0.35f);
    [SerializeField] private Color tier3Color = new Color(0.6f, 0.3f, 0.9f);

    private PlayerStats playerStats;
    private List<GameObject> spawnedUpgradeButtons = new List<GameObject>();
    private bool m_isPlaceholder;
    private static bool s_creatingPlaceholder;
    private float playerSearchCooldown;

    // Guarantees a DebugMenu exists in every scene (the full UI only lives in
    // GameMap1; elsewhere this acts as a placeholder that reports what is missing).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindObjectOfType<DebugMenu>() == null)
        {
            s_creatingPlaceholder = true;
            var go = new GameObject("DebugMenu (Runtime)");
            go.AddComponent<DebugMenu>();
            s_creatingPlaceholder = false;
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        m_isPlaceholder = s_creatingPlaceholder;

        if (Instance != null && Instance != this)
        {
            // No persistence across scenes, so duplicates only happen within
            // one scene: the instance with real panel references wins.
            bool oldUsable = Instance.debugPanel != null;
            bool newWired = !m_isPlaceholder && debugPanel != null;
            if (oldUsable || !newWired)
            {
                Destroy(gameObject);
                return;
            }
            Destroy(Instance.gameObject);
        }
        Instance = this;
        // NOTE: intentionally NOT DontDestroyOnLoad. All references (panel,
        // buttons, sliders) are scene objects; persisting only caused stale
        // references. Each scene gets its own instance (or a runtime placeholder
        // via EnsureExists).

        // A previous scene may have ended while slowed down - always start clean.
        Time.timeScale = 1f;

        RebindReferences();
        if (debugPanel != null) debugPanel.SetActive(false);
    }

    // Resolves scene references and wires listeners. Safe to call multiple
    // times: listeners are removed before being added.
    private void RebindReferences()
    {
        if (debugCanvas == null) debugCanvas = GetComponentInChildren<Canvas>(true);
        if (debugPanel == null)
        {
            if (debugCanvas != null)
                debugPanel = debugCanvas.GetComponentInChildren<Transform>(true)?.Find("Panel")?.gameObject;
        }
        if (debugPanel == null)
        {
            var canvasObj = GameObject.Find("DebugCanvas");
            if (canvasObj != null)
            {
                var panelT = canvasObj.transform.Find("Panel");
                if (panelT != null)
                {
                    debugPanel = panelT.gameObject;
                    debugCanvas = canvasObj.GetComponent<Canvas>();
                }
            }
        }

        if (upgradeListContainer == null && debugPanel != null)
        {
            var scroll = debugPanel.transform.Find("Scroll View");
            if (scroll != null) upgradeListContainer = scroll;
        }

        // Recover the standard scene buttons by name when references were lost.
        if (toggleButton == null)
        {
            var toolBtn = GameObject.Find("ToolBtn");
            if (toolBtn != null) toggleButton = toolBtn.GetComponent<Button>();
        }
        if (closeButton == null)
        {
            var exitBtn = GameObject.Find("ExitBtn");
            if (exitBtn != null) closeButton = exitBtn.GetComponent<Button>();
        }

        // Find player references (player may spawn after Awake)
        if (playerStats == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }

        // Setup UI (remove first so rebinds never stack duplicates)
        if (toggleButton != null)
        {
            if (!toggleButton.gameObject.activeInHierarchy)
                Debug.LogWarning("[DebugMenu] toggleButton '" + toggleButton.name + "' is assigned but inactive (or a parent HUD object is). It cannot receive clicks - check NorthPanel/ToolBtn active state.");
            // A fully transparent toggle can't be seen or aimed at - reveal it.
            var graphic = toggleButton.targetGraphic;
            if (graphic != null && graphic.color.a < 0.05f)
            {
                var c = graphic.color;
                c.a = 1f;
                graphic.color = c;
            }
            toggleButton.onClick.RemoveListener(ToggleDebugMenu);
            toggleButton.onClick.AddListener(ToggleDebugMenu);
        }
        else Debug.LogWarning("[DebugMenu] toggleButton is not assigned. Assign a button outside the panel, or use " + toggleKey + ".");
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseDebugMenu);
            closeButton.onClick.AddListener(CloseDebugMenu);
        }
        if (healthSlider != null)
        {
            healthSlider.onValueChanged.RemoveListener(OnHealthSliderChanged);
            healthSlider.onValueChanged.AddListener(OnHealthSliderChanged);
        }
        if (healthInputField != null)
        {
            healthInputField.onEndEdit.RemoveListener(OnHealthInputChanged);
            healthInputField.onEndEdit.AddListener(OnHealthInputChanged);
        }
        if (setHealthButton != null)
        {
            setHealthButton.onClick.RemoveListener(SetHealthFromInput);
            setHealthButton.onClick.AddListener(SetHealthFromInput);
        }
        if (healFullButton != null)
        {
            healFullButton.onClick.RemoveListener(HealFull);
            healFullButton.onClick.AddListener(HealFull);
        }
        if (killButton != null)
        {
            killButton.onClick.RemoveListener(KillPlayer);
            killButton.onClick.AddListener(KillPlayer);
        }

        RefreshUpgradeList();
    }

    private void Update()
    {
        // Toggle with hotkey (F1 hardcoded as backup in case the serialized key got lost)
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(toggleKey))
            ToggleDebugMenu();

        // Player may spawn after Awake, so (re)acquire lazily (throttled to avoid per-frame FindGameObjectWithTag)
        if (playerStats == null)
        {
            playerSearchCooldown -= Time.unscaledDeltaTime;
            if (playerSearchCooldown <= 0f)
            {
                playerSearchCooldown = 0.5f;
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerStats = player.GetComponent<PlayerStats>();
            }
        }

        // Update health slider from actual health
        if (playerStats != null && healthSlider != null && debugPanel != null && debugPanel.activeSelf)
        {
            healthSlider.maxValue = playerStats.MaxHealth;
            healthSlider.value = playerStats.CurrentHealth;
            if (healthInputField && !healthInputField.isFocused)
                healthInputField.text = Mathf.RoundToInt(playerStats.CurrentHealth).ToString();
        }
    }

    public void ToggleDebugMenu()
    {
        if (!EnsurePanelReference()) return;

        // The canvas itself must be active or nothing renders
        if (debugCanvas != null && !debugCanvas.gameObject.activeSelf)
            debugCanvas.gameObject.SetActive(true);

        bool newState = !debugPanel.activeSelf;
        debugPanel.SetActive(newState);

        // Slow motion when panel is open, restore normal speed when closed
        Time.timeScale = newState ? 0.1f : 1f; // Slowed but inputs still work

        if (newState) RefreshUpgradeList();
    }

    public void CloseDebugMenu()
    {
        if (!EnsurePanelReference()) return;
        debugPanel.SetActive(false);
        Time.timeScale = 1f; // Normal speed
    }

    private void OnDestroy()
    {
        // Never leave the game stuck in slow motion because of the debug menu
        if (Instance == this && Time.timeScale < 1f)
            Time.timeScale = 1f;
    }

    // Re-acquires the panel reference if it is missing (e.g. runtime
    // placeholder in a scene where the panel lives under another name).
    // Returns false and logs when the panel cannot be resolved.
    private bool EnsurePanelReference()
    {
        if (debugPanel == null)
        {
            if (debugCanvas == null) debugCanvas = GetComponentInChildren<Canvas>(true);
            if (debugCanvas != null)
                debugPanel = debugCanvas.GetComponentInChildren<Transform>(true)?.Find("Panel")?.gameObject;
            if (debugPanel == null)
            {
                // Resolve via the canvas to avoid grabbing an unrelated "Panel"
                var canvasObj = GameObject.Find("DebugCanvas");
                if (canvasObj != null)
                {
                    var panelT = canvasObj.transform.Find("Panel");
                    if (panelT != null)
                    {
                        debugPanel = panelT.gameObject;
                        debugCanvas = canvasObj.GetComponent<Canvas>();
                    }
                }
            }
        }
        if (debugPanel == null)
        {
            Debug.LogWarning("[DebugMenu] debugPanel reference is missing. Assign DebugCanvas/Panel in the Inspector (GameMap1 scene).");
            return false;
        }
        return true;
    }

    private void RefreshUpgradeList()
    {
        // Clear old buttons
        foreach (var btn in spawnedUpgradeButtons)
            Destroy(btn);
        spawnedUpgradeButtons.Clear();

        if (upgradeListContainer == null || upgradeButtonPrefab == null || allAvailableUpgrades == null) return;

        foreach (var upgrade in allAvailableUpgrades)
        {
            if (upgrade == null) continue;
            
            bool owned = false;
            var levelUpPanel = GetLevelUpPanel();
            if (levelUpPanel != null)
            {
                owned = levelUpPanel.OwnedUpgrades.Contains(upgrade);
            }

            var btnObj = Instantiate(upgradeButtonPrefab, upgradeListContainer);
            var btn = btnObj.GetComponent<Button>();
            var txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = upgrade.UpgradeName + (owned ? " [OWNED]" : "");
            
            var img = btnObj.GetComponent<Image>();
            if (img) img.color = owned ? Color.green : Color.white;

            btn.onClick.AddListener(() => AddUpgrade(upgrade));
            spawnedUpgradeButtons.Add(btnObj);
        }
    }

    // LevelUpPanel hides itself during play, so a plain FindObjectOfType
    // (active objects only) would miss it. The singleton survives hiding.
    private LevelUpPanel GetLevelUpPanel()
    {
        var panel = LevelUpPanel.Instance;
        if (panel == null)
            panel = FindObjectOfType<LevelUpPanel>(true);
        return panel;
    }

    private void AddUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null) return;
        var panel = GetLevelUpPanel();
        if (panel != null)
        {
            panel.GrantUpgrade(upgrade);
            RefreshUpgradeList();
        }
        else Debug.LogWarning("[DebugMenu] No LevelUpPanel in scene - cannot grant upgrade.");
    }

    private void OnHealthSliderChanged(float value)
    {
        if (healthInputField) healthInputField.text = Mathf.RoundToInt(value).ToString();
    }

    private void OnHealthInputChanged(string text)
    {
        if (float.TryParse(text, out float val) && healthSlider)
            healthSlider.value = Mathf.Clamp(val, 0, healthSlider.maxValue);
    }

    private void SetHealthFromInput()
    {
        if (playerStats == null) return;
        if (healthInputField && float.TryParse(healthInputField.text, out float val))
        {
            playerStats.SetHealth(val);
            // Slider will update via Update()
        }
        else Debug.LogWarning("[DebugMenu] Set HP failed: enter a number in the HP field.");
    }

    private void HealFull()
    {
        if (playerStats != null)
        {
            playerStats.Heal(playerStats.MaxHealth);
        }
    }

    private void KillPlayer()
    {
        if (playerStats != null)
        {
            playerStats.TakeDamage(playerStats.MaxHealth + 1);
        }
    }

    private int GetUpgradeTier(UpgradeSO upgrade)
    {
        if (upgrade == null) return 1;
        string n = upgrade.UpgradeName ?? string.Empty;
        if (n.EndsWith(" III") || n.EndsWith("_3") || n.EndsWith(" 3")) return 3;
        if (n.EndsWith(" II") || n.EndsWith("_2") || n.EndsWith("_2A") || n.EndsWith("_2B") || n.EndsWith(" 2")) return 2;
        if (n.EndsWith(" I") || n.EndsWith("_1") || n.EndsWith(" 1")) return 1;
        if (n.Contains("III") || n.Contains("_3")) return 3;
        if (n.Contains("II") || n.Contains("_2")) return 2;
        return 1;
    }

    private Color GetTierColor(int tier)
    {
        if (tier >= 3) return tier3Color;
        if (tier == 2) return tier2Color;
        return tier1Color;
    }
}