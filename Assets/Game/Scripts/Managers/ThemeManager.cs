using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages environment visual themes (camera background, container wall colors, danger line tint).
/// Independent from Ball Kits, with PlayerPrefs persistence.
/// </summary>
public class ThemeManager : Singleton<ThemeManager>
{
    private const string PREF_EQUIPPED_THEME_ID = "Equipped_Theme_ID";

    [Header("Available Themes")]
    [SerializeField] private ThemeDataSO defaultTheme;
    [SerializeField] private List<ThemeDataSO> availableThemes = new List<ThemeDataSO>();

    [Header("Equipped State")]
    [SerializeField] private ThemeDataSO currentEquippedTheme;

    public static Action<ThemeDataSO> OnThemeChanged;

    public ThemeDataSO CurrentEquippedTheme => currentEquippedTheme;
    public List<ThemeDataSO> AvailableThemes => availableThemes;

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
    }

    private void Start()
    {
        LoadEquippedTheme();
        ApplyCurrentTheme();
    }

    public void RegisterThemes(List<ThemeDataSO> themes, ThemeDataSO defaultThemeData = null)
    {
        availableThemes = themes ?? new List<ThemeDataSO>();
        if (defaultThemeData != null)
        {
            defaultTheme = defaultThemeData;
        }

        LoadEquippedTheme();
        ApplyCurrentTheme();
    }

    private void LoadEquippedTheme()
    {
        string savedThemeId = PlayerPrefs.GetString(PREF_EQUIPPED_THEME_ID, string.Empty);

        if (!string.IsNullOrEmpty(savedThemeId))
        {
            ThemeDataSO foundTheme = availableThemes.Find(t => t != null && t.themeId == savedThemeId);
            if (foundTheme != null)
            {
                currentEquippedTheme = foundTheme;
                return;
            }
        }

        // Fallback to default theme or first available
        if (defaultTheme != null)
        {
            currentEquippedTheme = defaultTheme;
        }
        else if (availableThemes != null && availableThemes.Count > 0)
        {
            currentEquippedTheme = availableThemes[0];
        }
    }

    /// <summary>
    /// Equips a specified environment theme and updates camera/container visuals.
    /// </summary>
    public void EquipTheme(ThemeDataSO theme)
    {
        if (theme == null) return;

        currentEquippedTheme = theme;
        PlayerPrefs.SetString(PREF_EQUIPPED_THEME_ID, theme.themeId);
        PlayerPrefs.Save();

        Debug.Log($"[ThemeManager] Successfully equipped Theme: {theme.themeName} ({theme.themeId})");
        ApplyCurrentTheme();
        OnThemeChanged?.Invoke(currentEquippedTheme);
    }

    /// <summary>
    /// Equips an environment theme by string ID.
    /// </summary>
    public bool EquipThemeById(string themeId)
    {
        ThemeDataSO theme = availableThemes.Find(t => t != null && t.themeId == themeId);
        if (theme != null)
        {
            EquipTheme(theme);
            return true;
        }

        Debug.LogWarning($"[ThemeManager] Could not find theme with ID '{themeId}' to equip.");
        return false;
    }

    /// <summary>
    /// Applies the current equipped theme to Main Camera background.
    /// </summary>
    public void ApplyCurrentTheme()
    {
        if (currentEquippedTheme == null) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = currentEquippedTheme.backgroundColor;
        }
    }
}
