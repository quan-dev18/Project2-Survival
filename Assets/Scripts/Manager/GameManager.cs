using System;
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
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

    public void SetIsWin(bool value) => IsWin = value;
    [SerializeField] private GameOverPanel panel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentState = GameState.Menu;
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"GameState: {currentState}");
        Time.timeScale = currentState == GameState.Playing ? 1f : 0f;

        if (currentState == GameState.Playing)
            IsWin = false;

        if (currentState == GameState.GameOver)
        {
            if (panel != null) panel.Show();
        }

        OnStateChanged?.Invoke(currentState);
    }
}
