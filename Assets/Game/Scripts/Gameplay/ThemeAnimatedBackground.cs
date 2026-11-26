using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders subtle, lightweight animated ambient backgrounds driven by the active ThemeDataSO:
/// - Dark Cosmic: twinkling drifting stars
/// - Ocean Depth: rising swaying bubbles
/// - Sky Breeze: drifting soft clouds
/// - Sunset Glow: floating warm golden embers
/// </summary>
public class ThemeAnimatedBackground : MonoBehaviour
{
    public enum AmbientMode
    {
        Stars = 0,
        Bubbles = 1,
        Clouds = 2,
        Embers = 3
    }

    private class AmbientItem
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float baseScale;
        public float baseAlpha;
        public float phase;
        public float pulseSpeed;
        public float swayAmplitude;
    }

    [Header("Spawn Bounds (World Space)")]
    [SerializeField] private Vector2 boundsHalfSize = new Vector2(4.2f, 6.4f);
    [SerializeField] private int sortingOrder = -40;

    private readonly List<AmbientItem> activeItems = new List<AmbientItem>();
    private AmbientMode currentMode = AmbientMode.Stars;
    private ThemeDataSO lastAppliedTheme;
    private bool hasBuiltPool = false;

    private static Sprite starSprite;
    private static Sprite bubbleSprite;
    private static Sprite cloudSprite;
    private static Sprite emberSprite;

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += HandleThemeChanged;
        SyncWithCurrentTheme();
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void Start()
    {
        SyncWithCurrentTheme();
    }

    public void SyncWithCurrentTheme()
    {
        ThemeDataSO activeTheme = ThemeManager.HasInstance ? ThemeManager.Instance.CurrentEquippedTheme : null;
        ApplyTheme(activeTheme);
    }

    private void HandleThemeChanged(ThemeDataSO theme)
    {
        ApplyTheme(theme);
    }

    public void ApplyTheme(ThemeDataSO theme)
    {
        AmbientMode desiredMode = ResolveModeForTheme(theme);
        if (hasBuiltPool && desiredMode == currentMode && lastAppliedTheme == theme) return;

        lastAppliedTheme = theme;
        ApplyMode(desiredMode);
    }

    private static AmbientMode ResolveModeForTheme(ThemeDataSO theme)
    {
        if (theme == null) return AmbientMode.Stars;

        string id = !string.IsNullOrEmpty(theme.themeId) ? theme.themeId.ToLowerInvariant() : theme.themeName.ToLowerInvariant();
        if (id.Contains("ocean") || id.Contains("aqua") || id.Contains("sea"))
        {
            return AmbientMode.Bubbles;
        }
        if (id.Contains("sky") || id.Contains("cloud"))
        {
            return AmbientMode.Clouds;
        }
        if (id.Contains("sunset") || id.Contains("warm") || id.Contains("fire"))
        {
            return AmbientMode.Embers;
        }
        return AmbientMode.Stars;
    }

    public void ApplyMode(AmbientMode mode)
    {
        currentMode = mode;
        hasBuiltPool = true;
        ClearItems();

        switch (mode)
        {
            case AmbientMode.Stars:
                SpawnAmbientPool(38, GetStarSprite(), new Color(0.85f, 0.92f, 1f),
                    minScale: 0.08f, maxScale: 0.24f, minAlpha: 0.22f, maxAlpha: 0.85f,
                    vxRange: new Vector2(-0.08f, 0.08f), vyRange: new Vector2(-0.14f, -0.03f), sway: 0f);
                break;

            case AmbientMode.Bubbles:
                SpawnAmbientPool(26, GetBubbleSprite(), new Color(0.65f, 0.95f, 1f),
                    minScale: 0.14f, maxScale: 0.42f, minAlpha: 0.14f, maxAlpha: 0.42f,
                    vxRange: new Vector2(-0.05f, 0.05f), vyRange: new Vector2(0.28f, 0.68f), sway: 0.24f);
                break;

            case AmbientMode.Clouds:
                SpawnAmbientPool(11, GetCloudSprite(), new Color(1f, 1f, 1f),
                    minScale: 0.9f, maxScale: 1.85f, minAlpha: 0.14f, maxAlpha: 0.30f,
                    vxRange: new Vector2(0.16f, 0.42f), vyRange: new Vector2(-0.02f, 0.02f), sway: 0.04f);
                break;

            case AmbientMode.Embers:
                SpawnAmbientPool(30, GetEmberSprite(), new Color(1f, 0.72f, 0.28f),
                    minScale: 0.09f, maxScale: 0.25f, minAlpha: 0.25f, maxAlpha: 0.82f,
                    vxRange: new Vector2(-0.12f, 0.12f), vyRange: new Vector2(0.18f, 0.52f), sway: 0.22f);
                break;
        }
    }

    private void ClearItems()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] != null && activeItems[i].transform != null)
            {
                Destroy(activeItems[i].transform.gameObject);
            }
        }
        activeItems.Clear();
    }

    private void SpawnAmbientPool(
        int count,
        Sprite sprite,
        Color baseTint,
        float minScale,
        float maxScale,
        float minAlpha,
        float maxAlpha,
        Vector2 vxRange,
        Vector2 vyRange,
        float sway)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject($"Ambient_{currentMode}_{i}");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(
                Random.Range(-boundsHalfSize.x, boundsHalfSize.x),
                Random.Range(-boundsHalfSize.y, boundsHalfSize.y),
                2f
            );

            float s = Random.Range(minScale, maxScale);
            obj.transform.localScale = new Vector3(s, s, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            float alpha = Random.Range(minAlpha, maxAlpha);
            Color tint = baseTint;
            if (currentMode == AmbientMode.Embers)
            {
                tint = Color.Lerp(new Color(1f, 0.52f, 0.20f), new Color(1f, 0.88f, 0.35f), Random.value);
            }
            else if (currentMode == AmbientMode.Stars)
            {
                tint = Color.Lerp(new Color(0.75f, 0.88f, 1f), new Color(1f, 0.96f, 0.82f), Random.value);
            }
            tint.a = alpha;
            sr.color = tint;

            activeItems.Add(new AmbientItem
            {
                transform = obj.transform,
                renderer = sr,
                velocity = new Vector2(Random.Range(vxRange.x, vxRange.y), Random.Range(vyRange.x, vyRange.y)),
                baseScale = s,
                baseAlpha = alpha,
                phase = Random.Range(0f, Mathf.PI * 2f),
                pulseSpeed = Random.Range(1.4f, 3.6f),
                swayAmplitude = sway
            });
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time;

        for (int i = 0; i < activeItems.Count; i++)
        {
            AmbientItem item = activeItems[i];
            if (item == null || item.transform == null) continue;

            Vector3 pos = item.transform.localPosition;
            pos.x += item.velocity.x * dt;
            pos.y += item.velocity.y * dt;

            if (item.swayAmplitude > 0f)
            {
                pos.x += Mathf.Sin(t * item.pulseSpeed + item.phase) * item.swayAmplitude * dt;
            }

            // Wrap seamlessly around camera bounds
            float pad = item.baseScale + 0.5f;
            if (pos.x > boundsHalfSize.x + pad) pos.x = -boundsHalfSize.x - pad;
            else if (pos.x < -boundsHalfSize.x - pad) pos.x = boundsHalfSize.x + pad;

            if (pos.y > boundsHalfSize.y + pad) pos.y = -boundsHalfSize.y - pad;
            else if (pos.y < -boundsHalfSize.y - pad) pos.y = boundsHalfSize.y + pad;

            item.transform.localPosition = pos;

            if (currentMode == AmbientMode.Stars || currentMode == AmbientMode.Embers)
            {
                float twinkle = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * item.pulseSpeed + item.phase));
                Color c = item.renderer.color;
                c.a = item.baseAlpha * twinkle;
                item.renderer.color = c;

                float scalePulse = item.baseScale * (0.85f + 0.25f * twinkle);
                item.transform.localScale = new Vector3(scalePulse, scalePulse, 1f);
            }
        }
    }

    #region Procedural Ambient Sprites

    private static Sprite GetStarSprite()
    {
        if (starSprite != null) return starSprite;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 rel = new Vector2(x, y) - center;
                float dist = rel.magnitude / (size * 0.5f);
                float core = Mathf.Clamp01(1f - dist * 1.4f);
                float cross = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(rel.x), Mathf.Abs(rel.y)) / 2.2f) * Mathf.Clamp01(1f - dist);
                float a = Mathf.Clamp01(core * core + cross * 0.7f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return starSprite;
    }

    private static Sprite GetBubbleSprite()
    {
        if (bubbleSprite != null) return bubbleSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 highlight = new Vector2(size * 0.36f, size * 0.66f);
        float maxR = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d > maxR)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float norm = d / maxR;
                float rim = norm > 0.78f ? Mathf.Clamp01(1f - Mathf.Abs(norm - 0.89f) / 0.11f) : 0.14f;
                float spec = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), highlight) / 8f);
                float alpha = Mathf.Clamp01(rim + spec * 0.85f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        bubbleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return bubbleSprite;
    }

    private static Sprite GetCloudSprite()
    {
        if (cloudSprite != null) return cloudSprite;

        int w = 128;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Vector3[] puffs =
        {
            new Vector3(42f, 26f, 22f),
            new Vector3(64f, 34f, 26f),
            new Vector3(88f, 26f, 20f),
            new Vector3(64f, 20f, 22f)
        };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = 0f;
                for (int p = 0; p < puffs.Length; p++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(puffs[p].x, puffs[p].y));
                    a = Mathf.Max(a, Mathf.Clamp01(1f - d / puffs[p].z));
                }
                a = Mathf.SmoothStep(0f, 1f, a);
                if (y < 14) a *= Mathf.Clamp01((y - 4f) / 10f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        cloudSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w * 0.5f);
        return cloudSprite;
    }

    private static Sprite GetEmberSprite()
    {
        if (emberSprite != null) return emberSprite;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float t = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), c) / maxR);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, t * t));
            }
        }

        tex.Apply();
        emberSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return emberSprite;
    }

    #endregion
}
