#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeometryBoxMaze.Editor
{
    /// <summary>
    /// Complete one-click scene, prefab, sprite, and 5-level asset generator for
    /// the standalone Geometry Box Tilt Maze mini-game under Assets/GeometryBoxMaze/.
    /// Menu: Tools -> Geometry Box Maze -> Build Scene & Assets.
    /// </summary>
    [InitializeOnLoad]
    public static class GeometryBoxMazeSetup
    {
        public const string ROOT_FOLDER = "Assets/GeometryBoxMaze";
        public const string SCENES_FOLDER = "Assets/GeometryBoxMaze/Scenes";
        public const string PREFABS_FOLDER = "Assets/GeometryBoxMaze/Prefabs";
        public const string DATA_FOLDER = "Assets/GeometryBoxMaze/Data";
        public const string SCENE_PATH = "Assets/GeometryBoxMaze/Scenes/GeometryBoxMaze.unity";
        public const string BALL_PREFAB_PATH = "Assets/GeometryBoxMaze/Prefabs/GeometryBall.prefab";
        public const string CATALOG_PATH = "Assets/GeometryBoxMaze/Data/GeometryMazeLevelCatalog.asset";

        static GeometryBoxMazeSetup()
        {
            EditorApplication.delayCall += AutoBuildInitialAssetsAndSceneOnce;
        }

        private static void AutoBuildInitialAssetsAndSceneOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool("GeometryBoxMaze_InitialBuildV4", false)) return;
            SessionState.SetBool("GeometryBoxMaze_InitialBuildV4", true);

            EnsureBuildSettingsScenes();

            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.path;

            bool needsMazeUpgrade = !File.Exists("Assets/GeometryBoxMaze/Sprites/Desk_Wood_Bg.png")
                                    || !File.Exists("Assets/GeometryBoxMaze/Sprites/Eraser_Bumper.png")
                                    || !File.Exists("Assets/GeometryBoxMaze/Sprites/Gold_Pin.png");
            if (!needsMazeUpgrade && File.Exists(CATALOG_PATH))
            {
                GeometryMazeLevelCatalog existingCat = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(CATALOG_PATH);
                GeometryMazeLevelData lvl1 = existingCat != null ? existingCat.GetLevelByIndex(0) : null;
                if (lvl1 == null || lvl1.walls == null || lvl1.walls.Count < 6)
                {
                    needsMazeUpgrade = true;
                }
            }
            if (!needsMazeUpgrade && File.Exists(SCENE_PATH))
            {
                string sceneRaw = File.ReadAllText(SCENE_PATH);
                if (!sceneRaw.Contains("mainSwitchGameButton") || sceneRaw.Contains("mainSwitchGameButton: {fileID: 0}"))
                {
                    needsMazeUpgrade = true;
                }
            }

            if (needsMazeUpgrade || !File.Exists(SCENE_PATH) || !File.Exists(CATALOG_PATH) || !File.Exists(BALL_PREFAB_PATH))
            {
                if (previousScene.isDirty)
                {
                    Debug.LogWarning("[GeometryBoxMazeSetup] Active scene has unsaved changes; run 'Tools -> Geometry Box Maze -> 1. Build Scene & All Assets' to apply the upgrade.");
                    return;
                }
                bool shouldRestorePrevious = !string.IsNullOrEmpty(previousPath) && previousPath != SCENE_PATH;
                BuildSceneAndAllAssets(openResultScene: !shouldRestorePrevious);
                if (shouldRestorePrevious)
                {
                    EditorSceneManager.OpenScene(previousPath);
                }
                return;
            }

            // Repair null levelCatalog reference on existing GeometryBoxMaze.unity if needed
            if (previousPath == SCENE_PATH && !previousScene.isDirty)
            {
                RepairOpenSceneCatalogIfNeeded(previousScene);
            }
            else if (File.ReadAllText(SCENE_PATH).Contains("levelCatalog: {fileID: 0}") && !previousScene.isDirty)
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
                RepairOpenSceneCatalogIfNeeded(SceneManager.GetActiveScene());
                if (!string.IsNullOrEmpty(previousPath) && previousPath != SCENE_PATH)
                {
                    EditorSceneManager.OpenScene(previousPath);
                }
            }
        }

        private static void EnsureBuildSettingsScenes()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;
            string[] required = { "Assets/Game/Scene/GameTest.unity", SCENE_PATH };
            foreach (string path in required)
            {
                if (!File.Exists(path)) continue;
                int idx = scenes.FindIndex(s => s.path == path);
                if (idx < 0)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    changed = true;
                }
                else if (!scenes[idx].enabled)
                {
                    scenes[idx].enabled = true;
                    changed = true;
                }
            }
            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void RepairOpenSceneCatalogIfNeeded(Scene scene)
        {
            GeometryMazeManager mgr = Object.FindAnyObjectByType<GeometryMazeManager>(FindObjectsInactive.Include);
            if (mgr == null) return;

            SerializedObject mgrSo = new SerializedObject(mgr);
            SerializedProperty catProp = mgrSo.FindProperty("levelCatalog");
            if (catProp != null && catProp.objectReferenceValue == null)
            {
                GeometryMazeLevelCatalog catalog = EnsureFiveLevelsAndCatalog();
                catProp.objectReferenceValue = catalog;
                mgrSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mgr);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[GeometryBoxMazeSetup] Repaired LevelCatalog reference in GeometryBoxMaze.unity!");
            }
        }

        [MenuItem("Tools/Geometry Box Maze/1. Build Scene & All Assets")]
        public static void BuildMenuAction()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && active.path != SCENE_PATH)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
            }
            BuildSceneAndAllAssets(openResultScene: true);
        }

        [MenuItem("Tools/Geometry Box Maze/2. Open GeometryBoxMaze Scene")]
        public static void OpenSceneAction()
        {
            if (!File.Exists(SCENE_PATH))
            {
                BuildMenuAction();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }
        }

        public static void BuildSceneAndAllAssets(bool openResultScene)
        {
            EnsureFolder(ROOT_FOLDER);
            EnsureFolder(SCENES_FOLDER);
            EnsureFolder(PREFABS_FOLDER);
            EnsureFolder(DATA_FOLDER);
            EnsureBuildSettingsScenes();

            // 1. Generate nostalgic geometry-box sprites
            GeometryBoxSpriteGenerator.GenerateAllSprites();

            Sprite ballSprite = GeometryBoxSpriteGenerator.LoadSprite("Chrome_Steel_Ball.png");
            Sprite ballGlintSprite = GeometryBoxSpriteGenerator.LoadSprite("Chrome_Ball_Glint.png");
            Sprite shadowSprite = GeometryBoxSpriteGenerator.LoadSprite("Ball_Shadow.png");
            Sprite deskWoodSprite = GeometryBoxSpriteGenerator.LoadSprite("Desk_Wood_Bg.png");
            Sprite traySprite = GeometryBoxSpriteGenerator.LoadSprite("Tray_Plastic_Base.png");
            Sprite wallSprite = GeometryBoxSpriteGenerator.LoadSprite("Maze_Wall_Segment.png");
            Sprite holeSprite = GeometryBoxSpriteGenerator.LoadSprite("Hole_Pit.png");
            Sprite goalSprite = GeometryBoxSpriteGenerator.LoadSprite("Goal_Pocket.png");
            Sprite obstacleSprite = GeometryBoxSpriteGenerator.LoadSprite("Obstacle_Bar.png");
            Sprite eraserSprite = GeometryBoxSpriteGenerator.LoadSprite("Eraser_Bumper.png");
            Sprite goldPinSprite = GeometryBoxSpriteGenerator.LoadSprite("Gold_Pin.png");
            Sprite switchSprite = GeometryBoxSpriteGenerator.LoadSprite("Switch_Plate.png");
            Sprite lockedGateSprite = GeometryBoxSpriteGenerator.LoadSprite("Locked_Gate.png");
            Sprite hingeSprite = GeometryBoxSpriteGenerator.LoadSprite("Box_Hinge.png");
            Sprite claspSprite = GeometryBoxSpriteGenerator.LoadSprite("Box_Clasp.png");
            Sprite protractorSprite = GeometryBoxSpriteGenerator.LoadSprite("Protractor_Watermark.png");
            Sprite rulerSprite = GeometryBoxSpriteGenerator.LoadSprite("Ruler_Edge.png");
            Sprite starSprite = GeometryBoxSpriteGenerator.LoadSprite("Star_Icon.png");
            Sprite circleSprite = GeometryBoxSpriteGenerator.LoadSprite("Circle_Soft.png");
            Sprite uiCardSprite = GeometryBoxSpriteGenerator.LoadSprite("UI_Card_9Slice.png");
            Sprite uiBtnSprite = GeometryBoxSpriteGenerator.LoadSprite("UI_Button_9Slice.png");

            // 2. Create Ball Prefab & 5 Progressive Levels + Catalog
            GeometryMazeBallController ballPrefab = EnsureBallPrefab(ballSprite, ballGlintSprite, shadowSprite, circleSprite);
            GeometryMazeLevelCatalog catalog = EnsureFiveLevelsAndCatalog();

            // 3. Create fresh standalone GeometryBoxMaze.unity scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (portrait framing optimized for 1080x1920 + top/bottom HUD clearance)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8.4f;
            cam.transform.position = new Vector3(0f, 0.25f, -10f);
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.18f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();

            // EventSystem
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();

            // Root Systems
            GameObject systemsObj = new GameObject("[GeometryMazeSystems]");
            GeometryMazeAudio audioComp = systemsObj.AddComponent<GeometryMazeAudio>();
            GeometryMazeTiltInput tiltInput = systemsObj.AddComponent<GeometryMazeTiltInput>();
            GeometryMazeManager manager = systemsObj.AddComponent<GeometryMazeManager>();

            // Board Root
            GameObject boardObj = new GameObject("[GeometryMazeBoard]");
            GeometryMazeBoardBuilder boardBuilder = boardObj.AddComponent<GeometryMazeBoardBuilder>();
            boardBuilder.SetSprites(
                deskWoodSprite, traySprite, wallSprite, holeSprite, goalSprite, obstacleSprite,
                eraserSprite, goldPinSprite, switchSprite, lockedGateSprite, hingeSprite, claspSprite,
                protractorSprite, rulerSprite, circleSprite);
            boardBuilder.SetBallPrefab(ballPrefab);

            SerializedObject bbSo = new SerializedObject(boardBuilder);
            SetProp(bbSo, "tiltInput", tiltInput);
            bbSo.ApplyModifiedPropertiesWithoutUndo();

            // UI Canvas & Panels
            GeometryMazeUI mazeUI = BuildMazeCanvas(uiCardSprite, uiBtnSprite, starSprite, circleSprite);

            // Reload persisted catalog after NewScene so cross-asset GUID reference never serializes null
            if (catalog == null)
            {
                catalog = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(CATALOG_PATH);
            }

            // Wire GeometryMazeManager references
            SerializedObject mgrSo = new SerializedObject(manager);
            SetProp(mgrSo, "levelCatalog", catalog);
            SetProp(mgrSo, "boardBuilder", boardBuilder);
            SetProp(mgrSo, "tiltInput", tiltInput);
            SetProp(mgrSo, "mazeUI", mazeUI);
            mgrSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (openResultScene)
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }

            Debug.Log($"[GeometryBoxMazeSetup] Created Geometry Box Tilt Maze scene at '{SCENE_PATH}' with 5 levels & tilt controls!");
        }

        private static GeometryMazeBallController EnsureBallPrefab(Sprite ballSprite, Sprite glintSprite, Sprite shadowSprite, Sprite rippleSprite)
        {
            GameObject root = new GameObject("GeometryBall");

            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = ballSprite;
            sr.sortingOrder = 8;

            GameObject shadowObj = new GameObject("BallShadow");
            shadowObj.transform.SetParent(root.transform, false);
            shadowObj.transform.localPosition = new Vector3(0.055f, -0.07f, 0f);
            shadowObj.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
            SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
            shadowSr.sprite = shadowSprite;
            shadowSr.sortingOrder = 5;

            GameObject glintObj = new GameObject("BallGlint");
            glintObj.transform.SetParent(root.transform, false);
            glintObj.transform.localPosition = Vector3.zero;
            glintObj.transform.localScale = new Vector3(1.24f, 1.24f, 1f);
            SpriteRenderer glintSr = glintObj.AddComponent<SpriteRenderer>();
            glintSr.sprite = glintSprite;
            glintSr.sortingOrder = 9;

            root.AddComponent<Rigidbody2D>();
            root.AddComponent<CircleCollider2D>();
            GeometryMazeBallController controller = root.AddComponent<GeometryMazeBallController>();

            SerializedObject so = new SerializedObject(controller);
            SetProp(so, "shadowTransform", shadowObj.transform);
            SetProp(so, "glintTransform", glintObj.transform);
            SetProp(so, "rippleSprite", rippleSprite);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, BALL_PREFAB_PATH);
            Object.DestroyImmediate(root);
            return savedPrefab.GetComponent<GeometryMazeBallController>();
        }

        private static GeometryMazeLevelCatalog EnsureFiveLevelsAndCatalog()
        {
            List<GeometryMazeLevelData> levelList = new List<GeometryMazeLevelData>
            {
                CreateGeneratedCampaignLevel(new GeometryMazeGenerator.GenerationConfig
                {
                    levelNumber = 1,
                    levelName = "Classic Box I",
                    difficultyLabel = "EASY",
                    modeLabel = "5x7 MAZE",
                    cols = 5,
                    rows = 7,
                    seed = 1104,
                    holeCount = 1,
                    pinCount = 3,
                    includeEraserBumper = false,
                    includeSwitchGate = false,
                    includeMovingSlider = false,
                    includeSpinner = false,
                    threeStarTime = 18f,
                    twoStarTime = 34f,
                    timeTrialLimit = 0f,
                    trayColor = new Color(0.79f, 0.91f, 0.98f, 1f),
                    outerFrameColor = new Color(0.18f, 0.47f, 0.76f, 1f),
                    wallColor = new Color(0.95f, 0.98f, 1.0f, 1f),
                }),

                CreateGeneratedCampaignLevel(new GeometryMazeGenerator.GenerationConfig
                {
                    levelNumber = 2,
                    levelName = "Eraser Labyrinth",
                    difficultyLabel = "EASY+",
                    modeLabel = "6x8 MAZE",
                    cols = 6,
                    rows = 8,
                    seed = 2048,
                    holeCount = 2,
                    pinCount = 3,
                    includeEraserBumper = true,
                    includeSwitchGate = false,
                    includeMovingSlider = false,
                    includeSpinner = false,
                    threeStarTime = 24f,
                    twoStarTime = 42f,
                    timeTrialLimit = 0f,
                    trayColor = new Color(0.78f, 0.96f, 0.89f, 1f),
                    outerFrameColor = new Color(0.12f, 0.56f, 0.42f, 1f),
                    wallColor = new Color(0.95f, 1.0f, 0.97f, 1f),
                }),

                CreateGeneratedCampaignLevel(new GeometryMazeGenerator.GenerationConfig
                {
                    levelNumber = 3,
                    levelName = "Switch Escape",
                    difficultyLabel = "MEDIUM",
                    modeLabel = "6x9 SWITCH",
                    cols = 6,
                    rows = 9,
                    seed = 3317,
                    holeCount = 2,
                    pinCount = 3,
                    includeEraserBumper = true,
                    includeSwitchGate = true,
                    includeMovingSlider = false,
                    includeSpinner = false,
                    threeStarTime = 28f,
                    twoStarTime = 50f,
                    timeTrialLimit = 0f,
                    trayColor = new Color(0.84f, 0.88f, 0.92f, 1f),
                    outerFrameColor = new Color(0.26f, 0.32f, 0.40f, 1f),
                    wallColor = new Color(0.96f, 0.97f, 0.99f, 1f),
                }),

                CreateGeneratedCampaignLevel(new GeometryMazeGenerator.GenerationConfig
                {
                    levelNumber = 4,
                    levelName = "Slider Rush",
                    difficultyLabel = "HARD",
                    modeLabel = "7x10 TRIAL",
                    cols = 7,
                    rows = 10,
                    seed = 4471,
                    holeCount = 2,
                    pinCount = 3,
                    includeEraserBumper = true,
                    includeSwitchGate = false,
                    includeMovingSlider = true,
                    includeSpinner = false,
                    threeStarTime = 30f,
                    twoStarTime = 48f,
                    timeTrialLimit = 65f,
                    trayColor = new Color(0.84f, 0.83f, 0.97f, 1f),
                    outerFrameColor = new Color(0.32f, 0.28f, 0.64f, 1f),
                    wallColor = new Color(0.96f, 0.96f, 1.0f, 1f),
                }),

                CreateGeneratedCampaignLevel(new GeometryMazeGenerator.GenerationConfig
                {
                    levelNumber = 5,
                    levelName = "Master Box",
                    difficultyLabel = "EXPERT",
                    modeLabel = "7x11 PRO",
                    cols = 7,
                    rows = 11,
                    seed = 5593,
                    holeCount = 3,
                    pinCount = 3,
                    includeEraserBumper = true,
                    includeSwitchGate = true,
                    includeMovingSlider = false,
                    includeSpinner = true,
                    threeStarTime = 36f,
                    twoStarTime = 62f,
                    timeTrialLimit = 0f,
                    trayColor = new Color(0.76f, 0.87f, 0.98f, 1f),
                    outerFrameColor = new Color(0.14f, 0.34f, 0.62f, 1f),
                    wallColor = new Color(0.98f, 0.98f, 1.0f, 1f),
                }),
            };

            GeometryMazeLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GeometryMazeLevelCatalog>();
                AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
            }
            catalog.levels = levelList;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(CATALOG_PATH, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(CATALOG_PATH);
        }

        private static GeometryMazeLevelData CreateGeneratedCampaignLevel(GeometryMazeGenerator.GenerationConfig cfg)
        {
            string assetPath = $"{DATA_FOLDER}/GeometryMazeLevel_{cfg.levelNumber:00}.asset";
            GeometryMazeLevelData asset = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<GeometryMazeLevelData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            GeometryMazeGenerator.PopulateLevelData(asset, cfg);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GeometryMazeUI BuildMazeCanvas(Sprite cardSprite, Sprite btnSprite, Sprite starSprite, Sprite circleSprite)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("BallMerge/UI/LilitaOne SDF");
            if (font == null) font = TMP_Settings.defaultFontAsset;

            GameObject canvasObj = new GameObject("[GeometryMaze_Canvas]");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GeometryMazeUI mazeUI = canvasObj.AddComponent<GeometryMazeUI>();

            // -------------------------------------------------------------
            // 1. MAIN MENU PANEL
            // -------------------------------------------------------------
            GameObject mainMenu = CreateFullScreenPanel(canvasObj.transform, "MainMenuPanel");
            CreateUIImage(mainMenu.transform, "DimOverlay", Vector2.zero, new Vector2(1080, 1920), null, new Color(0.04f, 0.07f, 0.14f, 0.72f), false);
            GameObject menuCard = CreateUICard(mainMenu.transform, "MenuWindow", new Vector2(0f, 20f), new Vector2(900f, 1100f), cardSprite, new Color(0.11f, 0.16f, 0.26f, 0.96f));
            CreateUIText(menuCard.transform, "BadgeTop", "CHILDHOOD NOSTALGIA EDITION", new Vector2(0f, 470f), new Vector2(800f, 42f), 22f, new Color(0.45f, 0.88f, 1f), TextAlignmentOptions.Center, font);
            CreateUIText(menuCard.transform, "Title", "GEOMETRY BOX\nTILT MAZE", new Vector2(0f, 355f), new Vector2(820f, 175f), 64f, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center, font);
            TMP_Text mainStarsTxt = CreateUIText(menuCard.transform, "TotalStarsText", "STARS COLLECTED: 0 / 15", new Vector2(0f, 224f), new Vector2(740f, 46f), 26f, new Color(0.88f, 0.94f, 1f), TextAlignmentOptions.Center, font);

            Button mainPlayBtn = CreateUIButton(menuCard.transform, "PlayButton", "PLAY LEVELS", new Vector2(0f, 108f), new Vector2(580f, 108f), btnSprite, new Color(0.18f, 0.78f, 0.44f), Color.white, 36f, font);
            Button mainLevelsBtn = CreateUIButton(menuCard.transform, "LevelsButton", "SELECT TRAY", new Vector2(-148f, -14f), new Vector2(276f, 88f), btnSprite, new Color(0.22f, 0.56f, 0.90f), Color.white, 24f, font);
            Button mainRandomBtn = CreateUIButton(menuCard.transform, "RandomMazeButton", "RANDOM MAZE", new Vector2(148f, -14f), new Vector2(276f, 88f), btnSprite, new Color(0.62f, 0.36f, 0.86f), Color.white, 24f, font);
            Button mainSwitchBtn = CreateUIButton(menuCard.transform, "SwitchGameButton", "SWITCH GAME: BALL MERGE", new Vector2(0f, -124f), new Vector2(580f, 84f), btnSprite, new Color(0.95f, 0.62f, 0.14f), Color.white, 25f, font);

            Slider menuSensSlider = CreateSensitivitySlider(menuCard.transform, "MenuSensitivityRow", new Vector2(0f, -264f), cardSprite, btnSprite, circleSprite, font, out TMP_Text menuSensTxt);
            CreateUIText(menuCard.transform, "BottomTip", "Mobile: Tilt phone like Gyroscope  |  PC: WASD / Arrows", new Vector2(0f, -388f), new Vector2(820f, 44f), 21f, new Color(0.70f, 0.80f, 0.95f), TextAlignmentOptions.Center, font);

            // -------------------------------------------------------------
            // 2. LEVEL SELECT PANEL
            // -------------------------------------------------------------
            GameObject levelSelect = CreateFullScreenPanel(canvasObj.transform, "LevelSelectPanel");
            CreateUIImage(levelSelect.transform, "DimOverlay", Vector2.zero, new Vector2(1080, 1920), null, new Color(0.05f, 0.08f, 0.14f, 0.95f), false);
            GameObject lsHeader = CreateUICard(levelSelect.transform, "HeaderCard", new Vector2(0f, 760f), new Vector2(920f, 150f), cardSprite, new Color(0.14f, 0.22f, 0.36f, 1f));
            CreateUIText(lsHeader.transform, "HeaderTitle", "SELECT GEOMETRY TRAY", new Vector2(0f, 0f), new Vector2(860f, 80f), 46f, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center, font);

            GameObject gridHost = CreateUICard(levelSelect.transform, "GridCard", new Vector2(0f, 15f), new Vector2(920f, 1210f), cardSprite, new Color(0.09f, 0.13f, 0.21f, 0.96f));
            ScrollRect lsScroll = gridHost.AddComponent<ScrollRect>();
            lsScroll.horizontal = false;
            lsScroll.vertical = true;
            gridHost.AddComponent<RectMask2D>();

            GameObject gridContainer = new GameObject("LevelsList");
            gridContainer.transform.SetParent(gridHost.transform, false);
            RectTransform glRt = gridContainer.AddComponent<RectTransform>();
            glRt.anchorMin = new Vector2(0f, 1f);
            glRt.anchorMax = new Vector2(1f, 1f);
            glRt.pivot = new Vector2(0.5f, 1f);
            glRt.anchoredPosition = Vector2.zero;
            glRt.sizeDelta = new Vector2(0f, 900f);
            VerticalLayoutGroup vlg = gridContainer.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 24, 24);
            vlg.spacing = 20f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = gridContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            lsScroll.content = glRt;

            Button lsBackBtn = CreateUIButton(levelSelect.transform, "BackButton", "BACK TO MENU", new Vector2(0f, -720f), new Vector2(460f, 96f), btnSprite, new Color(0.84f, 0.28f, 0.28f), Color.white, 28f, font);

            // -------------------------------------------------------------
            // 3. GAMEPLAY HUD PANEL (clean top & bottom bars framing central maze tray)
            // -------------------------------------------------------------
            GameObject hud = CreateFullScreenPanel(canvasObj.transform, "GameplayHUDPanel");
            GameObject topBar = CreateUICard(hud.transform, "TopHUDBar", new Vector2(0f, 835f), new Vector2(980f, 160f), cardSprite, new Color(0.10f, 0.15f, 0.24f, 0.94f));
            TMP_Text hudTitle = CreateUIText(topBar.transform, "LevelTitleText", "LVL 1: FIRST ROLL", new Vector2(-150f, 34f), new Vector2(560f, 52f), 32f, new Color(1f, 0.90f, 0.30f), TextAlignmentOptions.Left, font);
            TMP_Text hudMode = CreateUIText(topBar.transform, "ModeBadgeText", "EASY • CLASSIC", new Vector2(-230f, -14f), new Vector2(360f, 36f), 22f, new Color(0.45f, 0.88f, 1f), TextAlignmentOptions.Left, font);
            TMP_Text hudPins = CreateUIText(topBar.transform, "PinsText", "PINS: 0/3", new Vector2(18f, -14f), new Vector2(180f, 40f), 25f, new Color(1f, 0.86f, 0.32f), TextAlignmentOptions.Center, font);
            TMP_Text hudTimer = CreateUIText(topBar.transform, "TimerText", "TIME: 0.0s", new Vector2(205f, -14f), new Vector2(215f, 44f), 26f, Color.white, TextAlignmentOptions.Right, font);
            Button hudPauseBtn = CreateUIButton(topBar.transform, "PauseButton", "II", new Vector2(405f, 0f), new Vector2(96f, 96f), btnSprite, new Color(0.22f, 0.56f, 0.90f), Color.white, 34f, font);

            // Toast notification banner for Calibration feedback
            TMP_Text toastTxt = CreateUIText(hud.transform, "CalibrationToastText", "TILT ZEROED!", new Vector2(0f, 690f), new Vector2(720f, 58f), 28f, new Color(0.32f, 1f, 0.56f), TextAlignmentOptions.Center, font);
            toastTxt.gameObject.SetActive(false);

            // Bottom HUD bar with live Spirit-Level Bubble, Calibrate Button, & Retry Button
            GameObject botBar = CreateUICard(hud.transform, "BottomHUDBar", new Vector2(0f, -825f), new Vector2(980f, 165f), cardSprite, new Color(0.10f, 0.15f, 0.24f, 0.94f));

            // Bubble Spirit-Level Indicator (left)
            Image levelOuter = CreateUIImage(botBar.transform, "SpiritLevelRing", new Vector2(-365f, 12f), new Vector2(94f, 94f), circleSprite, new Color(0.18f, 0.26f, 0.40f, 1f), true);
            CreateUIImage(levelOuter.transform, "CenterTarget", Vector2.zero, new Vector2(22f, 22f), circleSprite, new Color(1f, 1f, 1f, 0.24f), true);
            Image bubble = CreateUIImage(levelOuter.transform, "BubbleDot", Vector2.zero, new Vector2(28f, 28f), circleSprite, new Color(0.30f, 0.95f, 0.62f, 1f), true);

            Button hudCalibrateBtn = CreateUIButton(botBar.transform, "CalibrateButton", "CALIBRATE TILT", new Vector2(-30f, 14f), new Vector2(420f, 84f), btnSprite, new Color(0.18f, 0.72f, 0.54f), Color.white, 26f, font);
            Button hudRestartBtn = CreateUIButton(botBar.transform, "RestartButton", "RETRY", new Vector2(335f, 14f), new Vector2(215f, 84f), btnSprite, new Color(0.92f, 0.62f, 0.14f), Color.white, 26f, font);
            TMP_Text hudHint = CreateUIText(botBar.transform, "ControlsHintText", "WASD / ARROWS OR DRAG TO TILT", new Vector2(0f, -52f), new Vector2(920f, 36f), 19f, new Color(0.75f, 0.85f, 0.98f), TextAlignmentOptions.Center, font);

            // -------------------------------------------------------------
            // 4. PAUSE PANEL
            // -------------------------------------------------------------
            GameObject pause = CreateFullScreenPanel(canvasObj.transform, "PausePanel");
            CreateUIImage(pause.transform, "DimOverlay", Vector2.zero, new Vector2(1080, 1920), null, new Color(0f, 0f, 0f, 0.78f), false);
            GameObject pauseWin = CreateUICard(pause.transform, "PauseWindow", Vector2.zero, new Vector2(840f, 1080f), cardSprite, new Color(0.12f, 0.17f, 0.27f, 1f));
            CreateUIText(pauseWin.transform, "Title", "PAUSED", new Vector2(0f, 435f), new Vector2(720f, 82f), 54f, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center, font);
            Slider pauseSensSlider = CreateSensitivitySlider(pauseWin.transform, "PauseSensitivityRow", new Vector2(0f, 290f), cardSprite, btnSprite, circleSprite, font, out TMP_Text pauseSensTxt);
            Button pauseMuteBtn = CreateUIButton(pauseWin.transform, "MuteButton", "SOUND: ON", new Vector2(-175f, 140f), new Vector2(320f, 84f), btnSprite, new Color(0.24f, 0.50f, 0.86f), Color.white, 24f, font);
            TMP_Text pauseMuteTxt = pauseMuteBtn.GetComponentInChildren<TMP_Text>();
            Button pauseCalBtn = CreateUIButton(pauseWin.transform, "CalibrateButton", "CALIBRATE", new Vector2(175f, 140f), new Vector2(320f, 84f), btnSprite, new Color(0.18f, 0.72f, 0.54f), Color.white, 24f, font);

            Button pauseResumeBtn = CreateUIButton(pauseWin.transform, "ResumeButton", "RESUME", new Vector2(0f, 10f), new Vector2(490f, 92f), btnSprite, new Color(0.18f, 0.78f, 0.44f), Color.white, 30f, font);
            Button pauseRestartBtn = CreateUIButton(pauseWin.transform, "RestartButton", "RESTART LEVEL", new Vector2(0f, -104f), new Vector2(490f, 92f), btnSprite, new Color(0.95f, 0.65f, 0.12f), Color.white, 28f, font);
            Button pauseSwitchBtn = CreateUIButton(pauseWin.transform, "SwitchGameButton", "SWITCH GAME", new Vector2(0f, -218f), new Vector2(490f, 88f), btnSprite, new Color(0.62f, 0.36f, 0.86f), Color.white, 27f, font);
            Button pauseHomeBtn = CreateUIButton(pauseWin.transform, "HomeButton", "MAIN MENU", new Vector2(0f, -332f), new Vector2(490f, 92f), btnSprite, new Color(0.86f, 0.28f, 0.28f), Color.white, 28f, font);

            // -------------------------------------------------------------
            // 5. LEVEL COMPLETE PANEL
            // -------------------------------------------------------------
            GameObject winPanel = CreateFullScreenPanel(canvasObj.transform, "LevelCompletePanel");
            CreateUIImage(winPanel.transform, "DimOverlay", Vector2.zero, new Vector2(1080, 1920), null, new Color(0f, 0f, 0f, 0.78f), false);
            GameObject winWin = CreateUICard(winPanel.transform, "WinWindow", Vector2.zero, new Vector2(840f, 880f), cardSprite, new Color(0.12f, 0.19f, 0.26f, 1f));
            TMP_Text winTitle = CreateUIText(winWin.transform, "WinTitleText", "LEVEL 1 CLEARED!", new Vector2(0f, 320f), new Vector2(760f, 90f), 50f, new Color(0.30f, 0.96f, 0.56f), TextAlignmentOptions.Center, font);

            Image[] winStars = new Image[3];
            winStars[0] = CreateUIImage(winWin.transform, "WinStar_1", new Vector2(-112f, 175f), new Vector2(80f, 80f), starSprite, new Color(1f, 0.88f, 0.22f), true);
            winStars[1] = CreateUIImage(winWin.transform, "WinStar_2", new Vector2(0f, 198f), new Vector2(98f, 98f), starSprite, new Color(1f, 0.88f, 0.22f), true);
            winStars[2] = CreateUIImage(winWin.transform, "WinStar_3", new Vector2(112f, 175f), new Vector2(80f, 80f), starSprite, new Color(1f, 0.88f, 0.22f), true);

            TMP_Text winTime = CreateUIText(winWin.transform, "WinTimeText", "TIME: 0.0s • 3 / 3 STARS", new Vector2(0f, 65f), new Vector2(760f, 56f), 32f, Color.white, TextAlignmentOptions.Center, font);
            Button winNextBtn = CreateUIButton(winWin.transform, "NextButton", "NEXT LEVEL", new Vector2(0f, -65f), new Vector2(480f, 96f), btnSprite, new Color(0.18f, 0.78f, 0.44f), Color.white, 30f, font);
            Button winReplayBtn = CreateUIButton(winWin.transform, "ReplayButton", "REPLAY", new Vector2(0f, -185f), new Vector2(480f, 90f), btnSprite, new Color(0.95f, 0.65f, 0.12f), Color.white, 28f, font);
            Button winLevelsBtn = CreateUIButton(winWin.transform, "LevelsButton", "ALL LEVELS", new Vector2(0f, -300f), new Vector2(480f, 90f), btnSprite, new Color(0.22f, 0.56f, 0.90f), Color.white, 28f, font);

            // -------------------------------------------------------------
            // 6. GAME OVER PANEL
            // -------------------------------------------------------------
            GameObject gameOver = CreateFullScreenPanel(canvasObj.transform, "GameOverPanel");
            CreateUIImage(gameOver.transform, "DimOverlay", Vector2.zero, new Vector2(1080, 1920), null, new Color(0f, 0f, 0f, 0.78f), false);
            GameObject goWin = CreateUICard(gameOver.transform, "GameOverWindow", Vector2.zero, new Vector2(820f, 680f), cardSprite, new Color(0.18f, 0.13f, 0.19f, 1f));
            TMP_Text goTitle = CreateUIText(goWin.transform, "GameOverTitleText", "FELL INTO PIT HOLE!", new Vector2(0f, 200f), new Vector2(740f, 88f), 46f, new Color(1f, 0.36f, 0.36f), TextAlignmentOptions.Center, font);
            TMP_Text goHint = CreateUIText(goWin.transform, "GameOverHintText", "Tilt gently near hole rims!", new Vector2(0f, 95f), new Vector2(720f, 68f), 26f, new Color(0.90f, 0.92f, 0.98f), TextAlignmentOptions.Center, font);
            Button goRetryBtn = CreateUIButton(goWin.transform, "RetryButton", "TRY AGAIN", new Vector2(0f, -65f), new Vector2(460f, 98f), btnSprite, new Color(0.18f, 0.78f, 0.44f), Color.white, 32f, font);
            Button goMenuBtn = CreateUIButton(goWin.transform, "MenuButton", "MAIN MENU", new Vector2(0f, -195f), new Vector2(460f, 90f), btnSprite, new Color(0.84f, 0.28f, 0.28f), Color.white, 28f, font);

            // Wire ALL Serialized Fields on GeometryMazeUI
            SerializedObject uiSo = new SerializedObject(mazeUI);
            SetProp(uiSo, "mainMenuPanel", mainMenu);
            SetProp(uiSo, "levelSelectPanel", levelSelect);
            SetProp(uiSo, "gameplayHudPanel", hud);
            SetProp(uiSo, "pausePanel", pause);
            SetProp(uiSo, "levelCompletePanel", winPanel);
            SetProp(uiSo, "gameOverPanel", gameOver);

            SetProp(uiSo, "mainMenuStarsText", mainStarsTxt);
            SetProp(uiSo, "mainPlayButton", mainPlayBtn);
            SetProp(uiSo, "mainLevelsButton", mainLevelsBtn);
            SetProp(uiSo, "mainRandomMazeButton", mainRandomBtn);
            SetProp(uiSo, "mainSwitchGameButton", mainSwitchBtn);
            SetProp(uiSo, "menuSensitivitySlider", menuSensSlider);
            SetProp(uiSo, "menuSensitivityText", menuSensTxt);

            SetProp(uiSo, "levelGridContainer", gridContainer.transform);
            SetProp(uiSo, "levelSelectBackButton", lsBackBtn);
            SetProp(uiSo, "uiCardSprite", cardSprite);
            SetProp(uiSo, "uiButtonSprite", btnSprite);
            SetProp(uiSo, "starSprite", starSprite);

            SetProp(uiSo, "hudLevelTitleText", hudTitle);
            SetProp(uiSo, "hudModeBadgeText", hudMode);
            SetProp(uiSo, "hudTimerText", hudTimer);
            SetProp(uiSo, "hudPinsText", hudPins);
            SetProp(uiSo, "hudControlsHintText", hudHint);
            SetProp(uiSo, "hudCalibrationToastText", toastTxt);
            SetProp(uiSo, "spiritLevelBubble", bubble.rectTransform);
            SetProp(uiSo, "hudCalibrateButton", hudCalibrateBtn);
            SetProp(uiSo, "hudRestartButton", hudRestartBtn);
            SetProp(uiSo, "hudPauseButton", hudPauseBtn);

            SetProp(uiSo, "pauseSensitivitySlider", pauseSensSlider);
            SetProp(uiSo, "pauseSensitivityText", pauseSensTxt);
            SetProp(uiSo, "pauseMuteButton", pauseMuteBtn);
            SetProp(uiSo, "pauseMuteText", pauseMuteTxt);
            SetProp(uiSo, "pauseCalibrateButton", pauseCalBtn);
            SetProp(uiSo, "pauseResumeButton", pauseResumeBtn);
            SetProp(uiSo, "pauseRestartButton", pauseRestartBtn);
            SetProp(uiSo, "pauseSwitchGameButton", pauseSwitchBtn);
            SetProp(uiSo, "pauseHomeButton", pauseHomeBtn);

            SetProp(uiSo, "winTitleText", winTitle);
            SetProp(uiSo, "winTimeText", winTime);
            SetPropArray(uiSo, "winStarIcons", winStars);
            SetProp(uiSo, "winNextButton", winNextBtn);
            SetProp(uiSo, "winReplayButton", winReplayBtn);
            SetProp(uiSo, "winLevelsButton", winLevelsBtn);

            SetProp(uiSo, "gameOverTitleText", goTitle);
            SetProp(uiSo, "gameOverHintText", goHint);
            SetProp(uiSo, "gameOverRetryButton", goRetryBtn);
            SetProp(uiSo, "gameOverMenuButton", goMenuBtn);
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            // Clean default EditMode visibility: show MainMenuPanel
            levelSelect.SetActive(false);
            hud.SetActive(false);
            pause.SetActive(false);
            winPanel.SetActive(false);
            gameOver.SetActive(false);

            return mazeUI;
        }

        private static Slider CreateSensitivitySlider(
            Transform parent,
            string name,
            Vector2 pos,
            Sprite cardSprite,
            Sprite fillSprite,
            Sprite circleSprite,
            TMP_FontAsset font,
            out TMP_Text labelTxt)
        {
            GameObject box = CreateUICard(parent, name, pos, new Vector2(680f, 118f), cardSprite, new Color(0.08f, 0.12f, 0.20f, 0.95f));
            labelTxt = CreateUIText(box.transform, "SensitivityLabel", "TILT SENSITIVITY: 1.2x", new Vector2(0f, 30f), new Vector2(620f, 36f), 22f, new Color(0.88f, 0.94f, 1f), TextAlignmentOptions.Center, font);

            GameObject sObj = new GameObject("Slider");
            sObj.transform.SetParent(box.transform, false);
            RectTransform sRt = sObj.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -18f);
            sRt.sizeDelta = new Vector2(560f, 40f);

            Slider slider = sObj.AddComponent<Slider>();
            slider.minValue = 0.5f;
            slider.maxValue = 2.4f;
            slider.value = 1.15f;

            Image bg = CreateUIImage(sObj.transform, "Track", Vector2.zero, new Vector2(560f, 22f), cardSprite, new Color(0.18f, 0.24f, 0.36f, 1f), false);
            if (cardSprite != null) bg.type = Image.Type.Sliced;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.22f);
            faRt.anchorMax = new Vector2(1f, 0.78f);
            faRt.offsetMin = new Vector2(6f, 0f);
            faRt.offsetMax = new Vector2(-6f, 0f);
            Image fill = CreateUIImage(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, fillSprite, new Color(0.25f, 0.85f, 0.96f, 1f), false);
            if (fillSprite != null) fill.type = Image.Type.Sliced;
            slider.fillRect = fill.rectTransform;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sObj.transform, false);
            RectTransform haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(16f, 0f);
            haRt.offsetMax = new Vector2(-16f, 0f);
            Image handle = CreateUIImage(handleArea.transform, "Handle", Vector2.zero, new Vector2(38f, 38f), circleSprite, Color.white, true);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;

            return slider;
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static GameObject CreateUICard(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color tint)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null) img.type = Image.Type.Sliced;
            img.color = tint;
            return obj;
        }

        private static Image CreateUIImage(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color tint, bool preserveAspect)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.preserveAspect = preserveAspect;
            return img;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align, TMP_FontAsset font)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TMP_Text txt = obj.AddComponent<TextMeshProUGUI>();
            if (font != null) txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyles.Bold;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }

        private static Button CreateUIButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Sprite btnSprite, Color bgColor, Color textColor, float fontSize, TMP_FontAsset font)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = btnSprite;
            if (btnSprite != null) img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            CreateUIText(obj.transform, "Text", label, Vector2.zero, size, fontSize, textColor, TextAlignmentOptions.Center, font);
            return btn;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string child = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }

        private static void SetProp(SerializedObject so, string name, Object value)
        {
            SerializedProperty prop = so.FindProperty(name);
            if (prop != null) prop.objectReferenceValue = value;
            else Debug.LogWarning($"[GeometryBoxMazeSetup] Serialized field '{name}' not found on {so.targetObject.GetType().Name}");
        }

        private static void SetPropArray(SerializedObject so, string name, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(name);
            if (prop != null && prop.isArray && values != null)
            {
                prop.ClearArray();
                for (int i = 0; i < values.Length; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            }
        }
    }
}
#endif
