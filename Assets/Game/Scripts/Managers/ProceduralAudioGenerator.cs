using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime PCM waveform synthesizer generating crisp arcade sound effects
/// whenever custom AudioClips aren't assigned in AudioManager.
/// </summary>
public static class ProceduralAudioGenerator
{
    private const int SAMPLE_RATE = 44100;
    private static readonly Dictionary<string, AudioClip> cachedClips = new Dictionary<string, AudioClip>();

    public static AudioClip GetOrCreateClip(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (cachedClips.TryGetValue(key, out AudioClip existing) && existing != null)
        {
            return existing;
        }

        AudioClip clip = SynthesizeByKey(key);
        if (clip != null)
        {
            cachedClips[key] = clip;
        }
        return clip;
    }

    private static AudioClip SynthesizeByKey(string key)
    {
        switch (key)
        {
            case "Click":
                return BuildToneSweep("SFX_Click", 0.055f, 760f, 1080f, 0.28f, addNoise: false);

            case "Drop":
                return BuildToneSweep("SFX_Drop", 0.11f, 440f, 190f, 0.26f, addNoise: false);

            case "Merge":
                return BuildMergePop("SFX_Merge", 0.19f);

            case "BlockHit":
                return BuildImpact("SFX_BlockHit", 0.11f, 210f, 90f, 0.25f, 0.32f);

            case "BlockBreak":
                return BuildImpact("SFX_BlockBreak", 0.24f, 280f, 65f, 0.42f, 0.36f);

            case "IceCrack":
                return BuildCrystalChime("SFX_IceCrack", 0.22f);

            case "Explosion":
                return BuildImpact("SFX_Explosion", 0.42f, 130f, 34f, 0.55f, 0.42f);

            case "KeyUnlock":
                return BuildArpeggio("SFX_KeyUnlock", new[] { 784f, 987.77f, 1318.51f }, 0.11f, 0.28f);

            case "LockedClink":
                return BuildDualTone("SFX_LockedClink", 0.12f, 980f, 1460f, 0.24f);

            case "Bumper":
                return BuildBumperBoing("SFX_Bumper", 0.16f);

            case "BonusDrop":
                return BuildArpeggio("SFX_BonusDrop", new[] { 880f, 1174.66f }, 0.10f, 0.28f);

            case "Teleport":
                return BuildToneSweep("SFX_Teleport", 0.22f, 320f, 1380f, 0.28f, addNoise: false);

            case "CoinTick":
                return BuildDualTone("SFX_CoinTick", 0.055f, 1318.5f, 1975.5f, 0.18f);

            case "ChestOpen":
                return BuildArpeggio("SFX_ChestOpen", new[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }, 0.11f, 0.34f);

            case "LevelWin":
                return BuildArpeggio("SFX_LevelWin", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.14f, 0.32f);

            case "LevelLose":
                return BuildArpeggio("SFX_LevelLose", new[] { 392f, 349.23f, 293.66f }, 0.16f, 0.28f);

            default:
                return BuildToneSweep($"SFX_{key}", 0.08f, 520f, 680f, 0.2f, addNoise: false);
        }
    }

    private static AudioClip BuildToneSweep(string name, float duration, float startFreq, float endFreq, float amp, bool addNoise)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SAMPLE_RATE * duration));
        float[] data = new float[count];
        float phase = 0f;
        System.Random rng = new System.Random(name.GetHashCode());

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            float env = AttackDecayEnvelope(t, 0.08f);
            float sample = Mathf.Sin(phase) * env * amp;
            if (addNoise)
            {
                sample += (float)(rng.NextDouble() * 2.0 - 1.0) * env * amp * 0.35f;
            }
            data[i] = Mathf.Clamp(sample, -0.8f, 0.8f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip BuildMergePop(string name, float duration)
    {
        int count = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[count];
        float p1 = 0f;
        float p2 = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float f1 = Mathf.Lerp(480f, 920f, Mathf.SmoothStep(0f, 1f, t));
            float f2 = f1 * 1.5f;
            p1 += 2f * Mathf.PI * f1 / SAMPLE_RATE;
            p2 += 2f * Mathf.PI * f2 / SAMPLE_RATE;

            float env = AttackDecayEnvelope(t, 0.06f);
            float s = (Mathf.Sin(p1) * 0.7f + Mathf.Sin(p2) * 0.3f) * env * 0.32f;
            data[i] = s;
        }

        return CreateClip(name, data);
    }

    private static AudioClip BuildImpact(string name, float duration, float startFreq, float endFreq, float noiseMix, float amp)
    {
        int count = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[count];
        float phase = 0f;
        System.Random rng = new System.Random(name.GetHashCode());
        float lastNoise = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Sqrt(t));
            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            float bodyEnv = Mathf.Exp(-t * 6.5f) * Mathf.Clamp01(t / 0.02f);
            float rawNoise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lastNoise = Mathf.Lerp(lastNoise, rawNoise, 0.45f);
            float noiseEnv = Mathf.Exp(-t * 9f);

            float s = (Mathf.Sin(phase) * bodyEnv + lastNoise * noiseMix * noiseEnv) * amp;
            data[i] = Mathf.Clamp(s, -0.8f, 0.8f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip BuildCrystalChime(string name, float duration)
    {
        int count = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[count];
        System.Random rng = new System.Random(42);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float timeSec = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 7.5f) * Mathf.Clamp01(t / 0.015f);
            float tone = Mathf.Sin(2f * Mathf.PI * 1680f * timeSec) * 0.5f
                       + Mathf.Sin(2f * Mathf.PI * 2540f * timeSec) * 0.35f
                       + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.25f * Mathf.Exp(-t * 14f);
            data[i] = tone * env * 0.26f;
        }

        return CreateClip(name, data);
    }

    private static AudioClip BuildDualTone(string name, float duration, float f1, float f2, float amp)
    {
        int count = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float timeSec = (float)i / SAMPLE_RATE;
            float env = AttackDecayEnvelope(t, 0.05f);
            float s = (Mathf.Sin(2f * Mathf.PI * f1 * timeSec) + Mathf.Sin(2f * Mathf.PI * f2 * timeSec)) * 0.5f * env * amp;
            data[i] = s;
        }

        return CreateClip(name, data);
    }

    private static AudioClip BuildBumperBoing(string name, float duration)
    {
        int count = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = 340f + Mathf.Sin(t * Mathf.PI * 2.5f) * 180f + t * 240f;
            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;
            float env = AttackDecayEnvelope(t, 0.06f);
            data[i] = Mathf.Sin(phase) * env * 0.30f;
        }

        return CreateClip(name, data);
    }

    private static AudioClip BuildArpeggio(string name, float[] notes, float noteDur, float amp)
    {
        float totalDur = notes.Length * noteDur;
        int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * totalDur);
        float[] data = new float[totalSamples];

        for (int n = 0; n < notes.Length; n++)
        {
            int startSample = Mathf.RoundToInt(n * noteDur * SAMPLE_RATE);
            int noteLen = Mathf.RoundToInt(noteDur * 1.45f * SAMPLE_RATE);
            float freq = notes[n];

            for (int i = 0; i < noteLen; i++)
            {
                int idx = startSample + i;
                if (idx >= totalSamples) break;
                float t = (float)i / noteLen;
                float timeSec = (float)i / SAMPLE_RATE;
                float env = AttackDecayEnvelope(t, 0.06f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * timeSec) * 0.75f
                           + Mathf.Sin(2f * Mathf.PI * freq * 2f * timeSec) * 0.25f;
                data[idx] = Mathf.Clamp(data[idx] + wave * env * amp, -0.8f, 0.8f);
            }
        }

        return CreateClip(name, data);
    }

    private static float AttackDecayEnvelope(float t, float attackFraction)
    {
        float attack = t < attackFraction ? (t / attackFraction) : 1f;
        float decay = Mathf.Exp(-Mathf.Max(0f, t - attackFraction) * 4.5f);
        float endFade = t > 0.88f ? Mathf.Clamp01((1f - t) / 0.12f) : 1f;
        return attack * decay * endFade;
    }

    private static AudioClip CreateClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }
}
