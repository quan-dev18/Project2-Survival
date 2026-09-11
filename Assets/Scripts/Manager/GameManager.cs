using System;
using UnityEngine;

public enum GameState
{
    Menu,
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
        currentState = GameState.Menu;
        TotalElapsedTime = 0f;
        KillCount = 0;
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
            TotalElapsedTime += Time.deltaTime;
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"GameState: {currentState}");
#endif
        Time.timeScale = currentState == GameState.Playing ? 1f : 0f;

        if (currentState == GameState.Playing)
        {
            IsWin = false;
            if (UserData.Instance != null)
                UserData.Instance.ResetSessionGold();
        }

        if (currentState == GameState.GameOver)
        {
            if (panel != null) panel.Show();
        }

        OnStateChanged?.Invoke(currentState);
    }
}
