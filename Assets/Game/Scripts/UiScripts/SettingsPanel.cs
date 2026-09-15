using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production Settings UI Panel with Audio & Music volume sliders and Back navigation.
/// </summary>
public class SettingsPanel : UIBasePanel
{
    [Header("Settings UI References")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Button backButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);
        showBackgroundDim = true;
        animateScale = true;

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        if (AudioManager.HasInstance)
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = AudioManager.Instance.MasterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
            if (muteToggle != null) muteToggle.isOn = AudioManager.Instance.IsMuted;
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    private void OnMuteToggled(bool isMuted)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMute(isMuted);
        }
    }

    private void OnBackClicked()
    {
        uiManager.GoBack();
    }
}
