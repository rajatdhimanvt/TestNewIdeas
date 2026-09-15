using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SoundEffect
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

/// <summary>
/// Production-grade Audio Manager handling BGM and SFX playback, volume control, and settings.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    private const string PREF_MASTER_VOL = "Audio_MasterVolume";
    private const string PREF_MUSIC_VOL = "Audio_MusicVolume";
    private const string PREF_SFX_VOL = "Audio_SFXVolume";
    private const string PREF_MUTE = "Audio_Muted";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips Library")]
    [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();
    [SerializeField] private List<SoundEffect> musicTracks = new List<SoundEffect>();

    private readonly Dictionary<string, SoundEffect> sfxDict = new Dictionary<string, SoundEffect>();
    private readonly Dictionary<string, SoundEffect> musicDict = new Dictionary<string, SoundEffect>();

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;
    public bool IsMuted { get; private set; } = false;

    protected override void Awake()
    {
        base.Awake();
        EnsureAudioSources();
        BuildLibraries();
        LoadSettings();
    }

    private void OnEnable()
    {
        GameEvents.OnPlaySFX += PlaySFX;
        GameEvents.OnPlayMusic += PlayMusic;
    }

    private void OnDisable()
    {
        GameEvents.OnPlaySFX -= PlaySFX;
        GameEvents.OnPlayMusic -= PlayMusic;
    }

    public void Init()
    {
        // Registers itself to DependencyManager if not already registered
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void BuildLibraries()
    {
        sfxDict.Clear();
        for (int i = 0; i < soundEffects.Count; i++)
        {
            var sfx = soundEffects[i];
            if (sfx != null && !string.IsNullOrEmpty(sfx.name))
            {
                sfxDict[sfx.name] = sfx;
            }
        }

        musicDict.Clear();
        for (int i = 0; i < musicTracks.Count; i++)
        {
            var track = musicTracks[i];
            if (track != null && !string.IsNullOrEmpty(track.name))
            {
                musicDict[track.name] = track;
            }
        }
    }

    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1f);
        MusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 1f);
        SFXVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);
        IsMuted = PlayerPrefs.GetInt(PREF_MUTE, 0) == 1;

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = IsMuted ? 0f : (MasterVolume * MusicVolume);
        }

        if (sfxSource != null)
        {
            sfxSource.volume = IsMuted ? 0f : (MasterVolume * SFXVolume);
        }
    }

    #region Playback Methods

    /// <summary>
    /// Plays an SFX by registered name. Safe if not found.
    /// </summary>
    public void PlaySFX(string soundName)
    {
        if (IsMuted || string.IsNullOrEmpty(soundName)) return;

        if (sfxDict.TryGetValue(soundName, out SoundEffect sfx))
        {
            if (sfx.clip != null)
            {
                sfxSource.pitch = sfx.pitch;
                sfxSource.PlayOneShot(sfx.clip, sfx.volume * SFXVolume * MasterVolume);
            }
        }
        else
        {
            // Helpful debug log instead of exception
            Debug.Log($"[AudioManager] SFX '{soundName}' requested (clip not registered in Inspector).");
        }
    }

    /// <summary>
    /// Directly plays an AudioClip as SFX.
    /// </summary>
    public void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (IsMuted || clip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, volumeScale * SFXVolume * MasterVolume);
    }

    /// <summary>
    /// Backward-compatible method name for PlaySFX.
    /// </summary>
    public void PlayAudio(string soundName)
    {
        PlaySFX(soundName);
    }

    /// <summary>
    /// Plays background music by name.
    /// </summary>
    public void PlayMusic(string trackName)
    {
        if (musicDict.TryGetValue(trackName, out SoundEffect track))
        {
            if (track.clip != null)
            {
                musicSource.clip = track.clip;
                musicSource.volume = IsMuted ? 0f : (track.volume * MusicVolume * MasterVolume);
                musicSource.Play();
            }
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    #endregion

    #region Volume & Mute Controls

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, MasterVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, MusicVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_SFX_VOL, SFXVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMute(bool mute)
    {
        IsMuted = mute;
        PlayerPrefs.SetInt(PREF_MUTE, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void ToggleMute()
    {
        SetMute(!IsMuted);
    }

    #endregion
}
