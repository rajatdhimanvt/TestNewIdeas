using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Level Victory Panel.
/// Displays level completion victory banner, animated coin/unused-drop bonus breakdown,
/// milestone chest trigger, and options to play Next Level or Replay.
/// </summary>
public class LevelWinPanel : UIBasePanel
{
    [Header("Displays")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text dropsUsedText;
    [SerializeField] private TMP_Text coinBonusText;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button mainMenuButton;

    private int currentLevelNumber = 1;
    private Coroutine chestRoutine;

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

    public void SetupLevelWin(int levelNum, int finalScore, int dropsUsed, int stars)
    {
        currentLevelNumber = levelNum;

        if (titleText != null) titleText.text = $"LEVEL {levelNum} PASSED!";

        if (starsText != null)
        {
            string starStr = "";
            for (int s = 0; s < 3; s++) starStr += s < stars ? "★ " : "☆ ";
            starsText.text = starStr.Trim();
        }

        if (scoreText != null) scoreText.text = $"FINAL SCORE: {finalScore}";
        if (dropsUsedText != null) dropsUsedText.text = $"DROPS USED: {dropsUsed}";
    }

    public override void OnShow()
    {
        base.OnShow();
        StopChestRoutine();

        GameplayController gc = FindAnyObjectByType<GameplayController>();
        if (gc != null && gc.ActiveLevelData != null)
        {
            currentLevelNumber = gc.ActiveLevelData.levelNumber;
            if (titleText != null) titleText.text = $"LEVEL {currentLevelNumber} PASSED!";
            if (scoreText != null) scoreText.text = $"FINAL SCORE: {gc.CurrentScore}";
            if (dropsUsedText != null) dropsUsedText.text = $"DROPS USED: {gc.TotalDropsUsed}";

            ShowCoinBreakdown(gc.LastRewardSummary);
        }

        Transform windowBox = transform.Find("WindowBox");
        if (windowBox != null)
        {
            windowBox.DOKill();
            windowBox.localScale = Vector3.one * 0.72f;
            windowBox.DOScale(1f, 0.34f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        if (starsText != null)
        {
            starsText.transform.DOKill();
            starsText.transform.localScale = Vector3.zero;
            starsText.transform.DOScale(1f, 0.38f).SetDelay(0.14f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }
    }

    public override void OnHide()
    {
        StopChestRoutine();
        base.OnHide();
    }

    private void ShowCoinBreakdown(LevelRewardSummary reward)
    {
        EnsureCoinBonusText();
        if (coinBonusText == null || reward.totalCoinsEarned <= 0) return;

        string detail = reward.unusedDrops > 0
            ? $"Base +{reward.baseLevelCoins}  |  {reward.unusedDrops} Drops × 20 = +{reward.unusedDropsBonusCoins}"
            : $"Level Reward";

        int displayed = 0;
        int lastTick = -1;
        coinBonusText.text = $"{detail}\n+0 COINS";
        DOTween.To(() => displayed, v =>
        {
            displayed = v;
            coinBonusText.text = $"{detail}\n+{displayed:N0} COINS EARNED!";
            int step = displayed / 25;
            if (step > lastTick)
            {
                lastTick = step;
                GameEvents.OnPlaySFX?.Invoke("CoinTick");
            }
        }, reward.totalCoinsEarned, 0.75f).SetDelay(0.2f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);

        if (reward.hasUnclaimedMilestoneChest)
        {
            chestRoutine = StartCoroutine(DelayedMilestoneChestRoutine(reward.levelNumber));
        }
    }

    private IEnumerator DelayedMilestoneChestRoutine(int levelNum)
    {
        yield return new WaitForSecondsRealtime(0.85f);
        chestRoutine = null;
        MilestoneChestModal.Show(transform.root, levelNum);
    }

    private void StopChestRoutine()
    {
        if (chestRoutine != null)
        {
            StopCoroutine(chestRoutine);
            chestRoutine = null;
        }
    }

    private void EnsureCoinBonusText()
    {
        if (coinBonusText != null) return;
        Transform windowBox = transform.Find("WindowBox");
        if (windowBox == null) return;

        Transform existing = windowBox.Find("CoinBonusText");
        if (existing != null)
        {
            coinBonusText = existing.GetComponent<TMP_Text>();
            return;
        }

        GameObject obj = new GameObject("CoinBonusText");
        obj.transform.SetParent(windowBox, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, 22f);
        rt.sizeDelta = new Vector2(760f, 64f);

        coinBonusText = obj.AddComponent<TextMeshProUGUI>();
        if (scoreText != null && scoreText.font != null)
        {
            coinBonusText.font = scoreText.font;
        }
        coinBonusText.fontSize = 23f;
        coinBonusText.fontStyle = FontStyles.Bold;
        coinBonusText.color = new Color(1f, 0.88f, 0.28f);
        coinBonusText.alignment = TextAlignmentOptions.Center;
    }

    private void OnNextLevelClicked()
    {
        StopChestRoutine();
        Debug.Log("[LevelWinPanel] Next level clicked.");
        if (uiManager != null)
        {
            uiManager.HidePanel<LevelWinPanel>();
        }

        if (GameplayController.HasInstance)
        {
            GameplayController.Instance.StartLevelMode(currentLevelNumber + 1);
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.RestartGame();
        }
        else if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
        }
    }

    private void OnReplayClicked()
    {
        StopChestRoutine();
        if (uiManager != null)
        {
            uiManager.HidePanel<LevelWinPanel>();
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.RestartGame();
        }
        else if (uiManager != null)
        {
            uiManager.ShowPanel<GameplayHUDPanel>();
        }
    }

    private void OnMainMenuClicked()
    {
        StopChestRoutine();
        if (uiManager != null)
        {
            uiManager.HidePanel<LevelWinPanel>();
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else if (uiManager != null)
        {
            uiManager.ShowPanel<MainMenuPanel>();
        }
    }
}
