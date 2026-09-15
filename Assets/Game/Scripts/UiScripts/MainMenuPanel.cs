using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Main Menu UI Panel.
/// Provides entry points for Play, Settings, High Score display, and Quit dialog.
/// </summary>
public class MainMenuPanel : UIBasePanel
{
    [Header("Primary Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Player & Score Display (Optional)")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text levelText;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        UpdateDisplayInfo();
    }

    public void UpdateDisplayInfo()
    {
        // Optional hook for displaying player or game info
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuPanel] Play button clicked.");
        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnSettingsClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<SettingsPanel>(false);
        }
    }

    private void OnQuitClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPopup(
                title: "Quit Game",
                message: "Are you sure you want to exit the game?",
                onConfirm: () =>
                {
                    if (GameManager.HasInstance)
                    {
                        GameManager.Instance.QuitGame();
                    }
                },
                onCancel: null,
                confirmText: "Yes",
                cancelText: "No"
            );
        }
    }
}