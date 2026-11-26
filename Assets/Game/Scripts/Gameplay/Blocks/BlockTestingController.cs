using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Direct-play scene controller & lightweight HUD for Block testing.unity.
/// Lets designers immediately drop balls, break blocks, switch between levels,
/// and test levels directly from the Visual Level Editor without splash/menu delays.
/// </summary>
public class BlockTestingController : MonoBehaviour
{
    public const string PREF_PLAYTEST_LEVEL_GUID = "BallMerge_PlayTestLevelGuid";

    [Header("Test Level Selection")]
    [SerializeField] private LevelDataSO startLevel;
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Scene References")]
    [SerializeField] private GameplayController gameplayController;
    [SerializeField] private ContainerBoundary containerBoundary;
    [SerializeField] private LevelBlockManager blockManager;
    [SerializeField] private BallDropper ballDropper;
    [SerializeField] private DangerLine dangerLine;

    [Header("Test HUD References")]
    [SerializeField] private TMP_Text levelTitleText;
    [SerializeField] private TMP_Text blocksText;
    [SerializeField] private Image blocksProgressFill;
    [SerializeField] private TMP_Text dropsText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text nextBallNameText;
    [SerializeField] private Image nextBallImage;
    [SerializeField] private Button prevLevelButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;

    [Header("Result Overlay")]
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private Transform resultCard;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultStarsText;
    [SerializeField] private TMP_Text resultSubtitleText;
    [SerializeField] private Button resultNextButton;
    [SerializeField] private Button resultRetryButton;

    private LevelDatabaseSO database;
    private int lastShownScore = -1;

    private void Awake()
    {
        if (gameplayController == null) gameplayController = FindAnyObjectByType<GameplayController>();
        if (containerBoundary == null) containerBoundary = FindAnyObjectByType<ContainerBoundary>();
        if (blockManager == null) blockManager = FindAnyObjectByType<LevelBlockManager>();
        if (ballDropper == null) ballDropper = FindAnyObjectByType<BallDropper>();
        if (dangerLine == null) dangerLine = FindAnyObjectByType<DangerLine>();

        database = LevelDatabaseSO.LoadDefault();
    }

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnRemainingDropsChanged += HandleDropsChanged;
        GameEvents.OnBlocksRemainingChanged += HandleBlocksChanged;
        GameEvents.OnBallDropped += HandleBallDropped;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnRemainingDropsChanged -= HandleDropsChanged;
        GameEvents.OnBlocksRemainingChanged -= HandleBlocksChanged;
        GameEvents.OnBallDropped -= HandleBallDropped;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        WireButtons();
        if (resultOverlay != null) resultOverlay.SetActive(false);

        ResolveEditorPlayTestOverride();
        LoadAndPlayCurrentLevel();
    }

    private void WireButtons()
    {
        HookButton(prevLevelButton, PlayPreviousLevel);
        HookButton(nextLevelButton, PlayNextLevel);
        HookButton(restartButton, LoadAndPlayCurrentLevel);
        HookButton(resultNextButton, PlayNextLevel);
        HookButton(resultRetryButton, LoadAndPlayCurrentLevel);
    }

    private static void HookButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(Vector3.one * -0.08f, 0.14f, 8, 1f).SetUpdate(true);
            GameEvents.OnPlaySFX?.Invoke("Click");
            action?.Invoke();
        });
    }

    private void ResolveEditorPlayTestOverride()
    {
#if UNITY_EDITOR
        string levelPath = UnityEditor.EditorPrefs.GetString(PREF_PLAYTEST_LEVEL_GUID, string.Empty);
        if (!string.IsNullOrEmpty(levelPath))
        {
            UnityEditor.EditorPrefs.DeleteKey(PREF_PLAYTEST_LEVEL_GUID);
            LevelDataSO overrideLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>(levelPath);
            if (overrideLevel != null)
            {
                startLevel = overrideLevel;
                if (database != null && database.levels != null)
                {
                    int idx = database.levels.IndexOf(overrideLevel);
                    if (idx >= 0) currentLevelIndex = idx;
                }
                return;
            }
        }
#endif
        if (startLevel == null && database != null && database.levels != null && database.levels.Count > 0)
        {
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, database.levels.Count - 1);
            startLevel = database.levels[currentLevelIndex];
        }
        else if (startLevel != null && database != null && database.levels != null)
        {
            int idx = database.levels.IndexOf(startLevel);
            if (idx >= 0) currentLevelIndex = idx;
        }
    }

    public void PlayPreviousLevel()
    {
        if (database == null || database.levels == null || database.levels.Count == 0)
        {
            LoadAndPlayCurrentLevel();
            return;
        }
        currentLevelIndex = (currentLevelIndex - 1 + database.levels.Count) % database.levels.Count;
        startLevel = database.levels[currentLevelIndex];
        LoadAndPlayCurrentLevel();
    }

    public void PlayNextLevel()
    {
        if (database == null || database.levels == null || database.levels.Count == 0)
        {
            LoadAndPlayCurrentLevel();
            return;
        }
        currentLevelIndex = (currentLevelIndex + 1) % database.levels.Count;
        startLevel = database.levels[currentLevelIndex];
        LoadAndPlayCurrentLevel();
    }

    public void LoadAndPlayCurrentLevel()
    {
        if (resultOverlay != null) resultOverlay.SetActive(false);

        if (startLevel == null)
        {
            Debug.LogWarning("[BlockTestingController] No LevelDataSO assigned to test!");
            return;
        }

        if (gameplayController != null)
        {
            gameplayController.SetGameplayVisible(true);
            gameplayController.InitializeLevel(startLevel);
        }

        if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.InGame)
        {
            // Changing state to InGame automatically triggers GameplayController.StartSession()
            GameManager.Instance.ChangeState(GameState.InGame);
        }
        else if (gameplayController != null)
        {
            gameplayController.StartSession();
        }

        RefreshStaticHUD();
        StartCoroutine(DelayedNextBallRefresh());
    }

    private IEnumerator DelayedNextBallRefresh()
    {
        yield return null;
        RefreshNextBallPreview();
    }

    private void RefreshStaticHUD()
    {
        if (startLevel == null) return;

        if (levelTitleText != null)
        {
            string title = string.IsNullOrEmpty(startLevel.levelName) ? $"LEVEL {startLevel.levelNumber}" : startLevel.levelName;
            levelTitleText.text = $"LVL {startLevel.levelNumber}: {title}";
        }

        lastShownScore = 0;
        HandleScoreChanged(0);
        if (gameplayController != null)
        {
            HandleDropsChanged(gameplayController.RemainingDrops);
        }
        if (blockManager != null)
        {
            HandleBlocksChanged(blockManager.RemainingRequiredBlocks, blockManager.TotalRequiredBlocks);
        }
    }

    private void HandleScoreChanged(int _)
    {
        if (scoreText == null || gameplayController == null) return;
        int cur = gameplayController.CurrentScore;
        scoreText.text = $"SCORE: {cur}";
        if (cur > lastShownScore && lastShownScore >= 0)
        {
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * 0.14f, 0.18f, 6, 0.5f).SetLink(scoreText.gameObject);
        }
        lastShownScore = cur;
    }

    private void HandleDropsChanged(int remaining)
    {
        if (dropsText == null || startLevel == null) return;
        dropsText.text = startLevel.maxDrops > 0
            ? $"DROPS: {remaining} / {startLevel.maxDrops}"
            : "DROPS: UNLIMITED";

        dropsText.transform.DOKill();
        dropsText.transform.localScale = Vector3.one;
        if (startLevel.maxDrops > 0 && remaining > 0 && remaining <= 3)
        {
            dropsText.color = new Color(1f, 0.28f, 0.28f);
            dropsText.transform.DOScale(1.12f, 0.38f).SetLoops(-1, LoopType.Yoyo).SetLink(dropsText.gameObject);
        }
        else
        {
            dropsText.color = new Color(1f, 0.52f, 0.52f);
        }
    }

    private void HandleBlocksChanged(int remaining, int total)
    {
        if (blocksText != null)
        {
            blocksText.text = total > 0 ? $"BLOCKS: {remaining} / {total}" : "NO TARGET BLOCKS";
            blocksText.transform.DOKill();
            blocksText.transform.localScale = Vector3.one;
            if (total > 0 && remaining < total)
            {
                blocksText.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.5f).SetLink(blocksText.gameObject);
            }
        }

        if (blocksProgressFill != null)
        {
            float clearedRatio = total > 0 ? Mathf.Clamp01((float)(total - remaining) / total) : 1f;
            blocksProgressFill.DOKill();
            blocksProgressFill.DOFillAmount(clearedRatio, 0.28f).SetEase(Ease.OutCubic).SetLink(blocksProgressFill.gameObject);
        }
    }

    private void HandleBallDropped()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedNextBallRefresh());
        }
    }

    private static Sprite fallbackCircleSprite;

    private void RefreshNextBallPreview()
    {
        if (ballDropper == null || ballDropper.NextBallData == null) return;

        BallDataSO next = ballDropper.NextBallData;
        if (nextBallNameText != null) nextBallNameText.text = next.ballName;
        if (nextBallImage != null)
        {
            nextBallImage.color = next.ballColor;
            nextBallImage.sprite = next.ballSprite != null ? next.ballSprite : GetFallbackCircleSprite();
            nextBallImage.transform.DOKill();
            nextBallImage.transform.localScale = Vector3.one * 0.65f;
            nextBallImage.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetLink(nextBallImage.gameObject);
        }
    }

    private static Sprite GetFallbackCircleSprite()
    {
        if (fallbackCircleSprite != null) return fallbackCircleSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01((r - d) * 0.7f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        fallbackCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return fallbackCircleSprite;
    }

    private void HandleLevelCompleted(int levelNum, bool isSuccess)
    {
        ShowResult(
            isSuccess,
            isSuccess ? $"LEVEL {levelNum} CLEARED!" : "OUT OF DROPS!",
            isSuccess
                ? $"Score: {(gameplayController != null ? gameplayController.CurrentScore : 0)} | Drops Used: {(gameplayController != null ? gameplayController.TotalDropsUsed : 0)}"
                : "Try merging balls right beside the blocks!"
        );
    }

    private void HandleGameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.GameOver && (resultOverlay == null || !resultOverlay.activeSelf))
        {
            ShowResult(false, "OVERFLOW / GAME OVER", "Balls crossed the danger line! Try again.");
        }
    }

    private void ShowResult(bool isWin, string title, string subtitle)
    {
        if (resultOverlay == null) return;
        resultOverlay.SetActive(true);

        Image bgImg = resultOverlay.GetComponent<Image>();
        if (bgImg != null)
        {
            bgImg.DOKill();
            Color targetCol = new Color(0f, 0f, 0f, 0.78f);
            bgImg.color = new Color(0f, 0f, 0f, 0f);
            bgImg.DOColor(targetCol, 0.25f).SetUpdate(true).SetLink(resultOverlay);
        }

        if (resultCard != null)
        {
            resultCard.DOKill();
            resultCard.localScale = Vector3.one * 0.72f;
            resultCard.DOScale(1f, 0.32f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(resultCard.gameObject);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
            resultTitleText.color = isWin ? new Color(0.25f, 0.95f, 0.55f) : new Color(1f, 0.42f, 0.35f);
        }

        if (resultStarsText != null)
        {
            if (isWin)
            {
                int stars = 1;
                if (startLevel != null && startLevel.maxDrops > 0 && gameplayController != null)
                {
                    float ratio = (float)gameplayController.RemainingDrops / startLevel.maxDrops;
                    if (ratio >= 0.45f) stars = 3;
                    else if (ratio >= 0.2f) stars = 2;
                }
                else
                {
                    stars = 3;
                }

                string s = "";
                for (int i = 0; i < 3; i++) s += i < stars ? "★ " : "☆ ";
                resultStarsText.text = s.Trim();
                resultStarsText.gameObject.SetActive(true);
                resultStarsText.transform.DOKill();
                resultStarsText.transform.localScale = Vector3.zero;
                resultStarsText.transform.DOScale(1f, 0.36f).SetDelay(0.14f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(resultStarsText.gameObject);
            }
            else
            {
                resultStarsText.gameObject.SetActive(false);
            }
        }

        if (resultSubtitleText != null)
        {
            resultSubtitleText.text = subtitle;
        }
        if (resultNextButton != null)
        {
            resultNextButton.gameObject.SetActive(isWin && database != null && database.TotalLevels > 1);
        }
    }
}
