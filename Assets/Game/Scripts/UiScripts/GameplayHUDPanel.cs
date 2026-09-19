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

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += UpdateScore;
        GameEvents.OnHighScoreChanged += UpdateHighScore;
        GameEvents.OnRemainingDropsChanged += UpdateRemainingDrops;
        GameEvents.OnBallDropped += UpdateNextBallPreview;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= UpdateScore;
        GameEvents.OnHighScoreChanged -= UpdateHighScore;
        GameEvents.OnRemainingDropsChanged -= UpdateRemainingDrops;
        GameEvents.OnBallDropped -= UpdateNextBallPreview;
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
                    if (dropsContainer != null) dropsContainer.SetActive(true);
                    UpdateRemainingDrops(gameplayController.RemainingDrops);
                }
                else
                {
                    if (modeTitleText != null) modeTitleText.text = "ENDLESS MODE";
                    if (dropsContainer != null) dropsContainer.SetActive(false);
                }
            }
        }

        UpdateNextBallPreview();
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
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
                if (nextData.ballSprite != null)
                {
                    nextBallImage.sprite = nextData.ballSprite;
                }
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
