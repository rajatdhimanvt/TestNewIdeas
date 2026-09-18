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

    [Header("Theme Select Buttons")]
    [SerializeField] private Button darkThemeButton;
    [SerializeField] private Button oceanThemeButton;
    [SerializeField] private Button skyThemeButton;
    [SerializeField] private Button sunsetThemeButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (darkThemeButton != null) darkThemeButton.onClick.AddListener(() => ApplyTheme("dark_theme"));
        if (oceanThemeButton != null) oceanThemeButton.onClick.AddListener(() => ApplyTheme("ocean_theme"));
        if (skyThemeButton != null) skyThemeButton.onClick.AddListener(() => ApplyTheme("sky_theme"));
        if (sunsetThemeButton != null) sunsetThemeButton.onClick.AddListener(() => ApplyTheme("sunset_theme"));

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
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
    }

    private void ApplyTheme(string themeId)
    {
        if (ThemeManager.HasInstance)
        {
            ThemeManager.Instance.ApplyTheme(themeId);
            RefreshDisplay();
            if (uiManager != null)
            {
                uiManager.ShowPopup("Theme Applied!", $"Applied visual theme '{themeId}'.");
            }
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
