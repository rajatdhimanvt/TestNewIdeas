using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Level Victory Panel.
/// Displays level completion victory banner, score stats, and options to play Next Level or Replay.
/// </summary>
public class LevelWinPanel : UIBasePanel
{
    [Header("Displays")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text dropsUsedText;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button mainMenuButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(OnReplayClicked);
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
            if (gc.ActiveLevelData != null)
            {
                if (titleText != null) titleText.text = $"LEVEL {gc.ActiveLevelData.levelNumber} PASSED!";
            }

            if (scoreText != null) scoreText.text = $"FINAL SCORE: {gc.CurrentScore}";
            if (dropsUsedText != null) dropsUsedText.text = $"DROPS USED: {gc.TotalDropsUsed}";
        }
    }

    private void OnNextLevelClicked()
    {
        Debug.Log("[LevelWinPanel] Next level clicked.");
        if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnReplayClicked()
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
