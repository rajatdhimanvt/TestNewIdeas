using TMPro;
using UnityEngine;

/// <summary>
/// Runtime UI styling resource lookup (LilitaOne SDF font + procedural rounded sprites).
/// Used by dynamic modals, LevelMapController, and HUD components.
/// </summary>
public static class BallMergeUITheme
{
    public const string FONT_RESOURCE_PATH = "BallMerge/UI/LilitaOne SDF";

    private static TMP_FontAsset cachedFont;
    private static bool attemptedFontLoad;
    private static Sprite cachedCircleSprite;
    private static Sprite cachedStarSprite;

    public static TMP_FontAsset GetFont()
    {
        if (cachedFont == null && !attemptedFontLoad)
        {
            attemptedFontLoad = true;
            cachedFont = Resources.Load<TMP_FontAsset>(FONT_RESOURCE_PATH);
        }
        return cachedFont;
    }

    public static void ApplyFont(TMP_Text tmp)
    {
        if (tmp == null) return;
        TMP_FontAsset font = GetFont();
        if (font != null)
        {
            tmp.font = font;
        }
    }

    public static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        const int size = 96;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float radius = center - 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) * 0.65f + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedCircleSprite;
    }

    public static Sprite GetStarSprite()
    {
        if (cachedStarSprite != null) return cachedStarSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Vector2[] poly = new Vector2[10];
        Vector2 center = new Vector2(size * 0.5f, size * 0.48f);
        float outerR = size * 0.44f;
        float innerR = size * 0.19f;
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.Deg2Rad * (90f + i * 36f);
            float r = (i % 2 == 0) ? outerR : innerR;
            poly[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = PointInPolygon(p, poly);
                float minDist = DistanceToPolygonEdges(p, poly);
                float signedDist = inside ? minDist : -minDist;
                float alpha = Mathf.Clamp01(signedDist * 0.85f + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedStarSprite;
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y + 1e-6f) + poly[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private static float DistanceToPolygonEdges(Vector2 p, Vector2[] poly)
    {
        float minD = float.MaxValue;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = poly[j];
            Vector2 b = poly[i];
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            float t = lenSq > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq) : 0f;
            float d = Vector2.Distance(p, a + ab * t);
            if (d < minD) minD = d;
        }
        return minD;
    }
}
