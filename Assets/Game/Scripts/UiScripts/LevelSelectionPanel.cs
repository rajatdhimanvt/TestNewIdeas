using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel managing the 50-Level Campaign Grid Map.
/// Displays unlocked/locked levels, 1-3 star ratings, best scores, and star progress badges.
/// </summary>
public class LevelSelectionPanel : UIBasePanel
{
    private const int TOTAL_LEVELS = 50;

    [Header("Header Elements")]
    [SerializeField] private TMP_Text totalStarsText;
    [SerializeField] private TMP_Text coinBalanceText;
    [SerializeField] private Button backButton;

    [Header("Grid Container")]
    [SerializeField] private Transform levelGridContainer;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        PlayerDataManager.OnCoinsChanged += UpdateCoinDisplay;
        PlayerDataManager.OnLevelStarsUpdated += (lvl, stars) => RefreshDisplay();
    }

    private void OnDestroy()
    {
        PlayerDataManager.OnCoinsChanged -= UpdateCoinDisplay;
    }

    public override void OnShow()
    {
        base.OnShow();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        UpdateCoinDisplay(PlayerDataManager.HasInstance ? PlayerDataManager.Instance.Coins : 0);

        if (PlayerDataManager.HasInstance && totalStarsText != null)
        {
            int earned = PlayerDataManager.Instance.GetTotalStarsEarned(TOTAL_LEVELS);
            totalStarsText.text = $"⭐ STARS: {earned} / {TOTAL_LEVELS * 3}";
        }

        PopulateLevelGrid();
    }

    private void UpdateCoinDisplay(int newBalance)
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = $"🪙 {newBalance:N0}";
        }
    }

    private void PopulateLevelGrid()
    {
        if (levelGridContainer == null) return;

        // Clear existing level items
        for (int i = levelGridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(levelGridContainer.GetChild(i).gameObject);
        }

        int highestUnlocked = PlayerDataManager.HasInstance ? PlayerDataManager.Instance.HighestUnlockedLevel : 1;

        for (int i = 1; i <= TOTAL_LEVELS; i++)
        {
            int levelNum = i;
            bool isUnlocked = levelNum <= highestUnlocked;
            int stars = PlayerDataManager.HasInstance ? PlayerDataManager.Instance.GetLevelStars(levelNum) : 0;
            int highScore = PlayerDataManager.HasInstance ? PlayerDataManager.Instance.GetLevelHighScore(levelNum) : 0;

            CreateLevelCardElement(levelNum, isUnlocked, stars, highScore);
        }
    }

    private void CreateLevelCardElement(int levelNum, bool isUnlocked, int stars, int highScore)
    {
        GameObject cardObj = new GameObject($"LevelCard_{levelNum}");
        cardObj.transform.SetParent(levelGridContainer, false);

        RectTransform rect = cardObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(170, 190);

        Image bg = cardObj.AddComponent<Image>();
        bg.color = isUnlocked 
            ? (stars > 0 ? new Color(0.12f, 0.22f, 0.32f, 0.95f) : new Color(0.16f, 0.18f, 0.26f, 0.95f))
            : new Color(0.1f, 0.1f, 0.14f, 0.8f);

        Button btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.interactable = isUnlocked;

        // Level Number / Lock Label
        GameObject labelObj = new GameObject("LevelLabel");
        labelObj.transform.SetParent(cardObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, 30);
        labelRect.sizeDelta = new Vector2(160, 50);

        TMP_Text labelTxt = labelObj.AddComponent<TextMeshProUGUI>();
        labelTxt.text = isUnlocked ? $"LVL {levelNum}" : "🔒";
        labelTxt.fontSize = isUnlocked ? 30 : 36;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.color = isUnlocked ? Color.white : Color.gray;

        // Stars Display (e.g. ⭐⭐⭐)
        GameObject starsObj = new GameObject("StarsText");
        starsObj.transform.SetParent(cardObj.transform, false);
        RectTransform starsRect = starsObj.AddComponent<RectTransform>();
        starsRect.anchoredPosition = new Vector2(0, -20);
        starsRect.sizeDelta = new Vector2(160, 35);

        TMP_Text starsTxt = starsObj.AddComponent<TextMeshProUGUI>();
        string starString = "";
        for (int s = 0; s < 3; s++)
        {
            starString += s < stars ? "★ " : "☆ ";
        }
        starsTxt.text = isUnlocked ? starString.Trim() : "LOCKED";
        starsTxt.fontSize = isUnlocked ? 22 : 18;
        starsTxt.alignment = TextAlignmentOptions.Center;
        starsTxt.color = isUnlocked ? new Color(1.0f, 0.85f, 0.2f) : Color.gray;

        // HighScore Subtext
        if (isUnlocked && highScore > 0)
        {
            GameObject hsObj = new GameObject("HighScoreText");
            hsObj.transform.SetParent(cardObj.transform, false);
            RectTransform hsRect = hsObj.AddComponent<RectTransform>();
            hsRect.anchoredPosition = new Vector2(0, -60);
            hsRect.sizeDelta = new Vector2(160, 25);

            TMP_Text hsTxt = hsObj.AddComponent<TextMeshProUGUI>();
            hsTxt.text = $"{highScore} Pts";
            hsTxt.fontSize = 18;
            hsTxt.alignment = TextAlignmentOptions.Center;
            hsTxt.color = new Color(0.2f, 0.8f, 1f);
        }

        btn.onClick.AddListener(() => StartCampaignLevel(levelNum));
    }

    private void StartCampaignLevel(int levelNum)
    {
        if (GameplayController.HasInstance)
        {
            GameplayController.Instance.StartLevelMode(levelNum);
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnBackClicked()
    {
        if (uiManager != null)
        {
            uiManager.GoBack();
        }
    }
}
