using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Splash Screen Panel.
/// Displays game branding and a 3-second animated loading progress bar on game startup.
/// </summary>
public class SplashScreenPanel : UIBasePanel
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text progressText;

    public override void Init(UIManager manager)
    {
        base.Init(manager);
        showBackgroundDim = false;
        animateScale = false;
    }

    public override void OnShow()
    {
        base.OnShow();
        SetProgress(0f);
    }

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (loadingSlider != null)
        {
            loadingSlider.value = value;
        }

        if (progressText != null)
        {
            int percent = Mathf.RoundToInt(value * 100f);
            progressText.text = $"LOADING... {percent}%";
        }
    }
}
