using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Self-contained procedural SFX synthesizer for the Geometry Box Tilt Maze.
    /// Generates crisp plastic-on-metal taps, hole-fall bloops, calibration chimes, and win fanfare
    /// with zero external audio clip dependencies.
    /// </summary>
    public class GeometryMazeAudio : MonoBehaviour
    {
        private const string PREF_MUTED = "GeometryMaze_AudioMuted";

        private static GeometryMazeAudio instance;

        private AudioSource sfxSource;
        private AudioClip wallTapClip;
        private AudioClip holeFallClip;
        private AudioClip goalWinClip;
        private AudioClip clickClip;
        private AudioClip calibrateClip;
        private AudioClip pinClip;
        private AudioClip switchClip;
        private AudioClip bounceClip;

        public static bool IsMuted
        {
            get => PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;
            set => PlayerPrefs.SetInt(PREF_MUTED, value ? 1 : 0);
        }

        private void Awake()
        {
            instance = this;
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            wallTapClip = BuildToneClip("GbmTap", 0.055f, 340f, 170f, 0.35f, false);
            holeFallClip = BuildToneClip("GbmHoleFall", 0.36f, 440f, 110f, 0.45f, false);
            goalWinClip = BuildArpeggioClip("GbmGoalWin", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.085f, 0.42f);
            clickClip = BuildToneClip("GbmClick", 0.045f, 680f, 420f, 0.25f, false);
            calibrateClip = BuildArpeggioClip("GbmCalibrate", new[] { 587.33f, 880f }, 0.065f, 0.3f);
            pinClip = BuildArpeggioClip("GbmPin", new[] { 987.77f, 1318.5f }, 0.06f, 0.34f);
            switchClip = BuildArpeggioClip("GbmSwitch", new[] { 440f, 659.25f, 880f }, 0.06f, 0.32f);
            bounceClip = BuildToneClip("GbmBounce", 0.14f, 220f, 490f, 0.38f, true);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void PlayWallTap(float normalizedImpact)
        {
            if (instance == null || IsMuted || instance.wallTapClip == null) return;
            instance.sfxSource.pitch = Mathf.Lerp(0.88f, 1.24f, normalizedImpact);
            instance.sfxSource.PlayOneShot(instance.wallTapClip, Mathf.Clamp(normalizedImpact * 0.75f, 0.12f, 0.75f));
        }

        public static void PlayHoleFall()
        {
            if (instance == null || IsMuted || instance.holeFallClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.holeFallClip, 0.85f);
        }

        public static void PlayGoalWin()
        {
            if (instance == null || IsMuted || instance.goalWinClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.goalWinClip, 0.9f);
        }

        public static void PlayClick()
        {
            if (instance == null || IsMuted || instance.clickClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.clickClip, 0.6f);
        }

        public static void PlayCalibrate()
        {
            if (instance == null || IsMuted || instance.calibrateClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.calibrateClip, 0.75f);
        }

        public static void PlayPinPickup()
        {
            if (instance == null || IsMuted || instance.pinClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.pinClip, 0.8f);
        }

        public static void PlaySwitchOpen()
        {
            if (instance == null || IsMuted || instance.switchClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.switchClip, 0.8f);
        }

        public static void PlayEraserBounce()
        {
            if (instance == null || IsMuted || instance.bounceClip == null) return;
            instance.sfxSource.pitch = 1f;
            instance.sfxSource.PlayOneShot(instance.bounceClip, 0.85f);
        }

        private static AudioClip BuildToneClip(string name, float duration, float startFreq, float endFreq, float gain, bool sineOnly)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / sampleRate;
                float env = Mathf.Sin(Mathf.Min(1f, t * 10f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t, 1.6f);
                float wave = Mathf.Sin(phase) + (sineOnly ? 0f : 0.25f * Mathf.Sin(phase * 2.1f));
                samples[i] = Mathf.Clamp(wave * env * gain, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip BuildArpeggioClip(string name, float[] notes, float noteDuration, float gain)
        {
            const int sampleRate = 22050;
            int perNote = Mathf.CeilToInt(sampleRate * noteDuration);
            int total = perNote * notes.Length;
            float[] samples = new float[total];

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                float phase = 0f;
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)perNote;
                    phase += (2f * Mathf.PI * freq) / sampleRate;
                    float env = Mathf.Sin(Mathf.Min(1f, t * 8f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t, 1.15f);
                    samples[n * perNote + i] = Mathf.Sin(phase) * env * gain;
                }
            }

            AudioClip clip = AudioClip.Create(name, total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
