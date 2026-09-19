using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal Panel displaying full visual environment preview for a selected Theme
/// (Mock Game Container, Wall Colors, Camera Background, and Danger Line Warning colors).
/// </summary>
public class ThemePreviewPanel : UIBasePanel
{
    [Header("Theme Header")]
    [SerializeField] private TMP_Text themeTitleText;
    [SerializeField] private TMP_Text themeSubtitleText;

    [Header("Mock Environment Visual Preview")]
    [SerializeField] private Image previewBackgroundBox;
    [SerializeField] private Image previewWallLeft;
    [SerializeField] private Image previewWallRight;
    [SerializeField] private Image previewWallBottom;
    [SerializeField] private Image previewDangerLine;

    [Header("Palette Color Info Labels")]
    [SerializeField] private TMP_Text bgColorText;
    [SerializeField] private TMP_Text wallColorText;
    [SerializeField] private TMP_Text dangerColorText;

    [Header("Action Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private TMP_Text applyButtonText;
    [SerializeField] private Button closeButton;

    private ThemeDataSO currentPreviewTheme;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    /// <summary>
    /// Opens preview details for the specified ThemeDataSO.
    /// </summary>
    public void SetupPreview(ThemeDataSO theme)
    {
        currentPreviewTheme = theme;
        if (currentPreviewTheme == null) return;

        if (themeTitleText != null)
        {
            themeTitleText.text = currentPreviewTheme.themeName.ToUpper();
        }

        if (themeSubtitleText != null)
        {
            themeSubtitleText.text = "ENVIRONMENT VISUAL PALETTE PREVIEW";
        }

        // Apply Mock Visuals
        if (previewBackgroundBox != null) previewBackgroundBox.color = theme.backgroundColor;
        if (previewWallLeft != null) previewWallLeft.color = theme.wallColor;
        if (previewWallRight != null) previewWallRight.color = theme.wallColor;
        if (previewWallBottom != null) previewWallBottom.color = theme.wallColor;
        if (previewDangerLine != null) previewDangerLine.color = theme.dangerLineColor;

        // Apply Info Text Labels
        if (bgColorText != null)
        {
            bgColorText.text = $"• Background: #{ColorUtility.ToHtmlStringRGB(theme.backgroundColor)} (Camera Viewport)";
        }

        if (wallColorText != null)
        {
            wallColorText.text = $"• Container Walls: #{ColorUtility.ToHtmlStringRGB(theme.wallColor)} (Side/Bottom Boundaries)";
        }

        if (dangerColorText != null)
        {
            dangerColorText.text = $"• Danger Line: #{ColorUtility.ToHtmlStringRGB(theme.dangerLineColor)} (Overflow Warning)";
        }

        RefreshApplyButtonState();
    }

    private void RefreshApplyButtonState()
    {
        if (currentPreviewTheme == null || !ThemeManager.HasInstance) return;

        bool isEquipped = ThemeManager.Instance.CurrentEquippedTheme == currentPreviewTheme;

        if (applyButtonText != null)
        {
            applyButtonText.text = isEquipped ? "ACTIVE THEME" : "APPLY THIS THEME";
        }

        if (applyButton != null)
        {
            applyButton.interactable = !isEquipped;
        }
    }

    private void OnApplyClicked()
    {
        if (currentPreviewTheme != null && ThemeManager.HasInstance)
        {
            ThemeManager.Instance.EquipTheme(currentPreviewTheme);
            RefreshApplyButtonState();

            // Refresh parent Theme Shop Panel
            ThemeShopPanel shopPanel = uiManager != null ? uiManager.GetPanel<ThemeShopPanel>() : null;
            if (shopPanel != null)
            {
                shopPanel.OnShow();
            }

            if (uiManager != null)
            {
                uiManager.ShowPopup("Theme Applied!", $"Applied theme '{currentPreviewTheme.themeName}'.");
            }
        }
    }

    private void OnCloseClicked()
    {
        if (uiManager != null)
        {
            uiManager.HidePanel<ThemePreviewPanel>();
        }
    }
}
