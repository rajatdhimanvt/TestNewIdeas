using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Pause UI Panel.
/// Provides options to Resume gameplay, Restart level, open Settings, or return to Main Menu.
/// </summary>
public class PausePanel : UIBasePanel
{
    [Header("Header")]
    [SerializeField] private TMP_Text titleText;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);
        showBackgroundDim = true;
        animateScale = true;

        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    public override void OnShow()
    {
        base.OnShow();

        GameplayController gc = FindAnyObjectByType<GameplayController>();
        if (gc != null && titleText != null)
        {
            if (gc.ActiveLevelData != null && gc.ActiveLevelData.gameMode == GameMode.Level)
            {
                titleText.text = $"LEVEL {gc.ActiveLevelData.levelNumber} PAUSED";
            }
            else
            {
                titleText.text = "GAME PAUSED";
            }
        }
    }

    private void OnResumeClicked()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.SetPause(false);
        }

        if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
        }
    }

    private void OnRestartClicked()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnSettingsClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<SettingsPanel>(true);
        }
    }

    private void OnMainMenuClicked()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.SetPause(false);
            GameManager.Instance.ReturnToMainMenu();
        }
    }
}
