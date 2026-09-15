using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Production-grade GameManager orchestrating game states, initialization sequences,
/// pause/resume, and central system coordination.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Boot Configuration")]
    [SerializeField] private bool autoInitializeOnStart = true;

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.None;

    public GameState CurrentState => currentState;
    public bool IsInitialized { get; private set; }
    public bool IsPaused { get; private set; }

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (autoInitializeOnStart)
        {
            StartCoroutine(BootSequenceRoutine());
        }
    }

    /// <summary>
    /// Predictable boot sequence ensuring all core subsystems are ready before gameplay starts.
    /// </summary>
    public IEnumerator BootSequenceRoutine()
    {
        ChangeState(GameState.Boot);
        IsInitialized = false;

        Debug.Log("[GameManager] Boot sequence initiated...");

        // 1. Dependency Manager registration
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }

        // 2. Audio Manager initialization
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.Init();
        }

        // 5. UI Manager initialization
        UIManager uiManager = DependencyManager.Instance != null 
            ? DependencyManager.Instance.TryResolve<UIManager>() 
            : FindAnyObjectByType<UIManager>();

        if (uiManager != null)
        {
            uiManager.Init();
        }

        yield return null; // Ensure frame renders

        IsInitialized = true;
        Debug.Log("[GameManager] Boot sequence completed.");

        // 6. Transition to Main Menu state
        ChangeState(GameState.MainMenu);
    }

    /// <summary>
    /// Changes the current game state and invokes lifecycle events.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        GameState oldState = currentState;
        currentState = newState;

        Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");

        OnStateChanged(oldState, newState);
        GameEvents.OnGameStateChanged?.Invoke(newState, oldState);
    }

    private void OnStateChanged(GameState oldState, GameState newState)
    {
        UIManager uiManager = DependencyManager.Instance != null 
            ? DependencyManager.Instance.TryResolve<UIManager>() 
            : null;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                IsPaused = false;
                if (uiManager != null)
                {
                    uiManager.ShowPanel<MainMenuPanel>();
                }
                break;

            case GameState.InGame:
                Time.timeScale = 1f;
                IsPaused = false;
                if (uiManager != null)
                {
                    // If you have a gameplay HUD panel, show it here
                }
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                IsPaused = true;
                GameEvents.OnGamePaused?.Invoke(true);
                break;

            case GameState.GameOver:
                Time.timeScale = 1f;
                IsPaused = false;
                break;
        }
    }

    #region Public Gameplay Flow Controls

    /// <summary>
    /// Begins active gameplay.
    /// </summary>
    public void StartGame()
    {
        ChangeState(GameState.InGame);
    }

    /// <summary>
    /// Pauses or unpauses gameplay.
    /// </summary>
    public void SetPause(bool pause)
    {
        if (pause && currentState == GameState.InGame)
        {
            ChangeState(GameState.Paused);
        }
        else if (!pause && currentState == GameState.Paused)
        {
            ChangeState(GameState.InGame);
        }
    }

    public void TogglePause()
    {
        SetPause(!IsPaused);
    }

    /// <summary>
    /// Triggers game over sequence.
    /// </summary>
    public void TriggerGameOver(int finalScore = 0)
    {
        GameEvents.OnScoreChanged?.Invoke(finalScore);
        ChangeState(GameState.GameOver);
    }

    /// <summary>
    /// Returns to main menu state.
    /// </summary>
    public void ReturnToMainMenu()
    {
        ChangeState(GameState.MainMenu);
    }

    /// <summary>
    /// Restarts current game level.
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Quits the game application safely.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}