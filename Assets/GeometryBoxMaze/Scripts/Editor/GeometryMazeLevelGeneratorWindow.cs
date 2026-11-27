#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeometryBoxMaze.Editor
{
    /// <summary>
    /// Interactive Visual Level Designer & Solvability Puzzle Validator for Geometry Box Tilt Maze.
    /// Lets you hand-draw custom wall lines, toggle grid walls, place Start/Goal, Holes, Gold Pins,
    /// Eraser Bumpers, Paired Switch-Gates & Moving Obstacles, verify exact ball-clearance puzzle paths
    /// (ensuring players must solve switches to win), save assets, and one-click Play-Test live.
    /// Menu: Tools -> Geometry Box Maze -> 3. Level Generator Tool.
    /// </summary>
    public class GeometryMazeLevelGeneratorWindow : EditorWindow
    {
        private enum PaintTool
        {
            WallLineDraw,
            GridEdgeToggle,
            EraseItem,
            MoveStart,
            MoveGoal,
            PlaceHole,
            PlacePin,
            PlaceEraserBumper,
            PlaceSwitchAndGate,
            PlaceMovingSlider,
            PlaceSpinner
        }

        private enum TrayThemePreset
        {
            SkyBlueBox,
            MintGreenBox,
            VintageSteelBox,
            IndigoPastelBox,
            RoyalCobaltBox,
            CustomColors
        }

        private enum PuzzleCheckStatus
        {
            VerifiedSolvable,
            SwitchBypassable,
            SwitchUnreachable,
            PathBlocked
        }

        private static readonly string[] TOOL_LABELS =
        {
            "Wall Line (Drag)",
            "Grid Wall (Click)",
            "Erase (or R-Click)",
            "Move Start",
            "Move Goal",
            "Place Pit Hole",
            "Place Gold Pin",
            "Eraser Bumper",
            "Switch + Gate (2-Step)",
            "Moving Slider (2-Step)",
            "Spinner Cross"
        };

        // Catalog & Working State
        private GeometryMazeLevelCatalog catalog;
        private int selectedCatalogIndex = 0;
        private GeometryMazeLevelData workingCopy;
        private TrayThemePreset themePreset = TrayThemePreset.SkyBlueBox;

        // Designer Controls
        private PaintTool activeTool = PaintTool.WallLineDraw;
        private bool snapToGrid = true;
        private bool axisLockWalls = true;
        private float customWallThickness = 0.20f;
        private float placeHoleRadius = 0.34f;
        private float placeBumperAngle = 0f;
        private float placeGateLength = 1.25f;
        private float placeGateAngle = 0f;

        // Auto-Generator Base Settings
        private int genCols = 6;
        private int genRows = 9;
        private int genSeed = 2026;
        private int genHoles = 2;
        private int genPins = 3;
        private bool genEraser = true;
        private bool genSwitch = true;
        private bool genSlider = false;
        private bool genSpinner = false;

        // Interactive Mouse State
        private Vector2 leftScrollPos;
        private bool isDraggingWall;
        private Vector2 dragStartWorld;
        private Vector2 currentMouseWorld;
        private bool hasPendingSecondPoint;
        private Vector2 pendingFirstPointWorld;
        private bool mouseInsideBoard;
        private string statusToast = "Draw walls, place traps & verify puzzle path before saving.";

        // Live Path Validation Cache
        private PuzzleCheckStatus puzzleStatus = PuzzleCheckStatus.VerifiedSolvable;
        private string puzzleMessage = "";
        private readonly List<Vector2> verifiedPhase1Path = new List<Vector2>();
        private readonly List<Vector2> verifiedPhase2Path = new List<Vector2>();
        private readonly List<Vector2> bypassPath = new List<Vector2>();

        [MenuItem("Tools/Geometry Box Maze/3. Level Generator Tool")]
        public static void ShowWindow()
        {
            GeometryMazeLevelGeneratorWindow win = GetWindow<GeometryMazeLevelGeneratorWindow>("Geometry Box Level Designer");
            win.minSize = new Vector2(960f, 680f);
            win.LoadCatalogAndCurrentLevel();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            LoadCatalogAndCurrentLevel();
        }

        private void OnDisable()
        {
            if (workingCopy != null)
            {
                DestroyImmediate(workingCopy);
                workingCopy = null;
            }
        }

        private void LoadCatalogAndCurrentLevel()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(GeometryBoxMazeSetup.CATALOG_PATH);
            EnsureWorkingCopy();
            if (catalog != null && catalog.LevelCount > 0)
            {
                selectedCatalogIndex = Mathf.Clamp(selectedCatalogIndex, 0, catalog.LevelCount - 1);
                LoadFromAsset(catalog.GetLevelByIndex(selectedCatalogIndex));
            }
            else
            {
                ResetToBlankLevel(1);
            }
        }

        private void EnsureWorkingCopy()
        {
            if (workingCopy == null)
            {
                workingCopy = CreateInstance<GeometryMazeLevelData>();
                workingCopy.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void LoadFromAsset(GeometryMazeLevelData src)
        {
            EnsureWorkingCopy();
            if (src == null)
            {
                ResetToBlankLevel(1);
                return;
            }

            workingCopy.levelNumber = src.levelNumber;
            workingCopy.levelName = src.levelName;
            workingCopy.difficultyLabel = src.difficultyLabel;
            workingCopy.modeLabel = src.modeLabel;
            workingCopy.trayInnerSize = src.trayInnerSize;
            workingCopy.trayColor = src.trayColor;
            workingCopy.outerFrameColor = src.outerFrameColor;
            workingCopy.wallColor = src.wallColor;
            workingCopy.threeStarTime = src.threeStarTime;
            workingCopy.twoStarTime = src.twoStarTime;
            workingCopy.timeTrialLimit = src.timeTrialLimit;
            workingCopy.startPosition = src.startPosition;
            workingCopy.goalPosition = src.goalPosition;
            workingCopy.ballRadius = src.ballRadius;
            workingCopy.walls = new List<MazeWallDefinition>(src.walls ?? new List<MazeWallDefinition>());
            workingCopy.holes = new List<MazeHoleDefinition>(src.holes ?? new List<MazeHoleDefinition>());
            workingCopy.obstacles = new List<MazeObstacleDefinition>(src.obstacles ?? new List<MazeObstacleDefinition>());
            workingCopy.bonusPins = new List<Vector2>(src.bonusPins ?? new List<Vector2>());
            workingCopy.eraserBumpers = new List<MazeBumperDefinition>(src.eraserBumpers ?? new List<MazeBumperDefinition>());
            workingCopy.switchGates = new List<MazeSwitchGateDefinition>(src.switchGates ?? new List<MazeSwitchGateDefinition>());

            themePreset = TrayThemePreset.CustomColors;
            if (src.ballRadius <= 0.235f) { genCols = 7; genRows = 11; }
            else if (src.ballRadius <= 0.255f) { genCols = 6; genRows = 9; }
            else { genCols = 5; genRows = 7; }

            hasPendingSecondPoint = false;
            isDraggingWall = false;
            RevalidatePuzzlePath();
        }

        private void ResetToBlankLevel(int levelNum)
        {
            EnsureWorkingCopy();
            workingCopy.levelNumber = levelNum;
            workingCopy.levelName = $"Custom Puzzle {levelNum}";
            workingCopy.difficultyLabel = "CUSTOM";
            workingCopy.modeLabel = "PUZZLE";
            workingCopy.trayInnerSize = new Vector2(7.6f, 11.6f);
            GetThemeColors(TrayThemePreset.SkyBlueBox, out workingCopy.trayColor, out workingCopy.outerFrameColor, out workingCopy.wallColor);
            workingCopy.threeStarTime = 22f;
            workingCopy.twoStarTime = 40f;
            workingCopy.timeTrialLimit = 0f;
            workingCopy.startPosition = new Vector2(-2.6f, -4.6f);
            workingCopy.goalPosition = new Vector2(2.6f, 4.6f);
            workingCopy.ballRadius = 0.25f;
            workingCopy.walls = new List<MazeWallDefinition>();
            workingCopy.holes = new List<MazeHoleDefinition>();
            workingCopy.obstacles = new List<MazeObstacleDefinition>();
            workingCopy.bonusPins = new List<Vector2>();
            workingCopy.eraserBumpers = new List<MazeBumperDefinition>();
            workingCopy.switchGates = new List<MazeSwitchGateDefinition>();
            hasPendingSecondPoint = false;
            isDraggingWall = false;
            RevalidatePuzzlePath();
        }

        private void OnGUI()
        {
            EnsureWorkingCopy();
            DrawTopToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawLeftInspectorColumn();
            DrawInteractiveBoardArea();
            EditorGUILayout.EndHorizontal();

            if (mouseInsideBoard && (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag))
            {
                Repaint();
            }
        }

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            catalog = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(GeometryBoxMazeSetup.CATALOG_PATH);
            if (catalog != null && catalog.LevelCount > 0)
            {
                string[] options = new string[catalog.LevelCount];
                for (int i = 0; i < catalog.LevelCount; i++)
                {
                    GeometryMazeLevelData lvl = catalog.GetLevelByIndex(i);
                    options[i] = lvl != null ? $"Lvl {lvl.levelNumber}: {lvl.levelName}" : $"Slot {i + 1}";
                }

                int nextIdx = EditorGUILayout.Popup(selectedCatalogIndex, options, EditorStyles.toolbarPopup, GUILayout.Width(210f));
                if (nextIdx != selectedCatalogIndex)
                {
                    selectedCatalogIndex = nextIdx;
                    LoadFromAsset(catalog.GetLevelByIndex(selectedCatalogIndex));
                }
            }

            if (GUILayout.Button("+ New Blank Tray", EditorStyles.toolbarButton, GUILayout.Width(120f)))
            {
                int nextNum = catalog != null ? catalog.LevelCount + 1 : 1;
                ResetToBlankLevel(nextNum);
                statusToast = $"Created blank puzzle tray for Level {nextNum}. Draw lines & place Goal!";
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.38f, 0.92f, 0.56f);
            if (GUILayout.Button("Save Level Asset", EditorStyles.toolbarButton, GUILayout.Width(130f)))
            {
                SaveWorkingCopyToAsset(out _);
            }

            GUI.backgroundColor = new Color(0.36f, 0.80f, 1f);
            if (GUILayout.Button("Save & Play Test Live!", EditorStyles.toolbarButton, GUILayout.Width(155f)))
            {
                SaveAndPlayTestLevel();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftInspectorColumn()
        {
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUILayout.Width(355f));
            GUILayout.Space(6f);

            // 1. LIVE PUZZLE VERIFICATION BOX
            DrawValidationCard();

            GUILayout.Space(8f);

            // 2. INTERACTIVE PAINT TOOLS PALETTE
            EditorGUILayout.LabelField("Paint & Draw Tools", EditorStyles.boldLabel);
            int pickedTool = GUILayout.SelectionGrid((int)activeTool, TOOL_LABELS, 2, GUILayout.Height(150f));
            if (pickedTool != (int)activeTool)
            {
                activeTool = (PaintTool)pickedTool;
                hasPendingSecondPoint = false;
                isDraggingWall = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            snapToGrid = EditorGUILayout.Toggle("Snap to Grid (Cells)", snapToGrid);
            if (activeTool == PaintTool.WallLineDraw)
            {
                axisLockWalls = EditorGUILayout.Toggle("Axis Lock Horizontal/Vert", axisLockWalls);
                customWallThickness = EditorGUILayout.Slider("Wall Thickness", customWallThickness, 0.14f, 0.36f);
            }
            else if (activeTool == PaintTool.PlaceHole)
            {
                placeHoleRadius = EditorGUILayout.Slider("Pit Hole Radius", placeHoleRadius, 0.24f, 0.48f);
            }
            else if (activeTool == PaintTool.PlaceEraserBumper)
            {
                placeBumperAngle = EditorGUILayout.Slider("Eraser Angle (Deg)", placeBumperAngle, -90f, 90f);
            }
            else if (activeTool == PaintTool.PlaceSwitchAndGate)
            {
                placeGateLength = EditorGUILayout.Slider("Locked Gate Length", placeGateLength, 0.7f, 2.8f);
                placeGateAngle = EditorGUILayout.Slider("Locked Gate Angle", placeGateAngle, 0f, 90f);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(8f);

            // 3. AUTO MAZE BASE GENERATOR
            EditorGUILayout.LabelField("Procedural Maze Base Generator", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            genCols = EditorGUILayout.IntSlider("Grid Columns", genCols, 4, 8);
            genRows = EditorGUILayout.IntSlider("Grid Rows", genRows, 5, 12);
            EditorGUILayout.BeginHorizontal();
            genSeed = EditorGUILayout.IntField("Seed", genSeed);
            if (GUILayout.Button("Random", GUILayout.Width(72f)))
            {
                genSeed = (Environment.TickCount & 0x7FFFFFFF) ^ UnityEngine.Random.Range(10, 99999);
            }
            EditorGUILayout.EndHorizontal();

            genHoles = EditorGUILayout.IntSlider("Holes", genHoles, 0, 5);
            genPins = EditorGUILayout.IntSlider("Gold Pins", genPins, 0, 5);
            genEraser = EditorGUILayout.Toggle("Eraser Bumper", genEraser);
            genSwitch = EditorGUILayout.Toggle("Switch + Locked Gate", genSwitch);
            genSlider = EditorGUILayout.Toggle("Moving Slider", genSlider);
            genSpinner = EditorGUILayout.Toggle("Center Spinner", genSpinner);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.56f, 0.85f, 1f);
            if (GUILayout.Button("Generate Maze Base", GUILayout.Height(30f)))
            {
                GenerateBaseMazeIntoWorkingCopy();
            }
            GUI.backgroundColor = new Color(1f, 0.56f, 0.52f);
            if (GUILayout.Button("Clear All", GUILayout.Width(86f), GUILayout.Height(30f)))
            {
                workingCopy.walls.Clear();
                workingCopy.holes.Clear();
                workingCopy.bonusPins.Clear();
                workingCopy.eraserBumpers.Clear();
                workingCopy.switchGates.Clear();
                workingCopy.obstacles.Clear();
                hasPendingSecondPoint = false;
                RevalidatePuzzlePath();
                statusToast = "Cleared all walls and obstacles.";
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(8f);

            // 4. LEVEL SETTINGS & THEME
            EditorGUILayout.LabelField("Level Properties & Styling", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            workingCopy.levelNumber = EditorGUILayout.IntSlider("Level #", workingCopy.levelNumber, 1, 50);
            workingCopy.levelName = EditorGUILayout.TextField("Name", workingCopy.levelName);
            workingCopy.difficultyLabel = EditorGUILayout.TextField("Difficulty", workingCopy.difficultyLabel);
            workingCopy.modeLabel = EditorGUILayout.TextField("Mode Badge", workingCopy.modeLabel);
            workingCopy.ballRadius = EditorGUILayout.Slider("Ball Radius", workingCopy.ballRadius, 0.21f, 0.34f);

            TrayThemePreset newTheme = (TrayThemePreset)EditorGUILayout.EnumPopup("Box Color Theme", themePreset);
            if (newTheme != themePreset)
            {
                themePreset = newTheme;
                if (themePreset != TrayThemePreset.CustomColors)
                {
                    GetThemeColors(themePreset, out workingCopy.trayColor, out workingCopy.outerFrameColor, out workingCopy.wallColor);
                }
            }
            workingCopy.trayColor = EditorGUILayout.ColorField("Tray Plastic", workingCopy.trayColor);
            workingCopy.outerFrameColor = EditorGUILayout.ColorField("Frame Casing", workingCopy.outerFrameColor);
            workingCopy.wallColor = EditorGUILayout.ColorField("Wall Ribs", workingCopy.wallColor);

            workingCopy.threeStarTime = EditorGUILayout.FloatField("3-Star Time (s)", workingCopy.threeStarTime);
            workingCopy.twoStarTime = EditorGUILayout.FloatField("2-Star Time (s)", workingCopy.twoStarTime);
            workingCopy.timeTrialLimit = EditorGUILayout.FloatField("Time Limit (0=None)", workingCopy.timeTrialLimit);
            EditorGUILayout.EndVertical();

            GUILayout.Space(12f);
            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationCard()
        {
            MessageType msgType = MessageType.Info;
            if (puzzleStatus == PuzzleCheckStatus.VerifiedSolvable) msgType = MessageType.Info;
            else if (puzzleStatus == PuzzleCheckStatus.SwitchBypassable) msgType = MessageType.Warning;
            else msgType = MessageType.Error;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Puzzle Path Check", EditorStyles.boldLabel);
            if (GUILayout.Button("Re-Verify", GUILayout.Width(76f)))
            {
                RevalidatePuzzlePath();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(puzzleMessage, msgType);
            EditorGUILayout.LabelField(
                $"Walls: {workingCopy.walls.Count} | Holes: {workingCopy.holes.Count} | Pins: {workingCopy.bonusPins.Count} | Gates: {workingCopy.switchGates.Count}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawInteractiveBoardArea()
        {
            Rect areaRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(areaRect, new Color(0.14f, 0.10f, 0.08f, 1f)); // Warm desk backdrop

            float statusH = 28f;
            Rect canvasRect = new Rect(areaRect.x + 18f, areaRect.y + 14f, areaRect.width - 36f, areaRect.height - statusH - 22f);

            // Compute centered aspect-locked Geometry Box tray rect (outer ~8.5 x 12.5, inner 7.6 x 11.6)
            Vector2 innerSize = workingCopy.trayInnerSize;
            float trayRatio = innerSize.x / Mathf.Max(0.1f, innerSize.y);
            float availRatio = canvasRect.width / Mathf.Max(1f, canvasRect.height);

            float innerPxW, innerPxH;
            if (availRatio > trayRatio)
            {
                innerPxH = Mathf.Max(120f, canvasRect.height * 0.88f);
                innerPxW = innerPxH * trayRatio;
            }
            else
            {
                innerPxW = Mathf.Max(100f, canvasRect.width * 0.88f);
                innerPxH = innerPxW / trayRatio;
            }

            Rect innerRect = new Rect(
                canvasRect.center.x - innerPxW * 0.5f,
                canvasRect.center.y - innerPxH * 0.5f,
                innerPxW,
                innerPxH);

            float pad = innerPxW * 0.055f;
            Rect frameRect = new Rect(innerRect.x - pad, innerRect.y - pad, innerRect.width + pad * 2f, innerRect.height + pad * 2f);
            Rect chromeRect = new Rect(frameRect.x - pad * 0.25f, frameRect.y - pad * 0.25f, frameRect.width + pad * 0.5f, frameRect.height + pad * 0.5f);

            if (Event.current.type == EventType.Repaint)
            {
                // Desk shadow, metallic rim, plastic frame & floor
                EditorGUI.DrawRect(new Rect(chromeRect.x + 8f, chromeRect.y + 10f, chromeRect.width, chromeRect.height), new Color(0f, 0f, 0f, 0.45f));
                EditorGUI.DrawRect(chromeRect, new Color(0.76f, 0.82f, 0.88f, 1f));
                EditorGUI.DrawRect(frameRect, workingCopy.outerFrameColor);
                EditorGUI.DrawRect(innerRect, workingCopy.trayColor);

                // Hinges & Latch cues on Geometry Box edge
                EditorGUI.DrawRect(new Rect(chromeRect.x - 6f, chromeRect.y + chromeRect.height * 0.22f, 10f, 28f), new Color(0.85f, 0.89f, 0.94f));
                EditorGUI.DrawRect(new Rect(chromeRect.x - 6f, chromeRect.y + chromeRect.height * 0.72f, 10f, 28f), new Color(0.85f, 0.89f, 0.94f));
                EditorGUI.DrawRect(new Rect(chromeRect.xMax - 4f, chromeRect.center.y - 18f, 10f, 36f), new Color(0.88f, 0.91f, 0.96f));

                DrawGridOverlay(innerRect);
                DrawMazeElements(innerRect);
                DrawValidatedPathsOverlay(innerRect);
                DrawToolHoverAndPreview(innerRect);
            }

            HandleBoardMouseEvents(innerRect);

            // Bottom Helper Status Strip
            Rect footerRect = new Rect(areaRect.x + 12f, areaRect.yMax - statusH, areaRect.width - 24f, statusH - 2f);
            string coordLabel = mouseInsideBoard ? $"Cursor: ({currentMouseWorld.x:F2}, {currentMouseWorld.y:F2})" : "Move mouse over tray to design";
            GUI.Label(footerRect, $"{coordLabel}   |   Tool: {TOOL_LABELS[(int)activeTool]}   |   {statusToast}", EditorStyles.whiteMiniLabel);
        }

        private void DrawGridOverlay(Rect innerRect)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.15f, 0.30f, 0.45f, snapToGrid ? 0.22f : 0.10f);
            for (int c = 1; c < genCols; c++)
            {
                float x = Mathf.Lerp(innerRect.xMin, innerRect.xMax, c / (float)genCols);
                Handles.DrawLine(new Vector3(x, innerRect.yMin), new Vector3(x, innerRect.yMax));
            }
            for (int r = 1; r < genRows; r++)
            {
                float y = Mathf.Lerp(innerRect.yMax, innerRect.yMin, r / (float)genRows);
                Handles.DrawLine(new Vector3(innerRect.xMin, y), new Vector3(innerRect.xMax, y));
            }
            Handles.EndGUI();
        }

        private void DrawMazeElements(Rect innerRect)
        {
            Handles.BeginGUI();

            // 1. Holes
            if (workingCopy.holes != null)
            {
                foreach (MazeHoleDefinition h in workingCopy.holes)
                {
                    Vector2 p = WorldToGui(h.position, innerRect);
                    float rPx = WorldSizeToPixels(h.radius, innerRect);
                    Handles.color = new Color(0.14f, 0.20f, 0.30f, 0.9f);
                    Handles.DrawSolidDisc(p, Vector3.forward, rPx);
                    Handles.color = new Color(0.03f, 0.04f, 0.07f, 1f);
                    Handles.DrawSolidDisc(p, Vector3.forward, rPx * 0.78f);
                }
            }

            // 2. Start Pad & Goal Cup
            Vector2 startGui = WorldToGui(workingCopy.startPosition, innerRect);
            float ballPx = WorldSizeToPixels(workingCopy.ballRadius, innerRect);
            Handles.color = new Color(0.22f, 0.62f, 0.98f, 0.42f);
            Handles.DrawSolidDisc(startGui, Vector3.forward, ballPx * 1.45f);
            Handles.color = new Color(0.84f, 0.90f, 0.97f, 1f);
            Handles.DrawSolidDisc(startGui, Vector3.forward, ballPx);

            Vector2 goalGui = WorldToGui(workingCopy.goalPosition, innerRect);
            float goalPx = WorldSizeToPixels(Mathf.Clamp(workingCopy.ballRadius * 1.48f, 0.34f, 0.48f), innerRect);
            Handles.color = new Color(1f, 0.86f, 0.25f, 1f);
            Handles.DrawSolidDisc(goalGui, Vector3.forward, goalPx);
            Handles.color = new Color(0.10f, 0.80f, 0.46f, 1f);
            Handles.DrawSolidDisc(goalGui, Vector3.forward, goalPx * 0.78f);

            // 3. Collectible Gold Pins
            if (workingCopy.bonusPins != null)
            {
                foreach (Vector2 pin in workingCopy.bonusPins)
                {
                    Vector2 p = WorldToGui(pin, innerRect);
                    Handles.color = new Color(1f, 0.82f, 0.18f, 1f);
                    Handles.DrawSolidDisc(p, Vector3.forward, ballPx * 0.82f);
                    Handles.color = Color.white;
                    Handles.DrawSolidDisc(p + new Vector2(-2f, -2f), Vector3.forward, ballPx * 0.24f);
                }
            }

            // 4. Walls (oriented rectangles with drop shadows)
            if (workingCopy.walls != null)
            {
                foreach (MazeWallDefinition w in workingCopy.walls)
                {
                    DrawOrientedBox(w.position + new Vector2(0.06f, -0.07f), w.size, w.rotationDeg, innerRect, new Color(0.05f, 0.10f, 0.20f, 0.28f));
                    DrawOrientedBox(w.position, w.size, w.rotationDeg, innerRect, workingCopy.wallColor);
                }
            }

            // 5. Eraser Bumpers (Two-tone Coral + Blue!)
            if (workingCopy.eraserBumpers != null)
            {
                foreach (MazeBumperDefinition b in workingCopy.eraserBumpers)
                {
                    DrawOrientedBox(b.position, b.size, b.rotationDeg, innerRect, new Color(0.95f, 0.42f, 0.48f, 1f));
                    DrawOrientedBox(b.position, new Vector2(b.size.x * 0.35f, b.size.y), b.rotationDeg, innerRect, new Color(0.22f, 0.50f, 0.88f, 1f));
                }
            }

            // 6. Switch Plates + Locked Gates + Connection Wire
            if (workingCopy.switchGates != null)
            {
                foreach (MazeSwitchGateDefinition sg in workingCopy.switchGates)
                {
                    Vector2 swGui = WorldToGui(sg.switchPosition, innerRect);
                    Vector2 gtGui = WorldToGui(sg.gatePosition, innerRect);

                    Handles.color = new Color(0.14f, 0.88f, 0.98f, 0.72f);
                    Handles.DrawDottedLine(swGui, gtGui, 4f);

                    Handles.DrawSolidDisc(swGui, Vector3.forward, ballPx * 1.15f);
                    DrawOrientedBox(sg.gatePosition, sg.gateSize, sg.gateRotationDeg, innerRect, new Color(0.10f, 0.76f, 0.94f, 1f));
                }
            }

            // 7. Moving Obstacles / Spinners
            if (workingCopy.obstacles != null)
            {
                foreach (MazeObstacleDefinition obs in workingCopy.obstacles)
                {
                    if (obs.isSpinner)
                    {
                        DrawOrientedBox(obs.startPosition, obs.size, 25f, innerRect, new Color(1f, 0.54f, 0.16f, 1f));
                    }
                    else
                    {
                        Vector2 p1 = WorldToGui(obs.startPosition, innerRect);
                        Vector2 p2 = WorldToGui(obs.endPosition, innerRect);
                        Handles.color = new Color(1f, 0.65f, 0.2f, 0.75f);
                        Handles.DrawDottedLine(p1, p2, 4f);
                        DrawOrientedBox((obs.startPosition + obs.endPosition) * 0.5f, obs.size, 0f, innerRect, new Color(1f, 0.54f, 0.16f, 1f));
                    }
                }
            }

            Handles.EndGUI();

            // Clean readable miniature labels
            GUI.Label(new Rect(startGui.x - 24f, startGui.y + ballPx + 2f, 48f, 16f), "START", EditorStyles.whiteMiniLabel);
            GUI.Label(new Rect(goalGui.x - 20f, goalGui.y + goalPx + 2f, 40f, 16f), "GOAL", EditorStyles.whiteMiniLabel);
        }

        private void DrawValidatedPathsOverlay(Rect innerRect)
        {
            Handles.BeginGUI();

            if (verifiedPhase1Path.Count >= 2)
            {
                Vector3[] pts = ToGuiPoly(verifiedPhase1Path, innerRect);
                Handles.color = new Color(0.12f, 0.92f, 1f, 0.92f);
                Handles.DrawAAPolyLine(4f, pts);
            }

            if (verifiedPhase2Path.Count >= 2)
            {
                Vector3[] pts = ToGuiPoly(verifiedPhase2Path, innerRect);
                Handles.color = new Color(0.20f, 1f, 0.46f, 0.95f);
                Handles.DrawAAPolyLine(4f, pts);
            }

            if (bypassPath.Count >= 2)
            {
                Vector3[] pts = ToGuiPoly(bypassPath, innerRect);
                Handles.color = new Color(1f, 0.45f, 0.12f, 0.95f);
                Handles.DrawAAPolyLine(4f, pts);
            }

            Handles.EndGUI();
        }

        private void DrawToolHoverAndPreview(Rect innerRect)
        {
            if (!mouseInsideBoard) return;

            Handles.BeginGUI();

            if (activeTool == PaintTool.WallLineDraw && isDraggingWall)
            {
                Vector2 endPos = ResolveWallDragEnd(dragStartWorld, currentMouseWorld);
                Vector2 diff = endPos - dragStartWorld;
                float len = diff.magnitude;
                if (len > 0.05f)
                {
                    float ang = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                    DrawOrientedBox((dragStartWorld + endPos) * 0.5f, new Vector2(len + customWallThickness, customWallThickness), ang, innerRect, new Color(1f, 0.92f, 0.30f, 0.85f));
                }
            }
            else if (activeTool == PaintTool.GridEdgeToggle)
            {
                if (TryFindNearestGridEdge(currentMouseWorld, out _, out _, out _, out Vector2 pos, out Vector2 size))
                {
                    DrawOrientedBox(pos, size, 0f, innerRect, new Color(1f, 0.90f, 0.25f, 0.65f));
                }
            }
            else if (hasPendingSecondPoint && (activeTool == PaintTool.PlaceSwitchAndGate || activeTool == PaintTool.PlaceMovingSlider))
            {
                Vector2 p1 = WorldToGui(pendingFirstPointWorld, innerRect);
                Vector2 snapped = snapToGrid ? SnapToCellCenter(currentMouseWorld) : currentMouseWorld;
                Vector2 p2 = WorldToGui(snapped, innerRect);
                Handles.color = new Color(1f, 0.95f, 0.3f, 0.9f);
                Handles.DrawDottedLine(p1, p2, 5f);
                Handles.DrawSolidDisc(p1, Vector3.forward, 8f);
            }
            else
            {
                Vector2 snapped = snapToGrid && activeTool != PaintTool.EraseItem ? SnapToCellCenter(currentMouseWorld) : currentMouseWorld;
                Vector2 cursorGui = WorldToGui(snapped, innerRect);
                Handles.color = activeTool == PaintTool.EraseItem ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(1f, 0.95f, 0.4f, 0.7f);
                Handles.DrawWireDisc(cursorGui, Vector3.forward, 10f);
            }

            Handles.EndGUI();
        }

        private void HandleBoardMouseEvents(Rect innerRect)
        {
            Event e = Event.current;
            mouseInsideBoard = innerRect.Contains(e.mousePosition);
            if (!mouseInsideBoard)
            {
                if (e.type == EventType.MouseUp && isDraggingWall)
                {
                    CommitWallLineDrag();
                    e.Use();
                }
                return;
            }

            currentMouseWorld = GuiToWorld(e.mousePosition, innerRect);

            // Universal Right-Click Quick Erase
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                isDraggingWall = false;
                hasPendingSecondPoint = false;
                if (TryEraseNearestAt(currentMouseWorld))
                {
                    RevalidatePuzzlePath();
                }
                e.Use();
                return;
            }

            if (e.button != 0) return;

            if (activeTool == PaintTool.WallLineDraw)
            {
                if (e.type == EventType.MouseDown)
                {
                    dragStartWorld = snapToGrid ? SnapToGridIntersection(currentMouseWorld) : currentMouseWorld;
                    isDraggingWall = true;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && isDraggingWall)
                {
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && isDraggingWall)
                {
                    CommitWallLineDrag();
                    e.Use();
                }
                return;
            }

            if (e.type != EventType.MouseDown) return;

            Vector2 snappedCell = snapToGrid ? SnapToCellCenter(currentMouseWorld) : currentMouseWorld;

            switch (activeTool)
            {
                case PaintTool.GridEdgeToggle:
                    if (TryFindNearestGridEdge(currentMouseWorld, out _, out _, out _, out Vector2 edgePos, out Vector2 edgeSize))
                    {
                        ToggleWallAt(edgePos, edgeSize);
                        RevalidatePuzzlePath();
                    }
                    break;

                case PaintTool.EraseItem:
                    if (TryEraseNearestAt(currentMouseWorld))
                    {
                        RevalidatePuzzlePath();
                    }
                    break;

                case PaintTool.MoveStart:
                    workingCopy.startPosition = snappedCell;
                    RevalidatePuzzlePath();
                    statusToast = "Moved Start point.";
                    break;

                case PaintTool.MoveGoal:
                    workingCopy.goalPosition = snappedCell;
                    RevalidatePuzzlePath();
                    statusToast = "Moved Goal target cup.";
                    break;

                case PaintTool.PlaceHole:
                    workingCopy.holes.Add(new MazeHoleDefinition(snappedCell, placeHoleRadius));
                    RevalidatePuzzlePath();
                    statusToast = "Added pit hole.";
                    break;

                case PaintTool.PlacePin:
                    workingCopy.bonusPins.Add(snappedCell);
                    statusToast = "Added Gold Bonus Pin.";
                    break;

                case PaintTool.PlaceEraserBumper:
                    workingCopy.eraserBumpers.Add(new MazeBumperDefinition(snappedCell, new Vector2(1.05f, 0.44f), placeBumperAngle, 8.2f));
                    RevalidatePuzzlePath();
                    statusToast = "Added Eraser Bounce Pad.";
                    break;

                case PaintTool.PlaceSwitchAndGate:
                    if (!hasPendingSecondPoint)
                    {
                        pendingFirstPointWorld = snappedCell;
                        hasPendingSecondPoint = true;
                        statusToast = "Switch placed! Now click where to place its Locked Gate.";
                    }
                    else
                    {
                        Vector2 gatePos = snapToGrid ? SnapToGridEdgeOrCenter(currentMouseWorld) : currentMouseWorld;
                        workingCopy.switchGates.Add(new MazeSwitchGateDefinition(pendingFirstPointWorld, gatePos, new Vector2(placeGateLength, 0.24f), placeGateAngle));
                        hasPendingSecondPoint = false;
                        RevalidatePuzzlePath();
                        statusToast = "Paired Switch & Locked Gate added!";
                    }
                    break;

                case PaintTool.PlaceMovingSlider:
                    if (!hasPendingSecondPoint)
                    {
                        pendingFirstPointWorld = snappedCell;
                        hasPendingSecondPoint = true;
                        statusToast = "Slider Start set! Click Slider End position.";
                    }
                    else
                    {
                        workingCopy.obstacles.Add(MazeObstacleDefinition.CreateSlider(pendingFirstPointWorld, snappedCell, new Vector2(1.1f, 0.24f), 1.6f));
                        hasPendingSecondPoint = false;
                        statusToast = "Moving Slider obstacle added.";
                    }
                    break;

                case PaintTool.PlaceSpinner:
                    workingCopy.obstacles.Add(MazeObstacleDefinition.CreateSpinner(snappedCell, new Vector2(2.1f, 0.24f), 65f));
                    statusToast = "Rotating Spinner added.";
                    break;
            }

            e.Use();
        }

        private void CommitWallLineDrag()
        {
            isDraggingWall = false;
            Vector2 endPos = ResolveWallDragEnd(dragStartWorld, currentMouseWorld);
            Vector2 diff = endPos - dragStartWorld;
            float len = diff.magnitude;
            if (len < 0.25f) return;

            float ang = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            workingCopy.walls.Add(new MazeWallDefinition((dragStartWorld + endPos) * 0.5f, new Vector2(len + customWallThickness, customWallThickness), ang));
            RevalidatePuzzlePath();
            statusToast = "Drew custom wall line.";
        }

        private Vector2 ResolveWallDragEnd(Vector2 start, Vector2 rawCursor)
        {
            Vector2 candidate = snapToGrid ? SnapToGridIntersection(rawCursor) : rawCursor;
            if (axisLockWalls)
            {
                Vector2 d = candidate - start;
                if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) candidate.y = start.y;
                else candidate.x = start.x;
            }
            return candidate;
        }

        private void ToggleWallAt(Vector2 pos, Vector2 size)
        {
            for (int i = workingCopy.walls.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(workingCopy.walls[i].position, pos) < 0.35f)
                {
                    workingCopy.walls.RemoveAt(i);
                    statusToast = "Removed grid wall.";
                    return;
                }
            }
            workingCopy.walls.Add(new MazeWallDefinition(pos, size, 0f));
            statusToast = "Placed grid wall.";
        }

        private bool TryEraseNearestAt(Vector2 worldPos)
        {
            const float thresh = 0.42f;

            for (int i = workingCopy.bonusPins.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(workingCopy.bonusPins[i], worldPos) <= thresh)
                {
                    workingCopy.bonusPins.RemoveAt(i);
                    statusToast = "Erased Gold Pin.";
                    return true;
                }
            }
            for (int i = workingCopy.holes.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(workingCopy.holes[i].position, worldPos) <= Mathf.Max(thresh, workingCopy.holes[i].radius))
                {
                    workingCopy.holes.RemoveAt(i);
                    statusToast = "Erased Pit Hole.";
                    return true;
                }
            }
            for (int i = workingCopy.switchGates.Count - 1; i >= 0; i--)
            {
                MazeSwitchGateDefinition sg = workingCopy.switchGates[i];
                if (Vector2.Distance(sg.switchPosition, worldPos) <= thresh ||
                    PointToObbDistance(worldPos, sg.gatePosition, sg.gateSize, sg.gateRotationDeg) <= thresh * 0.7f)
                {
                    workingCopy.switchGates.RemoveAt(i);
                    statusToast = "Erased Switch + Gate pair.";
                    return true;
                }
            }
            for (int i = workingCopy.eraserBumpers.Count - 1; i >= 0; i--)
            {
                MazeBumperDefinition b = workingCopy.eraserBumpers[i];
                if (PointToObbDistance(worldPos, b.position, b.size, b.rotationDeg) <= thresh * 0.7f)
                {
                    workingCopy.eraserBumpers.RemoveAt(i);
                    statusToast = "Erased Eraser Bumper.";
                    return true;
                }
            }
            for (int i = workingCopy.obstacles.Count - 1; i >= 0; i--)
            {
                MazeObstacleDefinition obs = workingCopy.obstacles[i];
                Vector2 mid = (obs.startPosition + obs.endPosition) * 0.5f;
                if (Vector2.Distance(mid, worldPos) <= thresh * 1.4f)
                {
                    workingCopy.obstacles.RemoveAt(i);
                    statusToast = "Erased dynamic obstacle.";
                    return true;
                }
            }
            for (int i = workingCopy.walls.Count - 1; i >= 0; i--)
            {
                MazeWallDefinition w = workingCopy.walls[i];
                if (PointToObbDistance(worldPos, w.position, w.size, w.rotationDeg) <= thresh * 0.65f)
                {
                    workingCopy.walls.RemoveAt(i);
                    statusToast = "Erased Wall segment.";
                    return true;
                }
            }
            return false;
        }

        private void GenerateBaseMazeIntoWorkingCopy()
        {
            GeometryMazeGenerator.GenerationConfig cfg = new GeometryMazeGenerator.GenerationConfig
            {
                levelNumber = workingCopy.levelNumber,
                levelName = workingCopy.levelName,
                difficultyLabel = workingCopy.difficultyLabel,
                modeLabel = workingCopy.modeLabel,
                cols = genCols,
                rows = genRows,
                seed = genSeed,
                holeCount = genHoles,
                pinCount = genPins,
                includeEraserBumper = genEraser,
                includeSwitchGate = genSwitch,
                includeMovingSlider = genSlider,
                includeSpinner = genSpinner,
                threeStarTime = workingCopy.threeStarTime,
                twoStarTime = workingCopy.twoStarTime,
                timeTrialLimit = workingCopy.timeTrialLimit,
                trayColor = workingCopy.trayColor,
                outerFrameColor = workingCopy.outerFrameColor,
                wallColor = workingCopy.wallColor
            };
            GeometryMazeGenerator.PopulateLevelData(workingCopy, cfg);
            RevalidatePuzzlePath();
            statusToast = $"Generated {genCols}x{genRows} maze base (seed {genSeed}). Tweak walls or save!";
        }

        private void RevalidatePuzzlePath()
        {
            verifiedPhase1Path.Clear();
            verifiedPhase2Path.Clear();
            bypassPath.Clear();

            Vector2 size = workingCopy.trayInnerSize;
            const float step = 0.18f;
            int gCols = Mathf.Max(8, Mathf.FloorToInt(size.x / step));
            int gRows = Mathf.Max(8, Mathf.FloorToInt(size.y / step));
            float cellW = size.x / gCols;
            float cellH = size.y / gRows;

            Vector2Int startCell = WorldToSolverCell(workingCopy.startPosition, size, gCols, gRows);
            Vector2Int goalCell = WorldToSolverCell(workingCopy.goalPosition, size, gCols, gRows);

            bool hasSwitches = workingCopy.switchGates != null && workingCopy.switchGates.Count > 0;
            if (!hasSwitches)
            {
                bool[,] blocked = BuildBlockedGrid(size, gCols, gRows, cellW, cellH, passLockedGates: true);
                blocked[startCell.x, startCell.y] = false;
                blocked[goalCell.x, goalCell.y] = false;

                if (TryRunSolverBfs(startCell, goalCell, gCols, gRows, blocked, size, cellW, cellH, verifiedPhase2Path))
                {
                    puzzleStatus = PuzzleCheckStatus.VerifiedSolvable;
                    puzzleMessage = "VERIFIED SOLVABLE! Ball has a clean clear path from Start to Goal.";
                }
                else
                {
                    puzzleStatus = PuzzleCheckStatus.PathBlocked;
                    puzzleMessage = "PATH BLOCKED! Open a corridor >= 2x ball radius between Start and Goal.";
                }
                return;
            }

            // Puzzle validation WITH Switch + Locked Gates ("Ball sahi way se switch dabakar hi Goal ja sake!")
            bool[,] lockedBlocked = BuildBlockedGrid(size, gCols, gRows, cellW, cellH, passLockedGates: false);
            lockedBlocked[startCell.x, startCell.y] = false;
            lockedBlocked[goalCell.x, goalCell.y] = false;

            // Check 1: Can ball reach Goal BEFORE pressing switches? (If yes -> Shortcut bypass warning!)
            if (TryRunSolverBfs(startCell, goalCell, gCols, gRows, lockedBlocked, size, cellW, cellH, bypassPath))
            {
                puzzleStatus = PuzzleCheckStatus.SwitchBypassable;
                puzzleMessage = "BYPASS DETECTED! Ball can reach Goal without touching the Switch! Seal the shortcut with a wall or Locked Gate.";
                return;
            }

            // Check 2: Can ball reach the first Switch Plate while gates are locked?
            Vector2 swWorld = workingCopy.switchGates[0].switchPosition;
            Vector2Int swCell = WorldToSolverCell(swWorld, size, gCols, gRows);
            lockedBlocked[swCell.x, swCell.y] = false;

            if (!TryRunSolverBfs(startCell, swCell, gCols, gRows, lockedBlocked, size, cellW, cellH, verifiedPhase1Path))
            {
                puzzleStatus = PuzzleCheckStatus.SwitchUnreachable;
                puzzleMessage = "SWITCH BLOCKED! Ball cannot reach the Cyan Switch Plate from Start.";
                return;
            }

            // Check 3: After switch opens gates, can ball reach Goal?
            bool[,] openBlocked = BuildBlockedGrid(size, gCols, gRows, cellW, cellH, passLockedGates: true);
            openBlocked[swCell.x, swCell.y] = false;
            openBlocked[goalCell.x, goalCell.y] = false;

            if (TryRunSolverBfs(swCell, goalCell, gCols, gRows, openBlocked, size, cellW, cellH, verifiedPhase2Path))
            {
                puzzleStatus = PuzzleCheckStatus.VerifiedSolvable;
                puzzleMessage = "PUZZLE VERIFIED! Ball MUST reach Switch first, unlock Gate, then reach Goal!";
            }
            else
            {
                puzzleStatus = PuzzleCheckStatus.PathBlocked;
                puzzleMessage = "GOAL STILL BLOCKED! Even after Switch opens Gate, no path leads to Goal.";
            }
        }

        private bool[,] BuildBlockedGrid(Vector2 traySize, int gCols, int gRows, float cellW, float cellH, bool passLockedGates)
        {
            bool[,] blocked = new bool[gCols, gRows];
            float wallMargin = workingCopy.ballRadius * 0.84f;
            float holeFactor = 0.72f;

            for (int c = 0; c < gCols; c++)
            {
                for (int r = 0; r < gRows; r++)
                {
                    Vector2 wp = new Vector2(
                        -traySize.x * 0.5f + (c + 0.5f) * cellW,
                        -traySize.y * 0.5f + (r + 0.5f) * cellH);

                    if (Mathf.Abs(wp.x) > traySize.x * 0.5f - wallMargin * 0.75f ||
                        Mathf.Abs(wp.y) > traySize.y * 0.5f - wallMargin * 0.75f)
                    {
                        blocked[c, r] = true;
                        continue;
                    }

                    bool hit = false;
                    if (workingCopy.walls != null)
                    {
                        for (int i = 0; i < workingCopy.walls.Count; i++)
                        {
                            MazeWallDefinition w = workingCopy.walls[i];
                            if (PointToObbDistance(wp, w.position, w.size, w.rotationDeg) < wallMargin)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }

                    if (!hit && !passLockedGates && workingCopy.switchGates != null)
                    {
                        for (int i = 0; i < workingCopy.switchGates.Count; i++)
                        {
                            MazeSwitchGateDefinition sg = workingCopy.switchGates[i];
                            if (PointToObbDistance(wp, sg.gatePosition, sg.gateSize, sg.gateRotationDeg) < wallMargin)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }

                    if (!hit && workingCopy.holes != null)
                    {
                        for (int i = 0; i < workingCopy.holes.Count; i++)
                        {
                            if (Vector2.Distance(wp, workingCopy.holes[i].position) < workingCopy.holes[i].radius * holeFactor)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }

                    blocked[c, r] = hit;
                }
            }
            return blocked;
        }

        private static bool TryRunSolverBfs(
            Vector2Int start,
            Vector2Int target,
            int gCols,
            int gRows,
            bool[,] blocked,
            Vector2 traySize,
            float cellW,
            float cellH,
            List<Vector2> outWorldPath)
        {
            outWorldPath.Clear();
            bool[,] visited = new bool[gCols, gRows];
            Vector2Int[,] parent = new Vector2Int[gCols, gRows];
            Queue<Vector2Int> q = new Queue<Vector2Int>();

            visited[start.x, start.y] = true;
            parent[start.x, start.y] = new Vector2Int(-1, -1);
            q.Enqueue(start);

            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };
            bool found = false;

            while (q.Count > 0)
            {
                Vector2Int cur = q.Dequeue();
                if (cur == target)
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nx = cur.x + dx[i];
                    int ny = cur.y + dy[i];
                    if (nx < 0 || nx >= gCols || ny < 0 || ny >= gRows) continue;
                    if (visited[nx, ny] || blocked[nx, ny]) continue;

                    visited[nx, ny] = true;
                    parent[nx, ny] = cur;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }

            if (!found) return false;

            Vector2Int trace = target;
            while (trace.x >= 0)
            {
                outWorldPath.Add(new Vector2(
                    -traySize.x * 0.5f + (trace.x + 0.5f) * cellW,
                    -traySize.y * 0.5f + (trace.y + 0.5f) * cellH));
                trace = parent[trace.x, trace.y];
            }
            outWorldPath.Reverse();
            return true;
        }

        private static Vector2Int WorldToSolverCell(Vector2 wp, Vector2 traySize, int gCols, int gRows)
        {
            float u = Mathf.Clamp01((wp.x + traySize.x * 0.5f) / traySize.x);
            float v = Mathf.Clamp01((wp.y + traySize.y * 0.5f) / traySize.y);
            int c = Mathf.Clamp(Mathf.FloorToInt(u * gCols), 0, gCols - 1);
            int r = Mathf.Clamp(Mathf.FloorToInt(v * gRows), 0, gRows - 1);
            return new Vector2Int(c, r);
        }

        private bool SaveWorkingCopyToAsset(out GeometryMazeLevelData savedAsset)
        {
            Directory.CreateDirectory(GeometryBoxMazeSetup.DATA_FOLDER);
            string path = $"{GeometryBoxMazeSetup.DATA_FOLDER}/GeometryMazeLevel_{workingCopy.levelNumber:00}.asset";
            GeometryMazeLevelData asset = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelData>(path);
            if (asset == null)
            {
                asset = CreateInstance<GeometryMazeLevelData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.levelNumber = workingCopy.levelNumber;
            asset.levelName = workingCopy.levelName;
            asset.difficultyLabel = workingCopy.difficultyLabel;
            asset.modeLabel = workingCopy.modeLabel;
            asset.trayInnerSize = workingCopy.trayInnerSize;
            asset.trayColor = workingCopy.trayColor;
            asset.outerFrameColor = workingCopy.outerFrameColor;
            asset.wallColor = workingCopy.wallColor;
            asset.threeStarTime = workingCopy.threeStarTime;
            asset.twoStarTime = workingCopy.twoStarTime;
            asset.timeTrialLimit = workingCopy.timeTrialLimit;
            asset.startPosition = workingCopy.startPosition;
            asset.goalPosition = workingCopy.goalPosition;
            asset.ballRadius = workingCopy.ballRadius;
            asset.walls = new List<MazeWallDefinition>(workingCopy.walls);
            asset.holes = new List<MazeHoleDefinition>(workingCopy.holes);
            asset.obstacles = new List<MazeObstacleDefinition>(workingCopy.obstacles);
            asset.bonusPins = new List<Vector2>(workingCopy.bonusPins);
            asset.eraserBumpers = new List<MazeBumperDefinition>(workingCopy.eraserBumpers);
            asset.switchGates = new List<MazeSwitchGateDefinition>(workingCopy.switchGates);
            EditorUtility.SetDirty(asset);

            catalog = AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(GeometryBoxMazeSetup.CATALOG_PATH);
            if (catalog == null)
            {
                catalog = CreateInstance<GeometryMazeLevelCatalog>();
                AssetDatabase.CreateAsset(catalog, GeometryBoxMazeSetup.CATALOG_PATH);
            }
            if (catalog.levels == null) catalog.levels = new List<GeometryMazeLevelData>();
            if (!catalog.levels.Contains(asset))
            {
                catalog.levels.Add(asset);
                catalog.levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            selectedCatalogIndex = Mathf.Max(0, catalog.levels.IndexOf(asset));
            savedAsset = asset;
            statusToast = $"Saved '{path}' ({asset.walls.Count} walls, {asset.holes.Count} holes, {asset.switchGates.Count} switches).";
            return true;
        }

        private void SaveAndPlayTestLevel()
        {
            if (!SaveWorkingCopyToAsset(out GeometryMazeLevelData saved)) return;

            if (!File.Exists(GeometryBoxMazeSetup.SCENE_PATH))
            {
                GeometryBoxMazeSetup.BuildSceneAndAllAssets(openResultScene: true);
            }
            else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != GeometryBoxMazeSetup.SCENE_PATH)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(GeometryBoxMazeSetup.SCENE_PATH);
            }

            EditorPrefs.SetInt("GeometryMaze_EditorPlayTestLevel", saved.levelNumber);
            EditorApplication.isPlaying = true;
        }

        // -------------------------------------------------------------
        // COORDINATE & GEOMETRY HELPERS
        // -------------------------------------------------------------

        private Vector2 WorldToGui(Vector2 worldPos, Rect innerRect)
        {
            Vector2 size = workingCopy.trayInnerSize;
            float u = (worldPos.x + size.x * 0.5f) / size.x;
            float v = (worldPos.y + size.y * 0.5f) / size.y;
            return new Vector2(
                Mathf.Lerp(innerRect.xMin, innerRect.xMax, u),
                Mathf.Lerp(innerRect.yMax, innerRect.yMin, v));
        }

        private Vector2 GuiToWorld(Vector2 guiPos, Rect innerRect)
        {
            Vector2 size = workingCopy.trayInnerSize;
            float u = Mathf.Clamp01((guiPos.x - innerRect.xMin) / Mathf.Max(1f, innerRect.width));
            float v = Mathf.Clamp01((innerRect.yMax - guiPos.y) / Mathf.Max(1f, innerRect.height));
            return new Vector2(u * size.x - size.x * 0.5f, v * size.y - size.y * 0.5f);
        }

        private float WorldSizeToPixels(float worldLen, Rect innerRect)
        {
            return (worldLen / Mathf.Max(0.1f, workingCopy.trayInnerSize.x)) * innerRect.width;
        }

        private Vector2 SnapToCellCenter(Vector2 worldPos)
        {
            Vector2 size = workingCopy.trayInnerSize;
            float cellW = size.x / genCols;
            float cellH = size.y / genRows;
            int c = Mathf.Clamp(Mathf.FloorToInt((worldPos.x + size.x * 0.5f) / cellW), 0, genCols - 1);
            int r = Mathf.Clamp(Mathf.FloorToInt((worldPos.y + size.y * 0.5f) / cellH), 0, genRows - 1);
            return new Vector2(-size.x * 0.5f + (c + 0.5f) * cellW, -size.y * 0.5f + (r + 0.5f) * cellH);
        }

        private Vector2 SnapToGridIntersection(Vector2 worldPos)
        {
            Vector2 size = workingCopy.trayInnerSize;
            float cellW = size.x / genCols;
            float cellH = size.y / genRows;
            int c = Mathf.Clamp(Mathf.RoundToInt((worldPos.x + size.x * 0.5f) / cellW), 0, genCols);
            int r = Mathf.Clamp(Mathf.RoundToInt((worldPos.y + size.y * 0.5f) / cellH), 0, genRows);
            return new Vector2(-size.x * 0.5f + c * cellW, -size.y * 0.5f + r * cellH);
        }

        private Vector2 SnapToGridEdgeOrCenter(Vector2 worldPos)
        {
            return TryFindNearestGridEdge(worldPos, out _, out _, out _, out Vector2 edgePos, out _) ? edgePos : SnapToCellCenter(worldPos);
        }

        private bool TryFindNearestGridEdge(Vector2 worldPos, out bool isVertical, out int c, out int r, out Vector2 center, out Vector2 size)
        {
            Vector2 tray = workingCopy.trayInnerSize;
            float cellW = tray.x / genCols;
            float cellH = tray.y / genRows;
            float left = -tray.x * 0.5f;
            float bottom = -tray.y * 0.5f;
            float thick = genCols >= 7 ? 0.16f : 0.20f;

            float bestDist = 0.65f;
            isVertical = false;
            c = -1;
            r = -1;
            center = Vector2.zero;
            size = Vector2.zero;

            for (int col = 0; col < genCols - 1; col++)
            {
                for (int row = 0; row < genRows; row++)
                {
                    Vector2 p = new Vector2(left + (col + 1) * cellW, bottom + (row + 0.5f) * cellH);
                    float d = Vector2.Distance(worldPos, p);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        isVertical = true;
                        c = col;
                        r = row;
                        center = p;
                        size = new Vector2(thick, cellH + thick);
                    }
                }
            }

            for (int col = 0; col < genCols; col++)
            {
                for (int row = 0; row < genRows - 1; row++)
                {
                    Vector2 p = new Vector2(left + (col + 0.5f) * cellW, bottom + (row + 1) * cellH);
                    float d = Vector2.Distance(worldPos, p);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        isVertical = false;
                        c = col;
                        r = row;
                        center = p;
                        size = new Vector2(cellW + thick, thick);
                    }
                }
            }

            return c >= 0;
        }

        private void DrawOrientedBox(Vector2 worldCenter, Vector2 worldSize, float rotDeg, Rect innerRect, Color color)
        {
            float rad = rotDeg * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (worldSize.x * 0.5f);
            Vector2 up = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)) * (worldSize.y * 0.5f);

            Vector3[] verts =
            {
                WorldToGui(worldCenter - right - up, innerRect),
                WorldToGui(worldCenter + right - up, innerRect),
                WorldToGui(worldCenter + right + up, innerRect),
                WorldToGui(worldCenter - right + up, innerRect)
            };
            Handles.DrawSolidRectangleWithOutline(verts, color, new Color(0f, 0f, 0f, 0.25f));
        }

        private Vector3[] ToGuiPoly(List<Vector2> worldPts, Rect innerRect)
        {
            Vector3[] arr = new Vector3[worldPts.Count];
            for (int i = 0; i < worldPts.Count; i++)
            {
                arr[i] = WorldToGui(worldPts[i], innerRect);
            }
            return arr;
        }

        private static float PointToObbDistance(Vector2 p, Vector2 center, Vector2 size, float rotDeg)
        {
            Vector2 d = p - center;
            float rad = -rotDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            Vector2 local = new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
            Vector2 half = size * 0.5f;
            Vector2 q = new Vector2(Mathf.Max(Mathf.Abs(local.x) - half.x, 0f), Mathf.Max(Mathf.Abs(local.y) - half.y, 0f));
            return q.magnitude;
        }

        private static void GetThemeColors(TrayThemePreset preset, out Color tray, out Color frame, out Color wall)
        {
            switch (preset)
            {
                case TrayThemePreset.MintGreenBox:
                    tray = new Color(0.78f, 0.96f, 0.89f, 1f);
                    frame = new Color(0.12f, 0.56f, 0.42f, 1f);
                    wall = new Color(0.95f, 1f, 0.97f, 1f);
                    break;
                case TrayThemePreset.VintageSteelBox:
                    tray = new Color(0.84f, 0.88f, 0.92f, 1f);
                    frame = new Color(0.26f, 0.32f, 0.40f, 1f);
                    wall = new Color(0.96f, 0.97f, 0.99f, 1f);
                    break;
                case TrayThemePreset.IndigoPastelBox:
                    tray = new Color(0.84f, 0.83f, 0.97f, 1f);
                    frame = new Color(0.32f, 0.28f, 0.64f, 1f);
                    wall = new Color(0.96f, 0.96f, 1f, 1f);
                    break;
                case TrayThemePreset.RoyalCobaltBox:
                    tray = new Color(0.76f, 0.87f, 0.98f, 1f);
                    frame = new Color(0.14f, 0.34f, 0.62f, 1f);
                    wall = new Color(0.98f, 0.98f, 1f, 1f);
                    break;
                default:
                    tray = new Color(0.79f, 0.91f, 0.98f, 1f);
                    frame = new Color(0.18f, 0.47f, 0.76f, 1f);
                    wall = new Color(0.95f, 0.98f, 1f, 1f);
                    break;
            }
        }
    }
}
#endif
