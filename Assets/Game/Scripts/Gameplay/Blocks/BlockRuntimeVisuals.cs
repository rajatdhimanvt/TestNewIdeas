using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Runtime visual & VFX helper for destructible blocks and merge shockwaves.
/// Provides cached procedural fallback sprites (when custom assets aren't assigned)
/// plus particle bursts, floating damage/bonus text, flying key orbs, and merge blast rings.
/// </summary>
public static class BlockRuntimeVisuals
{
    private static Sprite defaultCrateSprite;
    private static Sprite defaultShelfSprite;
    private static Sprite ringSprite;
    private static Sprite chipSprite;
    private static Sprite padlockSprite;
    private static Sprite softGlowSprite;
    private static readonly List<Sprite> defaultCracks = new List<Sprite>();

    public static Sprite GetDefaultCrateSprite()
    {
        if (defaultCrateSprite != null) return defaultCrateSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color fill = new Color(0.96f, 0.62f, 0.22f, 1f);
        Color border = new Color(0.75f, 0.32f, 0.08f, 1f);
        Color cross = new Color(0.92f, 0.16f, 0.14f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < 10 || x >= size - 10 || y < 10 || y >= size - 10;
                float d1 = Mathf.Abs(x - y);
                float d2 = Mathf.Abs((size - 1 - x) - y);
                bool isCross = (d1 < 7f || d2 < 7f) && x > 12 && x < size - 12 && y > 12 && y < size - 12;

                if (isBorder) tex.SetPixel(x, y, border);
                else if (isCross) tex.SetPixel(x, y, cross);
                else tex.SetPixel(x, y, fill);
            }
        }

        tex.Apply();
        defaultCrateSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return defaultCrateSprite;
    }

    public static Sprite GetDefaultShelfSprite()
    {
        if (defaultShelfSprite != null) return defaultShelfSprite;

        int w = 64;
        int h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color main = new Color(0.55f, 0.34f, 0.18f, 1f);
        Color top = new Color(0.72f, 0.48f, 0.26f, 1f);
        Color bottom = new Color(0.38f, 0.22f, 0.10f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (y >= h - 3) tex.SetPixel(x, y, top);
                else if (y < 3) tex.SetPixel(x, y, bottom);
                else tex.SetPixel(x, y, main);
            }
        }

        tex.Apply();
        defaultShelfSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        return defaultShelfSprite;
    }

    public static Sprite GetFallbackCrackSprite(int stage, int totalStages = 3)
    {
        EnsureDefaultCracks();
        if (defaultCracks.Count == 0) return null;
        int idx = Mathf.Clamp(stage, 0, defaultCracks.Count - 1);
        return defaultCracks[idx];
    }

    private static void EnsureDefaultCracks()
    {
        if (defaultCracks.Count > 0) return;

        for (int stage = 0; stage < 3; stage++)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color crackCol = new Color(0.12f, 0.08f, 0.06f, 0.85f);
            DrawJaggedLine(pixels, size, new Vector2(64, 64), new Vector2(20, 108), crackCol, 3);
            DrawJaggedLine(pixels, size, new Vector2(64, 64), new Vector2(106, 86), crackCol, 2);

            if (stage >= 1)
            {
                DrawJaggedLine(pixels, size, new Vector2(64, 64), new Vector2(26, 24), crackCol, 3);
                DrawJaggedLine(pixels, size, new Vector2(40, 88), new Vector2(14, 70), crackCol, 2);
            }

            if (stage >= 2)
            {
                DrawJaggedLine(pixels, size, new Vector2(64, 64), new Vector2(108, 22), crackCol, 3);
                DrawJaggedLine(pixels, size, new Vector2(84, 74), new Vector2(115, 52), crackCol, 2);
                DrawJaggedLine(pixels, size, new Vector2(48, 42), new Vector2(62, 12), crackCol, 2);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            defaultCracks.Add(Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size));
        }
    }

    private static void DrawJaggedLine(Color[] pixels, int texSize, Vector2 a, Vector2 b, Color col, int thickness)
    {
        int steps = 40;
        Vector2 prev = a;
        Vector2 perp = new Vector2(-(b.y - a.y), b.x - a.x).normalized;

        for (int s = 1; s <= steps; s++)
        {
            float t = (float)s / steps;
            float jitter = (s < steps) ? Mathf.Sin(s * 2.7f + a.x) * 3.5f : 0f;
            Vector2 next = Vector2.Lerp(a, b, t) + perp * jitter;
            DrawSegment(pixels, texSize, prev, next, col, thickness);
            prev = next;
        }
    }

    private static void DrawSegment(Color[] pixels, int texSize, Vector2 p0, Vector2 p1, Color col, int thickness)
    {
        int samples = Mathf.Max(4, Mathf.CeilToInt(Vector2.Distance(p0, p1) * 2f));
        for (int i = 0; i <= samples; i++)
        {
            Vector2 p = Vector2.Lerp(p0, p1, (float)i / samples);
            int cx = Mathf.RoundToInt(p.x);
            int cy = Mathf.RoundToInt(p.y);
            for (int dy = -thickness; dy <= thickness; dy++)
            {
                for (int dx = -thickness; dx <= thickness; dx++)
                {
                    if (dx * dx + dy * dy > thickness * thickness) continue;
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                    {
                        pixels[py * texSize + px] = col;
                    }
                }
            }
        }
    }

    public static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.48f;
        float innerR = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;
                if (d >= innerR && d <= outerR)
                {
                    float mid = (innerR + outerR) * 0.5f;
                    float half = (outerR - innerR) * 0.5f;
                    alpha = 1f - Mathf.Clamp01(Mathf.Abs(d - mid) / half);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }

    public static Sprite GetChipSprite()
    {
        if (chipSprite != null) return chipSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                tex.SetPixel(x, y, edge ? new Color(1f, 1f, 1f, 0.4f) : Color.white);
            }
        }
        tex.Apply();
        chipSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return chipSprite;
    }

    public static void SpawnShockwaveRing(Vector3 position, float targetRadius, Color color, float duration = 0.28f)
    {
        GameObject ringObj = new GameObject("MergeShockwaveRing");
        ringObj.transform.position = new Vector3(position.x, position.y, -0.2f);
        ringObj.transform.localScale = Vector3.one * 0.25f;

        SpriteRenderer sr = ringObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.color = color;
        sr.sortingOrder = 25;

        float targetDiameter = targetRadius * 2f;
        ringObj.transform.DOScale(targetDiameter, duration).SetEase(Ease.OutQuad).SetLink(ringObj);
        sr.DOFade(0f, duration).SetEase(Ease.OutCubic).SetLink(ringObj)
            .OnComplete(() => { if (ringObj != null) Object.Destroy(ringObj); });
    }

    public static void SpawnDebrisBurst(Vector3 position, Color color, int count = 14, float speed = 3.5f, float scale = 0.14f)
    {
        GameObject fxObj = new GameObject("BlockDebrisFX");
        fxObj.transform.position = new Vector3(position.x, position.y, -0.3f);

        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(scale * 0.6f, scale * 1.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 1.4f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            Color.Lerp(color, Color.white, 0.25f),
            Color.Lerp(color, Color.black, 0.15f)
        );
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer pr = fxObj.GetComponent<ParticleSystemRenderer>();
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            pr.sharedMaterial = new Material(spriteShader);
        }
        pr.sortingOrder = 30;

        ps.Play();
    }

    public static void SpawnFloatingTag(string text, Vector3 position, Color color, float fontSize = 4.2f)
    {
        GameObject obj = new GameObject($"FloatingTag_{text}");
        obj.transform.position = new Vector3(position.x, position.y, -1f);

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 110;

        obj.transform.DOMoveY(position.y + 0.9f, 0.6f).SetEase(Ease.OutCubic).SetLink(obj);
        obj.transform.localScale = Vector3.one * 0.65f;
        obj.transform.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetLink(obj);
        tmp.DOFade(0f, 0.28f).SetDelay(0.32f).SetLink(obj)
            .OnComplete(() => { if (obj != null) Object.Destroy(obj); });
    }

    public static Sprite GetDefaultPadlockSprite()
    {
        if (padlockSprite != null) return padlockSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color gold = new Color(1f, 0.82f, 0.22f, 1f);
        Color darkGold = new Color(0.75f, 0.50f, 0.08f, 1f);
        Color keyhole = new Color(0.12f, 0.10f, 0.08f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = Color.clear;

                // Padlock body (bottom half)
                if (x >= 16 && x <= 48 && y >= 10 && y <= 36)
                {
                    bool rim = x < 19 || x > 45 || y < 13 || y > 33;
                    c = rim ? darkGold : gold;
                    float dx = x - 32f;
                    float dy = y - 25f;
                    if ((dx * dx + dy * dy < 16f) || (Mathf.Abs(dx) < 2.2f && y >= 16 && y <= 25))
                    {
                        c = keyhole;
                    }
                }
                else if (y > 36 && y <= 54)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 36f));
                    if (dist >= 9f && dist <= 14.5f)
                    {
                        c = new Color(0.82f, 0.86f, 0.92f, 1f);
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        padlockSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return padlockSprite;
    }

    public static Sprite GetSoftGlowSprite()
    {
        if (softGlowSprite != null) return softGlowSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float t = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), c) / maxR);
                float alpha = t * t;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        softGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return softGlowSprite;
    }

    public static void SpawnFlyingKeyOrb(Vector3 from, Vector3 to, Color color, System.Action onArrive)
    {
        GameObject orb = new GameObject("FlyingKeyOrb");
        orb.transform.position = new Vector3(from.x, from.y, -0.5f);
        orb.transform.localScale = Vector3.one * 0.65f;

        SpriteRenderer sr = orb.AddComponent<SpriteRenderer>();
        sr.sprite = GetSoftGlowSprite();
        sr.color = color;
        sr.sortingOrder = 50;

        Vector3 mid = (from + to) * 0.5f + new Vector3(0f, 0.85f, -0.5f);
        Vector3 dest = new Vector3(to.x, to.y, -0.5f);
        Vector3[] path = { orb.transform.position, mid, dest };

        orb.transform.DOScale(0.95f, 0.2f).SetLoops(2, LoopType.Yoyo).SetLink(orb);
        orb.transform.DOPath(path, 0.42f, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .SetLink(orb)
            .OnComplete(() =>
            {
                SpawnDebrisBurst(to, color, count: 14, speed: 3.4f, scale: 0.12f);
                onArrive?.Invoke();
                if (orb != null) Object.Destroy(orb);
            });
    }
}
