#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates crisp 2D PNG sprite assets for blocks, platforms, and progressive cracks
/// inside Assets/Game/Sprites/Blocks/.
/// </summary>
public static class BlockSpriteGenerator
{
    public const string SPRITES_FOLDER = "Assets/Game/Sprites/Blocks";

    public static void EnsureAllBlockSpritesExist()
    {
        EnsureFolder(SPRITES_FOLDER);

        CreateCrateSprite("Block_Crate_Weak.png",
            new Color(0.98f, 0.72f, 0.32f, 1f),
            new Color(0.84f, 0.46f, 0.14f, 1f),
            new Color(0.92f, 0.22f, 0.16f, 1f), 128);

        CreateCrateSprite("Block_Crate_Normal.png",
            new Color(0.95f, 0.56f, 0.16f, 1f),
            new Color(0.70f, 0.28f, 0.08f, 1f),
            new Color(0.86f, 0.14f, 0.14f, 1f), 128);

        CreateBrickStoneSprite("Block_Stone_Strong.png",
            new Color(0.45f, 0.53f, 0.64f, 1f),
            new Color(0.26f, 0.32f, 0.40f, 1f),
            new Color(0.62f, 0.70f, 0.80f, 1f), 128);

        CreateIronMetalSprite("Block_Iron_Heavy.png",
            new Color(0.32f, 0.36f, 0.44f, 1f),
            new Color(0.16f, 0.19f, 0.24f, 1f),
            new Color(0.75f, 0.82f, 0.90f, 1f), 128);

        CreateBombCrateSprite("Block_Bomb_Special.png", 128);
        CreateBonusCrateSprite("Block_Bonus_Special.png", 128);
        CreateWoodShelfSprite("Block_Shelf_Platform.png", 256, 40);

        CreateIceFrozenSprite("Block_Ice_Frozen.png", 128);
        CreateMovingPlatformSprite("Block_Moving_Platform.png", 256, 44);
        CreateKeyCrateSprite("Block_Key_Special.png", 128);
        CreateLockedGateSprite("Block_Locked_Gate.png", 128);
        CreatePadlockOverlaySprite("Block_Padlock_Overlay.png", 128);
        CreateMagnetCoreSprite("Block_Magnet_Special.png", 128);
        CreateBumperPadSprite("Block_Bumper_Pad.png", 128);

        CreateCrackSprite("Crack_Stage1.png", 1, 128);
        CreateCrackSprite("Crack_Stage2.png", 2, 128);
        CreateCrackSprite("Crack_Stage3.png", 3, 128);
        CreateSolidWhiteSprite("UI_White_Bar.png", 16);

        AssetDatabase.Refresh();
    }

    public static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_FOLDER}/{fileName}");
    }

    private static void CreateCrateSprite(string fileName, Color fill, Color border, Color cross, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool outerOutline = x < 3 || x >= size - 3 || y < 3 || y >= size - 3;
                bool frame = x < 13 || x >= size - 13 || y < 13 || y >= size - 13;
                float d1 = Mathf.Abs(x - y);
                float d2 = Mathf.Abs((size - 1 - x) - y);
                bool inInner = x >= 12 && x < size - 12 && y >= 12 && y < size - 12;
                bool isCross = inInner && (d1 < 7.5f || d2 < 7.5f);

                Color c = fill;
                if (outerOutline)
                {
                    c = border * 0.6f;
                    c.a = 1f;
                }
                else if (frame)
                {
                    // Subtle bevel highlight on top/left
                    bool highlight = (y >= size - 8 || x < 8);
                    c = highlight ? Color.Lerp(border, Color.white, 0.18f) : border;
                }
                else if (isCross)
                {
                    c = cross;
                }
                else
                {
                    // Subtle wood grain lines
                    float grain = Mathf.Sin(y * 0.45f) * 0.03f;
                    c = new Color(Mathf.Clamp01(fill.r + grain), Mathf.Clamp01(fill.g + grain), Mathf.Clamp01(fill.b + grain), 1f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateBrickStoneSprite(string fileName, Color fill, Color mortar, Color highlight, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 8 || x >= size - 8 || y < 8 || y >= size - 8;
                bool hMortar = Mathf.Abs(y - size / 2) < 4 || Mathf.Abs(y - size / 4) < 3 || Mathf.Abs(y - (size * 3) / 4) < 3;
                int row = (y * 4) / size;
                int offset = (row % 2 == 0) ? 0 : size / 4;
                bool vMortar = Mathf.Abs(((x + offset) % (size / 2)) - 2) < 3;

                Color c = fill;
                if (border || hMortar || vMortar)
                {
                    c = mortar;
                }
                else if (y > size - 14 || x < 14)
                {
                    c = Color.Lerp(fill, highlight, 0.35f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateIronMetalSprite(string fileName, Color body, Color darkRim, Color rivet, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Vector2[] rivets =
        {
            new Vector2(20, 20), new Vector2(size - 20, 20),
            new Vector2(20, size - 20), new Vector2(size - 20, size - 20)
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool frame = x < 12 || x >= size - 12 || y < 12 || y >= size - 12;
                bool innerEdge = (x >= 12 && x < 16) || (x >= size - 16 && x < size - 12) || (y >= 12 && y < 16) || (y >= size - 16 && y < size - 12);
                float d1 = Mathf.Abs(x - y);
                bool diagonalRib = d1 < 5f && x > 18 && x < size - 18;

                Color c = body;
                if (frame) c = darkRim;
                else if (innerEdge || diagonalRib) c = Color.Lerp(body, rivet, 0.32f);

                for (int r = 0; r < rivets.Length; r++)
                {
                    if (Vector2.Distance(new Vector2(x, y), rivets[r]) < 6.5f)
                    {
                        c = rivet;
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateBombCrateSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 10 || x >= size - 10 || y < 10 || y >= size - 10;
                bool hazardStripe = border && (((x + y) / 14) % 2 == 0);
                float dist = Vector2.Distance(new Vector2(x, y), center);

                Color c = new Color(0.22f, 0.14f, 0.16f, 1f);
                if (hazardStripe) c = new Color(0.95f, 0.72f, 0.12f, 1f);
                else if (border) c = new Color(0.75f, 0.14f, 0.14f, 1f);
                else if (dist < 28f) c = new Color(0.95f, 0.25f, 0.15f, 1f);
                else if (dist < 34f) c = new Color(1.0f, 0.65f, 0.15f, 1f);

                tex.SetPixel(x, y, c);
            }
        }

        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateBonusCrateSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 11 || x >= size - 11 || y < 11 || y >= size - 11;
                Vector2 rel = new Vector2(x, y) - center;
                float diamond = Mathf.Abs(rel.x) + Mathf.Abs(rel.y);

                Color c = new Color(0.14f, 0.65f, 0.45f, 1f);
                if (border) c = new Color(0.95f, 0.78f, 0.18f, 1f);
                else if (diamond < 28f) c = new Color(1f, 0.92f, 0.35f, 1f);
                else if (diamond < 35f) c = new Color(0.98f, 0.75f, 0.15f, 1f);

                tex.SetPixel(x, y, c);
            }
        }

        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateWoodShelfSprite(string fileName, int width, int height)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color main = new Color(0.58f, 0.35f, 0.18f, 1f);
        Color topHighlight = new Color(0.78f, 0.52f, 0.28f, 1f);
        Color bottomShadow = new Color(0.36f, 0.20f, 0.10f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool endCap = x < 6 || x >= width - 6;
                Color c = main;
                if (endCap) c = bottomShadow;
                else if (y >= height - 6) c = topHighlight;
                else if (y < 6) c = bottomShadow;

                tex.SetPixel(x, y, c);
            }
        }

        SaveAndConfigureSprite(tex, path, width);
    }

    private static void CreateIceFrozenSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color body = new Color(0.55f, 0.88f, 0.98f, 0.95f);
        Color rim = new Color(0.85f, 0.97f, 1.0f, 1.0f);
        Color deep = new Color(0.28f, 0.65f, 0.88f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool outer = x < 9 || x >= size - 9 || y < 9 || y >= size - 9;
                float streak = Mathf.Abs((x - y) - 24f);
                float streak2 = Mathf.Abs((x - y) + 28f);
                bool shine = (streak < 6f || streak2 < 4f) && x > 14 && x < size - 14 && y > 14 && y < size - 14;
                bool centerCrystal = Mathf.Abs(x - 64) + Mathf.Abs(y - 64) < 20;

                Color c = body;
                if (outer) c = (y >= size - 9 || x < 9) ? rim : deep;
                else if (shine) c = rim;
                else if (centerCrystal) c = Color.Lerp(body, rim, 0.55f);

                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateMovingPlatformSprite(string fileName, int width, int height)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color body = new Color(0.22f, 0.52f, 0.72f, 1f);
        Color rim = new Color(0.12f, 0.30f, 0.45f, 1f);
        Color glow = new Color(0.35f, 0.92f, 1.0f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool edge = x < 8 || x >= width - 8 || y < 6 || y >= height - 6;
                bool stripe = Mathf.Abs(y - height / 2) < 4 && x > 22 && x < width - 22;
                bool chevron = (x > 26 && x < 42 && Mathf.Abs(Mathf.Abs(y - height / 2) - (x - 26) * 0.6f) < 3f)
                            || (x > width - 42 && x < width - 26 && Mathf.Abs(Mathf.Abs(y - height / 2) - (width - 26 - x) * 0.6f) < 3f);

                Color c = body;
                if (edge) c = rim;
                else if (stripe || chevron) c = glow;
                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, width);
    }

    private static void CreateKeyCrateSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 ringCenter = new Vector2(46f, 64f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 10 || x >= size - 10 || y < 10 || y >= size - 10;
                float distRing = Vector2.Distance(new Vector2(x, y), ringCenter);
                bool keyRing = distRing >= 9f && distRing <= 16f;
                bool keyShaft = x >= 58 && x <= 96 && Mathf.Abs(y - 64) <= 4;
                bool keyTeeth = (x >= 80 && x <= 86 && y >= 50 && y < 64) || (x >= 90 && x <= 96 && y >= 52 && y < 64);

                Color c = new Color(0.12f, 0.48f, 0.56f, 1f);
                if (border) c = new Color(0.96f, 0.78f, 0.22f, 1f);
                else if (keyRing || keyShaft || keyTeeth) c = new Color(1.0f, 0.88f, 0.28f, 1f);

                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateLockedGateSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 12 || x >= size - 12 || y < 12 || y >= size - 12;
                bool chain1 = Mathf.Abs(x - y) < 4 && (((x + y) / 10) % 2 == 0);
                bool chain2 = Mathf.Abs((size - 1 - x) - y) < 4 && (((x + y) / 10) % 2 == 0);

                Color c = new Color(0.28f, 0.26f, 0.34f, 1f);
                if (border) c = new Color(0.55f, 0.45f, 0.22f, 1f);
                else if (chain1 || chain2) c = new Color(0.65f, 0.68f, 0.74f, 1f);

                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreatePadlockOverlaySprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color gold = new Color(1f, 0.82f, 0.22f, 1f);
        Color darkGold = new Color(0.72f, 0.46f, 0.08f, 1f);
        Color keyhole = new Color(0.12f, 0.10f, 0.08f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = Color.clear;
                if (x >= 32 && x <= 96 && y >= 20 && y <= 72)
                {
                    bool rim = x < 38 || x > 90 || y < 26 || y > 66;
                    c = rim ? darkGold : gold;
                    float dx = x - 64f;
                    float dy = y - 50f;
                    if ((dx * dx + dy * dy < 64f) || (Mathf.Abs(dx) < 4.5f && y >= 32 && y <= 50))
                    {
                        c = keyhole;
                    }
                }
                else if (y > 72 && y <= 108)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 72f));
                    if (dist >= 18f && dist <= 29f)
                    {
                        c = new Color(0.84f, 0.88f, 0.94f, 1f);
                    }
                }
                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateMagnetCoreSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(64f, 64f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 10 || x >= size - 10 || y < 10 || y >= size - 10;
                float d = Vector2.Distance(new Vector2(x, y), center);
                bool outerRing = d >= 26f && d <= 36f;
                bool core = d <= 16f;

                Color c = new Color(0.16f, 0.20f, 0.34f, 1f);
                if (border) c = x < 64 ? new Color(0.92f, 0.22f, 0.28f, 1f) : new Color(0.22f, 0.58f, 0.98f, 1f);
                else if (outerRing) c = new Color(0.45f, 0.88f, 1f, 1f);
                else if (core) c = Color.white;

                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateBumperPadSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(64f, 64f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 10 || x >= size - 10 || y < 10 || y >= size - 10;
                float d = Vector2.Distance(new Vector2(x, y), center);
                bool neonRing = d >= 22f && d <= 34f;
                bool core = d <= 14f;

                Color c = new Color(0.32f, 0.12f, 0.45f, 1f);
                if (border) c = new Color(0.95f, 0.25f, 0.78f, 1f);
                else if (neonRing) c = new Color(1.0f, 0.45f, 0.95f, 1f);
                else if (core) c = new Color(1.0f, 0.90f, 0.35f, 1f);

                tex.SetPixel(x, y, c);
            }
        }
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateSolidWhiteSprite(string fileName, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void CreateCrackSprite(string fileName, int stage, int size)
    {
        string path = $"{SPRITES_FOLDER}/{fileName}";
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Color crack = new Color(0.1f, 0.06f, 0.04f, 0.9f);
        DrawCrackLine(pixels, size, new Vector2(64, 64), new Vector2(22, 108), crack, 2);
        DrawCrackLine(pixels, size, new Vector2(64, 64), new Vector2(104, 84), crack, 2);

        if (stage >= 2)
        {
            DrawCrackLine(pixels, size, new Vector2(64, 64), new Vector2(24, 24), crack, 3);
            DrawCrackLine(pixels, size, new Vector2(44, 88), new Vector2(14, 70), crack, 2);
            DrawCrackLine(pixels, size, new Vector2(88, 72), new Vector2(114, 52), crack, 2);
        }

        if (stage >= 3)
        {
            DrawCrackLine(pixels, size, new Vector2(64, 64), new Vector2(108, 20), crack, 3);
            DrawCrackLine(pixels, size, new Vector2(48, 42), new Vector2(64, 10), crack, 3);
            DrawCrackLine(pixels, size, new Vector2(64, 92), new Vector2(68, 120), crack, 2);
            DrawCrackLine(pixels, size, new Vector2(36, 54), new Vector2(10, 48), crack, 2);
        }

        tex.SetPixels(pixels);
        SaveAndConfigureSprite(tex, path, size);
    }

    private static void DrawCrackLine(Color[] pixels, int size, Vector2 a, Vector2 b, Color col, int thickness)
    {
        int segments = 32;
        Vector2 prev = a;
        Vector2 perp = new Vector2(-(b.y - a.y), b.x - a.x).normalized;

        for (int s = 1; s <= segments; s++)
        {
            float t = (float)s / segments;
            float offset = (s < segments) ? Mathf.Sin(s * 2.9f + a.x + b.y) * 4f : 0f;
            Vector2 next = Vector2.Lerp(a, b, t) + perp * offset;

            int samples = Mathf.Max(4, Mathf.CeilToInt(Vector2.Distance(prev, next) * 2f));
            for (int i = 0; i <= samples; i++)
            {
                Vector2 p = Vector2.Lerp(prev, next, (float)i / samples);
                int cx = Mathf.RoundToInt(p.x);
                int cy = Mathf.RoundToInt(p.y);
                for (int dy = -thickness; dy <= thickness; dy++)
                {
                    for (int dx = -thickness; dx <= thickness; dx++)
                    {
                        if (dx * dx + dy * dy > thickness * thickness) continue;
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < size && py >= 0 && py < size)
                        {
                            pixels[py * size + px] = col;
                        }
                    }
                }
            }
            prev = next;
        }
    }

    private static void SaveAndConfigureSprite(Texture2D tex, string assetPath, float pixelsPerUnit)
    {
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(assetPath, png);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folder = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
