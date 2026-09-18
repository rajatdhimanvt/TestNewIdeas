using UnityEngine;

/// <summary>
/// ScriptableObject defining visual environment theme properties (background, wall colors, danger line color).
/// Completely independent from Ball Kits.
/// </summary>
[CreateAssetMenu(fileName = "ThemeData_", menuName = "BallMerge/Theme Data")]
public class ThemeDataSO : ScriptableObject
{
    [Header("Theme Metadata")]
    public string themeId = "dark_theme";
    public string themeName = "Dark Cosmic";
    public Sprite themeIcon;

    [Header("Environment Colors")]
    public Color backgroundColor = new Color(0.12f, 0.13f, 0.18f);
    public Color wallColor = new Color(0.2f, 0.6f, 1.0f, 0.8f);
    public Color dangerLineColor = new Color(1.0f, 0.3f, 0.3f, 0.4f);

    [Header("Optional Background Art")]
    public Sprite backgroundSprite;
}
