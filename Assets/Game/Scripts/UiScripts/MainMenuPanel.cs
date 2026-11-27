using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Production-grade Main Menu UI Panel.
/// Provides entry points for Endless Play, Campaign Levels, Ball Kits, Themes, Settings, and Quit dialog
/// with playful hyper-casual DOTween idle animations.
/// </summary>
public class MainMenuPanel : UIBasePanel
{
    [Header("Mode Buttons")]
    [SerializeField] private Button playEndlessButton;
    [SerializeField] private Transform playButtonTransform;
    [SerializeField] private Button playLevel1Button;
    [SerializeField] private TMP_Text playLevelButtonText;
    [SerializeField] private Button levelMapButton;
    [SerializeField] private Button dailyRewardButton;

    [Header("Daily & Campaign Badges")]
    [SerializeField] private GameObject dailyNotificationBadge;
    [SerializeField] private TMP_Text dailyBadgeText;
    [SerializeField] private TMP_Text campaignLevelText;
    [SerializeField] private TMP_Text campaignStarsText;

    [Header("Shop & Customization Buttons")]
    [SerializeField] private Button ballKitsButton;
    [SerializeField] private Button themesButton;

    [Header("Secondary Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button switchGameButton;

    [Header("Score & Info Display")]
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text coinBalanceText;

    [Header("Hero Mascots & Idle Animation")]
    [SerializeField] private Transform logoContainer;
    [SerializeField] private List<Transform> decorativeBalls = new List<Transform>();

    private Vector2 logoRestAnchoredPos;
    private bool hasCachedRestPos = false;
    private readonly List<Vector2> ballRestPos = new List<Vector2>();

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
        if (switchGameButton != null) switchGameButton.onClick.AddListener(OnSwitchGameClicked);

        CacheRestPositions();
        PlayerDataManager.OnCoinsChanged += UpdateCoinDisplay;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopIdleAnimations();
        PlayerDataManager.OnCoinsChanged -= UpdateCoinDisplay;
    }

    public override void OnShow()
    {
        base.OnShow();
        UpdateDisplayInfo();
        StartIdleAnimations();
    }

    public override void OnHide()
    {
        StopIdleAnimations();
        base.OnHide();
    }

    private void CacheRestPositions()
    {
        if (hasCachedRestPos) return;
        hasCachedRestPos = true;

        if (logoContainer is RectTransform logoRt)
        {
            logoRestAnchoredPos = logoRt.anchoredPosition;
        }

        ballRestPos.Clear();
        for (int i = 0; i < decorativeBalls.Count; i++)
        {
            if (decorativeBalls[i] is RectTransform brt)
            {
                ballRestPos.Add(brt.anchoredPosition);
            }
            else
            {
                ballRestPos.Add(Vector2.zero);
            }
        }
    }

    private void StartIdleAnimations()
    {
        CacheRestPositions();
        StopIdleAnimations();

        if (playButtonTransform != null)
        {
            playButtonTransform.localScale = Vector3.one;
            playButtonTransform.DOScale(1.045f, 0.85f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        if (logoContainer is RectTransform logoRt)
        {
            logoRt.anchoredPosition = logoRestAnchoredPos;
            logoRt.DOAnchorPosY(logoRestAnchoredPos.y + 8f, 1.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        for (int i = 0; i < decorativeBalls.Count; i++)
        {
            Transform ballTf = decorativeBalls[i];
            if (ballTf == null) continue;

            if (ballTf is RectTransform brt && i < ballRestPos.Count)
            {
                brt.anchoredPosition = ballRestPos[i];
                brt.DOAnchorPosY(ballRestPos[i].y + 11f, 1.15f + (i * 0.22f))
                    .SetDelay(i * 0.18f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            ballTf.localScale = Vector3.one;
            ballTf.DOScale(new Vector3(1.06f, 0.95f, 1f), 0.9f + (i * 0.15f))
                .SetDelay(i * 0.12f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        if (dailyNotificationBadge != null && dailyNotificationBadge.activeSelf)
        {
            dailyNotificationBadge.transform.localScale = Vector3.one;
            dailyNotificationBadge.transform.DOScale(1.12f, 0.55f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }

    private void StopIdleAnimations()
    {
        if (playButtonTransform != null)
        {
            playButtonTransform.DOKill();
            playButtonTransform.localScale = Vector3.one;
        }
        if (logoContainer != null)
        {
            logoContainer.DOKill();
        }
        for (int i = 0; i < decorativeBalls.Count; i++)
        {
            if (decorativeBalls[i] != null)
            {
                decorativeBalls[i].DOKill();
                decorativeBalls[i].localScale = Vector3.one;
            }
        }
        if (dailyNotificationBadge != null)
        {
            dailyNotificationBadge.transform.DOKill();
            dailyNotificationBadge.transform.localScale = Vector3.one;
        }
    }

    public void UpdateDisplayInfo()
    {
        if (highScoreText != null)
        {
            int hs = PlayerPrefs.GetInt("BallMerge_HighScore", 0);
            highScoreText.text = $"BEST: {hs:N0}";
        }

        UpdateCoinDisplay(PlayerDataManager.HasInstance ? PlayerDataManager.Instance.Coins : 0);

        if (PlayerDataManager.HasInstance)
        {
            int highest = PlayerDataManager.Instance.HighestUnlockedLevel;
            int stars = PlayerDataManager.Instance.GetTotalStarsEarned(50);

            if (campaignLevelText != null) campaignLevelText.text = $"LEVEL {highest}";
            if (campaignStarsText != null) campaignStarsText.text = $"STARS: {stars} / 150";
            if (playLevelButtonText != null) playLevelButtonText.text = "PLAY LVL 1";

            bool dailyReady = PlayerDataManager.Instance.CanClaimDailyReward() || PlayerDataManager.Instance.CanFreeSpin();
            if (dailyNotificationBadge != null) dailyNotificationBadge.SetActive(dailyReady);
            if (dailyBadgeText != null) dailyBadgeText.text = "FREE!";
        }
    }

    private void UpdateCoinDisplay(int coins)
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = $"{coins:N0} COINS";
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
            if (gc != null) gc.StartLevelMode(1);
            if (GameManager.HasInstance)
            {
                GameManager.Instance.StartGame();
            }
            else if (uiManager != null)
            {
                uiManager.ShowPanel<GameplayHUDPanel>();
            }
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

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartGame();
        }
        else if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
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

    private void OnSwitchGameClicked()
    {
        StopIdleAnimations();
        DOTween.KillAll();
        Time.timeScale = 1f;

        const string targetScene = "GeometryBoxMaze";
        const string targetPath = "Assets/GeometryBoxMaze/Scenes/GeometryBoxMaze.unity";
        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            return;
        }

#if UNITY_EDITOR
        if (System.IO.File.Exists(targetPath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                targetPath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        Debug.LogWarning($"[MainMenuPanel] Scene '{targetScene}' not found in Build Settings.");
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