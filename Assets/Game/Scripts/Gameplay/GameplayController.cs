using UnityEngine;

/// <summary>
/// Master controller for active Ball Merge gameplay session.
/// Coordinates LevelData, BallDropper, ContainerBoundary, and score tracking.
/// </summary>
public class GameplayController : Singleton<GameplayController>
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

    [Header("Game Mode & Drops State")]
    [SerializeField] private int remainingDrops = 0;
    [SerializeField] private int totalDropsUsed = 0;
    [SerializeField] private bool levelCompleted = false;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsSessionActive => isSessionActive;
    public int RemainingDrops => remainingDrops;
    public int TotalDropsUsed => totalDropsUsed;
    public bool LevelCompleted => levelCompleted;
    public LevelDataSO ActiveLevelData => levelData;

    protected override void Awake()
    {
        base.Awake();
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

        if (!GameManager.HasInstance || GameManager.Instance.CurrentState != GameState.InGame)
        {
            SetGameplayVisible(false);
        }
    }

    public void SetGameplayVisible(bool visible)
    {
        if (container != null) container.gameObject.SetActive(visible);
        if (dangerLine != null) dangerLine.gameObject.SetActive(visible);
        if (dropper != null) dropper.gameObject.SetActive(visible);
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
        GameEvents.OnBallDropped += HandleBallDropped;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnGamePaused -= HandleGamePaused;
        GameEvents.OnScoreChanged -= HandleScoreAdded;
        GameEvents.OnBallDropped -= HandleBallDropped;
    }

    private void HandleGameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.InGame)
        {
            SetGameplayVisible(true);
            if (oldState != GameState.Paused)
            {
                StartSession();
            }
        }
        else if (newState == GameState.MainMenu || newState == GameState.Boot)
        {
            SetGameplayVisible(false);
            if (isSessionActive)
            {
                EndSession();
            }
        }
    }

    private void HandleGamePaused(bool isPaused)
    {
        if (dropper != null)
        {
            dropper.SetInputActive(!isPaused && isSessionActive);
        }
    }

    private void HandleBallDropped()
    {
        if (!isSessionActive) return;

        totalDropsUsed++;

        if (levelData != null && levelData.gameMode == GameMode.Level)
        {
            if (remainingDrops > 0)
            {
                remainingDrops--;
                GameEvents.OnRemainingDropsChanged?.Invoke(remainingDrops);
            }

            CheckLevelCompletionCriteria();
        }
    }

    private void HandleScoreAdded(int points)
    {
        AddScore(points);
    }

    public void StartLevelMode(int levelNum)
    {
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelDataSO>();
        }

        levelData.gameMode = GameMode.Level;
        levelData.levelNumber = levelNum;
        levelData.targetScore = 300 + (levelNum * 150);
        levelData.maxDrops = Mathf.Max(15, 35 - (levelNum / 5));

        InitializeLevel(levelData);
    }

    public void StartEndlessMode()
    {
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelDataSO>();
        }

        levelData.gameMode = GameMode.Endless;
        InitializeLevel(levelData);
    }

    public void AddScore(int points)
    {
        if (!isSessionActive) return;

        currentScore += points;

        if (PlayerDataManager.HasInstance)
        {
            PlayerDataManager.Instance.AddCoins(points);
        }

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("BallMerge_HighScore", highScore);
            PlayerPrefs.Save();
            GameEvents.OnHighScoreChanged?.Invoke(highScore);
        }

        if (levelData != null && levelData.gameMode == GameMode.Level)
        {
            CheckLevelCompletionCriteria();
        }
    }

    private void CheckLevelCompletionCriteria()
    {
        if (levelData == null || levelData.gameMode != GameMode.Level || levelCompleted || !isSessionActive) return;

        // Check Win Condition
        if (currentScore >= levelData.targetScore)
        {
            levelCompleted = true;
            Debug.Log($"[GameplayController] Level {levelData.levelNumber} COMPLETED! Target score {levelData.targetScore} reached.");

            int stars = 1;
            if (PlayerDataManager.HasInstance)
            {
                stars = PlayerDataManager.Instance.SaveLevelCompletion(
                    levelData.levelNumber, 
                    currentScore, 
                    remainingDrops, 
                    levelData.maxDrops, 
                    levelData.targetScore
                );
            }

            LevelWinPanel winPanel = UIManager.HasInstance ? UIManager.Instance.GetPanel<LevelWinPanel>() : null;
            if (winPanel != null)
            {
                winPanel.SetupLevelWin(levelData.levelNumber, currentScore, totalDropsUsed, stars);
            }

            GameEvents.OnLevelCompleted?.Invoke(levelData.levelNumber, true);
            GameEvents.OnPlaySFX?.Invoke("LevelWin");
            return;
        }

        // Check Lose Condition (Drops exhausted before target score reached)
        if (remainingDrops <= 0 && currentScore < levelData.targetScore)
        {
            Debug.Log($"[GameplayController] Level {levelData.levelNumber} FAILED! Out of drops ({levelData.maxDrops}). Score: {currentScore}/{levelData.targetScore}");
            GameEvents.OnLevelCompleted?.Invoke(levelData.levelNumber, false);
            GameEvents.OnPlaySFX?.Invoke("LevelLose");
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Destroys all active balls in the scene.
    /// </summary>
    public void ClearAllBalls()
    {
        Ball[] activeBalls = FindObjectsByType<Ball>(FindObjectsSortMode.None);
        for (int i = 0; i < activeBalls.Length; i++)
        {
            if (activeBalls[i] != null)
            {
                Destroy(activeBalls[i].gameObject);
            }
        }
    }

    /// <summary>
    /// Starts an active gameplay session.
    /// </summary>
    public virtual void StartSession()
    {
        ClearAllBalls();

        currentScore = 0;
        totalDropsUsed = 0;
        levelCompleted = false;
        isSessionActive = true;

        if (levelData != null && levelData.gameMode == GameMode.Level)
        {
            remainingDrops = levelData.maxDrops;
            GameEvents.OnLevelStarted?.Invoke(levelData.levelNumber);
            GameEvents.OnRemainingDropsChanged?.Invoke(remainingDrops);
            Debug.Log($"[GameplayController] Started LEVEL Mode: Level {levelData.levelNumber} (Target: {levelData.targetScore}, Max Drops: {levelData.maxDrops})");
        }
        else
        {
            remainingDrops = -1; // Unlimited
            Debug.Log("[GameplayController] Started ENDLESS Mode: Unlimited drops.");
        }

        if (dropper != null && levelData != null)
        {
            dropper.Setup(levelData);
            dropper.SetInputActive(true);
        }

        if (dangerLine != null && levelData != null)
        {
            dangerLine.Setup(levelData);
            dangerLine.SetSessionActive(true);
        }

        GameEvents.OnScoreChanged?.Invoke(0);
        GameEvents.OnHighScoreChanged?.Invoke(highScore);
    }

    /// <summary>
    /// Ends active gameplay session and clears container.
    /// </summary>
    public virtual void EndSession()
    {
        ClearAllBalls();

        isSessionActive = false;
        currentScore = 0;
        totalDropsUsed = 0;
        levelCompleted = false;

        Debug.Log("[GameplayController] Ball Merge session ended and container cleared.");

        if (dropper != null)
        {
            dropper.SetInputActive(false);
        }

        if (dangerLine != null)
        {
            dangerLine.ResetTimer();
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
