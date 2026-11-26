using System.Collections;
using UnityEngine;

/// <summary>
/// Master controller for active Ball Merge gameplay session.
/// Coordinates LevelData, BallDropper, ContainerBoundary, LevelBlockManager, and win/loss rules.
/// </summary>
public class GameplayController : Singleton<GameplayController>
{
    [Header("Level Configuration")]
    [SerializeField] private LevelDataSO levelData;

    [Header("Component References")]
    [SerializeField] private BallDropper dropper;
    [SerializeField] private ContainerBoundary container;
    [SerializeField] private DangerLine dangerLine;
    [SerializeField] private LevelBlockManager blockManager;
    [SerializeField] private ThemeAnimatedBackground animatedBackground;

    [Header("Session State")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private bool isSessionActive = false;

    [Header("Game Mode & Drops State")]
    [SerializeField] private int remainingDrops = 0;
    [SerializeField] private int totalDropsUsed = 0;
    [SerializeField] private bool levelCompleted = false;

    private Coroutine outOfMovesCoroutine;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsSessionActive => isSessionActive;
    public int RemainingDrops => remainingDrops;
    public int TotalDropsUsed => totalDropsUsed;
    public bool LevelCompleted => levelCompleted;
    public LevelDataSO ActiveLevelData => levelData;
    public LevelBlockManager BlockManager => blockManager;
    public LevelRewardSummary LastRewardSummary { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (dropper == null) dropper = FindAnyObjectByType<BallDropper>();
        if (container == null) container = FindAnyObjectByType<ContainerBoundary>();
        if (dangerLine == null) dangerLine = FindAnyObjectByType<DangerLine>();
        EnsureGameplaySupportComponents();
    }

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("BallMerge_HighScore", 0);
        if (levelData != null)
        {
            InitializeLevel(levelData);
        }

        if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.InGame)
        {
            SetGameplayVisible(false);
        }
    }

    private void EnsureGameplaySupportComponents()
    {
        if (blockManager == null)
        {
            blockManager = FindAnyObjectByType<LevelBlockManager>();
            if (blockManager == null)
            {
                GameObject bmObj = new GameObject("LevelBlockManager");
                bmObj.transform.SetParent(transform, false);
                blockManager = bmObj.AddComponent<LevelBlockManager>();
            }
        }

        if (animatedBackground == null)
        {
            animatedBackground = FindAnyObjectByType<ThemeAnimatedBackground>();
            if (animatedBackground == null)
            {
                GameObject bgObj = new GameObject("ThemeAnimatedBackground");
                bgObj.transform.SetParent(transform, false);
                animatedBackground = bgObj.AddComponent<ThemeAnimatedBackground>();
            }
        }

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<CameraShake2D>() == null)
        {
            mainCam.gameObject.AddComponent<CameraShake2D>();
        }
    }

    public void SetGameplayVisible(bool visible)
    {
        if (container != null)
        {
            if (container.gameObject != gameObject)
            {
                container.gameObject.SetActive(visible);
            }
            else
            {
                for (int i = 0; i < container.transform.childCount; i++)
                {
                    container.transform.GetChild(i).gameObject.SetActive(visible);
                }
            }
        }
        if (dangerLine != null) dangerLine.gameObject.SetActive(visible);
        if (dropper != null) dropper.gameObject.SetActive(visible);
        if (animatedBackground != null)
        {
            animatedBackground.gameObject.SetActive(visible);
            if (visible) animatedBackground.SyncWithCurrentTheme();
        }
    }

    public void InitializeLevel(LevelDataSO data)
    {
        levelData = data;
        EnsureGameplaySupportComponents();
        if (BallMergeManager.HasInstance)
        {
            BallMergeManager.Instance.SetLevelData(levelData);
        }

        if (container != null)
        {
            container.Setup(levelData);
        }

        if (dropper != null && dropper.gameObject.activeInHierarchy && isSessionActive)
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
        GameEvents.OnBonusDropsEarned += HandleBonusDropsEarned;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnGamePaused -= HandleGamePaused;
        GameEvents.OnScoreChanged -= HandleScoreAdded;
        GameEvents.OnBallDropped -= HandleBallDropped;
        GameEvents.OnBonusDropsEarned -= HandleBonusDropsEarned;
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
        if (dropper != null && isSessionActive)
        {
            dropper.SetInputActive(!isPaused);
        }
    }

    private void HandleBallDropped()
    {
        if (!isSessionActive) return;

        totalDropsUsed++;

        if (levelData != null && levelData.gameMode == GameMode.Level)
        {
            if (levelData.maxDrops > 0 && remainingDrops > 0)
            {
                remainingDrops--;
                GameEvents.OnRemainingDropsChanged?.Invoke(remainingDrops);
            }

            CheckLevelCompletionCriteria();
        }
    }

    private void HandleBonusDropsEarned(int bonusDrops)
    {
        if (!isSessionActive || bonusDrops <= 0) return;

        if (levelData != null && levelData.gameMode == GameMode.Level && levelData.maxDrops > 0)
        {
            remainingDrops += bonusDrops;
            GameEvents.OnRemainingDropsChanged?.Invoke(remainingDrops);

            if (outOfMovesCoroutine != null)
            {
                StopCoroutine(outOfMovesCoroutine);
                outOfMovesCoroutine = null;
            }
        }
    }

    private void HandleScoreAdded(int points)
    {
        AddScore(points);
    }

    public void NotifyBlockProgressChanged()
    {
        if (levelData != null && levelData.gameMode == GameMode.Level)
        {
            CheckLevelCompletionCriteria();
        }
    }

    public void StartLevelMode(int levelNum)
    {
        LevelDatabaseSO db = LevelDatabaseSO.LoadDefault();
        LevelDataSO dbLevel = db != null ? db.GetLevel(levelNum) : null;

        if (dbLevel != null)
        {
            levelData = dbLevel;
        }
        else
        {
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelDataSO>();
            }

            levelData.gameMode = GameMode.Level;
            levelData.levelNumber = levelNum;
            levelData.winCondition = LevelWinCondition.TargetScore;
            levelData.targetScore = 300 + (levelNum * 150);
            levelData.maxDrops = Mathf.Max(15, 35 - (levelNum / 5));
            levelData.blocks?.Clear();
        }

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

        bool hasRequiredBlocks = blockManager != null && blockManager.TotalRequiredBlocks > 0;
        bool blocksCleared = hasRequiredBlocks && blockManager.RemainingRequiredBlocks <= 0;
        bool scoreReached = currentScore >= levelData.targetScore;

        bool isWin = false;
        switch (levelData.winCondition)
        {
            case LevelWinCondition.DestroyAllBlocks:
                isWin = hasRequiredBlocks ? blocksCleared : scoreReached;
                break;
            case LevelWinCondition.BlocksAndScore:
                isWin = (hasRequiredBlocks ? blocksCleared : true) && scoreReached;
                break;
            case LevelWinCondition.TargetScore:
            default:
                isWin = scoreReached;
                break;
        }

        if (isWin)
        {
            CompleteLevel();
            return;
        }

        // Check Lose Condition when limited drops run out (wait for final balls & merges to settle)
        if (levelData.maxDrops > 0 && remainingDrops <= 0 && outOfMovesCoroutine == null)
        {
            outOfMovesCoroutine = StartCoroutine(WaitAndCheckOutOfMovesRoutine());
        }
    }

    private void CompleteLevel()
    {
        levelCompleted = true;
        if (outOfMovesCoroutine != null)
        {
            StopCoroutine(outOfMovesCoroutine);
            outOfMovesCoroutine = null;
        }

        if (dropper != null)
        {
            dropper.SetInputActive(false);
        }

        if (dangerLine != null)
        {
            dangerLine.SetSessionActive(false);
        }

        Debug.Log($"[GameplayController] Level {levelData.levelNumber} COMPLETED! Score: {currentScore}, Drops Used: {totalDropsUsed}");

        int targetForStars = Mathf.Max(100, levelData.targetScore);
        LevelRewardSummary reward;
        if (PlayerDataManager.HasInstance)
        {
            reward = PlayerDataManager.Instance.SaveLevelCompletionWithBreakdown(
                levelData.levelNumber,
                currentScore,
                remainingDrops,
                levelData.maxDrops,
                targetForStars
            );
        }
        else
        {
            int unused = levelData.maxDrops > 0 ? Mathf.Max(0, remainingDrops) : 0;
            int baseCoins = 50 + (levelData.levelNumber * 10) + 45;
            reward = new LevelRewardSummary
            {
                levelNumber = levelData.levelNumber,
                stars = 3,
                baseLevelCoins = baseCoins,
                unusedDrops = unused,
                unusedDropsBonusCoins = unused * 20,
                totalCoinsEarned = baseCoins + (unused * 20),
                hasUnclaimedMilestoneChest = levelData.levelNumber > 0 && (levelData.levelNumber % 5) == 0
            };
        }

        LastRewardSummary = reward;

        if (UIManager.HasInstance)
        {
            LevelWinPanel winPanel = UIManager.Instance.GetPanel<LevelWinPanel>();
            if (winPanel != null)
            {
                winPanel.SetupLevelWin(levelData.levelNumber, currentScore, totalDropsUsed, reward.stars);
                UIManager.Instance.ShowPanel<LevelWinPanel>(false);
            }
        }

        GameEvents.OnLevelCompleted?.Invoke(levelData.levelNumber, true);
        GameEvents.OnPlaySFX?.Invoke("LevelWin");
    }

    private IEnumerator WaitAndCheckOutOfMovesRoutine()
    {
        float initialWait = levelData != null ? Mathf.Max(0.55f, levelData.dropCooldown + 0.35f) : 0.85f;
        yield return new WaitForSeconds(initialWait);

        float settleTimer = 0f;
        const float maxSettleWait = 3.5f;

        while (settleTimer < maxSettleWait)
        {
            if (!isSessionActive || levelCompleted || remainingDrops > 0)
            {
                outOfMovesCoroutine = null;
                yield break;
            }

            bool mergesActive = BallMergeManager.HasInstance && BallMergeManager.Instance.ActiveMergeCount > 0;
            bool ballsMoving = false;
            if (!mergesActive)
            {
                Ball[] balls = FindObjectsByType<Ball>(FindObjectsSortMode.None);
                for (int i = 0; i < balls.Length; i++)
                {
                    if (balls[i] != null && balls[i].IsDropped && balls[i].Rigidbody != null)
                    {
                        if (balls[i].Rigidbody.linearVelocity.sqrMagnitude > 0.09f)
                        {
                            ballsMoving = true;
                            break;
                        }
                    }
                }
            }

            if (!mergesActive && !ballsMoving)
            {
                break;
            }

            settleTimer += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }

        outOfMovesCoroutine = null;

        if (!isSessionActive || levelCompleted || remainingDrops > 0) yield break;

        Debug.Log($"[GameplayController] Level {levelData.levelNumber} FAILED! Out of drops ({levelData.maxDrops}).");
        GameEvents.OnLevelCompleted?.Invoke(levelData.levelNumber, false);
        GameEvents.OnPlaySFX?.Invoke("LevelLose");
        TriggerGameOver();
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
        if (outOfMovesCoroutine != null)
        {
            StopCoroutine(outOfMovesCoroutine);
            outOfMovesCoroutine = null;
        }

        ClearAllBalls();
        if (BallMergeManager.HasInstance)
        {
            BallMergeManager.Instance.ResetMergeState();
        }

        currentScore = 0;
        totalDropsUsed = 0;
        levelCompleted = false;
        isSessionActive = true;

        if (blockManager == null)
        {
            blockManager = FindAnyObjectByType<LevelBlockManager>();
            if (blockManager == null && levelData != null && levelData.HasBlocks)
            {
                GameObject bmObj = new GameObject("LevelBlockManager");
                bmObj.transform.SetParent(transform, false);
                blockManager = bmObj.AddComponent<LevelBlockManager>();
            }
        }

        if (blockManager != null)
        {
            blockManager.LoadLevelBlocks(levelData, container);
        }

        if (levelData != null && levelData.gameMode == GameMode.Level)
        {
            remainingDrops = levelData.maxDrops;
            GameEvents.OnLevelStarted?.Invoke(levelData.levelNumber);
            GameEvents.OnRemainingDropsChanged?.Invoke(remainingDrops);
            Debug.Log($"[GameplayController] Started LEVEL Mode: Level {levelData.levelNumber} (Win: {levelData.winCondition}, Max Drops: {levelData.maxDrops})");
        }
        else
        {
            remainingDrops = -1; // Unlimited
            Debug.Log("[GameplayController] Started ENDLESS Mode: Unlimited drops.");
        }

        if (dropper != null && levelData != null)
        {
            dropper.SetInputActive(true);
            dropper.Setup(levelData);
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
        EndSessionInternal(resetScore: true);
    }

    private void EndSessionInternal(bool resetScore)
    {
        if (outOfMovesCoroutine != null)
        {
            StopCoroutine(outOfMovesCoroutine);
            outOfMovesCoroutine = null;
        }

        ClearAllBalls();
        if (BallMergeManager.HasInstance)
        {
            BallMergeManager.Instance.ResetMergeState();
        }

        if (blockManager != null)
        {
            blockManager.ClearLevelBlocks();
        }

        isSessionActive = false;
        if (resetScore)
        {
            currentScore = 0;
        }
        totalDropsUsed = 0;
        levelCompleted = false;

        Debug.Log("[GameplayController] Ball Merge session ended and container cleared.");

        if (dropper != null)
        {
            dropper.ClearHeldBall();
            dropper.SetInputActive(false);
        }

        if (dangerLine != null)
        {
            dangerLine.ResetTimer();
            dangerLine.SetSessionActive(false);
        }
    }

    /// <summary>
    /// Helper to trigger GameOver from gameplay rules (e.g. overflow or out of moves).
    /// </summary>
    public void TriggerGameOver()
    {
        int finalScore = currentScore;
        EndSessionInternal(resetScore: false);
        if (GameManager.HasInstance)
        {
            GameManager.Instance.TriggerGameOver(finalScore);
        }
    }
}
