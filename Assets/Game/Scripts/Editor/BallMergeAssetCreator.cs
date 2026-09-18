#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to generate default assets, sample Ball Kits, environment Themes, and automatically setup the game scene.
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

        // 4. Generate Visual Environment Themes (Independent from Kits)
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

        // Setup ThemeManager (Independent from BallKits)
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

        // Apply theme visual update
        tm.ApplyCurrentTheme();

        // Mark scene dirty so changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[BallMergeAssetCreator] Complete game scene with BallKitManager & ThemeManager setup successfully!");
    }
}
#endif
