using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Game Over / Defeat Panel.
/// Displays final score, best score, and provides options to Restart or return to Main Menu.
/// </summary>
public class GameOverPanel : UIBasePanel
{
    [Header("Header & Displays")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    public override void OnShow()
    {
        base.OnShow();

        GameplayController gc = FindAnyObjectByType<GameplayController>();
        if (gc != null)
        {
            if (finalScoreText != null) finalScoreText.text = $"SCORE: {gc.CurrentScore}";
            if (highScoreText != null) highScoreText.text = $"BEST: {gc.HighScore}";

            if (titleText != null)
            {
                if (gc.ActiveLevelData != null && gc.ActiveLevelData.gameMode == GameMode.Level)
                {
                    titleText.text = "LEVEL FAILED";
                }
                else
                {
                    titleText.text = "GAME OVER";
                }
            }
        }
    }

    private void OnRestartClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnMainMenuClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<MainMenuPanel>();
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }
}
