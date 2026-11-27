#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
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
        GameObject uiObj = FindOrCreateUICanvasObject();
        BuildMainMenuPanel(uiObj);

        UIManager uim = uiObj.GetComponent<UIManager>();
        if (uim != null)
        {
            SetPanelActiveStates(uiObj, typeof(MainMenuPanel));
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BallMergeAssetCreator] Hyper-Casual Main Menu Panel rebuilt successfully!");
    }

    [MenuItem("Tools/Ball Merge/7. Apply Pro UI Polish to GameTest & Prefabs")]
    public static void PolishGameTestUIAndPrefabsMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != BlockLevelSetupUtility.GAME_TEST_SCENE_PATH)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(BlockLevelSetupUtility.GAME_TEST_SCENE_PATH);
            }
            else
            {
                return;
            }
        }

        ApplyProUIPolishToActiveSceneAndPrefabs(saveScene: true);
    }

    public static void ApplyProUIPolishToActiveSceneAndPrefabs(bool saveScene)
    {
        SetupUICanvas(applyEditModeVisibility: true);
        SyncUIPanelPrefabs();

        Scene active = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(active);
        if (saveScene)
        {
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("[BallMergeAssetCreator] Pro UI Polish (LilitaOne SDF, Icons, Mascots, Map Art, Settings Sliders) applied & prefabs synced!");
    }

    private static void SyncUIPanelPrefabs()
    {
        GameObject uiObj = FindOrCreateUICanvasObject();
        if (uiObj == null) return;

        const string prefabFolder = "Assets/Game/Prefab/ui Panels";
        EnsureFolderExists(prefabFolder);

        UIBasePanel[] panels = uiObj.GetComponentsInChildren<UIBasePanel>(true);
        foreach (UIBasePanel panel in panels)
        {
            if (panel == null || panel.transform.parent != uiObj.transform) continue;

            GameObject go = panel.gameObject;
            bool wasActive = go.activeSelf;
            go.SetActive(true);
            string prefabPath = $"{prefabFolder}/{go.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            go.SetActive(wasActive);
        }
        AssetDatabase.SaveAssets();
    }

    private const string UI_RESOURCES_FOLDER = "Assets/Resources/BallMerge/UI";
    public const string LILITA_FONT_ASSET_PATH = "Assets/Resources/BallMerge/UI/LilitaOne SDF.asset";
    private const string LILITA_TTF_PATH = "Assets/Game/LilitaOne-Regular.ttf";

    private static TMP_FontAsset s_lilitaFont;
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

    // Pack 1 & Spritesheet UI Icons + Game Artwork
    private static Sprite s_iconShop;
    private static Sprite s_iconHome;
    private static Sprite s_iconCalendar;
    private static Sprite s_iconGear;
    private static Sprite s_iconTick;
    private static Sprite s_iconCoin;
    private static Sprite s_iconChest;
    private static Sprite s_iconWheel;
    private static Sprite s_iconBack;
    private static Sprite s_iconSpeaker;
    private static Sprite s_iconMusic;
    private static Sprite s_iconStar;
    private static Sprite s_iconClose;
    private static Sprite s_iconLock;
    private static Sprite s_mapBackdrop;
    private static Sprite s_slimeBody;
    private static Sprite s_slimeFace;
    private static Sprite s_slimeEyes;

    public static TMP_FontAsset EnsureLilitaOneFontAsset()
    {
        if (s_lilitaFont != null && s_lilitaFont.material != null) return s_lilitaFont;

        EnsureFolderExists(UI_RESOURCES_FOLDER);
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LILITA_FONT_ASSET_PATH);
        if (existing != null && existing.material != null && existing.atlasTexture != null)
        {
            s_lilitaFont = existing;
            return s_lilitaFont;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(LILITA_TTF_PATH);
        if (sourceFont == null)
        {
            Debug.LogWarning($"[BallMergeAssetCreator] Font not found at {LILITA_TTF_PATH}; falling back to TMP default font.");
            return TMP_Settings.defaultFontAsset;
        }

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(LILITA_FONT_ASSET_PATH);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);

        if (fontAsset == null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        fontAsset.name = "LilitaOne SDF";
        AssetDatabase.CreateAsset(fontAsset, LILITA_FONT_ASSET_PATH);

        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "LilitaOne SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "LilitaOne SDF Material";
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                fontAsset.material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
            }
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                fontAsset.material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.07f, 0.14f, 0.92f));
            }
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_FaceDilate))
            {
                fontAsset.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.03f);
            }
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        s_lilitaFont = fontAsset;
        return s_lilitaFont;
    }

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

    private static Sprite LoadFirstSprite(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static Dictionary<string, Sprite> LoadSpriteMap(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .GroupBy(s => s.name)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static void LoadUISprites()
    {
        s_lilitaFont = EnsureLilitaOneFontAsset();

        var pack2 = LoadSpriteMap("Assets/Game/Sprites/Full sprite sheet/UI-pack_Sprite_2.png");
        pack2.TryGetValue("UI-pack_Sprite_2_16", out s_btnGreen);
        pack2.TryGetValue("UI-pack_Sprite_2_9", out s_btnYellow);
        pack2.TryGetValue("UI-pack_Sprite_2_10", out s_btnBlue);
        pack2.TryGetValue("UI-pack_Sprite_2_17", out s_btnRed);
        pack2.TryGetValue("UI-pack_Sprite_2_0", out s_frameBlue);
        pack2.TryGetValue("UI-pack_Sprite_2_4", out s_frameCyan);
        pack2.TryGetValue("UI-pack_Sprite_2_2", out s_framePurple);
        pack2.TryGetValue("UI-pack_Sprite_2_5", out s_bannerBlue);
        pack2.TryGetValue("UI-pack_Sprite_2_1", out s_bannerYellow);

        var pack1 = LoadSpriteMap("Assets/Game/Sprites/Full sprite sheet/UI-pack_Sprite_1.png");
        pack1.TryGetValue("UI-pack_Sprite_1_0", out s_iconShop);
        pack1.TryGetValue("UI-pack_Sprite_1_2", out s_iconHome);
        pack1.TryGetValue("UI-pack_Sprite_1_3", out s_iconCalendar);
        pack1.TryGetValue("UI-pack_Sprite_1_4", out s_iconGear);
        pack1.TryGetValue("UI-pack_Sprite_1_5", out s_iconTick);
        pack1.TryGetValue("UI-pack_Sprite_1_16", out s_iconCoin);
        pack1.TryGetValue("UI-pack_Sprite_1_27", out s_iconChest);
        pack1.TryGetValue("UI-pack_Sprite_1_68", out s_iconWheel);
        pack1.TryGetValue("UI-pack_Sprite_1_80", out s_iconBack);

        var mainSheet = LoadSpriteMap("Assets/Game/Sprites/Main Sprite/Spritesheet.png");
        mainSheet.TryGetValue("Spritesheet_5", out s_iconSpeaker);
        mainSheet.TryGetValue("Spritesheet_10", out s_iconMusic);
        mainSheet.TryGetValue("Spritesheet_15", out s_iconStar);
        mainSheet.TryGetValue("Spritesheet_17", out s_iconClose);
        mainSheet.TryGetValue("Spritesheet_49", out s_iconLock);

        s_mapBackdrop = LoadFirstSprite("Assets/Game/Sprites/Main Sprite/Gemini_Generated_Image_edwvwoedwvwoedwv.png");
        s_slimeBody = LoadFirstSprite("Assets/Game/Sprites/Slime/ChatGPT Image Mar 26, 2026, 11_01_24 AM-Photoroom 2.png");
        s_slimeFace = LoadFirstSprite("Assets/Game/Sprites/Slime/ChatGPT Image Mar 30, 2026, 06_54_10 PM-Photoroom.png");
        s_slimeEyes = LoadFirstSprite("Assets/Game/Sprites/Slime/ChatGPT Image Mar 27, 2026, 12_57_54 PM.png");
    }

    private static GameObject FindOrCreateUICanvasObject()
    {
        GameObject uiObj = GameObject.Find("[UI]");
        if (uiObj == null) uiObj = GameObject.Find("UIManager_Canvas");
        if (uiObj == null)
        {
            UIManager existingUim = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (existingUim != null) uiObj = existingUim.gameObject;
        }
        if (uiObj == null) uiObj = new GameObject("[UI]");
        return uiObj;
    }

    private static void SetPanelActiveStates(GameObject uiObj, System.Type activePanelType)
    {
        UIBasePanel[] panels = uiObj.GetComponentsInChildren<UIBasePanel>(true);
        foreach (UIBasePanel panel in panels)
        {
            if (panel != null && panel.transform.parent == uiObj.transform)
            {
                panel.gameObject.SetActive(panel.GetType() == activePanelType);
            }
        }
    }

    private static void SetupUICanvas(bool applyEditModeVisibility = true)
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
        GameObject uiObj = FindOrCreateUICanvasObject();

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
        SerializedObject uimSo = new SerializedObject(uim);
        SafeSetProperty(uimSo, "scaler", scaler);
        uimSo.ApplyModifiedPropertiesWithoutUndo();

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

        if (applyEditModeVisibility)
        {
            SetPanelActiveStates(uiObj, typeof(SplashScreenPanel));
        }
    }

    private static void BuildSplashScreenPanel(GameObject parent)
    {
        SplashScreenPanel panel = EnsurePanel<SplashScreenPanel>(parent);
        ClearChildren(panel.gameObject);

        // Full Screen Deep Cosmic Background + Ambient Glow
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.06f, 0.09f, 0.16f, 1f));
        Image glow = CreateUIImage(panel.gameObject, "AmbientGlow", new Vector2(0, 220), new Vector2(820, 820), new Color(0.14f, 0.42f, 0.78f, 0.22f));
        glow.sprite = GetCircleSprite();
        glow.raycastTarget = false;

        // Cute Slime Mascot centerpiece
        GameObject mascot = CreateDecorBall(panel.gameObject, "SplashMascot", new Vector2(0, 40), 230f, new Color(0.22f, 0.86f, 0.56f), useSlimeMascot: true);
        mascot.transform.SetAsLastSibling();

        // Game Branding Title & Subtitle Pill
        TMP_Text title = CreateUIText(panel.gameObject, "TitleText", "BALL MERGE 2D", new Vector2(0, 340), new Vector2(920, 140), 82, new Color(1.0f, 0.86f, 0.2f), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;

        GameObject subPill = CreateUICard(panel.gameObject, "SubtitlePill", new Vector2(0, 235), new Vector2(440, 56), new Color(0.12f, 0.25f, 0.45f, 0.95f), s_frameCyan);
        TMP_Text subTitle = CreateUIText(subPill, "SubtitleText", "2D PHYSICS PUZZLE", Vector2.zero, new Vector2(420, 50), 24, new Color(0.38f, 0.92f, 1f), TextAlignmentOptions.Center);
        subTitle.fontStyle = FontStyles.Bold;

        // Loading Slider Container
        GameObject sliderObj = new GameObject("LoadingSlider");
        sliderObj.transform.SetParent(panel.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -390);
        sliderRect.sizeDelta = new Vector2(760, 48);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;

        // Slider Background Track
        Image sliderBg = CreateUIImage(sliderObj, "Background", Vector2.zero, new Vector2(760, 48), new Color(0.14f, 0.18f, 0.28f, 1f));
        if (s_frameBlue != null)
        {
            sliderBg.sprite = s_frameBlue;
            sliderBg.type = Image.Type.Sliced;
            sliderBg.color = Color.white;
        }

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        Image fillImg = CreateUIImage(fillArea, "Fill", Vector2.zero, Vector2.zero, new Color(0.18f, 0.8f, 0.44f));
        if (s_btnGreen != null)
        {
            fillImg.sprite = s_btnGreen;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = Color.white;
        }
        slider.fillRect = fillImg.rectTransform;
        slider.targetGraphic = sliderBg;

        TMP_Text progressTxt = CreateUIText(panel.gameObject, "ProgressText", "LOADING... 0%", new Vector2(0, -470), new Vector2(650, 54), 28, Color.white, TextAlignmentOptions.Center);

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
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.06f, 0.08f, 0.13f, 1f));

        // Subtle ambient aura glow behind the game logo
        Image ambientGlow = CreateUIImage(panel.gameObject, "AmbientGlow", new Vector2(0, 520), new Vector2(860, 860), new Color(0.12f, 0.38f, 0.76f, 0.16f));
        ambientGlow.sprite = GetCircleSprite();
        ambientGlow.raycastTarget = false;

        // -------------------------------------------------------------
        // 2. TOP STATUS BAR (Y = 820)
        // -------------------------------------------------------------
        GameObject topBar = new GameObject("TopStatusBar");
        topBar.transform.SetParent(panel.transform, false);
        RectTransform topBarRect = topBar.AddComponent<RectTransform>();
        topBarRect.anchoredPosition = new Vector2(0, 820);
        topBarRect.sizeDelta = new Vector2(980, 100);

        // Settings Button (Left: X = -430) with Gear Sprite Icon (no missing emoji glyph)
        Button settingsBtn = CreateUIButton(topBar, "SettingsButton", s_iconGear != null ? "" : "SET", new Vector2(-430, 0), new Vector2(92, 92), new Color(0.18f, 0.28f, 0.44f), Color.white, s_btnBlue);
        if (s_iconGear != null)
        {
            CreateUISpriteImage(settingsBtn.gameObject, "GearIcon", s_iconGear, Vector2.zero, new Vector2(54, 54), Color.white);
        }

        // HighScore Pill Badge (Center: X = -35)
        GameObject hsBadge = CreateUICard(topBar, "HighScoreBadge", new Vector2(-35, 0), new Vector2(390, 76), new Color(0.16f, 0.14f, 0.08f, 0.95f), s_frameCyan);
        if (s_iconStar != null)
        {
            CreateUISpriteImage(hsBadge, "StarIcon", s_iconStar, new Vector2(-148, 0), new Vector2(40, 40), new Color(1f, 0.9f, 0.25f));
        }
        TMP_Text highScoreTxt = CreateUIText(hsBadge, "HighScoreText", "BEST: 0", new Vector2(16, 0), new Vector2(300, 65), 26, new Color(1.0f, 0.88f, 0.25f), TextAlignmentOptions.Center);
        highScoreTxt.fontStyle = FontStyles.Bold;

        // Coin Counter Pill Badge (Right: X = 345)
        GameObject coinBadge = CreateUICard(topBar, "CoinBadge", new Vector2(345, 0), new Vector2(300, 76), new Color(0.18f, 0.16f, 0.08f, 0.95f), s_frameCyan);
        if (s_iconCoin != null)
        {
            CreateUISpriteImage(coinBadge, "CoinIcon", s_iconCoin, new Vector2(-112, 0), new Vector2(42, 42), Color.white);
        }
        TMP_Text coinBalanceText = CreateUIText(coinBadge, "CoinBalanceText", "0 COINS", new Vector2(0, 0), new Vector2(180, 65), 24, new Color(1.0f, 0.9f, 0.35f), TextAlignmentOptions.Center);
        coinBalanceText.fontStyle = FontStyles.Bold;
        GameObject plusBadge = CreateUICard(coinBadge, "PlusBadge", new Vector2(114, 0), new Vector2(46, 46), new Color(0.18f, 0.8f, 0.44f), s_btnGreen);
        TMP_Text plusTxt = CreateUIText(plusBadge, "PlusText", "+", Vector2.zero, new Vector2(46, 46), 28, Color.white, TextAlignmentOptions.Center);
        plusTxt.fontStyle = FontStyles.Bold;

        // -------------------------------------------------------------
        // 3. HERO GAME LOGO & FLOATING SLIME MASCOTS (Y = 530)
        // -------------------------------------------------------------
        GameObject logoContainer = new GameObject("LogoContainer");
        logoContainer.transform.SetParent(panel.transform, false);
        RectTransform logoRect = logoContainer.AddComponent<RectTransform>();
        logoRect.anchoredPosition = new Vector2(0, 530);
        logoRect.sizeDelta = new Vector2(920, 250);

        TMP_Text mainTitle = CreateUIText(logoContainer, "TitleText", "BALL MERGE", new Vector2(0, 38), new Vector2(850, 110), 86, new Color(1.0f, 0.86f, 0.18f), TextAlignmentOptions.Center);
        mainTitle.fontStyle = FontStyles.Bold;

        GameObject subPill = CreateUICard(logoContainer, "SubtitlePill", new Vector2(0, -42), new Vector2(390, 50), new Color(0.12f, 0.25f, 0.45f, 0.95f), s_frameCyan);
        TMP_Text subTitle = CreateUIText(subPill, "SubtitleText", "2D PHYSICS PUZZLE", Vector2.zero, new Vector2(370, 48), 22, new Color(0.35f, 0.92f, 1.0f), TextAlignmentOptions.Center);
        subTitle.fontStyle = FontStyles.Bold;

        // Cute Floating Jelly Slime Mascots
        List<Transform> decorBalls = new List<Transform>();

        GameObject ball1 = CreateDecorBall(logoContainer, "DecorBall_Cherry", new Vector2(-335, 12), 86f, new Color(1.0f, 0.24f, 0.30f), useSlimeMascot: true);
        decorBalls.Add(ball1.transform);

        GameObject ball2 = CreateDecorBall(logoContainer, "DecorBall_Melon", new Vector2(340, -14), 104f, new Color(0.20f, 0.86f, 0.46f), useSlimeMascot: true);
        decorBalls.Add(ball2.transform);

        GameObject ball3 = CreateDecorBall(logoContainer, "DecorBall_Plum", new Vector2(272, 82), 68f, new Color(0.70f, 0.34f, 0.94f), useSlimeMascot: true);
        decorBalls.Add(ball3.transform);

        // -------------------------------------------------------------
        // 4. GIANT HERO "PLAY" BUTTON (Y = 220)
        // -------------------------------------------------------------
        GameObject heroPlayBox = new GameObject("HeroPlayContainer");
        heroPlayBox.transform.SetParent(panel.transform, false);
        RectTransform heroPlayRect = heroPlayBox.AddComponent<RectTransform>();
        heroPlayRect.anchoredPosition = new Vector2(0, 220);
        heroPlayRect.sizeDelta = new Vector2(740, 165);

        Button playEndlessBtn = CreateUIButton(heroPlayBox, "PlayEndlessButton", "", Vector2.zero, new Vector2(740, 165), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        if (s_iconTick != null)
        {
            CreateUISpriteImage(playEndlessBtn.gameObject, "PlayBadgeIcon", s_iconTick, new Vector2(-265, 2), new Vector2(68, 68), new Color(1f, 0.98f, 0.85f));
        }

        TMP_Text playMainTxt = CreateUIText(playEndlessBtn.gameObject, "PlayTitle", "PLAY NOW", new Vector2(18, 20), new Vector2(620, 76), 56, Color.white, TextAlignmentOptions.Center);
        playMainTxt.fontStyle = FontStyles.Bold;
        TMP_Text playSubTxt = CreateUIText(playEndlessBtn.gameObject, "PlaySub", "ENDLESS HIGHSCORE MODE", new Vector2(18, -32), new Vector2(620, 40), 21, new Color(0.86f, 1.0f, 0.86f), TextAlignmentOptions.Center);
        playSubTxt.fontStyle = FontStyles.Bold;

        // -------------------------------------------------------------
        // 5. 50 CAMPAIGN LEVELS CARD (Y = -25)
        // -------------------------------------------------------------
        GameObject campaignCard = CreateUICard(panel.gameObject, "CampaignCard", new Vector2(0, -25), new Vector2(920, 195), new Color(0.1f, 0.16f, 0.26f, 0.95f), s_frameCyan);

        TMP_Text campTitle = CreateUIText(campaignCard, "CampTitle", "CAMPAIGN ADVENTURE", new Vector2(-165, 36), new Vector2(510, 50), 32, new Color(0.2f, 0.85f, 1.0f), TextAlignmentOptions.Left);
        campTitle.fontStyle = FontStyles.Bold;

        TMP_Text campLvlTxt = CreateUIText(campaignCard, "CampaignLevelText", "LEVEL 1", new Vector2(-315, -18), new Vector2(210, 45), 26, new Color(1.0f, 0.85f, 0.2f), TextAlignmentOptions.Left);
        campLvlTxt.fontStyle = FontStyles.Bold;

        if (s_iconStar != null)
        {
            CreateUISpriteImage(campaignCard, "CampaignStarIcon", s_iconStar, new Vector2(-175, -18), new Vector2(30, 30), new Color(1f, 0.9f, 0.25f));
        }
        TMP_Text campStarsTxt = CreateUIText(campaignCard, "CampaignStarsText", "STARS: 0 / 150", new Vector2(-28, -18), new Vector2(260, 45), 22, new Color(0.92f, 0.96f, 1f), TextAlignmentOptions.Left);

        Button playCampaignBtn = CreateUIButton(campaignCard, "PlayCampaignButton", "PLAY LVL 1", new Vector2(325, 0), new Vector2(225, 86), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        TMP_Text playCampaignBtnTxt = playCampaignBtn.GetComponentInChildren<TMP_Text>();
        if (playCampaignBtnTxt != null) playCampaignBtnTxt.fontSize = 25;

        Button mapBtn = CreateUIButton(campaignCard, "LevelMapButton", "MAP", new Vector2(120, 0), new Vector2(160, 86), new Color(0.2f, 0.6f, 0.86f), Color.white, s_btnYellow);
        TMP_Text mapBtnTxt = mapBtn.GetComponentInChildren<TMP_Text>();
        if (mapBtnTxt != null) mapBtnTxt.fontSize = 26;

        // -------------------------------------------------------------
        // 6. DAILY REWARDS & LUCKY WHEEL BANNER (Y = -245)
        // -------------------------------------------------------------
        GameObject dailyCard = CreateUICard(panel.gameObject, "DailyCard", new Vector2(0, -245), new Vector2(920, 150), new Color(0.22f, 0.15f, 0.26f, 0.95f), s_framePurple);

        if (s_iconWheel != null)
        {
            CreateUISpriteImage(dailyCard, "WheelIcon", s_iconWheel, new Vector2(-385, 0), new Vector2(76, 76), Color.white);
        }

        TMP_Text dailyTitle = CreateUIText(dailyCard, "DailyTitle", "DAILY REWARD & SPIN", new Vector2(-95, 22), new Vector2(490, 45), 29, new Color(1.0f, 0.84f, 0.22f), TextAlignmentOptions.Left);
        dailyTitle.fontStyle = FontStyles.Bold;

        CreateUIText(dailyCard, "DailySub", "Login streak coins & lucky wheel spin!", new Vector2(-95, -18), new Vector2(490, 35), 20, new Color(0.9f, 0.86f, 0.98f), TextAlignmentOptions.Left);

        Button dailyRewardBtn = CreateUIButton(dailyCard, "DailyRewardButton", "SPIN & CLAIM", new Vector2(320, 0), new Vector2(240, 82), new Color(0.95f, 0.65f, 0.1f), Color.white, s_btnYellow);
        TMP_Text dailyBtnTxt = dailyRewardBtn.GetComponentInChildren<TMP_Text>();
        if (dailyBtnTxt != null) dailyBtnTxt.fontSize = 24;

        GameObject notifBadge = CreateUICard(dailyCard, "DailyNotificationBadge", new Vector2(420, 40), new Vector2(88, 36), new Color(0.95f, 0.2f, 0.25f), s_btnRed);
        TMP_Text notifTxt = CreateUIText(notifBadge, "BadgeText", "FREE!", Vector2.zero, new Vector2(88, 36), 18, Color.white, TextAlignmentOptions.Center);
        notifTxt.fontStyle = FontStyles.Bold;

        // -------------------------------------------------------------
        // 7. BOTTOM NAVIGATION DOCK (Y = -480)
        // -------------------------------------------------------------
        GameObject bottomDock = CreateUICard(panel.gameObject, "BottomDock", new Vector2(0, -480), new Vector2(920, 140), new Color(0.12f, 0.14f, 0.22f, 0.95f), s_frameBlue);

        Button ballKitsBtn = CreateUIButton(bottomDock, "BallKitsButton", "BALL KITS", new Vector2(-295, 0), new Vector2(276, 94), new Color(0.2f, 0.55f, 0.85f), Color.white, s_btnBlue);
        TMP_Text kitsTxt = ballKitsBtn.GetComponentInChildren<TMP_Text>();
        if (kitsTxt != null) kitsTxt.fontSize = 23;
        AddButtonIcon(ballKitsBtn, s_iconShop, new Vector2(-94, 0), new Vector2(44, 44), Color.white, 20f);

        Button themesBtn = CreateUIButton(bottomDock, "ThemesButton", "THEMES", new Vector2(0, 0), new Vector2(276, 94), new Color(0.2f, 0.55f, 0.85f), Color.white, s_btnBlue);
        TMP_Text themesTxt = themesBtn.GetComponentInChildren<TMP_Text>();
        if (themesTxt != null) themesTxt.fontSize = 23;
        AddButtonIcon(themesBtn, s_iconCalendar, new Vector2(-94, 0), new Vector2(44, 44), Color.white, 20f);

        Button quitBtn = CreateUIButton(bottomDock, "QuitButton", "QUIT", new Vector2(295, 0), new Vector2(276, 94), new Color(0.85f, 0.25f, 0.2f), Color.white, s_btnRed);
        TMP_Text quitTxt = quitBtn.GetComponentInChildren<TMP_Text>();
        if (quitTxt != null) quitTxt.fontSize = 25;
        AddButtonIcon(quitBtn, s_iconClose, new Vector2(-90, 0), new Vector2(36, 36), Color.white, 16f);

        // Quick Game Switch Pill Button cleanly below dock
        Button switchGameBtn = CreateUIButton(panel.gameObject, "SwitchGameButton", "SWITCH GAME: TILT MAZE", new Vector2(0, -640), new Vector2(560, 84), new Color(0.95f, 0.65f, 0.12f), Color.white, s_btnYellow);
        TMP_Text switchTxt = switchGameBtn.GetComponentInChildren<TMP_Text>();
        if (switchTxt != null) switchTxt.fontSize = 24;

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
        SafeSetProperty(so, "switchGameButton", switchGameBtn);
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

        // Deep Ocean Sky Backdrop
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.06f, 0.11f, 0.20f, 1f));
        if (s_mapBackdrop != null)
        {
            Image fullArt = CreateUISpriteImage(panel.gameObject, "AmbientMapArt", s_mapBackdrop, Vector2.zero, new Vector2(1080, 1920), new Color(0.65f, 0.80f, 1f, 0.22f));
            fullArt.preserveAspect = false;
        }

        // Header Card
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 725), new Vector2(950, 165), new Color(0.12f, 0.18f, 0.28f, 0.95f), s_bannerBlue != null ? s_bannerBlue : s_frameBlue);
        CreateUIText(headerCard, "TitleText", "CAMPAIGN LEVELS", new Vector2(0, 28), new Vector2(900, 66), 50, new Color(1.0f, 0.92f, 0.35f), TextAlignmentOptions.Center);

        if (s_iconStar != null)
        {
            CreateUISpriteImage(headerCard, "TotalStarsIcon", s_iconStar, new Vector2(-410, -34), new Vector2(34, 34), new Color(1f, 0.9f, 0.25f));
        }
        TMP_Text totalStarsText = CreateUIText(headerCard, "TotalStarsText", "STARS: 0 / 150", new Vector2(-175, -34), new Vector2(420, 44), 25, new Color(1.0f, 0.9f, 0.3f), TextAlignmentOptions.Left);

        if (s_iconCoin != null)
        {
            CreateUISpriteImage(headerCard, "CoinBalanceIcon", s_iconCoin, new Vector2(180, -34), new Vector2(36, 36), Color.white);
        }
        TMP_Text coinBalanceText = CreateUIText(headerCard, "CoinBalanceText", "0 COINS", new Vector2(315, -34), new Vector2(215, 44), 25, new Color(1.0f, 0.9f, 0.3f), TextAlignmentOptions.Left);

        // Grid Container Box (hosts adventure road backdrop + LevelScrollView adventure map)
        GameObject gridCard = CreateUICard(panel.gameObject, "GridCard", new Vector2(0, 40), new Vector2(950, 1110), new Color(0.08f, 0.13f, 0.24f, 0.95f), s_frameCyan);
        if (s_mapBackdrop != null)
        {
            Image mapBg = CreateUISpriteImage(gridCard, "AdventureRoadArt", s_mapBackdrop, Vector2.zero, new Vector2(918, 1078), new Color(0.88f, 0.94f, 1f, 0.45f));
            mapBg.preserveAspect = false;
        }

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
                if (bgImg != null) bgImg.color = new Color(0.05f, 0.10f, 0.18f, 0.42f);

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
            scrollRectTransform.sizeDelta = new Vector2(910, 1070);

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
            contentRect.sizeDelta = new Vector2(910, 2900);

            scrollRect.content = contentRect;
            gridContentTf = contentObj.transform;
        }

        // Bottom Navigation Bar: Back + Jump To Current Level
        Button backButton = CreateUIButton(panel.gameObject, "BackButton", "BACK TO MENU", new Vector2(-215, -625), new Vector2(410, 96), new Color(0.82f, 0.28f, 0.28f), Color.white, s_btnRed);
        AddButtonIcon(backButton, s_iconBack, new Vector2(-150, 0), new Vector2(42, 42), Color.white, 20f);

        Button jumpButton = CreateUIButton(panel.gameObject, "JumpToCurrentButton", "CURRENT LVL", new Vector2(215, -625), new Vector2(410, 96), new Color(0.95f, 0.68f, 0.14f), Color.white, s_btnYellow);
        AddButtonIcon(jumpButton, s_iconStar, new Vector2(-145, 0), new Vector2(40, 40), Color.white, 18f);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "totalStarsText", totalStarsText);
        SafeSetProperty(so, "coinBalanceText", coinBalanceText);
        SafeSetProperty(so, "levelGridContainer", gridContentTf);
        SafeSetProperty(so, "levelMapController", adventureMap);
        SafeSetProperty(so, "backButton", backButton);
        SafeSetProperty(so, "jumpToCurrentButton", jumpButton);
        so.ApplyModifiedProperties();
    }

    private static void BuildDailyRewardPanel(GameObject parent)
    {
        DailyRewardPanel panel = EnsurePanel<DailyRewardPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background Dim
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.07f, 0.08f, 0.14f, 1f));

        // Header Section
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 740), new Vector2(940, 150), new Color(0.22f, 0.18f, 0.1f, 0.95f), s_bannerYellow != null ? s_bannerYellow : s_bannerBlue);
        CreateUIText(headerCard, "TitleText", "DAILY REWARDS & SPIN", new Vector2(0, 22), new Vector2(900, 62), 46, Color.white, TextAlignmentOptions.Center);
        TMP_Text statusText = CreateUIText(headerCard, "StatusText", "7-Day Login Streak Active!", new Vector2(0, -32), new Vector2(880, 40), 22, new Color(1f, 0.96f, 0.72f), TextAlignmentOptions.Center);

        // 1. Daily 7-Day Login Streak Box (Y = 460)
        GameObject streakCard = CreateUICard(panel.gameObject, "StreakCard", new Vector2(0, 460), new Vector2(940, 320), new Color(0.14f, 0.16f, 0.24f, 0.95f), s_framePurple);
        if (s_iconChest != null)
        {
            CreateUISpriteImage(streakCard, "ChestIcon", s_iconChest, new Vector2(-360, 20), new Vector2(100, 100), Color.white);
        }
        CreateUIText(streakCard, "StreakTitle", "7-DAY LOGIN STREAK REWARD", new Vector2(45, 108), new Vector2(760, 42), 27, new Color(0.25f, 0.88f, 1f), TextAlignmentOptions.Center);

        TMP_Text streakDayText = CreateUIText(streakCard, "StreakDayText", "DAY 1 REWARD: 50 COINS", new Vector2(45, 42), new Vector2(760, 52), 34, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);

        Button claimStreakButton = CreateUIButton(streakCard, "ClaimStreakButton", "CLAIM TODAY'S REWARD", new Vector2(0, -82), new Vector2(520, 82), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        AddButtonIcon(claimStreakButton, s_iconCoin, new Vector2(-205, 0), new Vector2(42, 42), Color.white, 20f);
        TMP_Text claimStreakText = claimStreakButton.GetComponentInChildren<TMP_Text>();

        // 2. Physics Spin Wheel Box (Y = -120)
        GameObject wheelCard = CreateUICard(panel.gameObject, "WheelCard", new Vector2(0, -120), new Vector2(940, 670), new Color(0.16f, 0.12f, 0.24f, 0.95f), s_frameBlue);
        CreateUIText(wheelCard, "WheelTitle", "LUCKY SPIN WHEEL", new Vector2(0, 290), new Vector2(900, 45), 34, new Color(1f, 0.8f, 0.22f), TextAlignmentOptions.Center);

        // Wheel Circular Frame + Wheel Sprite
        GameObject wheelContainer = new GameObject("WheelContainer");
        wheelContainer.transform.SetParent(wheelCard.transform, false);
        RectTransform wheelRt = wheelContainer.AddComponent<RectTransform>();
        wheelRt.anchoredPosition = new Vector2(0, 25);
        wheelRt.sizeDelta = new Vector2(420, 420);

        Image wheelOuter = wheelContainer.AddComponent<Image>();
        wheelOuter.sprite = GetCircleSprite();
        wheelOuter.color = new Color(1f, 0.82f, 0.24f, 1f);

        Image wheelInner = CreateUIImage(wheelContainer, "WheelInnerDisk", Vector2.zero, new Vector2(382, 382), new Color(0.24f, 0.16f, 0.42f, 1f));
        wheelInner.sprite = GetCircleSprite();
        wheelInner.raycastTarget = false;

        if (s_iconWheel != null)
        {
            CreateUISpriteImage(wheelContainer, "WheelCenterGraphic", s_iconWheel, Vector2.zero, new Vector2(320, 320), Color.white);
        }

        // Pointer Marker at top of wheel
        Image pointer = CreateUIImage(wheelCard, "WheelPointer", new Vector2(0, 236), new Vector2(44, 54), new Color(0.95f, 0.22f, 0.26f));
        if (s_btnRed != null)
        {
            pointer.sprite = s_btnRed;
            pointer.type = Image.Type.Sliced;
            pointer.color = Color.white;
        }

        Button spinButton = CreateUIButton(wheelCard, "SpinButton", "FREE SPIN", new Vector2(0, -265), new Vector2(470, 88), new Color(0.95f, 0.65f, 0.1f), Color.white, s_btnYellow);
        AddButtonIcon(spinButton, s_iconWheel, new Vector2(-175, 0), new Vector2(46, 46), Color.white, 20f);
        TMP_Text spinButtonText = spinButton.GetComponentInChildren<TMP_Text>();

        // Bottom Close Button
        Button closeButton = CreateUIButton(panel.gameObject, "CloseButton", "BACK TO MENU", new Vector2(0, -610), new Vector2(500, 96), new Color(0.82f, 0.28f, 0.28f), Color.white, s_btnRed);
        AddButtonIcon(closeButton, s_iconBack, new Vector2(-190, 0), new Vector2(42, 42), Color.white, 18f);

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

        // Sleek Translucent Top Header Bar
        CreateUIImage(panel.gameObject, "TopHeaderBar", new Vector2(0, 855), new Vector2(1080, 210), new Color(0.06f, 0.08f, 0.15f, 0.88f));

        // Center Score & Mode Ribbon
        GameObject modePill = CreateUICard(panel.gameObject, "ModePill", new Vector2(0, 912), new Vector2(420, 48), new Color(0.12f, 0.22f, 0.36f, 0.95f), s_bannerBlue != null ? s_bannerBlue : s_frameCyan);
        TMP_Text modeTitle = CreateUIText(modePill, "ModeTitleText", "ENDLESS MODE", Vector2.zero, new Vector2(400, 44), 25, Color.white, TextAlignmentOptions.Center);
        TMP_Text score = CreateUIText(panel.gameObject, "ScoreText", "0", new Vector2(0, 825), new Vector2(420, 90), 62, Color.white, TextAlignmentOptions.Center);
        score.fontStyle = FontStyles.Bold;

        // Left HighScore Badge
        GameObject hsCard = CreateUICard(panel.gameObject, "HighScoreContainer", new Vector2(-365, 845), new Vector2(280, 66), new Color(0.14f, 0.16f, 0.24f, 0.92f), s_frameBlue);
        if (s_iconStar != null)
        {
            CreateUISpriteImage(hsCard, "BestStarIcon", s_iconStar, new Vector2(-104, 0), new Vector2(32, 32), new Color(1f, 0.9f, 0.25f));
        }
        TMP_Text highScore = CreateUIText(hsCard, "HighScoreText", "BEST: 0", new Vector2(18, 0), new Vector2(215, 56), 25, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);

        // Right Drops Pill
        GameObject dropsObj = CreateUICard(panel.gameObject, "DropsContainer", new Vector2(365, 875), new Vector2(285, 58), new Color(0.18f, 0.12f, 0.16f, 0.92f), s_framePurple);
        TMP_Text drops = CreateUIText(dropsObj, "DropsText", "DROPS: 30 / 30", Vector2.zero, new Vector2(255, 50), 25, new Color(1f, 0.56f, 0.56f), TextAlignmentOptions.Center);

        // Right Blocks Remaining Pill
        GameObject blocksObj = CreateUICard(panel.gameObject, "BlocksContainer", new Vector2(365, 810), new Vector2(285, 54), new Color(0.18f, 0.14f, 0.10f, 0.92f), s_frameCyan);
        TMP_Text blocks = CreateUIText(blocksObj, "BlocksText", "BLOCKS: 0 / 0", Vector2.zero, new Vector2(255, 48), 24, new Color(1f, 0.78f, 0.28f), TextAlignmentOptions.Center);

        // Next Ball Preview Pill
        GameObject nextBallObj = CreateUICard(panel.gameObject, "NextBallContainer", new Vector2(-405, 690), new Vector2(180, 136), new Color(0.10f, 0.14f, 0.22f, 0.92f), s_frameCyan);
        CreateUIText(nextBallObj, "NextLabel", "NEXT", new Vector2(0, 44), new Vector2(160, 34), 21, new Color(0.55f, 0.88f, 1f), TextAlignmentOptions.Center);
        Image nextBallImg = CreateUIImage(nextBallObj, "NextBallImage", new Vector2(0, 0), new Vector2(58, 58), new Color(0.95f, 0.2f, 0.25f));
        nextBallImg.sprite = GetCircleSprite();
        nextBallImg.preserveAspect = true;
        TMP_Text nextBallName = CreateUIText(nextBallObj, "NextBallNameText", "Cherry", new Vector2(0, -45), new Vector2(166, 30), 20, Color.white, TextAlignmentOptions.Center);

        // Pause Button
        Button pause = CreateUIButton(panel.gameObject, "PauseButton", "II", new Vector2(445, 690), new Vector2(96, 96), new Color(0.2f, 0.25f, 0.35f), Color.white, s_btnBlue);

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
        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.78f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(850, 960), new Color(0.15f, 0.17f, 0.24f, 1f), s_frameBlue);

        GameObject banner = CreateUICard(window, "HeaderRibbon", new Vector2(0, 365), new Vector2(680, 110), new Color(0.16f, 0.35f, 0.65f), s_bannerBlue != null ? s_bannerBlue : s_frameCyan);
        TMP_Text title = CreateUIText(banner, "TitleText", "GAME PAUSED", Vector2.zero, new Vector2(640, 90), 50, Color.white, TextAlignmentOptions.Center);

        Button resume = CreateUIButton(window, "ResumeButton", "RESUME", new Vector2(0, 185), new Vector2(500, 98), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        AddButtonIcon(resume, s_iconTick, new Vector2(-180, 0), new Vector2(44, 44), Color.white, 16f);

        Button restart = CreateUIButton(window, "RestartButton", "REPLAY", new Vector2(0, 62), new Vector2(500, 98), new Color(0.95f, 0.65f, 0.1f), Color.white, s_btnYellow);

        Button settings = CreateUIButton(window, "SettingsButton", "SETTINGS", new Vector2(0, -61), new Vector2(500, 98), new Color(0.2f, 0.6f, 0.86f), Color.white, s_btnBlue);
        AddButtonIcon(settings, s_iconGear, new Vector2(-180, 0), new Vector2(42, 42), Color.white, 16f);

        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -184), new Vector2(500, 98), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
        AddButtonIcon(mainMenu, s_iconHome, new Vector2(-180, 0), new Vector2(42, 42), Color.white, 16f);

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
        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.78f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(850, 860), new Color(0.15f, 0.17f, 0.24f, 1f), s_frameBlue);

        GameObject banner = CreateUICard(window, "HeaderRibbon", new Vector2(0, 315), new Vector2(680, 110), new Color(0.85f, 0.22f, 0.22f), s_btnRed);
        TMP_Text title = CreateUIText(banner, "TitleText", "GAME OVER", Vector2.zero, new Vector2(640, 90), 54, Color.white, TextAlignmentOptions.Center);

        TMP_Text finalScore = CreateUIText(window, "FinalScoreText", "SCORE: 0", new Vector2(0, 155), new Vector2(750, 80), 46, Color.white, TextAlignmentOptions.Center);

        GameObject bestPill = CreateUICard(window, "BestScorePill", new Vector2(0, 48), new Vector2(460, 74), new Color(0.14f, 0.16f, 0.24f, 0.95f), s_frameCyan);
        if (s_iconStar != null)
        {
            CreateUISpriteImage(bestPill, "StarIcon", s_iconStar, new Vector2(-170, 0), new Vector2(40, 40), new Color(1f, 0.9f, 0.25f));
        }
        TMP_Text highScore = CreateUIText(bestPill, "HighScoreText", "BEST: 0", new Vector2(16, 0), new Vector2(360, 60), 34, new Color(1.0f, 0.88f, 0.24f), TextAlignmentOptions.Center);

        Button restart = CreateUIButton(window, "RestartButton", "REPLAY", new Vector2(0, -105), new Vector2(470, 102), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -238), new Vector2(470, 102), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
        AddButtonIcon(mainMenu, s_iconHome, new Vector2(-168, 0), new Vector2(42, 42), Color.white, 16f);

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

        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.78f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(860, 990), new Color(0.12f, 0.18f, 0.24f, 1f), s_frameBlue);

        GameObject banner = CreateUICard(window, "HeaderRibbon", new Vector2(0, 395), new Vector2(720, 108), new Color(0.95f, 0.65f, 0.1f), s_bannerYellow != null ? s_bannerYellow : s_bannerBlue);
        TMP_Text title = CreateUIText(banner, "TitleText", "LEVEL PASSED!", Vector2.zero, new Vector2(680, 90), 50, Color.white, TextAlignmentOptions.Center);

        // 3 Real Star Sprites + Clean Numeric Label
        GameObject starsRow = new GameObject("StarIconsContainer");
        starsRow.transform.SetParent(window.transform, false);
        RectTransform starsRowRt = starsRow.AddComponent<RectTransform>();
        starsRowRt.anchoredPosition = new Vector2(0, 272);
        starsRowRt.sizeDelta = new Vector2(360, 92);

        Sprite starSprite = s_iconStar != null ? s_iconStar : BallMergeUITheme.GetStarSprite();
        Image[] starIcons = new Image[3];
        starIcons[0] = CreateUISpriteImage(starsRow, "Star_1", starSprite, new Vector2(-108, -6), new Vector2(72, 72), new Color(1f, 0.9f, 0.22f));
        starIcons[1] = CreateUISpriteImage(starsRow, "Star_2", starSprite, new Vector2(0, 12), new Vector2(88, 88), new Color(1f, 0.9f, 0.22f));
        starIcons[2] = CreateUISpriteImage(starsRow, "Star_3", starSprite, new Vector2(108, -6), new Vector2(72, 72), new Color(1f, 0.9f, 0.22f));

        TMP_Text starsText = CreateUIText(window, "StarsText", "3 / 3 STARS", new Vector2(0, 194), new Vector2(750, 44), 28, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);
        TMP_Text score = CreateUIText(window, "ScoreText", "FINAL SCORE: 0", new Vector2(0, 134), new Vector2(750, 56), 38, Color.white, TextAlignmentOptions.Center);
        TMP_Text dropsUsed = CreateUIText(window, "DropsUsedText", "DROPS USED: 0", new Vector2(0, 76), new Vector2(750, 46), 28, new Color(0.7f, 0.88f, 1f), TextAlignmentOptions.Center);
        TMP_Text coinBonus = CreateUIText(window, "CoinBonusText", "", new Vector2(0, 12), new Vector2(760, 60), 23, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center);

        Button nextLevel = CreateUIButton(window, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -108), new Vector2(480, 94), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        AddButtonIcon(nextLevel, s_iconTick, new Vector2(-175, 0), new Vector2(42, 42), Color.white, 16f);

        Button replay = CreateUIButton(window, "ReplayButton", "REPLAY", new Vector2(0, -216), new Vector2(480, 94), new Color(0.2f, 0.6f, 0.86f), Color.white, s_btnYellow);
        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -324), new Vector2(480, 94), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
        AddButtonIcon(mainMenu, s_iconHome, new Vector2(-175, 0), new Vector2(42, 42), Color.white, 16f);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "titleText", title);
        SafeSetProperty(so, "starsText", starsText);
        SafeSetArrayProperty(so, "starIcons", starIcons);
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
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 680), new Vector2(940, 180), new Color(0.16f, 0.12f, 0.22f, 0.95f), s_bannerBlue != null ? s_bannerBlue : s_framePurple);

        CreateUIText(headerCard, "TitleText", "BALL KITS SHOP", new Vector2(0, 32), new Vector2(900, 70), 50, new Color(1.0f, 0.9f, 0.35f), TextAlignmentOptions.Center);
        TMP_Text currentKit = CreateUIText(headerCard, "CurrentKitNameText", "ACTIVE: CLASSIC FRUITS", new Vector2(-150, -35), new Vector2(560, 45), 24, Color.white, TextAlignmentOptions.Left);

        if (s_iconCoin != null)
        {
            CreateUISpriteImage(headerCard, "CoinIcon", s_iconCoin, new Vector2(180, -35), new Vector2(36, 36), Color.white);
        }
        TMP_Text coinBalanceText = CreateUIText(headerCard, "CoinBalanceText", "0 COINS", new Vector2(315, -35), new Vector2(215, 45), 24, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Left);

        // Kit Card 1: Classic Fruits (Y = 340)
        GameObject kit1Obj = CreateKitCard(panel.gameObject, "ClassicCard", "CLASSIC FRUITS", "Fruit Tiers (Cherry, Plum, Orange...)", new Vector2(0, 340), new Color[] { new Color(0.95f, 0.2f, 0.25f), new Color(0.65f, 0.25f, 0.85f), new Color(1.0f, 0.6f, 0.1f) });
        Button inspectClassic = CreateUIButton(kit1Obj, "InspectClassicButton", "VIEW TIERS", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button classicBtn = CreateUIButton(kit1Obj, "ClassicKitButton", "EQUIP", new Vector2(365, 0), new Vector2(155, 76), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnGreen);

        // Kit Card 2: Ocean World (Y = 100)
        GameObject kit2Obj = CreateKitCard(panel.gameObject, "OceanCard", "OCEAN WORLD", "Sea Tiers (Shell, Starfish, Jellyfish...)", new Vector2(0, 100), new Color[] { new Color(1.0f, 0.85f, 0.75f), new Color(1.0f, 0.4f, 0.4f), new Color(0.9f, 0.4f, 0.9f) });
        Button inspectOcean = CreateUIButton(kit2Obj, "InspectOceanButton", "VIEW TIERS", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button oceanBtn = CreateUIButton(kit2Obj, "OceanKitButton", "EQUIP", new Vector2(365, 0), new Vector2(155, 76), new Color(0.2f, 0.6f, 0.86f), Color.white, s_btnGreen);

        // Kit Card 3: Sky High (Y = -140)
        GameObject kit3Obj = CreateKitCard(panel.gameObject, "SkyCard", "SKY HIGH", "Sky Tiers (Cloud, Bird, Kite...)", new Vector2(0, -140), new Color[] { new Color(0.9f, 0.95f, 1.0f), new Color(0.3f, 0.75f, 1.0f), new Color(1.0f, 0.5f, 0.2f) });
        Button inspectSky = CreateUIButton(kit3Obj, "InspectSkyButton", "VIEW TIERS", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button skyBtn = CreateUIButton(kit3Obj, "SkyKitButton", "EQUIP", new Vector2(365, 0), new Vector2(155, 76), new Color(0.95f, 0.6f, 0.1f), Color.white, s_btnGreen);

        // Bottom Navigation Bar
        Button backBtn = CreateUIButton(panel.gameObject, "BackButton", "BACK TO MENU", new Vector2(0, -550), new Vector2(500, 96), new Color(0.82f, 0.28f, 0.28f), Color.white, s_btnRed);
        AddButtonIcon(backBtn, s_iconBack, new Vector2(-190, 0), new Vector2(42, 42), Color.white, 18f);

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

        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(880, 1150), new Color(0.14f, 0.16f, 0.22f, 1f), s_frameBlue);

        TMP_Text title = CreateUIText(window, "KitTitleText", "OCEAN WORLD", new Vector2(0, 480), new Vector2(800, 75), 48, new Color(0.2f, 0.8f, 1f), TextAlignmentOptions.Center);
        TMP_Text subtitle = CreateUIText(window, "KitSubtitleText", "6 BALL TIERS • DROPPABLE TIERS: 1 - 3", new Vector2(0, 425), new Vector2(800, 45), 22, new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Center);

        // Container for 6 tier cards
        GameObject containerObj = CreateUICard(window, "TierListContainer", new Vector2(0, 20), new Vector2(800, 740), new Color(0.1f, 0.12f, 0.17f, 0.95f), s_frameCyan);

        // Action Buttons
        Button equipBtn = CreateUIButton(window, "EquipButton", "EQUIP THIS KIT", new Vector2(-200, -480), new Vector2(360, 88), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        TMP_Text equipTxt = equipBtn.GetComponentInChildren<TMP_Text>();

        Button closeBtn = CreateUIButton(window, "CloseButton", "CLOSE", new Vector2(200, -480), new Vector2(360, 88), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
        AddButtonIcon(closeBtn, s_iconClose, new Vector2(-115, 0), new Vector2(34, 34), Color.white, 14f);

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
        GameObject cardObj = CreateUICard(parent, cardName, pos, new Vector2(940, 210), new Color(0.15f, 0.17f, 0.24f, 0.95f), s_frameBlue);

        // Glossy Swatch circles on left
        for (int i = 0; i < swatchColors.Length; i++)
        {
            Image dot = CreateUIImage(cardObj, $"Dot_{i}", new Vector2(-385 + (i * 44), 0), new Vector2(38, 38), swatchColors[i]);
            dot.sprite = GetCircleSprite();
        }

        CreateUIText(cardObj, "CardTitle", title, new Vector2(-55, 25), new Vector2(400, 45), 34, Color.white, TextAlignmentOptions.Left);
        CreateUIText(cardObj, "CardSub", subtitle, new Vector2(-55, -25), new Vector2(400, 40), 20, new Color(0.78f, 0.82f, 0.92f), TextAlignmentOptions.Left);

        return cardObj;
    }

    private static void BuildThemeShopPanel(GameObject parent)
    {
        ThemeShopPanel panel = EnsurePanel<ThemeShopPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // Header Section Card
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 720), new Vector2(940, 165), new Color(0.22f, 0.16f, 0.12f, 0.95f), s_bannerYellow != null ? s_bannerYellow : s_bannerBlue);

        CreateUIText(headerCard, "TitleText", "THEME SHOP", new Vector2(0, 28), new Vector2(900, 68), 50, Color.white, TextAlignmentOptions.Center);
        TMP_Text currentTheme = CreateUIText(headerCard, "CurrentThemeNameText", "ACTIVE: DARK COSMIC", new Vector2(-150, -32), new Vector2(560, 45), 24, Color.white, TextAlignmentOptions.Left);

        if (s_iconCoin != null)
        {
            CreateUISpriteImage(headerCard, "CoinIcon", s_iconCoin, new Vector2(180, -32), new Vector2(36, 36), Color.white);
        }
        TMP_Text coinBalanceText = CreateUIText(headerCard, "CoinBalanceText", "0 COINS", new Vector2(315, -32), new Vector2(215, 45), 24, new Color(1.0f, 0.9f, 0.35f), TextAlignmentOptions.Left);

        // Theme Cards
        GameObject t1 = CreateThemeCard(panel.gameObject, "DarkCard", "DARK COSMIC", "Cosmic Space Atmosphere", new Vector2(0, 430), new Color[] { new Color(0.12f, 0.13f, 0.18f), new Color(0.2f, 0.65f, 1.0f), new Color(1.0f, 0.3f, 0.3f) });
        Button inspectDark = CreateUIButton(t1, "InspectDarkButton", "PREVIEW", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button darkBtn = CreateUIButton(t1, "DarkThemeButton", "APPLY", new Vector2(365, 0), new Vector2(155, 76), new Color(0.2f, 0.25f, 0.35f), Color.white, s_btnGreen);

        GameObject t2 = CreateThemeCard(panel.gameObject, "OceanCard", "OCEAN DEPTH", "Underwater Aquatic Feel", new Vector2(0, 230), new Color[] { new Color(0.04f, 0.11f, 0.22f), new Color(0.12f, 0.82f, 0.85f), new Color(1.0f, 0.5f, 0.2f) });
        Button inspectOcean = CreateUIButton(t2, "InspectOceanButton", "PREVIEW", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button oceanBtn = CreateUIButton(t2, "OceanThemeButton", "APPLY", new Vector2(365, 0), new Vector2(155, 76), new Color(0.1f, 0.5f, 0.6f), Color.white, s_btnGreen);

        GameObject t3 = CreateThemeCard(panel.gameObject, "SkyCard", "SKY BREEZE", "Atmospheric Blue Sky", new Vector2(0, 30), new Color[] { new Color(0.16f, 0.29f, 0.49f), new Color(0.48f, 0.91f, 1.0f), new Color(1.0f, 0.4f, 0.7f) });
        Button inspectSky = CreateUIButton(t3, "InspectSkyButton", "PREVIEW", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button skyBtn = CreateUIButton(t3, "SkyThemeButton", "APPLY", new Vector2(365, 0), new Vector2(155, 76), new Color(0.25f, 0.6f, 0.85f), Color.white, s_btnGreen);

        GameObject t4 = CreateThemeCard(panel.gameObject, "SunsetCard", "SUNSET GLOW", "Warm Sunset Horizon", new Vector2(0, -170), new Color[] { new Color(0.16f, 0.08f, 0.21f), new Color(1.0f, 0.49f, 0.2f), new Color(1.0f, 0.82f, 0.0f) });
        Button inspectSunset = CreateUIButton(t4, "InspectSunsetButton", "PREVIEW", new Vector2(165, 0), new Vector2(225, 76), new Color(0.2f, 0.25f, 0.38f), Color.white, s_btnBlue);
        Button sunsetBtn = CreateUIButton(t4, "SunsetThemeButton", "APPLY", new Vector2(365, 0), new Vector2(155, 76), new Color(0.55f, 0.25f, 0.6f), Color.white, s_btnGreen);

        // Bottom Navigation Bar
        Button backBtn = CreateUIButton(panel.gameObject, "BackButton", "BACK TO MENU", new Vector2(0, -560), new Vector2(500, 96), new Color(0.82f, 0.28f, 0.28f), Color.white, s_btnRed);
        AddButtonIcon(backBtn, s_iconBack, new Vector2(-190, 0), new Vector2(42, 42), Color.white, 18f);

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

        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(900, 1200), new Color(0.14f, 0.16f, 0.22f, 1f), s_frameBlue);

        TMP_Text title = CreateUIText(window, "ThemeTitleText", "DARK COSMIC", new Vector2(0, 500), new Vector2(800, 75), 48, new Color(0.95f, 0.6f, 0.2f), TextAlignmentOptions.Center);
        TMP_Text subtitle = CreateUIText(window, "ThemeSubtitleText", "ENVIRONMENT VISUAL PALETTE PREVIEW", new Vector2(0, 445), new Vector2(800, 45), 22, new Color(0.72f, 0.82f, 0.96f), TextAlignmentOptions.Center);

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
        Button applyBtn = CreateUIButton(window, "ApplyButton", "APPLY THIS THEME", new Vector2(-200, -500), new Vector2(360, 88), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        TMP_Text applyTxt = applyBtn.GetComponentInChildren<TMP_Text>();

        Button closeBtn = CreateUIButton(window, "CloseButton", "CLOSE", new Vector2(200, -500), new Vector2(360, 88), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
        AddButtonIcon(closeBtn, s_iconClose, new Vector2(-115, 0), new Vector2(34, 34), Color.white, 14f);

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
        GameObject cardObj = CreateUICard(parent, cardName, pos, new Vector2(940, 175), new Color(0.15f, 0.17f, 0.24f, 0.95f), s_frameBlue);

        // Swatch circular badges on left
        for (int i = 0; i < swatchColors.Length; i++)
        {
            Image sw = CreateUIImage(cardObj, $"Swatch_{i}", new Vector2(-385 + (i * 44), 0), new Vector2(36, 36), swatchColors[i]);
            sw.sprite = GetCircleSprite();
        }

        CreateUIText(cardObj, "CardTitle", title, new Vector2(-55, 22), new Vector2(400, 42), 32, Color.white, TextAlignmentOptions.Left);
        CreateUIText(cardObj, "CardSub", subtitle, new Vector2(-55, -22), new Vector2(400, 36), 20, new Color(0.78f, 0.82f, 0.92f), TextAlignmentOptions.Left);

        return cardObj;
    }

    private static void BuildSettingsPanel(GameObject parent)
    {
        SettingsPanel panel = EnsurePanel<SettingsPanel>(parent);
        ClearChildren(panel.gameObject);

        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.78f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(860, 1020), new Color(0.15f, 0.18f, 0.26f, 1f), s_frameBlue);

        GameObject banner = CreateUICard(window, "HeaderRibbon", new Vector2(0, 405), new Vector2(660, 108), new Color(0.16f, 0.35f, 0.65f), s_bannerBlue != null ? s_bannerBlue : s_frameCyan);
        CreateUIText(banner, "TitleText", "SETTINGS", Vector2.zero, new Vector2(600, 88), 52, Color.white, TextAlignmentOptions.Center);

        Slider masterSlider = CreateSettingsSliderRow(window, "MasterVolumeRow", "MASTER", new Vector2(0, 220), s_iconSpeaker, s_btnBlue, out TMP_Text masterPct);
        Slider musicSlider = CreateSettingsSliderRow(window, "MusicVolumeRow", "MUSIC", new Vector2(0, 85), s_iconMusic, s_btnGreen, out TMP_Text musicPct);
        Slider sfxSlider = CreateSettingsSliderRow(window, "SfxVolumeRow", "SOUND FX", new Vector2(0, -50), s_iconStar, s_btnYellow, out TMP_Text sfxPct);

        Toggle muteToggle = CreateSettingsMuteRow(window, "MuteAllRow", new Vector2(0, -195), out TMP_Text muteStateTxt);

        Button backBtn = CreateUIButton(window, "BackButton", "CLOSE", new Vector2(0, -370), new Vector2(440, 96), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
        AddButtonIcon(backBtn, s_iconBack, new Vector2(-150, 0), new Vector2(40, 40), Color.white, 16f);

        SerializedObject so = new SerializedObject(panel);
        SafeSetProperty(so, "masterVolumeSlider", masterSlider);
        SafeSetProperty(so, "musicVolumeSlider", musicSlider);
        SafeSetProperty(so, "sfxVolumeSlider", sfxSlider);
        SafeSetProperty(so, "muteToggle", muteToggle);
        SafeSetProperty(so, "backButton", backBtn);
        SafeSetProperty(so, "masterPercentText", masterPct);
        SafeSetProperty(so, "musicPercentText", musicPct);
        SafeSetProperty(so, "sfxPercentText", sfxPct);
        SafeSetProperty(so, "muteStateText", muteStateTxt);
        so.ApplyModifiedProperties();
    }

    private static Slider CreateSettingsSliderRow(GameObject window, string rowName, string label, Vector2 pos, Sprite iconSprite, Sprite fillSprite, out TMP_Text percentText)
    {
        GameObject row = CreateUICard(window, rowName, pos, new Vector2(740, 112), new Color(0.11f, 0.14f, 0.22f, 0.95f), s_frameCyan);

        if (iconSprite != null)
        {
            CreateUISpriteImage(row, "RowIcon", iconSprite, new Vector2(-315, 0), new Vector2(48, 48), Color.white);
        }

        CreateUIText(row, "RowLabel", label, new Vector2(-190, 0), new Vector2(180, 50), 26, Color.white, TextAlignmentOptions.Left);

        // Slider Track & Handle
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sRt = sliderObj.AddComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(45, 0);
        sRt.sizeDelta = new Vector2(310, 46);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;

        Image trackBg = CreateUIImage(sliderObj, "Background", Vector2.zero, new Vector2(310, 26), new Color(0.08f, 0.1f, 0.16f, 1f));
        if (s_frameBlue != null)
        {
            trackBg.sprite = s_frameBlue;
            trackBg.type = Image.Type.Sliced;
            trackBg.color = new Color(0.24f, 0.28f, 0.42f, 1f);
        }

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.22f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.78f);
        fillAreaRt.offsetMin = new Vector2(6f, 0f);
        fillAreaRt.offsetMax = new Vector2(-6f, 0f);

        Image fillImg = CreateUIImage(fillArea, "Fill", Vector2.zero, Vector2.zero, new Color(0.2f, 0.72f, 1f, 1f));
        if (fillSprite != null)
        {
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = Color.white;
        }
        slider.fillRect = fillImg.rectTransform;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(18f, 0f);
        handleAreaRt.offsetMax = new Vector2(-18f, 0f);

        Image handleImg = CreateUIImage(handleArea, "Handle", Vector2.zero, new Vector2(44, 44), Color.white);
        handleImg.sprite = GetCircleSprite();
        slider.handleRect = handleImg.rectTransform;
        slider.targetGraphic = handleImg;

        percentText = CreateUIText(row, "PercentText", "100%", new Vector2(280, 0), new Vector2(120, 48), 26, new Color(1.0f, 0.9f, 0.35f), TextAlignmentOptions.Right);
        return slider;
    }

    private static Toggle CreateSettingsMuteRow(GameObject window, string rowName, Vector2 pos, out TMP_Text stateText)
    {
        GameObject row = CreateUICard(window, rowName, pos, new Vector2(740, 108), new Color(0.11f, 0.14f, 0.22f, 0.95f), s_framePurple);

        if (s_iconClose != null)
        {
            CreateUISpriteImage(row, "MuteIcon", s_iconClose, new Vector2(-315, 0), new Vector2(40, 40), new Color(1f, 0.5f, 0.45f));
        }
        CreateUIText(row, "RowLabel", "MUTE ALL AUDIO", new Vector2(-115, 0), new Vector2(340, 50), 27, Color.white, TextAlignmentOptions.Left);

        GameObject toggleObj = new GameObject("MuteToggle");
        toggleObj.transform.SetParent(row.transform, false);
        RectTransform tRt = toggleObj.AddComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(215, 0);
        tRt.sizeDelta = new Vector2(220, 64);

        Image toggleBg = toggleObj.AddComponent<Image>();
        if (s_btnGreen != null)
        {
            toggleBg.sprite = s_btnGreen;
            toggleBg.type = Image.Type.Sliced;
            toggleBg.color = Color.white;
        }
        else
        {
            toggleBg.color = new Color(0.18f, 0.72f, 0.44f);
        }

        Image activeMark = CreateUIImage(toggleObj, "MutedGraphic", Vector2.zero, new Vector2(220, 64), new Color(0.9f, 0.24f, 0.24f));
        if (s_btnRed != null)
        {
            activeMark.sprite = s_btnRed;
            activeMark.type = Image.Type.Sliced;
            activeMark.color = Color.white;
        }

        stateText = CreateUIText(toggleObj, "MuteStateText", "SOUND ON", Vector2.zero, new Vector2(200, 56), 24, Color.white, TextAlignmentOptions.Center);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        toggle.graphic = activeMark;
        toggle.isOn = false;

        return toggle;
    }

    private static void BuildGenericPopupPanel(GameObject parent)
    {
        GenericPopupPanel panel = EnsurePanel<GenericPopupPanel>(parent);
        ClearChildren(panel.gameObject);

        CreateUIImage(panel.gameObject, "BackgroundDim", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.78f));
        GameObject window = CreateUICard(panel.gameObject, "WindowBox", Vector2.zero, new Vector2(780, 540), new Color(0.16f, 0.19f, 0.28f, 1f), s_frameBlue);

        GameObject banner = CreateUICard(window, "HeaderRibbon", new Vector2(0, 195), new Vector2(560, 88), new Color(0.2f, 0.45f, 0.8f), s_bannerBlue != null ? s_bannerBlue : s_frameCyan);
        TMP_Text title = CreateUIText(banner, "TitleText", "ALERT", Vector2.zero, new Vector2(520, 76), 40, Color.white, TextAlignmentOptions.Center);

        TMP_Text message = CreateUIText(window, "MessageText", "Popup notification message", new Vector2(0, 32), new Vector2(680, 160), 28, Color.white, TextAlignmentOptions.Center);

        Button confirmBtn = CreateUIButton(window, "ConfirmButton", "OK", new Vector2(-155, -165), new Vector2(250, 84), new Color(0.18f, 0.8f, 0.44f), Color.white, s_btnGreen);
        TMP_Text confirmTxt = confirmBtn.GetComponentInChildren<TMP_Text>();

        Button cancelBtn = CreateUIButton(window, "CancelButton", "CANCEL", new Vector2(155, -165), new Vector2(250, 84), new Color(0.91f, 0.3f, 0.24f), Color.white, s_btnRed);
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

    private static void SafeSetArrayProperty(SerializedObject so, string propertyName, UnityEngine.Object[] items)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null && prop.isArray && items != null)
        {
            prop.ClearArray();
            for (int i = 0; i < items.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
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

    private static GameObject CreateDecorBall(GameObject parent, string name, Vector2 pos, float diameter, Color color, bool useSlimeMascot = false)
    {
        GameObject ballObj = new GameObject(name);
        ballObj.transform.SetParent(parent.transform, false);

        RectTransform rect = ballObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(diameter, diameter);

        Image img = ballObj.AddComponent<Image>();
        img.sprite = (useSlimeMascot && s_slimeBody != null) ? s_slimeBody : GetCircleSprite();
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;

        if (useSlimeMascot && s_slimeFace != null)
        {
            Image faceImg = CreateUISpriteImage(ballObj, "SlimeFace", s_slimeFace, new Vector2(0f, -diameter * 0.04f), new Vector2(diameter * 0.64f, diameter * 0.64f), Color.white);
            faceImg.raycastTarget = false;
        }
        else
        {
            GameObject shineObj = new GameObject("Shine");
            shineObj.transform.SetParent(ballObj.transform, false);
            RectTransform shineRect = shineObj.AddComponent<RectTransform>();
            float shineSize = diameter * 0.28f;
            shineRect.anchoredPosition = new Vector2(-diameter * 0.18f, diameter * 0.18f);
            shineRect.sizeDelta = new Vector2(shineSize, shineSize);

            Image shineImg = shineObj.AddComponent<Image>();
            shineImg.sprite = GetCircleSprite();
            shineImg.color = new Color(1f, 1f, 1f, 0.65f);
            shineImg.raycastTarget = false;
        }

        return ballObj;
    }

    #region UGUI Helper Methods

    private static Image CreateUISpriteImage(GameObject parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color tint)
    {
        Image img = CreateUIImage(parent, name, pos, size, tint);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static void AddButtonIcon(Button btn, Sprite icon, Vector2 iconPos, Vector2 iconSize, Color tint, float shiftTextX = 0f)
    {
        if (btn == null || icon == null) return;
        CreateUISpriteImage(btn.gameObject, "Icon", icon, iconPos, iconSize, tint);

        if (Mathf.Abs(shiftTextX) > 0.1f)
        {
            Transform txtTf = btn.transform.Find("Text");
            if (txtTf != null)
            {
                RectTransform rt = txtTf.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(shiftTextX, rt.anchoredPosition.y);
                    rt.sizeDelta = new Vector2(Mathf.Max(60f, rt.sizeDelta.x - Mathf.Abs(shiftTextX) * 1.4f), rt.sizeDelta.y);
                }
            }
        }
    }

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
        if (s_lilitaFont == null) s_lilitaFont = EnsureLilitaOneFontAsset();
        if (s_lilitaFont != null) text.font = s_lilitaFont;
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
