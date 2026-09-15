using UnityEngine;

/// <summary>
/// Foundation controller for active gameplay sessions.
/// Listens to GameState changes, coordinates game session loops, and tracks score.
/// Attach this or extend it for your specific game mechanics.
/// </summary>
public class GameplayController : MonoBehaviour
{
    [Header("Session State")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private bool isSessionActive = false;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsSessionActive => isSessionActive;

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.InGame)
        {
            StartSession();
        }
        else if (isSessionActive && (newState == GameState.GameOver || newState == GameState.MainMenu))
        {
            EndSession();
        }
    }

    /// <summary>
    /// Called when the game state enters InGame.
    /// </summary>
    public virtual void StartSession()
    {
        currentScore = 0;
        isSessionActive = true;
        Debug.Log("[GameplayController] New gameplay session started.");
        GameEvents.OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Adds score points during gameplay and dispatches score events.
    /// </summary>
    public virtual void AddScore(int points)
    {
        if (!isSessionActive) return;

        currentScore += points;
        GameEvents.OnScoreChanged?.Invoke(currentScore);

        if (currentScore > highScore)
        {
            highScore = currentScore;
            GameEvents.OnHighScoreChanged?.Invoke(highScore);
        }
    }

    /// <summary>
    /// Ends the current gameplay session.
    /// </summary>
    public virtual void EndSession()
    {
        if (!isSessionActive) return;

        isSessionActive = false;
        Debug.Log($"[GameplayController] Gameplay session ended. Final score: {currentScore}");
    }

    /// <summary>
    /// Helper to trigger GameOver from gameplay rules (e.g. player died or time up).
    /// </summary>
    public void TriggerGameOver()
    {
        EndSession();
        if (GameManager.HasInstance)
        {
            GameManager.Instance.TriggerGameOver(currentScore);
        }
    }
}
