using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production Settings UI Panel with Audio & Music volume sliders, Mute toggle, and Back navigation.
/// </summary>
public class SettingsPanel : UIBasePanel
{
    [Header("Settings UI References")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Button backButton;

    [Header("Optional Value Labels")]
    [SerializeField] private TMP_Text masterPercentText;
    [SerializeField] private TMP_Text musicPercentText;
    [SerializeField] private TMP_Text sfxPercentText;
    [SerializeField] private TMP_Text muteStateText;

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
            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
            if (muteToggle != null) muteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMuted);
            UpdateLabels(AudioManager.Instance.MasterVolume, AudioManager.Instance.MusicVolume, AudioManager.Instance.SFXVolume, AudioManager.Instance.IsMuted);
        }
    }

    private void UpdateLabels(float master, float music, float sfx, bool muted)
    {
        if (masterPercentText != null) masterPercentText.text = $"{Mathf.RoundToInt(master * 100f)}%";
        if (musicPercentText != null) musicPercentText.text = $"{Mathf.RoundToInt(music * 100f)}%";
        if (sfxPercentText != null) sfxPercentText.text = $"{Mathf.RoundToInt(sfx * 100f)}%";
        if (muteStateText != null)
        {
            muteStateText.text = muted ? "MUTED" : "SOUND ON";
            muteStateText.color = muted ? new Color(1f, 0.45f, 0.45f) : new Color(0.35f, 0.95f, 0.58f);
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMasterVolume(value);
            UpdateLabels(value, AudioManager.Instance.MusicVolume, AudioManager.Instance.SFXVolume, AudioManager.Instance.IsMuted);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateLabels(AudioManager.Instance.MasterVolume, value, AudioManager.Instance.SFXVolume, AudioManager.Instance.IsMuted);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetSFXVolume(value);
            UpdateLabels(AudioManager.Instance.MasterVolume, AudioManager.Instance.MusicVolume, value, AudioManager.Instance.IsMuted);
        }
    }

    private void OnMuteToggled(bool isMuted)
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMute(isMuted);
            UpdateLabels(AudioManager.Instance.MasterVolume, AudioManager.Instance.MusicVolume, AudioManager.Instance.SFXVolume, isMuted);
            if (!isMuted)
            {
                GameEvents.OnPlaySFX?.Invoke("Click");
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
