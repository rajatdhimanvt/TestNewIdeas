#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility to generate default assets, sample Ball Kits, environment Themes, 
/// and automatically build complete UGUI Canvas, panels, and visual button hierarchies in Unity.
/// Accessible via Unity Editor top menu: Tools -> Ball Merge.
/// </summary>
public static class BallMergeAssetCreator
{
    private const string DATA_FOLDER = "Assets/Game/Data";
    private const string KITS_FOLDER = "Assets/Game/Data/Kits";
    private const string THEMES_FOLDER = "Assets/Game/Data/Themes";

    [MenuItem("Tools/Ball Merge/1. Generate Default Level Assets, Kits & Themes")]
    public static void GenerateDefaultAssets()
    {
        EnsureFolderExists(DATA_FOLDER);
        EnsureFolderExists(KITS_FOLDER);
        EnsureFolderExists(THEMES_FOLDER);

        // 1. Generate Classic Kit
        var classicTiersConfig = new[]
        {
            new { name = "Cherry", radius = 0.35f, color = new Color(0.95f, 0.2f, 0.25f), score = 2, mass = 0.8f },
            new { name = "Plum", radius = 0.50f, color = new Color(0.65f, 0.25f, 0.85f), score = 4, mass = 1.0f },
            new { name = "Orange", radius = 0.70f, color = new Color(1.0f, 0.6f, 0.1f), score = 8, mass = 1.4f },
            new { name = "Apple", radius = 0.95f, color = new Color(0.3f, 0.85f, 0.3f), score = 16, mass = 1.9f },
            new { name = "Melon", radius = 1.25f, color = new Color(0.95f, 0.9f, 0.2f), score = 32, mass = 2.6f },
            new { name = "Watermelon", radius = 1.60f, color = new Color(0.1f, 0.75f, 0.5f), score = 64, mass = 3.5f }
        };
        BallKitSO classicKit = CreateKitAsset("classic_kit", "Classic Fruits", classicTiersConfig);

        // 2. Generate Ocean Kit
        var oceanTiersConfig = new[]
        {
            new { name = "Shell", radius = 0.35f, color = new Color(1.0f, 0.85f, 0.75f), score = 2, mass = 0.8f },
            new { name = "Starfish", radius = 0.50f, color = new Color(1.0f, 0.4f, 0.4f), score = 4, mass = 1.0f },
            new { name = "Jellyfish", radius = 0.70f, color = new Color(0.9f, 0.4f, 0.9f), score = 8, mass = 1.4f },
            new { name = "Crab", radius = 0.95f, color = new Color(0.95f, 0.3f, 0.2f), score = 16, mass = 1.9f },
            new { name = "Octopus", radius = 1.25f, color = new Color(0.5f, 0.2f, 0.7f), score = 32, mass = 2.6f },
            new { name = "Whale", radius = 1.60f, color = new Color(0.1f, 0.4f, 0.9f), score = 64, mass = 3.5f }
        };
        BallKitSO oceanKit = CreateKitAsset("ocean_kit", "Ocean World", oceanTiersConfig);

        // 3. Generate Sky Kit
        var skyTiersConfig = new[]
        {
            new { name = "Cloud", radius = 0.35f, color = new Color(0.9f, 0.95f, 1.0f), score = 2, mass = 0.8f },
            new { name = "Bird", radius = 0.50f, color = new Color(0.3f, 0.75f, 1.0f), score = 4, mass = 1.0f },
            new { name = "Kite", radius = 0.70f, color = new Color(1.0f, 0.5f, 0.2f), score = 8, mass = 1.4f },
            new { name = "Airship", radius = 0.95f, color = new Color(0.85f, 0.3f, 0.3f), score = 16, mass = 1.9f },
            new { name = "Moon", radius = 1.25f, color = new Color(0.95f, 0.95f, 0.7f), score = 32, mass = 2.6f },
            new { name = "Star", radius = 1.60f, color = new Color(1.0f, 0.85f, 0.1f), score = 64, mass = 3.5f }
        };
        BallKitSO skyKit = CreateKitAsset("sky_kit", "Sky High", skyTiersConfig);

        // 4. Generate Visual Environment Themes
        CreateThemeAsset("dark_theme", "Dark Cosmic", new Color(0.12f, 0.13f, 0.18f), new Color(0.2f, 0.65f, 1.0f, 0.8f), new Color(1.0f, 0.3f, 0.3f, 0.4f));
        CreateThemeAsset("ocean_theme", "Ocean Depth", new Color(0.04f, 0.11f, 0.22f), new Color(0.12f, 0.82f, 0.85f, 0.8f), new Color(1.0f, 0.5f, 0.2f, 0.4f));
        CreateThemeAsset("sky_theme", "Sky Breeze", new Color(0.16f, 0.29f, 0.49f), new Color(0.48f, 0.91f, 1.0f, 0.85f), new Color(1.0f, 0.4f, 0.7f, 0.4f));
        CreateThemeAsset("sunset_theme", "Sunset Glow", new Color(0.16f, 0.08f, 0.21f), new Color(1.0f, 0.49f, 0.2f, 0.85f), new Color(1.0f, 0.82f, 0.0f, 0.4f));

        // 5. Create or update LevelDataSO assets (Endless, Level 1 & Default)
        LevelDataSO endlessData = CreateOrUpdateLevelAsset($"{DATA_FOLDER}/LevelData_Endless.asset", "Endless Mode", GameMode.Endless, 0, 0, 0, classicKit);
        LevelDataSO level1Data = CreateOrUpdateLevelAsset($"{DATA_FOLDER}/LevelData_Level1.asset", "Level 1: Target 500", GameMode.Level, 1, 500, 30, classicKit);

        string defaultPath = $"{DATA_FOLDER}/LevelData_Default.asset";
        LevelDataSO levelData = AssetDatabase.LoadAssetAtPath<LevelDataSO>(defaultPath);
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(levelData, defaultPath);
        }

        levelData.gameMode = GameMode.Endless;
        levelData.levelNumber = 0;
        levelData.targetScore = 0;
        levelData.maxDrops = 0;
        levelData.levelName = "Classic Endless";
        levelData.defaultKit = classicKit;
        levelData.allTiers = classicKit.ballTiers;
        levelData.droppableTiers = classicKit.droppableTiers;
        levelData.containerWidth = 5.5f;
        levelData.containerHeight = 8.5f;
        levelData.wallThickness = 0.5f;
        levelData.dropHeight = 3.8f;
        levelData.dropCooldown = 0.5f;
        levelData.dangerLineY = 2.5f;
        levelData.dangerTimeLimit = 3.0f;

        EditorUtility.SetDirty(levelData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BallMergeAssetCreator] Successfully created Ball Kits, Environment Themes, Endless Data & Level 1 Data!");
        Selection.activeObject = levelData;
    }

    private static LevelDataSO CreateOrUpdateLevelAsset(string assetPath, string levelName, GameMode mode, int levelNum, int targetScore, int maxDrops, BallKitSO kit)
    {
        LevelDataSO data = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        data.gameMode = mode;
        data.levelName = levelName;
        data.levelNumber = levelNum;
        data.targetScore = targetScore;
        data.maxDrops = maxDrops;
        data.defaultKit = kit;
        if (kit != null)
        {
            data.allTiers = kit.ballTiers;
            data.droppableTiers = kit.droppableTiers;
        }
        data.containerWidth = 5.5f;
        data.containerHeight = 8.5f;
        data.wallThickness = 0.5f;
        data.dropHeight = 3.8f;
        data.dropCooldown = 0.5f;
        data.dangerLineY = 2.5f;
        data.dangerTimeLimit = 3.0f;

        EditorUtility.SetDirty(data);
        return data;
    }

    private static BallKitSO CreateKitAsset<TConfig>(string kitId, string kitName, TConfig[] tiersConfig) where TConfig : class
    {
        string kitFolderPath = $"{KITS_FOLDER}/{kitId}";
        EnsureFolderExists(kitFolderPath);

        var allTiers = new List<BallDataSO>();

        for (int i = 0; i < tiersConfig.Length; i++)
        {
            dynamic cfg = tiersConfig[i];
            string ballName = cfg.name;
            string ballAssetPath = $"{kitFolderPath}/BallData_Tier_{i}_{ballName}.asset";

            BallDataSO ballData = AssetDatabase.LoadAssetAtPath<BallDataSO>(ballAssetPath);
            if (ballData == null)
            {
                ballData = ScriptableObject.CreateInstance<BallDataSO>();
                AssetDatabase.CreateAsset(ballData, ballAssetPath);
            }

            ballData.tier = i;
            ballData.ballName = ballName;
            ballData.radius = cfg.radius;
            ballData.ballColor = cfg.color;
            ballData.scoreValue = cfg.score;
            ballData.mass = cfg.mass;
            ballData.bounciness = 0.2f;
            ballData.friction = 0.4f;

            EditorUtility.SetDirty(ballData);
            allTiers.Add(ballData);
        }

        string kitAssetPath = $"{KITS_FOLDER}/BallKit_{kitId}.asset";
        BallKitSO kitSO = AssetDatabase.LoadAssetAtPath<BallKitSO>(kitAssetPath);
        if (kitSO == null)
        {
            kitSO = ScriptableObject.CreateInstance<BallKitSO>();
            AssetDatabase.CreateAsset(kitSO, kitAssetPath);
        }

        kitSO.kitId = kitId;
        kitSO.kitName = kitName;
        kitSO.ballTiers = allTiers;
        kitSO.droppableTiers = new List<BallDataSO>
        {
            allTiers[0],
            allTiers[1],
            allTiers[2]
        };

        EditorUtility.SetDirty(kitSO);
        return kitSO;
    }

    private static ThemeDataSO CreateThemeAsset(string themeId, string themeName, Color bgCol, Color wallCol, Color dangerCol)
    {
        string themeAssetPath = $"{THEMES_FOLDER}/ThemeData_{themeId}.asset";
        ThemeDataSO themeSO = AssetDatabase.LoadAssetAtPath<ThemeDataSO>(themeAssetPath);
        if (themeSO == null)
        {
            themeSO = ScriptableObject.CreateInstance<ThemeDataSO>();
            AssetDatabase.CreateAsset(themeSO, themeAssetPath);
        }

        themeSO.themeId = themeId;
        themeSO.themeName = themeName;
        themeSO.backgroundColor = bgCol;
        themeSO.wallColor = wallCol;
        themeSO.dangerLineColor = dangerCol;

        EditorUtility.SetDirty(themeSO);
        return themeSO;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/Ball Merge/2. Auto Setup Complete Game Scene")]
    public static void AutoSetupScene()
    {
        // 1. Ensure assets & block library exist
        GenerateDefaultAssets();
        BlockLibrarySO blockLib = BlockLevelSetupUtility.EnsureBlockLibraryAndDefinitions();
        BlockLevelSetupUtility.EnsureSampleLevelsAndDatabase(blockLib);
        string levelPath = $"{DATA_FOLDER}/LevelData_Default.asset";
        LevelDataSO levelData = AssetDatabase.LoadAssetAtPath<LevelDataSO>(levelPath);

        // 2. Setup Main Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.18f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        if (cam.GetComponent<CameraShake2D>() == null) cam.gameObject.AddComponent<CameraShake2D>();

        // 3. Setup Managers GameObject
        GameObject managersObj = GameObject.Find("[Managers]");
        if (managersObj == null) managersObj = new GameObject("[Managers]");

        DependencyManager dm = managersObj.GetComponent<DependencyManager>();
        if (dm == null) dm = managersObj.AddComponent<DependencyManager>();

        GameManager gm = managersObj.GetComponent<GameManager>();
        if (gm == null) gm = managersObj.AddComponent<GameManager>();

        AudioManager am = managersObj.GetComponent<AudioManager>();
        if (am == null) am = managersObj.AddComponent<AudioManager>();

        BallMergeManager bmm = managersObj.GetComponent<BallMergeManager>();
        if (bmm == null) bmm = managersObj.AddComponent<BallMergeManager>();
        bmm.SetLevelData(levelData);

        FloatingScoreManager fsm = managersObj.GetComponent<FloatingScoreManager>();
        if (fsm == null) fsm = managersObj.AddComponent<FloatingScoreManager>();

        // Setup PlayerDataManager
        PlayerDataManager pdm = managersObj.GetComponent<PlayerDataManager>();
        if (pdm == null) pdm = managersObj.AddComponent<PlayerDataManager>();

        // Setup BallKitManager
        BallKitManager bkm = managersObj.GetComponent<BallKitManager>();
        if (bkm == null) bkm = managersObj.AddComponent<BallKitManager>();

        string classicPath = $"{KITS_FOLDER}/BallKit_classic_kit.asset";
        string oceanPath = $"{KITS_FOLDER}/BallKit_ocean_kit.asset";
        string skyPath = $"{KITS_FOLDER}/BallKit_sky_kit.asset";

        BallKitSO classicKit = AssetDatabase.LoadAssetAtPath<BallKitSO>(classicPath);
        BallKitSO oceanKit = AssetDatabase.LoadAssetAtPath<BallKitSO>(oceanPath);
        BallKitSO skyKit = AssetDatabase.LoadAssetAtPath<BallKitSO>(skyPath);

        var kits = new List<BallKitSO>();
        if (classicKit != null) kits.Add(classicKit);
        if (oceanKit != null) kits.Add(oceanKit);
        if (skyKit != null) kits.Add(skyKit);

        bkm.RegisterKits(kits, classicKit);

        // Setup ThemeManager
        ThemeManager tm = managersObj.GetComponent<ThemeManager>();
        if (tm == null) tm = managersObj.AddComponent<ThemeManager>();

        string darkThemePath = $"{THEMES_FOLDER}/ThemeData_dark_theme.asset";
        string oceanThemePath = $"{THEMES_FOLDER}/ThemeData_ocean_theme.asset";
        string skyThemePath = $"{THEMES_FOLDER}/ThemeData_sky_theme.asset";
        string sunsetThemePath = $"{THEMES_FOLDER}/ThemeData_sunset_theme.asset";

        ThemeDataSO darkTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>(darkThemePath);
        ThemeDataSO oceanTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>(oceanThemePath);
        ThemeDataSO skyTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>(skyThemePath);
        ThemeDataSO sunsetTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>(sunsetThemePath);

        var themes = new List<ThemeDataSO>();
        if (darkTheme != null) themes.Add(darkTheme);
        if (oceanTheme != null) themes.Add(oceanTheme);
        if (skyTheme != null) themes.Add(skyTheme);
        if (sunsetTheme != null) themes.Add(sunsetTheme);

        tm.RegisterThemes(themes, darkTheme);

        // 4. Setup Gameplay GameObject
        GameObject gameplayObj = GameObject.Find("[Gameplay]");
        if (gameplayObj == null) gameplayObj = new GameObject("[Gameplay]");

        GameplayController gc = gameplayObj.GetComponent<GameplayController>();
        if (gc == null) gc = gameplayObj.AddComponent<GameplayController>();

        ContainerBoundary cb = gameplayObj.GetComponent<ContainerBoundary>();
        if (cb == null) cb = gameplayObj.AddComponent<ContainerBoundary>();
        cb.Setup(levelData);

        // 5. Setup DangerLine
        GameObject dangerLineObj = GameObject.Find("DangerLine");
        if (dangerLineObj == null)
        {
            dangerLineObj = new GameObject("DangerLine");
            dangerLineObj.transform.SetParent(gameplayObj.transform);
        }
        DangerLine dl = dangerLineObj.GetComponent<DangerLine>();
        if (dl == null) dl = dangerLineObj.AddComponent<DangerLine>();
        dl.Setup(levelData);

        // 6. Setup BallDropper
        GameObject dropperObj = GameObject.Find("BallDropper");
        if (dropperObj == null)
        {
            dropperObj = new GameObject("BallDropper");
            dropperObj.transform.SetParent(gameplayObj.transform);
        }
        BallDropper bd = dropperObj.GetComponent<BallDropper>();
        if (bd == null) bd = dropperObj.AddComponent<BallDropper>();
        bd.Setup(levelData);

        // 6b. Setup LevelBlockManager
        GameObject blockMgrObj = GameObject.Find("LevelBlockManager");
        if (blockMgrObj == null)
        {
            blockMgrObj = new GameObject("LevelBlockManager");
            blockMgrObj.transform.SetParent(gameplayObj.transform);
        }
        LevelBlockManager lbm = blockMgrObj.GetComponent<LevelBlockManager>();
        if (lbm == null)
        {
            lbm = blockMgrObj.AddComponent<LevelBlockManager>();
        }

        // 6c. Setup ThemeAnimatedBackground
        GameObject bgFxObj = GameObject.Find("ThemeAnimatedBackground");
        if (bgFxObj == null)
        {
            bgFxObj = new GameObject("ThemeAnimatedBackground");
            bgFxObj.transform.SetParent(gameplayObj.transform);
        }
        ThemeAnimatedBackground tab = bgFxObj.GetComponent<ThemeAnimatedBackground>();
        if (tab == null)
        {
            tab = bgFxObj.AddComponent<ThemeAnimatedBackground>();
        }

        SerializedObject gcSo = new SerializedObject(gc);
        SafeSetProperty(gcSo, "blockManager", lbm);
        SafeSetProperty(gcSo, "animatedBackground", tab);
        gcSo.ApplyModifiedPropertiesWithoutUndo();

        // Initialize Level in GameplayController
        gc.InitializeLevel(levelData);

        // 7. Setup UI Canvas, Visual Hierarchy and Auto-Wire Buttons
        SetupUICanvas();

        // Apply theme visual update
        tm.ApplyCurrentTheme();

        // Mark scene dirty so changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[BallMergeAssetCreator] COMPLETE GAME SCENE & UI CANVAS GENERATED SUCCESSFULLY!");
    }

    [MenuItem("Tools/Ball Merge/3. Rebuild Main Menu Panel (Hyper-Casual)")]
    public static void RebuildMainMenuOnly()
    {
        LoadUISprites();
        GameObject uiObj = GameObject.Find("UIManager_Canvas");
        if (uiObj == null)
        {
            UIManager existingUim = Object.FindAnyObjectByType<UIManager>();
            if (existingUim != null) uiObj = existingUim.gameObject;
        }

        if (uiObj == null)
        {
            Debug.LogError("[BallMergeAssetCreator] UIManager_Canvas not found in active scene! Please run 'Tools -> Ball Merge -> 2. Auto Setup Complete Game Scene' first.");
            return;
        }

        BuildMainMenuPanel(uiObj);

        UIManager uim = uiObj.GetComponent<UIManager>();
        if (uim != null)
        {
            uim.Init();
            uim.ShowPanel<MainMenuPanel>();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[BallMergeAssetCreator] Hyper-Casual Main Menu Panel rebuilt successfully!");
    }


    private static Sprite s_btnGreen;
    private static Sprite s_btnYellow;
    private static Sprite s_btnBlue;
    private static Sprite s_btnRed;
    private static Sprite s_frameBlue;
    private static Sprite s_frameCyan;
    private static Sprite s_framePurple;
    private static Sprite s_bannerBlue;
    private static Sprite s_bannerYellow;
    private static Sprite s_circleSprite;

    private static Sprite GetCircleSprite()
    {
        if (s_circleSprite != null) return s_circleSprite;
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float radius = center - 1f;
        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        s_circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return s_circleSprite;
    }

    private static void LoadUISprites()
    {
        string p2 = "Assets/Game/Sprites/Full sprite sheet/UI-pack_Sprite_2.png";
        AssetDatabase.ImportAsset(p2, ImportAssetOptions.ForceUpdate);
        Sprite[] pack2 = AssetDatabase.LoadAllAssetsAtPath(p2).OfType<Sprite>().ToArray();

        s_btnGreen = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_16");
        s_btnYellow = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_9");
        s_btnBlue = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_10");
        s_btnRed = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_17");
        s_frameBlue = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_0");
        s_frameCyan = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_4");
        s_framePurple = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_2");
        s_bannerBlue = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_5");
        s_bannerYellow = pack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_1");

        Debug.Log($"[BallMergeAssetCreator] Loaded {pack2.Length} sprites: Green={s_btnGreen != null}, Yellow={s_btnYellow != null}, Blue={s_btnBlue != null}, Red={s_btnRed != null}, FrameBlue={s_frameBlue != null}, FrameCyan={s_frameCyan != null}");
    }

    private static void SetupUICanvas()
    {
        LoadUISprites();
        // 1. Setup EventSystem
        if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Setup [UI] Canvas
        GameObject uiObj = GameObject.Find("[UI]");
        if (uiObj == null) uiObj = new GameObject("[UI]");

        Canvas canvas = uiObj.GetComponent<Canvas>();
        if (canvas == null) canvas = uiObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = uiObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = uiObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        if (uiObj.GetComponent<GraphicRaycaster>() == null)
        {
            uiObj.AddComponent<GraphicRaycaster>();
        }

        UIManager uim = uiObj.GetComponent<UIManager>();
        if (uim == null) uim = uiObj.AddComponent<UIManager>();

        // Build Visual Hierarchy for Panels
        BuildSplashScreenPanel(uiObj);
        BuildMainMenuPanel(uiObj);
        BuildLevelSelectionPanel(uiObj);
        BuildDailyRewardPanel(uiObj);
        BuildGameplayHUDPanel(uiObj);
        BuildPausePanel(uiObj);
        BuildGameOverPanel(uiObj);
        BuildLevelWinPanel(uiObj);
        BuildBallKitShopPanel(uiObj);
        BuildBallKitPreviewPanel(uiObj);
        BuildThemeShopPanel(uiObj);
        BuildThemePreviewPanel(uiObj);
        BuildSettingsPanel(uiObj);
        BuildGenericPopupPanel(uiObj);

        uim.Init();
        uim.ShowPanel<SplashScreenPanel>();
    }

    private static void BuildSplashScreenPanel(GameObject parent)
    {
        SplashScreenPanel panel = EnsurePanel<SplashScreenPanel>(parent);
        ClearChildren(panel.gameObject);

        // Full Screen Dark Background
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // Game Branding Title
        TMP_Text title = CreateUIText(panel.gameObject, "TitleText", "BALL MERGE 2D", new Vector2(0, 300), new Vector2(900, 160), 72, new Color(0.95f, 0.85f, 0.2f), TextAlignmentOptions.Center);
        CreateUIText(panel.gameObject, "SubtitleText", "LOADING...", new Vector2(0, 190), new Vector2(600, 60), 28, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);

        // Loading Slider Container
        GameObject sliderObj = new GameObject("LoadingSlider");
        sliderObj.transform.SetParent(panel.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -400);
        sliderRect.sizeDelta = new Vector2(750, 40);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;

        // Slider Background Track
        Image sliderBg = CreateUIImage(sliderObj, "Background", Vector2.zero, new Vector2(750, 40), new Color(0.15f, 0.17f, 0.24f, 1f));

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        Image fillImg = CreateUIImage(fillArea, "Fill", Vector2.zero, new Vector2(750, 40), new Color(0.18f, 0.8f, 0.44f));
        slider.fillRect = fillImg.rectTransform;
        slider.targetGraphic = sliderBg;

        TMP_Text progressTxt = CreateUIText(panel.gameObject, "ProgressText", "LOADING... 0%", new Vector2(0, -470), new Vector2(600, 50), 26, Color.white, TextAlignmentOptions.Center);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "titleText", title);
        SafeSetProperty(so, "loadingSlider", slider);
        SafeSetProperty(so, "progressText", progressTxt);
        so.ApplyModifiedProperties();
    }

    #region Panel Visual Builders

    private static void BuildMainMenuPanel(GameObject parent)
    {
        MainMenuPanel panel = EnsurePanel<MainMenuPanel>(parent);
        ClearChildren(panel.gameObject);

        // 1. Deep Modern Gradient/Dark Backdrop
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.06f, 0.08f, 0.12f, 1f));

        // Subtle ambient aura glow behind the game logo
        Image ambientGlow = CreateUIImage(panel.gameObject, "AmbientGlow", new Vector2(0, 520), new Vector2(850, 850), new Color(0.12f, 0.35f, 0.7f, 0.14f));
        ambientGlow.sprite = GetCircleSprite();

        // -------------------------------------------------------------
        // 2. TOP STATUS BAR (Y = 820)
        // -------------------------------------------------------------
        GameObject topBar = new GameObject("TopStatusBar");
        topBar.transform.SetParent(panel.transform, false);
        RectTransform topBarRect = topBar.AddComponent<RectTransform>();
        topBarRect.anchoredPosition = new Vector2(0, 820);
        topBarRect.sizeDelta = new Vector2(980, 100);

        // Settings Button (Left: X = -430)
        Button settingsBtn = CreateUIButton(topBar, "SettingsButton", "⚙", new Vector2(-430, 0), new Vector2(90, 90), new Color(0.18f, 0.28f, 0.44f), Color.white, s_btnBlue);
        TMP_Text settingsTxt = settingsBtn.GetComponentInChildren<TMP_Text>();
        if (settingsTxt != null) settingsTxt.fontSize = 44;

        // HighScore Pill Badge (Center: X = 0)
        GameObject hsBadge = CreateUICard(topBar, "HighScoreBadge", new Vector2(0, 0), new Vector2(400, 75), new Color(0.16f, 0.14f, 0.08f, 0.95f), s_frameCyan);
        TMP_Text highScoreTxt = CreateUIText(hsBadge, "HighScoreText", "BEST: 0", Vector2.zero, new Vector2(380, 65), 26, new Color(1.0f, 0.88f, 0.25f), TextAlignmentOptions.Center);
        highScoreTxt.fontStyle = FontStyles.Bold;

        // Coin Counter Pill Badge (Right: X = 370)
        GameObject coinBadge = CreateUICard(topBar, "CoinBadge", new Vector2(370, 0), new Vector2(280, 75), new Color(0.18f, 0.16f, 0.08f, 0.95f), s_frameCyan);
        TMP_Text coinBalanceText = CreateUIText(coinBadge, "CoinBalanceText", "0 COINS", new Vector2(-15, 0), new Vector2(200, 65), 26, new Color(1.0f, 0.9f, 0.35f), TextAlignmentOptions.Center);
        coinBalanceText.fontStyle = FontStyles.Bold;
        // Shiny green plus badge on coin counter
        GameObject plusBadge = CreateUICard(coinBadge, "PlusBadge", new Vector2(105, 0), new Vector2(46, 46), new Color(0.18f, 0.8f, 0.44f), s_btnGreen);
        TMP_Text plusTxt = CreateUIText(plusBadge, "PlusText", "+", Vector2.zero, new Vector2(46, 46), 28, Color.white, TextAlignmentOptions.Center);
        plusTxt.fontStyle = FontStyles.Bold;

        // -------------------------------------------------------------
        // 3. HERO GAME LOGO & FLOATING EYE-CANDY (Y = 530)
        // -------------------------------------------------------------
        GameObject logoContainer = new GameObject("LogoContainer");
        logoContainer.transform.SetParent(panel.transform, false);
        RectTransform logoRect = logoContainer.AddComponent<RectTransform>();
        logoRect.anchoredPosition = new Vector2(0, 530);
        logoRect.sizeDelta = new Vector2(900, 240);

        // Title Main Banner / Text
        TMP_Text mainTitle = CreateUIText(logoContainer, "TitleText", "BALL MERGE", new Vector2(0, 35), new Vector2(850, 110), 82, new Color(1.0f, 0.85f, 0.15f), TextAlignmentOptions.Center);
        mainTitle.fontStyle = FontStyles.Bold;

        // Subtitle Pill
        GameObject subPill = CreateUICard(logoContainer, "SubtitlePill", new Vector2(0, -42), new Vector2(360, 48), new Color(0.12f, 0.25f, 0.45f, 0.95f), s_frameCyan);
        TMP_Text subTitle = CreateUIText(subPill, "SubtitleText", "2D  PHYSICS  PUZZLE", Vector2.zero, new Vector2(360, 48), 22, new Color(0.35f, 0.92f, 1.0f), TextAlignmentOptions.Center);
        subTitle.fontStyle = FontStyles.Bold;

        // Cute Floating Decorative Balls
        List<Transform> decorBalls = new List<Transform>();

        // Ball 1 (Cherry Red - Left)
        GameObject ball1 = CreateDecorBall(logoContainer, "DecorBall_Cherry", new Vector2(-310, 15), 72f, new Color(1.0f, 0.22f, 0.28f));
        decorBalls.Add(ball1.transform);

        // Ball 2 (Melon Green - Right)
        GameObject ball2 = CreateDecorBall(logoContainer, "DecorBall_Melon", new Vector2(320, -10), 92f, new Color(0.18f, 0.85f, 0.45f));
        decorBalls.Add(ball2.transform);

        // Ball 3 (Plum Purple - Upper Right)
        GameObject ball3 = CreateDecorBall(logoContainer, "DecorBall_Plum", new Vector2(250, 75), 62f, new Color(0.68f, 0.32f, 0.92f));
        decorBalls.Add(ball3.transform);

        // -------------------------------------------------------------
        // 4. GIANT HERO "PLAY" BUTTON (Y = 220)
        // -------------------------------------------------------------
        GameObject heroPlayBox = new GameObject("HeroPlayContainer");
        heroPlayBox.transform.SetParent(panel.transform, false);
        RectTransform heroPlayRect = heroPlayBox.AddComponent<RectTransform>();
        heroPlayRect.anchoredPosition = new Vector2(0, 220);
        heroPlayRect.sizeDelta = new Vector2(720, 160);

        Button playEndlessBtn = CreateUIButton(heroPlayBox, "PlayEndlessButton", "", Vector2.zero, new Vector2(720, 160), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);

        // Hero Play Button Labels
        TMP_Text playMainTxt = CreateUIText(playEndlessBtn.gameObject, "PlayTitle", "▶  PLAY NOW", new Vector2(0, 18), new Vector2(680, 75), 54, Color.white, TextAlignmentOptions.Center);
        playMainTxt.fontStyle = FontStyles.Bold;
        TMP_Text playSubTxt = CreateUIText(playEndlessBtn.gameObject, "PlaySub", "ENDLESS HIGHSCORE MODE", new Vector2(0, -32), new Vector2(680, 40), 20, new Color(0.85f, 1.0f, 0.85f), TextAlignmentOptions.Center);
        playSubTxt.fontStyle = FontStyles.Bold;

        // -------------------------------------------------------------
        // 5. 50 CAMPAIGN LEVELS CARD (Y = -25)
        // -------------------------------------------------------------
        GameObject campaignCard = CreateUICard(panel.gameObject, "CampaignCard", new Vector2(0, -25), new Vector2(920, 195), new Color(0.1f, 0.16f, 0.26f, 0.95f), s_frameCyan);

        // Left info
        TMP_Text campTitle = CreateUIText(campaignCard, "CampTitle", "CAMPAIGN ADVENTURE", new Vector2(-160, 36), new Vector2(500, 50), 32, new Color(0.2f, 0.85f, 1.0f), TextAlignmentOptions.Left);
        campTitle.fontStyle = FontStyles.Bold;

        TMP_Text campLvlTxt = CreateUIText(campaignCard, "CampaignLevelText", "LEVEL 1", new Vector2(-280, -18), new Vector2(250, 45), 26, new Color(1.0f, 0.85f, 0.2f), TextAlignmentOptions.Left);
        campLvlTxt.fontStyle = FontStyles.Bold;

        TMP_Text campStarsTxt = CreateUIText(campaignCard, "CampaignStarsText", "STARS: 0 / 150", new Vector2(-80, -18), new Vector2(300, 45), 21, new Color(0.85f, 0.9f, 0.98f), TextAlignmentOptions.Left);

        // Right buttons
        Button playCampaignBtn = CreateUIButton(campaignCard, "PlayCampaignButton", "PLAY LVL 1", new Vector2(325, 0), new Vector2(230, 85), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        TMP_Text playCampaignBtnTxt = playCampaignBtn.GetComponentInChildren<TMP_Text>();
        if (playCampaignBtnTxt != null) playCampaignBtnTxt.fontSize = 26;

        Button mapBtn = CreateUIButton(campaignCard, "LevelMapButton", "MAP", new Vector2(115, 0), new Vector2(160, 85), new Color(0.2f, 0.6f, 0.86f), Color.white, s_btnYellow);
        TMP_Text mapBtnTxt = mapBtn.GetComponentInChildren<TMP_Text>();
        if (mapBtnTxt != null) mapBtnTxt.fontSize = 26;

        // -------------------------------------------------------------
        // 6. DAILY REWARDS & LUCKY WHEEL BANNER (Y = -245)
        // -------------------------------------------------------------
        GameObject dailyCard = CreateUICard(panel.gameObject, "DailyCard", new Vector2(0, -245), new Vector2(920, 145), new Color(0.22f, 0.15f, 0.26f, 0.95f), s_framePurple);

        TMP_Text dailyTitle = CreateUIText(dailyCard, "DailyTitle", "DAILY REWARD & SPIN WHEEL", new Vector2(-140, 22), new Vector2(550, 45), 28, new Color(1.0f, 0.8f, 0.2f), TextAlignmentOptions.Left);
        dailyTitle.fontStyle = FontStyles.Bold;

        CreateUIText(dailyCard, "DailySub", "Free login streak coins & lucky wheel spin!", new Vector2(-140, -18), new Vector2(550, 35), 20, new Color(0.85f, 0.8f, 0.95f), TextAlignmentOptions.Left);

        Button dailyRewardBtn = CreateUIButton(dailyCard, "DailyRewardButton", "SPIN & CLAIM", new Vector2(320, 0), new Vector2(240, 80), new Color(0.95f, 0.65f, 0.1f), Color.white, s_btnYellow);
        TMP_Text dailyBtnTxt = dailyRewardBtn.GetComponentInChildren<TMP_Text>();
        if (dailyBtnTxt != null) dailyBtnTxt.fontSize = 25;

        // Animated Notification Pill Badge on Daily Reward
        GameObject notifBadge = CreateUICard(dailyCard, "DailyNotificationBadge", new Vector2(420, 36), new Vector2(80, 36), new Color(0.95f, 0.2f, 0.25f), s_btnRed);
        TMP_Text notifTxt = CreateUIText(notifBadge, "BadgeText", "FREE!", Vector2.zero, new Vector2(80, 36), 18, Color.white, TextAlignmentOptions.Center);
        notifTxt.fontStyle = FontStyles.Bold;

        // -------------------------------------------------------------
        // 7. BOTTOM NAVIGATION DOCK (Y = -480)
        // -------------------------------------------------------------
        GameObject bottomDock = CreateUICard(panel.gameObject, "BottomDock", new Vector2(0, -480), new Vector2(920, 135), new Color(0.12f, 0.14f, 0.22f, 0.95f), s_frameBlue);

        // 3 App-style action buttons
        Button ballKitsBtn = CreateUIButton(bottomDock, "BallKitsButton", "BALL KITS\nSKINS SHOP", new Vector2(-295, 0), new Vector2(280, 92), new Color(0.2f, 0.55f, 0.85f), Color.white, s_btnBlue);
        TMP_Text kitsTxt = ballKitsBtn.GetComponentInChildren<TMP_Text>();
        if (kitsTxt != null) kitsTxt.fontSize = 22;

        Button themesBtn = CreateUIButton(bottomDock, "ThemesButton", "THEMES\nWORLDS", new Vector2(0, 0), new Vector2(280, 92), new Color(0.2f, 0.55f, 0.85f), Color.white, s_btnBlue);
        TMP_Text themesTxt = themesBtn.GetComponentInChildren<TMP_Text>();
        if (themesTxt != null) themesTxt.fontSize = 22;

        Button quitBtn = CreateUIButton(bottomDock, "QuitButton", "QUIT\nGAME", new Vector2(295, 0), new Vector2(280, 92), new Color(0.85f, 0.25f, 0.2f), Color.white, s_btnRed);
        TMP_Text quitTxt = quitBtn.GetComponentInChildren<TMP_Text>();
        if (quitTxt != null) quitTxt.fontSize = 22;

        // -------------------------------------------------------------
        // 8. AUTO-WIRE SERIALIZED OBJECT PROPERTIES
        // -------------------------------------------------------------
        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "playEndlessButton", playEndlessBtn);
        SafeSetProperty(so, "playButtonTransform", heroPlayBox.transform);
        SafeSetProperty(so, "playLevel1Button", playCampaignBtn);
        SafeSetProperty(so, "playLevelButtonText", playCampaignBtnTxt);
        SafeSetProperty(so, "levelMapButton", mapBtn);
        SafeSetProperty(so, "dailyRewardButton", dailyRewardBtn);
        SafeSetProperty(so, "dailyNotificationBadge", notifBadge);
        SafeSetProperty(so, "dailyBadgeText", notifTxt);
        SafeSetProperty(so, "ballKitsButton", ballKitsBtn);
        SafeSetProperty(so, "themesButton", themesBtn);
        SafeSetProperty(so, "settingsButton", settingsBtn);
        SafeSetProperty(so, "quitButton", quitBtn);
        SafeSetProperty(so, "highScoreText", highScoreTxt);
        SafeSetProperty(so, "coinBalanceText", coinBalanceText);
        SafeSetProperty(so, "campaignLevelText", campLvlTxt);
        SafeSetProperty(so, "campaignStarsText", campStarsTxt);
        SafeSetProperty(so, "logoContainer", logoContainer.transform);
        SafeSetListProperty(so, "decorativeBalls", decorBalls);
        so.ApplyModifiedProperties();
    }

    private static void BuildLevelSelectionPanel(GameObject parent)
    {
        LevelSelectionPanel panel = EnsurePanel<LevelSelectionPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // Header Card
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 720), new Vector2(940, 160), new Color(0.12f, 0.18f, 0.28f, 0.95f));
        CreateUIText(headerCard, "TitleText", "CAMPAIGN LEVELS", new Vector2(0, 25), new Vector2(900, 70), 52, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);
        TMP_Text totalStarsText = CreateUIText(headerCard, "TotalStarsText", "TOTAL STARS: ⭐ 0 / 150", new Vector2(-150, -30), new Vector2(600, 45), 24, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Left);
        TMP_Text coinBalanceText = CreateUIText(headerCard, "CoinBalanceText", "🪙 0 COINS", new Vector2(250, -30), new Vector2(350, 45), 24, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Right);

        // Grid Container Box (hosts Canvas new ideas LevelScrollView adventure map)
        GameObject gridCard = CreateUICard(panel.gameObject, "GridCard", new Vector2(0, 50), new Vector2(940, 1100), new Color(0.12f, 0.14f, 0.2f, 0.95f));

        LevelMapController adventureMap = null;
        Transform gridContentTf = null;
        GameObject scrollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlockLevelSetupUtility.LEVEL_SCROLL_PREFAB_PATH);
        if (scrollPrefab != null)
        {
            GameObject scrollInstance = PrefabUtility.InstantiatePrefab(scrollPrefab, gridCard.transform) as GameObject;
            if (scrollInstance != null)
            {
                scrollInstance.name = "LevelScrollView";
                RectTransform srt = scrollInstance.GetComponent<RectTransform>();
                if (srt != null)
                {
                    srt.anchorMin = Vector2.zero;
                    srt.anchorMax = Vector2.one;
                    srt.pivot = new Vector2(0.5f, 0.5f);
                    srt.offsetMin = new Vector2(16f, 16f);
                    srt.offsetMax = new Vector2(-16f, -16f);
                }

                Image bgImg = scrollInstance.GetComponent<Image>();
                if (bgImg != null) bgImg.color = new Color(0.06f, 0.09f, 0.16f, 0.92f);

                adventureMap = scrollInstance.GetComponent<LevelMapController>();
                if (adventureMap != null)
                {
                    SerializedObject lmcSo = new SerializedObject(adventureMap);
                    SerializedProperty loadAllProp = lmcSo.FindProperty("loadAllInitially");
                    if (loadAllProp != null) loadAllProp.boolValue = false;
                    SerializedProperty initCountProp = lmcSo.FindProperty("initialLoadCount");
                    if (initCountProp != null) initCountProp.intValue = 50;
                    SerializedProperty batchProp = lmcSo.FindProperty("batchLoadCount");
                    if (batchProp != null) batchProp.intValue = 25;
                    lmcSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
        else
        {
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(gridCard.transform, false);
            RectTransform scrollRectTransform = scrollObj.AddComponent<RectTransform>();
            scrollRectTransform.anchoredPosition = Vector2.zero;
            scrollRectTransform.sizeDelta = new Vector2(900, 1060);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(900, 2900);

            scrollRect.content = contentRect;
            gridContentTf = contentObj.transform;
        }

        // Bottom Back Button
        Button backButton = CreateUIButton(panel.gameObject, "BackButton", "⬅ BACK TO MENU", new Vector2(0, -600), new Vector2(500, 95), new Color(0.25f, 0.3f, 0.4f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "totalStarsText", totalStarsText);
        SafeSetProperty(so, "coinBalanceText", coinBalanceText);
        SafeSetProperty(so, "levelGridContainer", gridContentTf);
        SafeSetProperty(so, "levelMapController", adventureMap);
        SafeSetProperty(so, "backButton", backButton);
        so.ApplyModifiedProperties();
    }

    private static void BuildDailyRewardPanel(GameObject parent)
    {
        DailyRewardPanel panel = EnsurePanel<DailyRewardPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background Dim
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // Header Section
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 740), new Vector2(940, 140), new Color(0.22f, 0.18f, 0.1f, 0.95f));
        CreateUIText(headerCard, "TitleText", "DAILY REWARDS & SPIN WHEEL", new Vector2(0, 20), new Vector2(900, 60), 46, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);
        TMP_Text statusText = CreateUIText(headerCard, "StatusText", "7-Day Login Streak Active!", new Vector2(0, -30), new Vector2(900, 40), 22, Color.white, TextAlignmentOptions.Center);

        // 1. Daily 7-Day Login Streak Box (Y = 460)
        GameObject streakCard = CreateUICard(panel.gameObject, "StreakCard", new Vector2(0, 460), new Vector2(940, 320), new Color(0.14f, 0.16f, 0.24f, 0.95f));
        CreateUIText(streakCard, "StreakTitle", "7-DAY LOGIN STREAK REWARDS", new Vector2(0, 110), new Vector2(900, 40), 26, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);

        TMP_Text streakDayText = CreateUIText(streakCard, "StreakDayText", "DAY 1 REWARD: 🪙 50", new Vector2(0, 40), new Vector2(800, 50), 32, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);

        Button claimStreakButton = CreateUIButton(streakCard, "ClaimStreakButton", "🎁 CLAIM TODAY'S REWARD", new Vector2(0, -80), new Vector2(500, 75), new Color(0.18f, 0.8f, 0.44f), Color.white);
        TMP_Text claimStreakText = claimStreakButton.GetComponentInChildren<TMP_Text>();

        // 2. Physics Spin Wheel Box (Y = -120)
        GameObject wheelCard = CreateUICard(panel.gameObject, "WheelCard", new Vector2(0, -120), new Vector2(940, 660), new Color(0.16f, 0.12f, 0.24f, 0.95f));
        CreateUIText(wheelCard, "WheelTitle", "LUCKY SPIN WHEEL", new Vector2(0, 290), new Vector2(900, 40), 32, new Color(0.95f, 0.65f, 0.1f), TextAlignmentOptions.Center);

        // Wheel Outer Circle Frame
        GameObject wheelContainer = CreateUICard(wheelCard, "WheelContainer", new Vector2(0, 30), new Vector2(460, 460), new Color(0.22f, 0.18f, 0.32f, 1f));

        // Pointer Arrow at top of wheel
        CreateUIImage(wheelCard, "WheelPointer", new Vector2(0, 240), new Vector2(40, 60), new Color(0.95f, 0.2f, 0.25f));

        Button spinButton = CreateUIButton(wheelCard, "SpinButton", "🎰 FREE SPIN", new Vector2(0, -260), new Vector2(450, 85), new Color(0.95f, 0.65f, 0.1f), Color.white);
        TMP_Text spinButtonText = spinButton.GetComponentInChildren<TMP_Text>();

        // Bottom Close Button
        Button closeButton = CreateUIButton(panel.gameObject, "CloseButton", "⬅ BACK TO MENU", new Vector2(0, -580), new Vector2(500, 95), new Color(0.25f, 0.3f, 0.4f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "wheelTransform", wheelContainer.transform);
        SafeSetProperty(so, "spinButton", spinButton);
        SafeSetProperty(so, "spinButtonText", spinButtonText);
        SafeSetProperty(so, "statusText", statusText);
        SafeSetProperty(so, "streakDayText", streakDayText);
        SafeSetProperty(so, "claimStreakButton", claimStreakButton);
        SafeSetProperty(so, "claimStreakText", claimStreakText);
        SafeSetProperty(so, "closeButton", closeButton);
        so.ApplyModifiedProperties();
    }

    private static void BuildGameplayHUDPanel(GameObject parent)
    {
        GameplayHUDPanel panel = EnsurePanel<GameplayHUDPanel>(parent);
        ClearChildren(panel.gameObject);

        // Top Header Bar
        CreateUIImage(panel.gameObject, "TopHeaderBar", new Vector2(0, 860), new Vector2(1080, 200), new Color(0.08f, 0.09f, 0.12f, 0.85f));

        TMP_Text modeTitle = CreateUIText(panel.gameObject, "ModeTitleText", "ENDLESS MODE", new Vector2(0, 900), new Vector2(600, 60), 36, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);
        TMP_Text score = CreateUIText(panel.gameObject, "ScoreText", "0", new Vector2(0, 825), new Vector2(600, 90), 56, Color.white, TextAlignmentOptions.Center);
        TMP_Text highScore = CreateUIText(panel.gameObject, "HighScoreText", "BEST: 0", new Vector2(-360, 840), new Vector2(300, 60), 28, new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Left);

        // Drops Container
        GameObject dropsObj = CreateUICard(panel.gameObject, "DropsContainer", new Vector2(360, 840), new Vector2(300, 60), new Color(0, 0, 0, 0));
        TMP_Text drops = CreateUIText(dropsObj, "DropsText", "DROPS: 30 / 30", Vector2.zero, new Vector2(300, 60), 28, new Color(1f, 0.4f, 0.4f), TextAlignmentOptions.Right);

        // Blocks Remaining Container
        GameObject blocksObj = CreateUICard(panel.gameObject, "BlocksContainer", new Vector2(360, 785), new Vector2(300, 50), new Color(0, 0, 0, 0));
        TMP_Text blocks = CreateUIText(blocksObj, "BlocksText", "BLOCKS: 0 / 0", Vector2.zero, new Vector2(300, 50), 26, new Color(1f, 0.72f, 0.25f), TextAlignmentOptions.Right);

        // Next Ball Preview
        GameObject nextBallObj = CreateUICard(panel.gameObject, "NextBallContainer", new Vector2(-420, 690), new Vector2(160, 120), new Color(0, 0, 0, 0));

        CreateUIText(nextBallObj, "NextLabel", "NEXT:", new Vector2(0, 35), new Vector2(160, 40), 24, Color.gray, TextAlignmentOptions.Center);
        Image nextBallImg = CreateUIImage(nextBallObj, "NextBallImage", new Vector2(0, -15), new Vector2(60, 60), new Color(0.95f, 0.2f, 0.25f));
        TMP_Text nextBallName = CreateUIText(nextBallObj, "NextBallNameText", "Cherry", new Vector2(0, -55), new Vector2(160, 30), 20, Color.white, TextAlignmentOptions.Center);

        // Pause Button
        Button pause = CreateUIButton(panel.gameObject, "PauseButton", "||", new Vector2(450, 690), new Vector2(90, 90), new Color(0.2f, 0.25f, 0.35f), Color.white);

        // Auto-wire SerializedObject
        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "scoreText", score);
        SafeSetProperty(so, "highScoreText", highScore);
        SafeSetProperty(so, "modeTitleText", modeTitle);
        SafeSetProperty(so, "dropsContainer", dropsObj);
        SafeSetProperty(so, "dropsText", drops);
        SafeSetProperty(so, "blocksContainer", blocksObj);
        SafeSetProperty(so, "blocksText", blocks);
        SafeSetProperty(so, "nextBallNameText", nextBallName);
        SafeSetProperty(so, "nextBallImage", nextBallImg);
        SafeSetProperty(so, "pauseButton", pause);
        so.ApplyModifiedProperties();
    }

    private static void BuildPausePanel(GameObject parent)
    {
        PausePanel panel = EnsurePanel<PausePanel>(parent);
        ClearChildren(panel.gameObject);

        // Dim & Window Box
        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.75f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(850, 950), new Color(0.15f, 0.17f, 0.24f, 1f));

        TMP_Text title = CreateUIText(window, "TitleText", "GAME PAUSED", new Vector2(0, 330), new Vector2(750, 100), 54, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);

        Button resume = CreateUIButton(window, "ResumeButton", "RESUME", new Vector2(0, 180), new Vector2(480, 95), new Color(0.18f, 0.8f, 0.44f), Color.white);
        Button restart = CreateUIButton(window, "RestartButton", "REPLAY", new Vector2(0, 60), new Vector2(480, 95), new Color(0.2f, 0.6f, 0.86f), Color.white);
        Button settings = CreateUIButton(window, "SettingsButton", "SETTINGS", new Vector2(0, -60), new Vector2(480, 95), new Color(0.61f, 0.35f, 0.71f), Color.white);
        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -180), new Vector2(480, 95), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "titleText", title);
        SafeSetProperty(so, "resumeButton", resume);
        SafeSetProperty(so, "restartButton", restart);
        SafeSetProperty(so, "settingsButton", settings);
        SafeSetProperty(so, "mainMenuButton", mainMenu);
        so.ApplyModifiedProperties();
    }

    private static void BuildGameOverPanel(GameObject parent)
    {
        GameOverPanel panel = EnsurePanel<GameOverPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dim & Window Box
        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.75f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(850, 850), new Color(0.15f, 0.17f, 0.24f, 1f));

        TMP_Text title = CreateUIText(window, "TitleText", "GAME OVER", new Vector2(0, 280), new Vector2(750, 100), 56, new Color(0.91f, 0.3f, 0.24f), TextAlignmentOptions.Center);
        TMP_Text finalScore = CreateUIText(window, "FinalScoreText", "SCORE: 0", new Vector2(0, 140), new Vector2(750, 80), 42, Color.white, TextAlignmentOptions.Center);
        TMP_Text highScore = CreateUIText(window, "HighScoreText", "BEST: 0", new Vector2(0, 40), new Vector2(750, 60), 32, new Color(0.95f, 0.85f, 0.2f), TextAlignmentOptions.Center);

        Button restart = CreateUIButton(window, "RestartButton", "REPLAY", new Vector2(0, -110), new Vector2(450, 100), new Color(0.18f, 0.8f, 0.44f), Color.white);
        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -240), new Vector2(450, 100), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "titleText", title);
        SafeSetProperty(so, "finalScoreText", finalScore);
        SafeSetProperty(so, "highScoreText", highScore);
        SafeSetProperty(so, "restartButton", restart);
        SafeSetProperty(so, "mainMenuButton", mainMenu);
        so.ApplyModifiedProperties();
    }

    private static void BuildLevelWinPanel(GameObject parent)
    {
        LevelWinPanel panel = EnsurePanel<LevelWinPanel>(parent);
        ClearChildren(panel.gameObject);

        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.75f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(850, 920), new Color(0.12f, 0.18f, 0.24f, 1f));

        TMP_Text title = CreateUIText(window, "TitleText", "LEVEL PASSED!", new Vector2(0, 330), new Vector2(750, 100), 54, new Color(0.95f, 0.85f, 0.2f), TextAlignmentOptions.Center);
        TMP_Text starsText = CreateUIText(window, "StarsText", "⭐⭐⭐", new Vector2(0, 240), new Vector2(750, 60), 44, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);
        TMP_Text score = CreateUIText(window, "ScoreText", "FINAL SCORE: 0", new Vector2(0, 160), new Vector2(750, 60), 36, Color.white, TextAlignmentOptions.Center);
        TMP_Text dropsUsed = CreateUIText(window, "DropsUsedText", "DROPS USED: 0", new Vector2(0, 80), new Vector2(750, 50), 28, new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Center);
        TMP_Text coinBonus = CreateUIText(window, "CoinBonusText", "", new Vector2(0, 22), new Vector2(760, 56), 23, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center);

        Button nextLevel = CreateUIButton(window, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -60), new Vector2(450, 95), new Color(0.18f, 0.8f, 0.44f), Color.white);
        Button replay = CreateUIButton(window, "ReplayButton", "REPLAY", new Vector2(0, -170), new Vector2(450, 95), new Color(0.2f, 0.6f, 0.86f), Color.white);
        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -280), new Vector2(450, 95), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "titleText", title);
        SafeSetProperty(so, "starsText", starsText);
        SafeSetProperty(so, "scoreText", score);
        SafeSetProperty(so, "dropsUsedText", dropsUsed);
        SafeSetProperty(so, "coinBonusText", coinBonus);
        SafeSetProperty(so, "nextLevelButton", nextLevel);
        SafeSetProperty(so, "replayButton", replay);
        SafeSetProperty(so, "mainMenuButton", mainMenu);
        so.ApplyModifiedProperties();
    }

    private static void BuildBallKitShopPanel(GameObject parent)
    {
        BallKitShopPanel panel = EnsurePanel<BallKitShopPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // Header Section Card
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 680), new Vector2(940, 180), new Color(0.16f, 0.12f, 0.22f, 0.95f));

        CreateUIText(headerCard, "TitleText", "BALL KITS SHOP", new Vector2(0, 30), new Vector2(900, 70), 52, new Color(0.75f, 0.45f, 0.9f), TextAlignmentOptions.Center);
        TMP_Text currentKit = CreateUIText(headerCard, "CurrentKitNameText", "ACTIVE: CLASSIC FRUITS", new Vector2(-150, -35), new Vector2(600, 45), 24, Color.white, TextAlignmentOptions.Left);
        TMP_Text coinBalanceText = CreateUIText(headerCard, "CoinBalanceText", "🪙 0 COINS", new Vector2(250, -35), new Vector2(350, 45), 24, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Right);

        // Kit Card 1: Classic Fruits (Y = 340)
        GameObject kit1Obj = CreateKitCard(panel.gameObject, "ClassicCard", "CLASSIC FRUITS", "Fruit Tiers (Cherry, Plum, Orange...)", new Vector2(0, 340), new Color[] { new Color(0.95f, 0.2f, 0.25f), new Color(0.65f, 0.25f, 0.85f), new Color(1.0f, 0.6f, 0.1f) });
        Button inspectClassic = CreateUIButton(kit1Obj, "InspectClassicButton", "🔍 VIEW TIERS", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button classicBtn = CreateUIButton(kit1Obj, "ClassicKitButton", "EQUIP", new Vector2(365, 0), new Vector2(150, 75), new Color(0.91f, 0.3f, 0.24f), Color.white);

        // Kit Card 2: Ocean World (Y = 100)
        GameObject kit2Obj = CreateKitCard(panel.gameObject, "OceanCard", "OCEAN WORLD", "Sea Tiers (Shell, Starfish, Jellyfish...)", new Vector2(0, 100), new Color[] { new Color(1.0f, 0.85f, 0.75f), new Color(1.0f, 0.4f, 0.4f), new Color(0.9f, 0.4f, 0.9f) });
        Button inspectOcean = CreateUIButton(kit2Obj, "InspectOceanButton", "🔍 VIEW TIERS", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button oceanBtn = CreateUIButton(kit2Obj, "OceanKitButton", "EQUIP", new Vector2(365, 0), new Vector2(150, 75), new Color(0.2f, 0.6f, 0.86f), Color.white);

        // Kit Card 3: Sky High (Y = -140)
        GameObject kit3Obj = CreateKitCard(panel.gameObject, "SkyCard", "SKY HIGH", "Sky Tiers (Cloud, Bird, Kite...)", new Vector2(0, -140), new Color[] { new Color(0.9f, 0.95f, 1.0f), new Color(0.3f, 0.75f, 1.0f), new Color(1.0f, 0.5f, 0.2f) });
        Button inspectSky = CreateUIButton(kit3Obj, "InspectSkyButton", "🔍 VIEW TIERS", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button skyBtn = CreateUIButton(kit3Obj, "SkyKitButton", "EQUIP", new Vector2(365, 0), new Vector2(150, 75), new Color(0.95f, 0.6f, 0.1f), Color.white);

        // Bottom Navigation Bar
        Button backBtn = CreateUIButton(panel.gameObject, "BackButton", "⬅ BACK TO MENU", new Vector2(0, -550), new Vector2(500, 95), new Color(0.25f, 0.3f, 0.4f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "currentKitNameText", currentKit);
        SafeSetProperty(so, "coinBalanceText", coinBalanceText);
        SafeSetProperty(so, "classicKitButton", classicBtn);
        SafeSetProperty(so, "oceanKitButton", oceanBtn);
        SafeSetProperty(so, "skyKitButton", skyBtn);
        SafeSetProperty(so, "inspectClassicButton", inspectClassic);
        SafeSetProperty(so, "inspectOceanButton", inspectOcean);
        SafeSetProperty(so, "inspectSkyButton", inspectSky);
        SafeSetProperty(so, "backButton", backBtn);
        so.ApplyModifiedProperties();
    }

    private static void BuildBallKitPreviewPanel(GameObject parent)
    {
        BallKitPreviewPanel panel = EnsurePanel<BallKitPreviewPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dim Background & Modal Window
        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.85f));

        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(880, 1150), new Color(0.14f, 0.16f, 0.22f, 1f));

        TMP_Text title = CreateUIText(window, "KitTitleText", "OCEAN WORLD", new Vector2(0, 480), new Vector2(800, 75), 48, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);
        TMP_Text subtitle = CreateUIText(window, "KitSubtitleText", "6 BALL TIERS • DROPPABLE TIERS: 1 - 3", new Vector2(0, 425), new Vector2(800, 45), 22, new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Center);

        // Container for 6 tier cards
        GameObject containerObj = CreateUICard(window, "TierListContainer", new Vector2(0, 20), new Vector2(800, 740), new Color(0.1f, 0.12f, 0.17f, 0.95f));

        // Action Buttons
        Button equipBtn = CreateUIButton(window, "EquipButton", "EQUIP THIS KIT", new Vector2(-200, -480), new Vector2(360, 85), new Color(0.18f, 0.8f, 0.44f), Color.white);
        TMP_Text equipTxt = equipBtn.GetComponentInChildren<TMP_Text>();

        Button closeBtn = CreateUIButton(window, "CloseButton", "✖ CLOSE", new Vector2(200, -480), new Vector2(360, 85), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "kitTitleText", title);
        SafeSetProperty(so, "kitSubtitleText", subtitle);
        SafeSetProperty(so, "tierListContainer", containerObj.transform);
        SafeSetProperty(so, "equipButton", equipBtn);
        SafeSetProperty(so, "equipButtonText", equipTxt);
        SafeSetProperty(so, "closeButton", closeBtn);
        so.ApplyModifiedProperties();
    }

    private static GameObject CreateKitCard(GameObject parent, string cardName, string title, string subtitle, Vector2 pos, Color[] swatchColors)
    {
        GameObject cardObj = CreateUICard(parent, cardName, pos, new Vector2(940, 210), new Color(0.15f, 0.17f, 0.24f, 0.95f));

        // Swatch circles on left
        for (int i = 0; i < swatchColors.Length; i++)
        {
            CreateUIImage(cardObj, $"Dot_{i}", new Vector2(-380 + (i * 45), 0), new Vector2(36, 36), swatchColors[i]);
        }

        CreateUIText(cardObj, "CardTitle", title, new Vector2(-60, 25), new Vector2(400, 45), 34, Color.white, TextAlignmentOptions.Left);
        CreateUIText(cardObj, "CardSub", subtitle, new Vector2(-60, -25), new Vector2(400, 40), 20, Color.gray, TextAlignmentOptions.Left);

        return cardObj;
    }

    private static void BuildThemeShopPanel(GameObject parent)
    {
        ThemeShopPanel panel = EnsurePanel<ThemeShopPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // Header Section Card
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 720), new Vector2(940, 160), new Color(0.22f, 0.16f, 0.12f, 0.95f));

        CreateUIText(headerCard, "TitleText", "THEME SHOP", new Vector2(0, 25), new Vector2(900, 70), 50, new Color(0.95f, 0.6f, 0.2f), TextAlignmentOptions.Center);
        TMP_Text currentTheme = CreateUIText(headerCard, "CurrentThemeNameText", "ACTIVE: DARK COSMIC", new Vector2(-150, -30), new Vector2(600, 45), 24, Color.white, TextAlignmentOptions.Left);
        TMP_Text coinBalanceText = CreateUIText(headerCard, "CoinBalanceText", "🪙 0 COINS", new Vector2(250, -30), new Vector2(350, 45), 24, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Right);

        // Theme Cards
        GameObject t1 = CreateThemeCard(panel.gameObject, "DarkCard", "DARK COSMIC", "Cosmic Space Atmosphere", new Vector2(0, 430), new Color[] { new Color(0.12f, 0.13f, 0.18f), new Color(0.2f, 0.65f, 1.0f), new Color(1.0f, 0.3f, 0.3f) });
        Button inspectDark = CreateUIButton(t1, "InspectDarkButton", "🔍 PREVIEW", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button darkBtn = CreateUIButton(t1, "DarkThemeButton", "APPLY", new Vector2(365, 0), new Vector2(150, 75), new Color(0.2f, 0.25f, 0.35f), Color.white);

        GameObject t2 = CreateThemeCard(panel.gameObject, "OceanCard", "OCEAN DEPTH", "Underwater Aquatic Feel", new Vector2(0, 230), new Color[] { new Color(0.04f, 0.11f, 0.22f), new Color(0.12f, 0.82f, 0.85f), new Color(1.0f, 0.5f, 0.2f) });
        Button inspectOcean = CreateUIButton(t2, "InspectOceanButton", "🔍 PREVIEW", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button oceanBtn = CreateUIButton(t2, "OceanThemeButton", "APPLY", new Vector2(365, 0), new Vector2(150, 75), new Color(0.1f, 0.5f, 0.6f), Color.white);

        GameObject t3 = CreateThemeCard(panel.gameObject, "SkyCard", "SKY BREEZE", "Atmospheric Blue Sky", new Vector2(0, 30), new Color[] { new Color(0.16f, 0.29f, 0.49f), new Color(0.48f, 0.91f, 1.0f), new Color(1.0f, 0.4f, 0.7f) });
        Button inspectSky = CreateUIButton(t3, "InspectSkyButton", "🔍 PREVIEW", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button skyBtn = CreateUIButton(t3, "SkyThemeButton", "APPLY", new Vector2(365, 0), new Vector2(150, 75), new Color(0.25f, 0.6f, 0.85f), Color.white);

        GameObject t4 = CreateThemeCard(panel.gameObject, "SunsetCard", "SUNSET GLOW", "Warm Sunset Horizon", new Vector2(0, -170), new Color[] { new Color(0.16f, 0.08f, 0.21f), new Color(1.0f, 0.49f, 0.2f), new Color(1.0f, 0.82f, 0.0f) });
        Button inspectSunset = CreateUIButton(t4, "InspectSunsetButton", "🔍 PREVIEW", new Vector2(170, 0), new Vector2(230, 75), new Color(0.2f, 0.25f, 0.38f), Color.white);
        Button sunsetBtn = CreateUIButton(t4, "SunsetThemeButton", "APPLY", new Vector2(365, 0), new Vector2(150, 75), new Color(0.55f, 0.25f, 0.6f), Color.white);

        // Bottom Navigation Bar
        Button backBtn = CreateUIButton(panel.gameObject, "BackButton", "⬅ BACK TO MENU", new Vector2(0, -560), new Vector2(500, 95), new Color(0.25f, 0.3f, 0.4f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "currentThemeNameText", currentTheme);
        SafeSetProperty(so, "coinBalanceText", coinBalanceText);
        SafeSetProperty(so, "darkThemeButton", darkBtn);
        SafeSetProperty(so, "oceanThemeButton", oceanBtn);
        SafeSetProperty(so, "skyThemeButton", skyBtn);
        SafeSetProperty(so, "sunsetThemeButton", sunsetBtn);
        SafeSetProperty(so, "inspectDarkButton", inspectDark);
        SafeSetProperty(so, "inspectOceanButton", inspectOcean);
        SafeSetProperty(so, "inspectSkyButton", inspectSky);
        SafeSetProperty(so, "inspectSunsetButton", inspectSunset);
        SafeSetProperty(so, "backButton", backBtn);
        so.ApplyModifiedProperties();
    }

    private static void BuildThemePreviewPanel(GameObject parent)
    {
        ThemePreviewPanel panel = EnsurePanel<ThemePreviewPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dim Background & Modal Window
        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.85f));

        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(900, 1200), new Color(0.14f, 0.16f, 0.22f, 1f));

        TMP_Text title = CreateUIText(window, "ThemeTitleText", "DARK COSMIC", new Vector2(0, 500), new Vector2(800, 75), 48, new Color(0.95f, 0.6f, 0.2f), TextAlignmentOptions.Center);
        TMP_Text subtitle = CreateUIText(window, "ThemeSubtitleText", "ENVIRONMENT VISUAL PALETTE PREVIEW", new Vector2(0, 445), new Vector2(800, 45), 22, Color.gray, TextAlignmentOptions.Center);

        // Mock Game Container (Visual Field Preview)
        GameObject fieldObj = CreateUICard(window, "PreviewBackgroundBox", new Vector2(0, 140), new Vector2(760, 520), new Color(0.12f, 0.13f, 0.18f, 1f));

        // Mock Container Walls
        Image wallLeft = CreateUIImage(fieldObj, "PreviewWallLeft", new Vector2(-360, 0), new Vector2(30, 520), new Color(0.2f, 0.6f, 1.0f, 0.85f));
        Image wallRight = CreateUIImage(fieldObj, "PreviewWallRight", new Vector2(360, 0), new Vector2(30, 520), new Color(0.2f, 0.6f, 1.0f, 0.85f));
        Image wallBottom = CreateUIImage(fieldObj, "PreviewWallBottom", new Vector2(0, -245), new Vector2(750, 30), new Color(0.2f, 0.6f, 1.0f, 0.85f));

        // Mock Danger Line
        Image dangerLine = CreateUIImage(fieldObj, "PreviewDangerLine", new Vector2(0, 170), new Vector2(700, 8), new Color(1.0f, 0.3f, 0.3f, 0.9f));

        // Text Badges showing details of what changes
        TMP_Text bgTxt = CreateUIText(window, "BgColorText", "• Background: #1F212E (Camera Viewport)", new Vector2(0, -170), new Vector2(780, 45), 24, Color.white, TextAlignmentOptions.Left);
        TMP_Text wallTxt = CreateUIText(window, "WallColorText", "• Container Walls: #33A6FF (Side/Bottom Boundaries)", new Vector2(0, -225), new Vector2(780, 45), 24, Color.white, TextAlignmentOptions.Left);
        TMP_Text dangerTxt = CreateUIText(window, "DangerColorText", "• Danger Line: #FF4D4D (Overflow Warning)", new Vector2(0, -280), new Vector2(780, 45), 24, Color.white, TextAlignmentOptions.Left);

        // Action Buttons
        Button applyBtn = CreateUIButton(window, "ApplyButton", "APPLY THIS THEME", new Vector2(-200, -500), new Vector2(360, 85), new Color(0.18f, 0.8f, 0.44f), Color.white);
        TMP_Text applyTxt = applyBtn.GetComponentInChildren<TMP_Text>();

        Button closeBtn = CreateUIButton(window, "CloseButton", "✖ CLOSE", new Vector2(200, -500), new Vector2(360, 85), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "themeTitleText", title);
        SafeSetProperty(so, "themeSubtitleText", subtitle);
        SafeSetProperty(so, "previewBackgroundBox", fieldObj.GetComponent<Image>());
        SafeSetProperty(so, "previewWallLeft", wallLeft);
        SafeSetProperty(so, "previewWallRight", wallRight);
        SafeSetProperty(so, "previewWallBottom", wallBottom);
        SafeSetProperty(so, "previewDangerLine", dangerLine);
        SafeSetProperty(so, "bgColorText", bgTxt);
        SafeSetProperty(so, "wallColorText", wallTxt);
        SafeSetProperty(so, "dangerColorText", dangerTxt);
        SafeSetProperty(so, "applyButton", applyBtn);
        SafeSetProperty(so, "applyButtonText", applyTxt);
        SafeSetProperty(so, "closeButton", closeBtn);
        so.ApplyModifiedProperties();
    }

    private static GameObject CreateThemeCard(GameObject parent, string cardName, string title, string subtitle, Vector2 pos, Color[] swatchColors)
    {
        GameObject cardObj = CreateUICard(parent, cardName, pos, new Vector2(940, 170), new Color(0.15f, 0.17f, 0.24f, 0.95f));

        // Swatch blocks on left
        for (int i = 0; i < swatchColors.Length; i++)
        {
            CreateUIImage(cardObj, $"Swatch_{i}", new Vector2(-380 + (i * 45), 0), new Vector2(36, 36), swatchColors[i]);
        }

        CreateUIText(cardObj, "CardTitle", title, new Vector2(-60, 20), new Vector2(400, 40), 32, Color.white, TextAlignmentOptions.Left);
        CreateUIText(cardObj, "CardSub", subtitle, new Vector2(-60, -20), new Vector2(400, 35), 20, Color.gray, TextAlignmentOptions.Left);

        return cardObj;
    }

    private static void BuildSettingsPanel(GameObject parent)
    {
        SettingsPanel panel = EnsurePanel<SettingsPanel>(parent);
        ClearChildren(panel.gameObject);

        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.75f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(750, 750), new Color(0.18f, 0.2f, 0.28f, 1f));

        CreateUIText(window, "TitleText", "SETTINGS", new Vector2(0, 260), new Vector2(650, 90), 48, Color.white, TextAlignmentOptions.Center);
        Button backBtn = CreateUIButton(window, "BackButton", "CLOSE", new Vector2(0, -220), new Vector2(380, 90), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "backButton", backBtn);
        so.ApplyModifiedProperties();
    }

    private static void BuildGenericPopupPanel(GameObject parent)
    {
        GenericPopupPanel panel = EnsurePanel<GenericPopupPanel>(parent);
        ClearChildren(panel.gameObject);

        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.75f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(750, 520), new Color(0.2f, 0.23f, 0.32f, 1f));

        TMP_Text title = CreateUIText(window, "TitleText", "ALERT", new Vector2(0, 170), new Vector2(650, 80), 40, new Color(0.95f, 0.85f, 0.2f), TextAlignmentOptions.Center);
        TMP_Text message = CreateUIText(window, "MessageText", "Popup notification message", new Vector2(0, 30), new Vector2(650, 140), 28, Color.white, TextAlignmentOptions.Center);

        Button confirmBtn = CreateUIButton(window, "ConfirmButton", "OK", new Vector2(-150, -150), new Vector2(240, 80), new Color(0.18f, 0.8f, 0.44f), Color.white);
        TMP_Text confirmTxt = confirmBtn.GetComponentInChildren<TMP_Text>();

        Button cancelBtn = CreateUIButton(window, "CancelButton", "CANCEL", new Vector2(150, -150), new Vector2(240, 80), new Color(0.91f, 0.3f, 0.24f), Color.white);
        TMP_Text cancelTxt = cancelBtn.GetComponentInChildren<TMP_Text>();

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "titleText", title);
        SafeSetProperty(so, "messageText", message);
        SafeSetProperty(so, "confirmButton", confirmBtn);
        SafeSetProperty(so, "confirmButtonText", confirmTxt);
        SafeSetProperty(so, "cancelButton", cancelBtn);
        SafeSetProperty(so, "cancelButtonText", cancelTxt);
        so.ApplyModifiedProperties();
    }

    #endregion

    private static void SafeSetProperty(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning($"[BallMergeAssetCreator] Property '{propertyName}' not found on {so.targetObject.GetType().Name}");
        }
    }

    private static void SafeSetListProperty(SerializedObject so, string propertyName, List<Transform> items)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null && prop.isArray)
        {
            prop.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }
    }

    private static GameObject CreateDecorBall(GameObject parent, string name, Vector2 pos, float diameter, Color color)
    {
        GameObject ballObj = new GameObject(name);
        ballObj.transform.SetParent(parent.transform, false);

        RectTransform rect = ballObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(diameter, diameter);

        Image img = ballObj.AddComponent<Image>();
        img.sprite = GetCircleSprite();
        img.color = color;

        // Cute white specular highlight reflection dot (top-left)
        GameObject shineObj = new GameObject("Shine");
        shineObj.transform.SetParent(ballObj.transform, false);
        RectTransform shineRect = shineObj.AddComponent<RectTransform>();
        float shineSize = diameter * 0.28f;
        shineRect.anchoredPosition = new Vector2(-diameter * 0.18f, diameter * 0.18f);
        shineRect.sizeDelta = new Vector2(shineSize, shineSize);

        Image shineImg = shineObj.AddComponent<Image>();
        shineImg.sprite = GetCircleSprite();
        shineImg.color = new Color(1f, 1f, 1f, 0.65f);

        return ballObj;
    }

    #region UGUI Helper Methods

    private static GameObject CreateUICard(GameObject parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        if (sprite == null)
        {
            if (name == "WindowBox")
                sprite = s_frameBlue;
            else if (name.Contains("Header") || name.Contains("Ribbon"))
                sprite = s_bannerBlue != null ? s_bannerBlue : s_frameBlue;
            else if (name.Contains("Container") || name.Contains("Badge") || name.Contains("Endless") || name.Contains("Grid") || name.Contains("Wheel"))
                sprite = s_frameCyan;
            else if (name.Contains("Daily") || name.Contains("Kits") || name.Contains("Themes") || name.Contains("Streak"))
                sprite = s_framePurple;
            else if (name.Contains("Card"))
                sprite = s_frameBlue;
        }

        Image img = obj.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = color;
        }
        return obj;
    }

    private static T EnsurePanel<T>(GameObject parent) where T : UIBasePanel
    {
        string panelName = typeof(T).Name;
        Transform child = parent.transform.Find(panelName);
        GameObject panelObj;

        if (child == null)
        {
            panelObj = new GameObject(panelName);
            panelObj.transform.SetParent(parent.transform, false);
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            panelObj = child.gameObject;
        }

        T component = panelObj.GetComponent<T>();
        if (component == null) component = panelObj.AddComponent<T>();

        return component;
    }

    private static Image CreateUIImage(GameObject parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text CreateUIText(GameObject parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        return text;
    }

    private static Button CreateUIButton(GameObject parent, string name, string labelText, Vector2 pos, Vector2 size, Color bgColor, Color textColor, Sprite sprite = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        if (sprite == null)
        {
            if (name.Contains("Endless") || name.Contains("Resume") || name.Contains("Next") || name.Contains("Confirm") || name.Contains("Spin") || name.Contains("Apply") || name.Contains("Equip") || name.Contains("Play1") || name.Contains("PlayLevel"))
                sprite = s_btnGreen;
            else if (name.Contains("Daily") || name.Contains("Map") || name.Contains("Restart") || name.Contains("Replay") || name.Contains("Streak"))
                sprite = s_btnYellow;
            else if (name.Contains("Quit") || name.Contains("Back") || name.Contains("Close") || name.Contains("Cancel") || name.Contains("MainMenu"))
                sprite = s_btnRed;
            else
                sprite = s_btnBlue;
        }

        Image img = obj.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = bgColor;
        }

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        // Label child text with bold style
        TMP_Text txt = CreateUIText(obj, "Text", labelText, Vector2.zero, size, size.y * 0.38f, textColor, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;

        return btn;
    }

    private static void ClearChildren(GameObject parent)
    {
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(parent.transform.GetChild(i).gameObject);
        }
    }

    #endregion
}
#endif
