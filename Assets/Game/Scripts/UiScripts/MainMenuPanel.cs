using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Main Menu UI Panel.
/// Provides entry points for Endless Play, Level 1 Campaign, Ball Kits, Themes, Settings, and Quit dialog.
/// </summary>
public class MainMenuPanel : UIBasePanel
{
    [Header("Mode Buttons")]
    [SerializeField] private Button playEndlessButton;
    [SerializeField] private Button playLevel1Button;

    [Header("Shop & Customization Buttons")]
    [SerializeField] private Button ballKitsButton;
    [SerializeField] private Button themesButton;

    [Header("Secondary Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Score & Info Display")]
    [SerializeField] private TMP_Text highScoreText;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (playEndlessButton != null) playEndlessButton.onClick.AddListener(OnPlayEndlessClicked);
        if (playLevel1Button != null) playLevel1Button.onClick.AddListener(OnPlayLevel1Clicked);
        if (ballKitsButton != null) ballKitsButton.onClick.AddListener(OnBallKitsClicked);
        if (themesButton != null) themesButton.onClick.AddListener(OnThemesClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    public override void OnShow()
    {
        base.OnShow();
        UpdateDisplayInfo();
    }

    public void UpdateDisplayInfo()
    {
        if (highScoreText != null)
        {
            int hs = PlayerPrefs.GetInt("BallMerge_HighScore", 0);
            highScoreText.text = $"BEST SCORE: {hs}";
        }
    }

    private void OnPlayEndlessClicked()
    {
        Debug.Log("[MainMenuPanel] Play Endless clicked.");
        LoadLevelDataAndStart("LevelData_Endless.asset");
    }

    private void OnPlayLevel1Clicked()
    {
        Debug.Log("[MainMenuPanel] Play Level 1 clicked.");
        LoadLevelDataAndStart("LevelData_Level1.asset");
    }

    private void LoadLevelDataAndStart(string assetFileName)
    {
#if UNITY_EDITOR
        LevelDataSO data = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>($"Assets/Game/Data/{assetFileName}");
        if (data != null)
        {
            GameplayController gc = FindAnyObjectByType<GameplayController>();
            if (gc != null)
            {
                gc.InitializeLevel(data);
            }
        }
#endif
        if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnBallKitsClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<BallKitShopPanel>(false);
        }
    }

    private void OnThemesClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<ThemeShopPanel>(false);
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