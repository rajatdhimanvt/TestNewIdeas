using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Gameplay HUD Panel.
/// Displays active score, high score, game mode title, remaining drops (for Level mode),
/// next ball preview, and provides a pause button.
/// </summary>
public class GameplayHUDPanel : UIBasePanel
{
    [Header("Score Displays")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Mode & Goals")]
    [SerializeField] private TMP_Text modeTitleText;
    [SerializeField] private GameObject dropsContainer;
    [SerializeField] private TMP_Text dropsText;
    [SerializeField] private GameObject blocksContainer;
    [SerializeField] private TMP_Text blocksText;

    [Header("Next Ball Preview")]
    [SerializeField] private TMP_Text nextBallNameText;
    [SerializeField] private Image nextBallImage;

    [Header("Controls")]
    [SerializeField] private Button pauseButton;

    private BallDropper ballDropper;
    private GameplayController gameplayController;

    public override void Init(UIManager manager)
    {
        base.Init(manager);
        EnsureBlocksCounter();

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
    }

    private void EnsureBlocksCounter()
    {
        if (blocksContainer != null && blocksText != null) return;

        Transform existing = transform.Find("BlocksContainer");
        if (existing != null)
        {
            blocksContainer = existing.gameObject;
            blocksText = existing.GetComponentInChildren<TMP_Text>(true);
            return;
        }

        GameObject obj = new GameObject("BlocksContainer");
        obj.transform.SetParent(transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        if (dropsContainer != null)
        {
            RectTransform dropsRt = dropsContainer.GetComponent<RectTransform>();
            rt.anchorMin = dropsRt.anchorMin;
            rt.anchorMax = dropsRt.anchorMax;
            rt.pivot = dropsRt.pivot;
            rt.anchoredPosition = dropsRt.anchoredPosition + new Vector2(0f, -55f);
            rt.sizeDelta = new Vector2(dropsRt.sizeDelta.x, 50f);
        }
        else
        {
            rt.anchoredPosition = new Vector2(360f, 785f);
            rt.sizeDelta = new Vector2(300f, 50f);
        }

        GameObject txtObj = new GameObject("BlocksText");
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TMP_Text tmp = txtObj.AddComponent<TextMeshProUGUI>();
        if (dropsText != null && dropsText.font != null) tmp.font = dropsText.font;
        else if (scoreText != null && scoreText.font != null) tmp.font = scoreText.font;
        else BallMergeUITheme.ApplyFont(tmp);
        tmp.text = "BLOCKS: 0 / 0";
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.72f, 0.25f);
        tmp.alignment = TextAlignmentOptions.Right;

        blocksContainer = obj;
        blocksText = tmp;
        blocksContainer.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += UpdateScore;
        GameEvents.OnHighScoreChanged += UpdateHighScore;
        GameEvents.OnRemainingDropsChanged += UpdateRemainingDrops;
        GameEvents.OnBlocksRemainingChanged += UpdateBlocksRemaining;
        GameEvents.OnBallDropped += UpdateNextBallPreview;
        GameEvents.OnNextBallUpdated += UpdateNextBallPreview;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= UpdateScore;
        GameEvents.OnHighScoreChanged -= UpdateHighScore;
        GameEvents.OnRemainingDropsChanged -= UpdateRemainingDrops;
        GameEvents.OnBlocksRemainingChanged -= UpdateBlocksRemaining;
        GameEvents.OnBallDropped -= UpdateNextBallPreview;
        GameEvents.OnNextBallUpdated -= UpdateNextBallPreview;
    }

    public override void OnShow()
    {
        base.OnShow();

        if (gameplayController == null) gameplayController = FindAnyObjectByType<GameplayController>();
        if (ballDropper == null) ballDropper = FindAnyObjectByType<BallDropper>();

        RefreshAllHUD();
    }

    public void RefreshAllHUD()
    {
        EnsureBlocksCounter();
        if (gameplayController != null)
        {
            UpdateScore(gameplayController.CurrentScore);
            UpdateHighScore(gameplayController.HighScore);

            LevelDataSO levelData = gameplayController.ActiveLevelData;
            if (levelData != null)
            {
                if (levelData.gameMode == GameMode.Level)
                {
                    if (modeTitleText != null) modeTitleText.text = $"LEVEL {levelData.levelNumber}";
                    if (dropsContainer != null) dropsContainer.SetActive(levelData.maxDrops > 0);
                    UpdateRemainingDrops(gameplayController.RemainingDrops);

                    if (LevelBlockManager.HasInstance && LevelBlockManager.Instance.TotalRequiredBlocks > 0)
                    {
                        UpdateBlocksRemaining(LevelBlockManager.Instance.RemainingRequiredBlocks, LevelBlockManager.Instance.TotalRequiredBlocks);
                    }
                    else
                    {
                        UpdateBlocksRemaining(0, 0);
                    }
                }
                else
                {
                    if (modeTitleText != null) modeTitleText.text = "ENDLESS MODE";
                    if (dropsContainer != null) dropsContainer.SetActive(false);
                    if (blocksContainer != null) blocksContainer.SetActive(false);
                }
            }
        }

        UpdateNextBallPreview();
    }

    private void UpdateBlocksRemaining(int remaining, int total)
    {
        EnsureBlocksCounter();
        if (blocksContainer != null)
        {
            blocksContainer.SetActive(total > 0);
        }
        if (blocksText != null && total > 0)
        {
            blocksText.text = $"BLOCKS: {remaining} / {total}";
            blocksText.transform.DOKill();
            blocksText.transform.localScale = Vector3.one;
            if (remaining < total)
            {
                blocksText.transform.DOPunchScale(Vector3.one * 0.14f, 0.18f, 6, 0.5f).SetLink(blocksText.gameObject);
            }
        }
    }

    private int lastDisplayedScore = -1;

    private void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0");
            if (score > lastDisplayedScore && lastDisplayedScore >= 0)
            {
                scoreText.transform.DOKill();
                scoreText.transform.localScale = Vector3.one;
                scoreText.transform.DOPunchScale(Vector3.one * 0.14f, 0.16f, 6, 0.5f).SetLink(scoreText.gameObject);
            }
            lastDisplayedScore = score;
        }
    }

    private void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"BEST: {highScore}";
        }
    }

    private void UpdateRemainingDrops(int remainingDrops)
    {
        if (dropsText != null)
        {
            if (gameplayController != null && gameplayController.ActiveLevelData != null)
            {
                int maxDrops = gameplayController.ActiveLevelData.maxDrops;
                dropsText.text = $"DROPS: {remainingDrops} / {maxDrops}";
                dropsText.transform.DOKill();
                dropsText.transform.localScale = Vector3.one;
                if (maxDrops > 0 && remainingDrops > 0 && remainingDrops <= 3)
                {
                    dropsText.color = new Color(1f, 0.25f, 0.25f);
                    dropsText.transform.DOScale(1.12f, 0.36f).SetLoops(-1, LoopType.Yoyo).SetLink(dropsText.gameObject);
                }
                else
                {
                    dropsText.color = new Color(1f, 0.45f, 0.45f);
                }
            }
            else
            {
                dropsText.text = $"DROPS: {remainingDrops}";
            }
        }
    }

    private void UpdateNextBallPreview()
    {
        if (ballDropper == null) ballDropper = FindAnyObjectByType<BallDropper>();

        if (ballDropper != null && ballDropper.NextBallData != null)
        {
            BallDataSO nextData = ballDropper.NextBallData;
            if (nextBallNameText != null)
            {
                nextBallNameText.text = nextData.ballName;
            }

            if (nextBallImage != null)
            {
                nextBallImage.color = nextData.ballColor;
                Sprite previewSprite = nextData.ballSprite;
                if (previewSprite == null && BallMergeManager.HasInstance)
                {
                    previewSprite = BallMergeManager.Instance.GetBallDisplaySprite();
                }
                if (previewSprite != null)
                {
                    nextBallImage.sprite = previewSprite;
                }
                nextBallImage.transform.DOKill();
                nextBallImage.transform.localScale = Vector3.one * 0.65f;
                nextBallImage.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetLink(nextBallImage.gameObject);
            }
        }
    }

    private void OnPauseClicked()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.SetPause(true);
        }
        if (uiManager != null)
        {
            uiManager.ShowPanel<PausePanel>(true);
        }
    }
}
