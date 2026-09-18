using UnityEngine;

/// <summary>
/// Master controller for active Ball Merge gameplay session.
/// Coordinates LevelData, BallDropper, ContainerBoundary, and score tracking.
/// </summary>
public class GameplayController : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelDataSO levelData;

    [Header("Component References")]
    [SerializeField] private BallDropper dropper;
    [SerializeField] private ContainerBoundary container;
    [SerializeField] private DangerLine dangerLine;

    [Header("Session State")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private bool isSessionActive = false;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsSessionActive => isSessionActive;

    private void Awake()
    {
        if (dropper == null) dropper = FindAnyObjectByType<BallDropper>();
        if (container == null) container = FindAnyObjectByType<ContainerBoundary>();
        if (dangerLine == null) dangerLine = FindAnyObjectByType<DangerLine>();
    }

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("BallMerge_HighScore", 0);
        if (levelData != null)
        {
            InitializeLevel(levelData);
        }
    }

    public void InitializeLevel(LevelDataSO data)
    {
        levelData = data;
        if (BallMergeManager.HasInstance)
        {
            BallMergeManager.Instance.SetLevelData(levelData);
        }

        if (container != null)
        {
            container.Setup(levelData);
        }

        if (dropper != null)
        {
            dropper.Setup(levelData);
        }

        if (dangerLine != null)
        {
            dangerLine.Setup(levelData);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnGamePaused += HandleGamePaused;
        GameEvents.OnScoreChanged += HandleScoreAdded;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnGamePaused -= HandleGamePaused;
        GameEvents.OnScoreChanged -= HandleScoreAdded;
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

    private void HandleGamePaused(bool isPaused)
    {
        if (dropper != null)
        {
            dropper.SetInputActive(!isPaused && isSessionActive);
        }
    }

    private void HandleScoreAdded(int points)
    {
        if (!isSessionActive) return;

        currentScore += points;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("BallMerge_HighScore", highScore);
            PlayerPrefs.Save();
            GameEvents.OnHighScoreChanged?.Invoke(highScore);
        }
    }

    /// <summary>
    /// Starts an active gameplay session.
    /// </summary>
    public virtual void StartSession()
    {
        currentScore = 0;
        isSessionActive = true;
        Debug.Log("[GameplayController] Ball Merge session started.");

        if (dropper != null)
        {
            dropper.SetInputActive(true);
        }

        if (dangerLine != null)
        {
            dangerLine.SetSessionActive(true);
        }

        GameEvents.OnScoreChanged?.Invoke(0);
        GameEvents.OnHighScoreChanged?.Invoke(highScore);
    }

    /// <summary>
    /// Ends active gameplay session.
    /// </summary>
    public virtual void EndSession()
    {
        if (!isSessionActive) return;

        isSessionActive = false;
        Debug.Log($"[GameplayController] Ball Merge session ended. Score: {currentScore}");

        if (dropper != null)
        {
            dropper.SetInputActive(false);
        }

        if (dangerLine != null)
        {
            dangerLine.SetSessionActive(false);
        }
    }

    /// <summary>
    /// Helper to trigger GameOver from gameplay rules (e.g. overflow).
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
