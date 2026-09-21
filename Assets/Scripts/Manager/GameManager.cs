using System;
using UnityEngine;

public enum GameState
{
    Menu,
    Tutorial,
    Playing,
    Paused,
    LevelUp,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<GameState> OnStateChanged;

    private GameState currentState;
    public GameState CurrentState => currentState;

    public bool IsWin { get; private set; }
    public float TotalElapsedTime { get; private set; }
    public int KillCount { get; private set; }
    public event Action<int> OnKillCountChanged;

    public void SetIsWin(bool value) => IsWin = value;
    [SerializeField] private GameOverPanel panel;
    public void AddKill()
    {
        KillCount++;
        OnKillCountChanged?.Invoke(KillCount);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Một số scene (GameMap2/3/4) quên kéo tay reference panel thành null.
        // Tự tìm GameOverPanel trong scene (kể cả panel đang ẩn) để chắc chắn
        // panel.Show() luôn có đối tượng gọi khi GameOver.
        if (panel == null)
        {
#if UNITY_2023_1_OR_NEWER
            panel = FindFirstObjectByType<GameOverPanel>(FindObjectsInactive.Include);
#else
            panel = FindObjectOfType<GameOverPanel>(true);
#endif
        }

        currentState = GameState.Menu;
        TotalElapsedTime = 0f;
        KillCount = 0;
    }

    private void Start()
    {
        // Nếu có TutorialController trong scene thì bắt đầu ở trạng thái Tutorial
        // Ngược lại (các map thường) thì chạy thẳng vào Playing
        var tutorial = FindFirstObjectByType<TutorialController>();
        if (tutorial != null)
        {
            SetState(GameState.Tutorial);
        }
        else
        {
            SetState(GameState.Playing);
        }
    }

    public void CompleteTutorial()
    {
        if (currentState == GameState.Tutorial)
        {
            SetState(GameState.Playing);
        }
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
            TotalElapsedTime += Time.deltaTime;
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;

        // Không cho chuyển sang LevelUp nếu đang Tutorial
        if (newState == GameState.LevelUp && currentState == GameState.Tutorial)
            return;

        currentState = newState;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"GameState: {currentState}");
#endif
        Time.timeScale = (currentState == GameState.Playing || currentState == GameState.Tutorial) ? 1f : 0f;

        if (currentState == GameState.Playing)
        {
            IsWin = false;
            if (UserData.Instance != null)
                UserData.Instance.ResetSessionGold();

            // Log session start
            if (PlayerEquipment.SelectedHeroIndex >= 0)
            {
                string heroId = $"hero_{PlayerEquipment.SelectedHeroIndex}";
                string weaponId = $"weapon_{PlayerEquipment.SelectedWeaponIndex}";
                string stageId = PlayerPrefs.GetString("SelectedMapIndex", "0");
                string skinId = UserData.Instance != null
                    ? UserData.Instance.GetEquippedSkinId(heroId) ?? "default"
                    : "default";
                FirebaseAnalyticsHelper.LogGameSessionStart(heroId, weaponId, stageId, skinId);
            }
        }

        if (currentState == GameState.GameOver)
        {
            // Log session end
            string result = IsWin ? "win" : "lose";
            string heroId = $"hero_{PlayerEquipment.SelectedHeroIndex}";
            string weaponId = $"weapon_{PlayerEquipment.SelectedWeaponIndex}";
            string stageId = PlayerPrefs.GetString("SelectedMapIndex", "0");
            int highestLevel = PlayerXP.Instance != null ? PlayerXP.Instance.CurrentLevel : 1;
            int gold = UserData.Instance != null ? UserData.Instance.SessionGold : 0;
            FirebaseAnalyticsHelper.LogGameSessionEnd(result, TotalElapsedTime, KillCount, gold, heroId, weaponId, stageId, highestLevel);

            if (panel != null) panel.Show();
        }

        OnStateChanged?.Invoke(currentState);
    }
}
