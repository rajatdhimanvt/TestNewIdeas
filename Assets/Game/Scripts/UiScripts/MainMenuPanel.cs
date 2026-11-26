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
    [SerializeField] private Button levelMapButton;
    [SerializeField] private Button dailyRewardButton;

    [Header("Shop & Customization Buttons")]
    [SerializeField] private Button ballKitsButton;
    [SerializeField] private Button themesButton;

    [Header("Secondary Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Score & Info Display")]
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text coinBalanceText;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (playEndlessButton != null) playEndlessButton.onClick.AddListener(OnPlayEndlessClicked);
        if (playLevel1Button != null) playLevel1Button.onClick.AddListener(OnPlayLevel1Clicked);
        if (levelMapButton != null) levelMapButton.onClick.AddListener(OnLevelMapClicked);
        if (dailyRewardButton != null) dailyRewardButton.onClick.AddListener(OnDailyRewardClicked);
        if (ballKitsButton != null) ballKitsButton.onClick.AddListener(OnBallKitsClicked);
        if (themesButton != null) themesButton.onClick.AddListener(OnThemesClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        PlayerDataManager.OnCoinsChanged += UpdateCoinDisplay;
    }

    private void OnDestroy()
    {
        PlayerDataManager.OnCoinsChanged -= UpdateCoinDisplay;
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

        UpdateCoinDisplay(PlayerDataManager.HasInstance ? PlayerDataManager.Instance.Coins : 0);
    }

    private void UpdateCoinDisplay(int coins)
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = $"🪙 {coins:N0}";
        }
    }

    private void OnLevelMapClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<LevelSelectionPanel>(true);
        }
    }

    private void OnDailyRewardClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<DailyRewardPanel>(false);
        }
    }

    private void OnPlayEndlessClicked()
    {
        Debug.Log("[MainMenuPanel] Play Endless clicked.");
        LoadLevelDataAndStart("LevelData_Endless.asset", isEndless: true);
    }

    private void OnPlayLevel1Clicked()
    {
        Debug.Log("[MainMenuPanel] Play Level 1 clicked.");
        LevelDatabaseSO db = LevelDatabaseSO.LoadDefault();
        LevelDataSO level1 = db != null ? db.GetLevel(1) : null;
        if (level1 != null)
        {
            GameplayController gc = FindAnyObjectByType<GameplayController>();
            if (gc != null) gc.InitializeLevel(level1);
            if (uiManager != null) uiManager.ShowPanel<GameplayHUDPanel>();
            if (GameManager.HasInstance) GameManager.Instance.StartGame();
            return;
        }
        LoadLevelDataAndStart("LevelData_Level1.asset", isEndless: false);
    }

    private void LoadLevelDataAndStart(string assetFileName, bool isEndless)
    {
        LevelDataSO data = null;
#if UNITY_EDITOR
        data = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>($"Assets/Game/Data/{assetFileName}");
#endif
        GameplayController gc = FindAnyObjectByType<GameplayController>();
        if (gc != null)
        {
            if (data != null)
            {
                gc.InitializeLevel(data);
            }
            else if (isEndless)
            {
                gc.StartEndlessMode();
            }
            else if (gc.ActiveLevelData != null)
            {
                gc.InitializeLevel(gc.ActiveLevelData);
            }
        }

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
            uiManager.ShowPanel<BallKitShopPanel>(true);
        }
    }

    private void OnThemesClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<ThemeShopPanel>(true);
        }
    }

    private void OnSettingsClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowPanel<SettingsPanel>(true);
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