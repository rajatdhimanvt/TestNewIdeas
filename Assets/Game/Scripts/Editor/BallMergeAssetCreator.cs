#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
        // 1. Ensure assets exist
        GenerateDefaultAssets();
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

    private static void SetupUICanvas()
    {
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
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("loadingSlider").objectReferenceValue = slider;
        so.FindProperty("progressText").objectReferenceValue = progressTxt;
        so.ApplyModifiedProperties();
    }

    #region Panel Visual Builders

    private static void BuildMainMenuPanel(GameObject parent)
    {
        MainMenuPanel panel = EnsurePanel<MainMenuPanel>(parent);
        ClearChildren(panel.gameObject);

        // Dark Background
        CreateUIImage(panel.gameObject, "Background", Vector2.zero, new Vector2(1080, 1920), new Color(0.08f, 0.09f, 0.14f, 1f));

        // 1. Header Card Box (Branding, Highscore & Coin Counter)
        GameObject headerCard = CreateUICard(panel.gameObject, "HeaderCard", new Vector2(0, 680), new Vector2(940, 240), new Color(0.12f, 0.14f, 0.22f, 0.95f));

        TMP_Text title = CreateUIText(headerCard, "TitleText", "BALL MERGE 2D", new Vector2(0, 50), new Vector2(900, 90), 64, new Color(0.95f, 0.85f, 0.2f), TextAlignmentOptions.Center);

        // HighScore Pill Badge
        GameObject hsBadge = CreateUICard(headerCard, "HighScoreBadge", new Vector2(-220, -45), new Vector2(400, 55), new Color(0.22f, 0.18f, 0.1f, 0.95f));
        TMP_Text highScore = CreateUIText(hsBadge, "HighScoreText", "BEST SCORE: 0", Vector2.zero, new Vector2(400, 55), 24, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);

        // Coin Counter Badge
        GameObject coinBadge = CreateUICard(headerCard, "CoinBadge", new Vector2(220, -45), new Vector2(400, 55), new Color(0.18f, 0.16f, 0.08f, 0.95f));
        TMP_Text coinBalanceText = CreateUIText(coinBadge, "CoinBalanceText", "🪙 0", Vector2.zero, new Vector2(400, 55), 26, new Color(1.0f, 0.88f, 0.3f), TextAlignmentOptions.Center);

        // 2. Daily Rewards Banner Card (Y = 410)
        GameObject dailyCard = CreateUICard(panel.gameObject, "DailyCard", new Vector2(0, 410), new Vector2(940, 110), new Color(0.22f, 0.16f, 0.08f, 0.95f));
        Button dailyRewardButton = CreateUIButton(dailyCard, "DailyRewardButton", "🎁 DAILY REWARDS & SPIN WHEEL", Vector2.zero, new Vector2(940, 110), new Color(0.95f, 0.65f, 0.1f), Color.white);

        // 3. Play Mode Card 1: Endless Mode Card (Y = 220)
        GameObject endlessCard = CreateUICard(panel.gameObject, "EndlessModeCard", new Vector2(0, 220), new Vector2(940, 200), new Color(0.1f, 0.22f, 0.18f, 0.95f));

        CreateUIText(endlessCard, "ModeTitle", "ENDLESS MODE", new Vector2(-160, 30), new Vector2(550, 50), 36, new Color(0.18f, 0.8f, 0.44f), TextAlignmentOptions.Left);
        CreateUIText(endlessCard, "ModeSubtitle", "Unlimited drops • Infinite Highscore Chase", new Vector2(-160, -20), new Vector2(550, 45), 20, new Color(0.7f, 0.85f, 0.75f), TextAlignmentOptions.Left);

        Button playEndless = CreateUIButton(endlessCard, "PlayEndlessButton", "PLAY NOW", new Vector2(270, 0), new Vector2(300, 85), new Color(0.18f, 0.8f, 0.44f), Color.white);

        // 4. Play Mode Card 2: Level 1 Campaign Card (Y = -30)
        GameObject levelCard = CreateUICard(panel.gameObject, "LevelModeCard", new Vector2(0, -30), new Vector2(940, 200), new Color(0.1f, 0.18f, 0.28f, 0.95f));

        CreateUIText(levelCard, "ModeTitle", "50 CAMPAIGN LEVELS", new Vector2(-180, 30), new Vector2(500, 50), 34, new Color(0.2f, 0.6f, 0.86f), TextAlignmentOptions.Left);
        CreateUIText(levelCard, "ModeSubtitle", "Target scores • Star Ratings • 50+ Levels", new Vector2(-180, -20), new Vector2(500, 45), 20, new Color(0.7f, 0.85f, 1.0f), TextAlignmentOptions.Left);

        Button levelMapButton = CreateUIButton(levelCard, "LevelMapButton", "🗺 MAP", new Vector2(165, 0), new Vector2(170, 85), new Color(0.2f, 0.6f, 0.86f), Color.white);
        Button playLevel1 = CreateUIButton(levelCard, "PlayLevel1Button", "PLAY 1", new Vector2(350, 0), new Vector2(150, 85), new Color(0.18f, 0.8f, 0.44f), Color.white);

        // 5. Customization Shop Cards Row (Y = -270)
        GameObject kitsCard = CreateUICard(panel.gameObject, "KitsCard", new Vector2(-245, -270), new Vector2(450, 200), new Color(0.2f, 0.14f, 0.26f, 0.95f));
        CreateUIText(kitsCard, "KitsTitle", "BALL KITS", new Vector2(0, 45), new Vector2(400, 50), 32, new Color(0.75f, 0.45f, 0.9f), TextAlignmentOptions.Center);
        CreateUIText(kitsCard, "KitsSub", "Fruits • Ocean • Sky", new Vector2(0, 5), new Vector2(400, 35), 20, Color.gray, TextAlignmentOptions.Center);
        Button ballKits = CreateUIButton(kitsCard, "BallKitsButton", "OPEN SHOP", new Vector2(0, -50), new Vector2(380, 60), new Color(0.61f, 0.35f, 0.71f), Color.white);

        GameObject themesCard = CreateUICard(panel.gameObject, "ThemesCard", new Vector2(245, -270), new Vector2(450, 200), new Color(0.26f, 0.16f, 0.12f, 0.95f));
        CreateUIText(themesCard, "ThemesTitle", "THEMES", new Vector2(0, 45), new Vector2(400, 50), 32, new Color(0.95f, 0.6f, 0.2f), TextAlignmentOptions.Center);
        CreateUIText(themesCard, "ThemesSub", "Visual Environments", new Vector2(0, 5), new Vector2(400, 35), 20, Color.gray, TextAlignmentOptions.Center);
        Button themes = CreateUIButton(themesCard, "ThemesButton", "OPEN SHOP", new Vector2(0, -50), new Vector2(380, 60), new Color(0.9f, 0.49f, 0.13f), Color.white);

        // 6. Bottom Navigation Bar (Y = -520)
        Button settings = CreateUIButton(panel.gameObject, "SettingsButton", "⚙ SETTINGS", new Vector2(-245, -520), new Vector2(450, 85), new Color(0.2f, 0.25f, 0.35f), Color.white);
        Button quit = CreateUIButton(panel.gameObject, "QuitButton", "✖ QUIT", new Vector2(245, -520), new Vector2(450, 85), new Color(0.85f, 0.25f, 0.2f), Color.white);

        // Auto-wire SerializedObject
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("playEndlessButton").objectReferenceValue = playEndless;
        so.FindProperty("playLevel1Button").objectReferenceValue = playLevel1;
        so.FindProperty("levelMapButton").objectReferenceValue = levelMapButton;
        so.FindProperty("dailyRewardButton").objectReferenceValue = dailyRewardButton;
        so.FindProperty("ballKitsButton").objectReferenceValue = ballKits;
        so.FindProperty("themesButton").objectReferenceValue = themes;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("highScoreText").objectReferenceValue = highScore;
        so.FindProperty("coinBalanceText").objectReferenceValue = coinBalanceText;
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

        // Grid Container Box
        GameObject gridCard = CreateUICard(panel.gameObject, "GridCard", new Vector2(0, 50), new Vector2(940, 1100), new Color(0.12f, 0.14f, 0.2f, 0.95f));

        // ScrollRect & Content
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(gridCard.transform, false);
        RectTransform scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        scrollRectTransform.anchoredPosition = Vector2.zero;
        scrollRectTransform.sizeDelta = new Vector2(900, 1060);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(900, 2500);

        scrollRect.content = contentRect;

        GridLayoutGroup gridGroup = contentObj.AddComponent<GridLayoutGroup>();
        gridGroup.cellSize = new Vector2(190, 190);
        gridGroup.spacing = new Vector2(25, 25);
        gridGroup.padding = new RectOffset(30, 30, 30, 30);
        gridGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridGroup.constraintCount = 4;

        // Bottom Back Button
        Button backButton = CreateUIButton(panel.gameObject, "BackButton", "⬅ BACK TO MENU", new Vector2(0, -600), new Vector2(500, 95), new Color(0.25f, 0.3f, 0.4f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("totalStarsText").objectReferenceValue = totalStarsText;
        so.FindProperty("coinBalanceText").objectReferenceValue = coinBalanceText;
        so.FindProperty("levelGridContainer").objectReferenceValue = contentObj.transform;
        so.FindProperty("backButton").objectReferenceValue = backButton;
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
        so.FindProperty("wheelTransform").objectReferenceValue = wheelContainer.transform;
        so.FindProperty("spinButton").objectReferenceValue = spinButton;
        so.FindProperty("spinButtonText").objectReferenceValue = spinButtonText;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("streakDayText").objectReferenceValue = streakDayText;
        so.FindProperty("claimStreakButton").objectReferenceValue = claimStreakButton;
        so.FindProperty("claimStreakText").objectReferenceValue = claimStreakText;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
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

        // Next Ball Preview
        GameObject nextBallObj = CreateUICard(panel.gameObject, "NextBallContainer", new Vector2(-420, 690), new Vector2(160, 120), new Color(0, 0, 0, 0));

        CreateUIText(nextBallObj, "NextLabel", "NEXT:", new Vector2(0, 35), new Vector2(160, 40), 24, Color.gray, TextAlignmentOptions.Center);
        Image nextBallImg = CreateUIImage(nextBallObj, "NextBallImage", new Vector2(0, -15), new Vector2(60, 60), new Color(0.95f, 0.2f, 0.25f));
        TMP_Text nextBallName = CreateUIText(nextBallObj, "NextBallNameText", "Cherry", new Vector2(0, -55), new Vector2(160, 30), 20, Color.white, TextAlignmentOptions.Center);

        // Pause Button
        Button pause = CreateUIButton(panel.gameObject, "PauseButton", "||", new Vector2(450, 690), new Vector2(90, 90), new Color(0.2f, 0.25f, 0.35f), Color.white);

        // Auto-wire SerializedObject
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("highScoreText").objectReferenceValue = highScore;
        so.FindProperty("modeTitleText").objectReferenceValue = modeTitle;
        so.FindProperty("dropsContainer").objectReferenceValue = dropsObj;
        so.FindProperty("dropsText").objectReferenceValue = drops;
        so.FindProperty("nextBallNameText").objectReferenceValue = nextBallName;
        so.FindProperty("nextBallImage").objectReferenceValue = nextBallImg;
        so.FindProperty("pauseButton").objectReferenceValue = pause;
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
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
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
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("finalScoreText").objectReferenceValue = finalScore;
        so.FindProperty("highScoreText").objectReferenceValue = highScore;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
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

        Button nextLevel = CreateUIButton(window, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -60), new Vector2(450, 95), new Color(0.18f, 0.8f, 0.44f), Color.white);
        Button replay = CreateUIButton(window, "ReplayButton", "REPLAY", new Vector2(0, -170), new Vector2(450, 95), new Color(0.2f, 0.6f, 0.86f), Color.white);
        Button mainMenu = CreateUIButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0, -280), new Vector2(450, 95), new Color(0.91f, 0.3f, 0.24f), Color.white);

        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("starsText").objectReferenceValue = starsText;
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("dropsUsedText").objectReferenceValue = dropsUsed;
        so.FindProperty("nextLevelButton").objectReferenceValue = nextLevel;
        so.FindProperty("replayButton").objectReferenceValue = replay;
        so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
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
        so.FindProperty("currentKitNameText").objectReferenceValue = currentKit;
        so.FindProperty("coinBalanceText").objectReferenceValue = coinBalanceText;
        so.FindProperty("classicKitButton").objectReferenceValue = classicBtn;
        so.FindProperty("oceanKitButton").objectReferenceValue = oceanBtn;
        so.FindProperty("skyKitButton").objectReferenceValue = skyBtn;
        so.FindProperty("inspectClassicButton").objectReferenceValue = inspectClassic;
        so.FindProperty("inspectOceanButton").objectReferenceValue = inspectOcean;
        so.FindProperty("inspectSkyButton").objectReferenceValue = inspectSky;
        so.FindProperty("backButton").objectReferenceValue = backBtn;
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
        so.FindProperty("kitTitleText").objectReferenceValue = title;
        so.FindProperty("kitSubtitleText").objectReferenceValue = subtitle;
        so.FindProperty("tierListContainer").objectReferenceValue = containerObj.transform;
        so.FindProperty("equipButton").objectReferenceValue = equipBtn;
        so.FindProperty("equipButtonText").objectReferenceValue = equipTxt;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
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
        so.FindProperty("currentThemeNameText").objectReferenceValue = currentTheme;
        so.FindProperty("coinBalanceText").objectReferenceValue = coinBalanceText;
        so.FindProperty("darkThemeButton").objectReferenceValue = darkBtn;
        so.FindProperty("oceanThemeButton").objectReferenceValue = oceanBtn;
        so.FindProperty("skyThemeButton").objectReferenceValue = skyBtn;
        so.FindProperty("sunsetThemeButton").objectReferenceValue = sunsetBtn;
        so.FindProperty("inspectDarkButton").objectReferenceValue = inspectDark;
        so.FindProperty("inspectOceanButton").objectReferenceValue = inspectOcean;
        so.FindProperty("inspectSkyButton").objectReferenceValue = inspectSky;
        so.FindProperty("inspectSunsetButton").objectReferenceValue = inspectSunset;
        so.FindProperty("backButton").objectReferenceValue = backBtn;
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
        so.FindProperty("themeTitleText").objectReferenceValue = title;
        so.FindProperty("themeSubtitleText").objectReferenceValue = subtitle;
        so.FindProperty("previewBackgroundBox").objectReferenceValue = fieldObj.GetComponent<Image>();
        so.FindProperty("previewWallLeft").objectReferenceValue = wallLeft;
        so.FindProperty("previewWallRight").objectReferenceValue = wallRight;
        so.FindProperty("previewWallBottom").objectReferenceValue = wallBottom;
        so.FindProperty("previewDangerLine").objectReferenceValue = dangerLine;
        so.FindProperty("bgColorText").objectReferenceValue = bgTxt;
        so.FindProperty("wallColorText").objectReferenceValue = wallTxt;
        so.FindProperty("dangerColorText").objectReferenceValue = dangerTxt;
        so.FindProperty("applyButton").objectReferenceValue = applyBtn;
        so.FindProperty("applyButtonText").objectReferenceValue = applyTxt;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
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
        so.FindProperty("backButton").objectReferenceValue = backBtn;
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
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
        so.FindProperty("confirmButtonText").objectReferenceValue = confirmTxt;
        so.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
        so.FindProperty("cancelButtonText").objectReferenceValue = cancelTxt;
        so.ApplyModifiedProperties();
    }

    #endregion

    #region UGUI Helper Methods

    private static GameObject CreateUICard(GameObject parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = color;
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

    private static Button CreateUIButton(GameObject parent, string name, string labelText, Vector2 pos, Vector2 size, Color bgColor, Color textColor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.15f;
        cb.pressedColor = bgColor * 0.85f;
        btn.colors = cb;

        // Label child text
        CreateUIText(obj, "Text", labelText, Vector2.zero, size, size.y * 0.38f, textColor, TextAlignmentOptions.Center);

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
