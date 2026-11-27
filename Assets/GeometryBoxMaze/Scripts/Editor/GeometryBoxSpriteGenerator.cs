#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GeometryBoxMaze.Editor
{
    /// <summary>
    /// Generates nostalgic childhood Geometry-Box sprites (chrome steel ball, molded plastic
    /// walls, sunken pit holes, finish dimple cup, ruler markings, obstacles, and 9-sliced UI).
    /// All outputs live under Assets/GeometryBoxMaze/Sprites/.
    /// </summary>
    public static class GeometryBoxSpriteGenerator
    {
        public const string SPRITES_FOLDER = "Assets/GeometryBoxMaze/Sprites";

        public static void GenerateAllSprites()
        {
            Directory.CreateDirectory(SPRITES_FOLDER);

            WriteAndConfigure("Chrome_Steel_Ball.png", CreateChromeBallTexture(128), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Chrome_Ball_Glint.png", CreateBallGlintTexture(96), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Ball_Shadow.png", CreateShadowTexture(96), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Desk_Wood_Bg.png", CreateWoodDeskTexture(512), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Tray_Plastic_Base.png", CreateTrayTexture(256), new Vector4(28, 28, 28, 28), FilterMode.Bilinear);
            WriteAndConfigure("Maze_Wall_Segment.png", CreateWallTexture(128), new Vector4(22, 22, 22, 22), FilterMode.Bilinear);
            WriteAndConfigure("Hole_Pit.png", CreateHoleTexture(128), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Goal_Pocket.png", CreateGoalTexture(128), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Obstacle_Bar.png", CreateObstacleTexture(128), new Vector4(20, 20, 20, 20), FilterMode.Bilinear);
            WriteAndConfigure("Eraser_Bumper.png", CreateEraserTexture(128, 64), new Vector4(16, 16, 16, 16), FilterMode.Bilinear);
            WriteAndConfigure("Gold_Pin.png", CreateGoldPinTexture(96), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Switch_Plate.png", CreateSwitchTexture(96), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Locked_Gate.png", CreateLockedGateTexture(128), new Vector4(20, 20, 20, 20), FilterMode.Bilinear);
            WriteAndConfigure("Box_Hinge.png", CreateHingeTexture(96, 64), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Box_Clasp.png", CreateClaspTexture(64, 112), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Protractor_Watermark.png", CreateProtractorTexture(256), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Ruler_Edge.png", CreateRulerTexture(512, 64), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Star_Icon.png", CreateStarTexture(64), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("Circle_Soft.png", CreateCircleTexture(96), Vector4.zero, FilterMode.Bilinear);
            WriteAndConfigure("UI_Card_9Slice.png", CreateUICardTexture(128, 26f, false), new Vector4(28, 28, 28, 28), FilterMode.Bilinear);
            WriteAndConfigure("UI_Button_9Slice.png", CreateUICardTexture(128, 26f, true), new Vector4(26, 26, 26, 26), FilterMode.Bilinear);

            AssetDatabase.Refresh();
        }

        public static Sprite LoadSprite(string fileName)
        {
            string path = $"{SPRITES_FOLDER}/{fileName}";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void WriteAndConfigure(string fileName, Texture2D tex, Vector4 border, FilterMode filter)
        {
            string assetPath = $"{SPRITES_FOLDER}/{fileName}";
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(assetPath, png);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.spriteBorder = border;
                ti.filterMode = filter;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }

        private static Texture2D CreateChromeBallTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / r;
                    float dy = (y + 0.5f - half) / r;
                    float d2 = dx * dx + dy * dy;
                    float dist = Mathf.Sqrt(d2) * r;
                    float alpha = Mathf.Clamp01((r - dist) * 0.9f + 0.5f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // 3D sphere normal approximation for realistic chrome reflection
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                    Vector3 n = new Vector3(dx, dy, nz).normalized;
                    Vector3 lightDir = new Vector3(-0.48f, 0.58f, 0.65f).normalized;
                    float ndl = Mathf.Clamp01(Vector3.Dot(n, lightDir));

                    // Studio horizon reflection band + specular gleam
                    float envBand = Mathf.Lerp(0.42f, 0.92f, Mathf.SmoothStep(-0.45f, 0.55f, n.y));
                    float fresnelRim = Mathf.Pow(1f - nz, 2.2f) * 0.28f;

                    Vector3 viewDir = Vector3.forward;
                    Vector3 halfVec = (lightDir + viewDir).normalized;
                    float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(n, halfVec)), 36f) * 0.95f;
                    float softSpec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(n, halfVec)), 6f) * 0.25f;

                    float lum = Mathf.Clamp01(envBand * 0.68f + ndl * 0.28f + softSpec + fresnelRim);
                    // Subtle brushed-steel swirl band so visual rotation of the core is palpable
                    float swirl = Mathf.Sin(dx * 5.2f + dy * 3.8f) * 0.045f + Mathf.Cos(dy * 7.5f) * 0.025f;
                    lum = Mathf.Clamp01(lum + swirl);

                    Color baseCol = new Color(lum * 0.92f, lum * 0.96f, Mathf.Min(1f, lum * 1.04f), 1f);
                    baseCol = Color.Lerp(baseCol, Color.white, Mathf.Clamp01(spec * 0.55f));
                    baseCol.a = alpha;
                    tex.SetPixel(x, y, baseCol);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBallGlintTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 2f;
            Vector2 specCenter = new Vector2(half - r * 0.34f, half + r * 0.34f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float centerDist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                    if (centerDist > r)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float dSpec = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), specCenter) / (r * 0.34f);
                    float mainGlow = Mathf.Clamp01(1f - dSpec);
                    mainGlow = mainGlow * mainGlow * 0.92f;

                    // Crisp bottom-right bounce rim highlight
                    float rimT = Mathf.Clamp01((centerDist / r - 0.78f) / 0.20f);
                    Vector2 dir = (new Vector2(x + 0.5f - half, y + 0.5f - half)).normalized;
                    float rimBounce = rimT * Mathf.Clamp01(Vector2.Dot(dir, new Vector2(0.55f, -0.65f))) * 0.38f;

                    float a = Mathf.Clamp01(mainGlow + rimBounce);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateWoodDeskTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    int plank = Mathf.FloorToInt(u * 4f);
                    float plankEdge = Mathf.Abs((u * 4f) - Mathf.Round(u * 4f));
                    float seam = plankEdge < 0.014f ? 0.62f : 1f;

                    // Organic vertical wood grain fibers
                    float wave = Mathf.Sin(v * 14f + plank * 2.1f) * 0.08f + Mathf.Sin(v * 37f + u * 22f) * 0.04f;
                    float grain = Mathf.Sin((u * 38f + plank * 3.7f + wave * 12f) * Mathf.PI);
                    float fineGrain = Mathf.Sin((u * 110f + v * 6f) * Mathf.PI) * 0.03f;

                    float shade = (0.86f + grain * 0.06f + fineGrain) * seam;
                    float vig = 1f - 0.28f * Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));

                    // Warm classroom walnut/oak study table tint
                    Color col = new Color(0.28f * shade * vig, 0.19f * shade * vig, 0.13f * shade * vig, 1f);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEraserTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float r = 10f;
            Vector2 ext = new Vector2(width * 0.5f - r - 2f, height * 0.5f - r - 2f);
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - ext;
                    float sdf = Mathf.Sqrt(Mathf.Max(q.x, 0f) * Mathf.Max(q.x, 0f) + Mathf.Max(q.y, 0f) * Mathf.Max(q.y, 0f))
                                + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
                    float alpha = Mathf.Clamp01(0.5f - sdf * 0.9f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Iconic classic two-tone Coral-Pink & Royal-Blue childhood rubber eraser!
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    Color baseCol = u < 0.64f ? new Color(0.95f, 0.42f, 0.48f, 1f) : new Color(0.22f, 0.50f, 0.88f, 1f);
                    if (Mathf.Abs(u - 0.64f) < 0.02f) baseCol = new Color(0.96f, 0.96f, 0.98f, 1f);

                    float bevel = v > 0.78f ? 1.12f : (v < 0.18f ? 0.76f : 1f);
                    baseCol.r = Mathf.Clamp01(baseCol.r * bevel);
                    baseCol.g = Mathf.Clamp01(baseCol.g * bevel);
                    baseCol.b = Mathf.Clamp01(baseCol.b * bevel);
                    baseCol.a = alpha;
                    tex.SetPixel(x, y, baseCol);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGoldPinTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((r - dist) * 0.9f + 0.5f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float t = dist / r;
                    float light = Mathf.Clamp01((-dx + dy) / r * 0.4f + 0.65f);
                    Color col;
                    if (t > 0.74f)
                        col = new Color(0.88f * light, 0.62f * light, 0.10f * light, alpha);
                    else if (t > 0.60f)
                        col = new Color(1f, 0.94f, 0.55f, alpha);
                    else
                        col = new Color(1f * light, 0.84f * light, 0.18f * light, alpha);

                    // Top-left specular glint
                    if (Vector2.Distance(new Vector2(dx, dy), new Vector2(-r * 0.30f, r * 0.30f)) < r * 0.22f)
                    {
                        col = Color.Lerp(col, Color.white, 0.75f);
                        col.a = alpha;
                    }
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSwitchTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                    float alpha = Mathf.Clamp01((r - dist) * 0.9f + 0.5f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float t = dist / r;
                    Color col = t > 0.76f
                        ? new Color(0.14f, 0.24f, 0.36f, alpha)
                        : (t > 0.65f ? new Color(0.75f, 0.95f, 1f, alpha) : new Color(0.14f, 0.86f, 0.95f, alpha));
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateLockedGateTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    bool band = ((x - y) / 14) % 2 == 0;
                    Color col = band ? new Color(0.12f, 0.76f, 0.92f, 1f) : new Color(0.08f, 0.45f, 0.65f, 1f);
                    if (edge < 0.08f) col = new Color(0.8f, 0.97f, 1f, 1f);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHingeTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    float cyl = Mathf.Sin(v * Mathf.PI) * 0.45f + 0.52f;
                    float seam = Mathf.Abs(u - 0.5f) < 0.04f ? 0.6f : 1f;
                    float rim = (u < 0.06f || u > 0.94f || v < 0.08f || v > 0.92f) ? 0.65f : 1f;
                    float l = Mathf.Clamp01(cyl * seam * rim);
                    tex.SetPixel(x, y, new Color(l * 0.86f, l * 0.90f, l * 0.96f, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateClaspTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    if (edge < 0.06f)
                    {
                        tex.SetPixel(x, y, new Color(0.32f, 0.36f, 0.44f, 1f));
                        continue;
                    }
                    float metal = 0.66f + Mathf.Sin(u * Mathf.PI) * 0.28f;
                    bool groove = Mathf.Abs(v - 0.3f) < 0.04f || Mathf.Abs(v - 0.7f) < 0.04f;
                    if (groove) metal *= 0.72f;
                    tex.SetPixel(x, y, new Color(metal * 0.90f, metal * 0.93f, metal * 0.98f, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateProtractorTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.22f);
            float outerR = size * 0.44f;
            float innerR = size * 0.25f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float dist = d.magnitude;
                    float a = 0f;

                    if (d.y >= 0f && dist <= outerR + 1.5f && dist >= innerR - 1.5f)
                    {
                        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                        bool onOuterArc = Mathf.Abs(dist - outerR) < 2.2f;
                        bool onInnerArc = Mathf.Abs(dist - innerR) < 2.0f;
                        bool majorTick = (Mathf.Abs((angle % 15f)) < 1.8f || Mathf.Abs((angle % 15f) - 15f) < 1.8f) && dist > outerR - 18f;
                        bool centerCross = (Mathf.Abs(d.x) < 1.5f || Mathf.Abs(d.y) < 1.8f);
                        if (onOuterArc || onInnerArc || majorTick || centerCross) a = 0.26f;
                        else a = 0.05f;
                    }
                    else if (Mathf.Abs(d.y) < 2.2f && Mathf.Abs(d.x) <= outerR)
                    {
                        a = 0.22f;
                    }
                    else if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) < 5f)
                    {
                        a = 0.24f;
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateShadowTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / (half - 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * 0.45f;
                    tex.SetPixel(x, y, new Color(0.03f, 0.05f, 0.1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTrayTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    // Faint molded grid & slight bevel border
                    float gridX = (x % 32 == 0) ? 0.025f : 0f;
                    float gridY = (y % 32 == 0) ? 0.025f : 0f;
                    float bevel = edge < 0.05f ? Mathf.Lerp(0.84f, 1.04f, edge / 0.05f) : 1f;
                    float centerSheen = 1f - 0.08f * Vector2.Distance(new Vector2(u, v), new Vector2(0.42f, 0.62f));

                    float shade = Mathf.Clamp01((0.92f + gridX + gridY) * bevel * centerSheen);
                    float alpha = edge < 0.008f ? 0f : 1f;
                    tex.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateWallTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    // Molded plastic rib: bright top/left catchlight, shaded bottom/right drop shadow
                    float topLight = Mathf.SmoothStep(0.65f, 1f, v) * 0.14f + Mathf.SmoothStep(0.35f, 0f, u) * 0.06f;
                    float botShade = Mathf.SmoothStep(0.28f, 0f, v) * 0.24f + Mathf.SmoothStep(0.75f, 1f, u) * 0.10f;
                    float rimDark = edge < 0.06f ? 0.85f : 1f;

                    float lum = Mathf.Clamp01((0.88f + topLight - botShade) * rimDark);
                    tex.SetPixel(x, y, new Color(lum, lum, lum, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHoleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float outerR = half - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((outerR - dist) * 0.9f + 0.5f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float t = dist / outerR;
                    Color col;
                    if (t < 0.72f)
                    {
                        // Deep sunken pit interior
                        float depth = Mathf.Lerp(0.02f, 0.09f, t / 0.72f);
                        col = new Color(depth * 0.9f, depth * 0.95f, depth * 1.15f, alpha);
                    }
                    else if (t < 0.88f)
                    {
                        // Beveled inner rim gradient (light catches bottom-right lip of a pit)
                        float rimFactor = (t - 0.72f) / 0.16f;
                        float directional = Mathf.Clamp01((-dx + dy) / outerR * 0.4f + 0.5f);
                        float v = Mathf.Lerp(0.14f, 0.34f, rimFactor) * Mathf.Lerp(0.75f, 1.3f, directional);
                        col = new Color(v * 0.82f, v * 0.90f, v * 1.05f, alpha);
                    }
                    else
                    {
                        // Outer plastic ring shadow
                        col = new Color(0.12f, 0.18f, 0.28f, alpha * 0.85f);
                    }
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGoalTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float outerR = half - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                    float alpha = Mathf.Clamp01((outerR - dist) * 0.9f + 0.5f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float t = dist / outerR;
                    Color col;
                    if (t < 0.45f)
                    {
                        // Rich emerald finish dimple
                        col = new Color(0.10f, 0.78f, 0.45f, alpha);
                    }
                    else if (t < 0.65f)
                    {
                        // Crisp white ring divider
                        col = new Color(0.95f, 1f, 0.96f, alpha);
                    }
                    else if (t < 0.88f)
                    {
                        // Outer emerald gold-tinged ring
                        col = new Color(0.16f, 0.88f, 0.52f, alpha);
                    }
                    else
                    {
                        // Metallic border lip
                        col = new Color(1f, 0.86f, 0.28f, alpha);
                    }
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateObstacleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    // Crisp molded chevron warning stripe pattern
                    bool stripe = (((x + y) / 14) % 2) == 0;
                    float bevel = v > 0.75f ? 1.12f : (v < 0.22f ? 0.78f : 1f);
                    if (edge < 0.06f) bevel *= 0.82f;

                    Color baseCol = stripe ? new Color(1f, 0.52f, 0.16f, 1f) : new Color(0.96f, 0.74f, 0.18f, 1f);
                    baseCol.r = Mathf.Clamp01(baseCol.r * bevel);
                    baseCol.g = Mathf.Clamp01(baseCol.g * bevel);
                    baseCol.b = Mathf.Clamp01(baseCol.b * bevel);
                    tex.SetPixel(x, y, baseCol);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRulerTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = y == 0 || y == height - 1;
                    int pxMod = x % 32;
                    bool isMajorTick = (x % 64) <= 2 && y < height * 0.55f;
                    bool isMinorTick = (pxMod <= 1) && y < height * 0.34f;
                    bool isMidTick = ((x % 32) == 16) && y < height * 0.22f;

                    if (isBorder)
                        tex.SetPixel(x, y, new Color(0.18f, 0.30f, 0.45f, 0.55f));
                    else if (isMajorTick)
                        tex.SetPixel(x, y, new Color(0.15f, 0.26f, 0.42f, 0.85f));
                    else if (isMinorTick || isMidTick)
                        tex.SetPixel(x, y, new Color(0.18f, 0.30f, 0.46f, 0.52f));
                    else
                        tex.SetPixel(x, y, new Color(0.88f, 0.94f, 1f, 0.14f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                    float a = Mathf.Clamp01((r - d) * 0.75f + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStarTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2[] poly = new Vector2[10];
            Vector2 c = new Vector2(size * 0.5f, size * 0.48f);
            float outer = size * 0.44f;
            float inner = size * 0.19f;
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.Deg2Rad * (90f + i * 36f);
                float r = (i % 2 == 0) ? outer : inner;
                poly[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = PointInPolygon(p, poly);
                    float d = DistToEdges(p, poly);
                    float a = Mathf.Clamp01((inside ? d : -d) * 0.85f + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateUICardTexture(int size, float cornerRadius, bool glossLip)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            Vector2 boxExtents = new Vector2(half - cornerRadius - 2f, half - cornerRadius - 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - boxExtents;
                    float sdf = Mathf.Sqrt(Mathf.Max(q.x, 0f) * Mathf.Max(q.x, 0f) + Mathf.Max(q.y, 0f) * Mathf.Max(q.y, 0f))
                                + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - cornerRadius;

                    float alpha = Mathf.Clamp01(0.5f - sdf * 0.85f);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float lum = 1f;
                    if (glossLip)
                    {
                        float v = y / (float)(size - 1);
                        if (v < 0.14f) lum = 0.78f;
                        else if (v > 0.86f) lum = 1.0f;
                        else if (v > 0.55f) lum = 0.98f;
                        else lum = 0.92f;
                    }
                    else if (sdf > -3.5f)
                    {
                        lum = 1.0f;
                    }
                    else
                    {
                        lum = 0.93f;
                    }

                    tex.SetPixel(x, y, new Color(lum, lum, lum, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y + 1e-6f) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float DistToEdges(Vector2 p, Vector2[] poly)
        {
            float minD = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
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
}
#endif
