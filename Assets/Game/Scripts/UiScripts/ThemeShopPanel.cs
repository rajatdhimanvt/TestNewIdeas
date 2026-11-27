using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Theme Selector Panel.
/// Allows player to pick and apply environment themes (Dark Cosmic, Ocean Depth, Sky Breeze, Sunset Glow).
/// </summary>
public class ThemeShopPanel : UIBasePanel
{
    [Header("Current Theme Display")]
    [SerializeField] private TMP_Text currentThemeNameText;
    [SerializeField] private TMP_Text coinBalanceText;

    [Header("Theme Select Buttons")]
    [SerializeField] private Button darkThemeButton;
    [SerializeField] private Button oceanThemeButton;
    [SerializeField] private Button skyThemeButton;
    [SerializeField] private Button sunsetThemeButton;

    [Header("Inspect Theme Buttons")]
    [SerializeField] private Button inspectDarkButton;
    [SerializeField] private Button inspectOceanButton;
    [SerializeField] private Button inspectSkyButton;
    [SerializeField] private Button inspectSunsetButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (darkThemeButton != null) darkThemeButton.onClick.AddListener(() => ApplyTheme("dark_theme"));
        if (oceanThemeButton != null) oceanThemeButton.onClick.AddListener(() => ApplyTheme("ocean_theme"));
        if (skyThemeButton != null) skyThemeButton.onClick.AddListener(() => ApplyTheme("sky_theme"));
        if (sunsetThemeButton != null) sunsetThemeButton.onClick.AddListener(() => ApplyTheme("sunset_theme"));

        if (inspectDarkButton != null) inspectDarkButton.onClick.AddListener(() => OpenThemePreview("dark_theme"));
        if (inspectOceanButton != null) inspectOceanButton.onClick.AddListener(() => OpenThemePreview("ocean_theme"));
        if (inspectSkyButton != null) inspectSkyButton.onClick.AddListener(() => OpenThemePreview("sky_theme"));
        if (inspectSunsetButton != null) inspectSunsetButton.onClick.AddListener(() => OpenThemePreview("sunset_theme"));

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        PlayerDataManager.OnCoinsChanged += UpdateCoinDisplay;
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
        if (ThemeManager.HasInstance && currentThemeNameText != null)
        {
            ThemeDataSO active = ThemeManager.Instance.CurrentTheme;
            currentThemeNameText.text = active != null ? $"ACTIVE: {active.themeName.ToUpper()}" : "ACTIVE: DARK COSMIC";
        }

        UpdateCoinDisplay(PlayerDataManager.HasInstance ? PlayerDataManager.Instance.Coins : 0);
        UpdateThemeButtonState("dark_theme", darkThemeButton, 0);
        UpdateThemeButtonState("ocean_theme", oceanThemeButton, 300);
        UpdateThemeButtonState("sky_theme", skyThemeButton, 600);
        UpdateThemeButtonState("sunset_theme", sunsetThemeButton, 1000);
    }

    private void UpdateThemeButtonState(string themeId, Button btn, int coinCost)
    {
        if (btn == null) return;

        bool isUnlocked = ThemeManager.HasInstance && ThemeManager.Instance.IsThemeUnlocked(themeId);
        bool isEquipped = ThemeManager.HasInstance && ThemeManager.Instance.CurrentEquippedTheme != null && ThemeManager.Instance.CurrentEquippedTheme.themeId == themeId;

        TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            if (isEquipped) txt.text = "APPLIED";
            else if (isUnlocked) txt.text = "APPLY";
            else txt.text = $"UNLOCK\n{coinCost} COINS";
        }
    }

    public void OpenThemePreview(string themeId)
    {
        if (!ThemeManager.HasInstance || uiManager == null) return;

        ThemeDataSO theme = ThemeManager.Instance.AvailableThemes.Find(t => t != null && t.themeId == themeId);
        if (theme != null)
        {
            ThemePreviewPanel previewPanel = uiManager.GetPanel<ThemePreviewPanel>();
            if (previewPanel != null)
            {
                previewPanel.SetupPreview(theme);
                uiManager.ShowPanel<ThemePreviewPanel>(false);
            }
        }
    }

    private void ApplyTheme(string themeId)
    {
        if (!ThemeManager.HasInstance) return;

        bool isUnlocked = ThemeManager.Instance.IsThemeUnlocked(themeId);
        int cost = themeId == "ocean_theme" ? 300 : (themeId == "sky_theme" ? 600 : (themeId == "sunset_theme" ? 1000 : 0));

        if (!isUnlocked)
        {
            if (ThemeManager.Instance.UnlockTheme(themeId, cost))
            {
                ThemeManager.Instance.EquipTheme(themeId);
                RefreshDisplay();
                if (uiManager != null) uiManager.ShowPopup("Theme Unlocked!", $"Unlocked and applied theme '{themeId}'!");
            }
            else
            {
                if (uiManager != null) uiManager.ShowPopup("Insufficient Coins", $"You need {cost} Coins to unlock this theme!");
            }
            return;
        }

        ThemeManager.Instance.EquipTheme(themeId);
        RefreshDisplay();
    }

    private void OnBackClicked()
    {
        if (uiManager != null)
        {
            uiManager.GoBack();
        }
    }

    private void UpdateCoinDisplay(int coins)
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = $"{coins:N0} COINS";
        }
    }
}
