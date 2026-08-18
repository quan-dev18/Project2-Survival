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
        Time.timeScale = currentState == GameState.LevelUp ? 0f : 1f;
        OnStateChanged?.Invoke(currentState);
    }
}
