#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Commercial-grade Visual Level Editor Window for 2D Ball Drop & Block Destruction levels.
/// Open via Tools -> Ball Merge -> 3. Open Visual Level Editor (Ctrl+Shift+L).
/// </summary>
public partial class BallMergeLevelEditorWindow : EditorWindow
{
    public enum EditorTool
    {
        Select = 0,
        Brush = 1,
        Erase = 2
    }

    // Layout dimensions
    private const float LEFT_PANEL_WIDTH = 270f;
    private const float RIGHT_PANEL_WIDTH = 305f;
    private const float BASE_PIXELS_PER_UNIT = 60f;
    private const float CONTAINER_FLOOR_Y = -4f;

    // Assets & Library
    private BlockLibrarySO blockLibrary;
    private readonly List<LevelDataSO> allLevels = new List<LevelDataSO>();
    private LevelDataSO currentLevel;
    private BlockDefinitionSO activePaletteBlock;

    // UI Scroll & Search
    private Vector2 levelListScroll;
    private Vector2 paletteScroll;
    private Vector2 inspectorScroll;
    private string levelSearchFilter = string.Empty;
    private bool showEndlessLevels = false;
    private string renameDraft = string.Empty;
    private bool isRenamingLevel = false;

    // Editor Preferences / Toggles
    private EditorTool currentTool = EditorTool.Select;
    private bool snapToGrid = true;
    private float gridSize = 0.25f;
    private bool showGrid = true;
    private bool showCameraFrame = true;
    private bool showMergeRadiusPreview = true;
    private bool showHPBadges = true;
    private bool autoSave = true;

    // Canvas Camera
    private float zoom = 1.0f;
    private Vector2 panOffset = Vector2.zero;
    private Rect lastCanvasRect;

    // Selection & Clipboard
    private readonly HashSet<int> selectedIndices = new HashSet<int>();
    private readonly List<LevelBlockPlacement> clipboard = new List<LevelBlockPlacement>();

    // Interaction State
    private enum DragAction
    {
        None,
        Pan,
        MoveBlocks,
        BoxSelect,
        ResizeCorner,
        RotateBlock,
        MovePathEnd
    }

    private DragAction dragAction = DragAction.None;
    private Vector2 dragStartCanvasPos;
    private Vector2 dragCurrentCanvasPos;
    private Vector2 dragStartWorldPos;
    private Vector2 panStartOffset;
    private bool dragRecordedUndo = false;
    private bool isSpaceHeld = false;

    // Move / Resize / Rotate state
    private readonly Dictionary<int, Vector2> moveStartPositions = new Dictionary<int, Vector2>();
    private int movePrimaryIndex = -1;
    private int gizmoTargetIndex = -1;
    private Vector2 resizeCornerSign;
    private Vector2 resizeAnchorWorld;
    private float resizeOrigRotation;
    private Vector2 resizeOrigSize;

    // Styles & Icons
    private GUIStyle headerStyle;
    private GUIStyle cardStyle;
    private GUIStyle badgeStyle;
    private Texture2D whiteTex;

    [MenuItem("Tools/Ball Merge/3. Open Visual Level Editor %#L")]
    public static void ShowWindow()
    {
        BallMergeLevelEditorWindow window = GetWindow<BallMergeLevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(1040f, 640f);
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += HandleUndoRedo;
        RefreshLibrariesAndLevels();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;
    }

    private void HandleUndoRedo()
    {
        if (currentLevel != null)
        {
            PruneInvalidSelection();
            EditorUtility.SetDirty(currentLevel);
        }
        Repaint();
    }

    private void RefreshLibrariesAndLevels()
    {
        blockLibrary = BlockLibrarySO.LoadDefault();
        if (blockLibrary == null)
        {
            blockLibrary = AssetDatabase.LoadAssetAtPath<BlockLibrarySO>(BlockLevelSetupUtility.BLOCK_LIBRARY_PATH);
        }

        if (blockLibrary != null && blockLibrary.blocks != null && blockLibrary.blocks.Count > 0 && activePaletteBlock == null)
        {
            activePaletteBlock = blockLibrary.blocks[0];
        }

        allLevels.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            LevelDataSO lvl = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (lvl != null)
            {
                allLevels.Add(lvl);
            }
        }

        allLevels.Sort((a, b) =>
        {
            int modeCmp = b.gameMode.CompareTo(a.gameMode); // Level mode first
            if (modeCmp != 0) return modeCmp;
            int numCmp = a.levelNumber.CompareTo(b.levelNumber);
            return numCmp != 0 ? numCmp : string.Compare(a.name, b.name, StringComparison.Ordinal);
        });

        if (currentLevel == null && allLevels.Count > 0)
        {
            SelectLevel(allLevels.FirstOrDefault(l => l.gameMode == GameMode.Level) ?? allLevels[0]);
        }
        else if (currentLevel != null)
        {
            PruneInvalidSelection();
        }
    }

    private void SelectLevel(LevelDataSO level)
    {
        currentLevel = level;
        selectedIndices.Clear();
        isRenamingLevel = false;
        dragAction = DragAction.None;
        if (currentLevel != null && currentLevel.blocks != null)
        {
            for (int i = 0; i < currentLevel.blocks.Count; i++)
            {
                currentLevel.blocks[i]?.EnsureGuid();
            }
        }
        ResetCanvasView();
        Repaint();
    }

    private void OnGUI()
    {
        InitStyles();
        HandleGlobalKeyboardShortcuts();

        DrawTopToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawLeftSidebar();
        DrawCenterWorkspace();
        DrawRightInspector();
        EditorGUILayout.EndHorizontal();

        // Keep smooth ghost cursor & marquee updates while hovering or dragging
        if (Event.current.type == EventType.MouseMove || dragAction != DragAction.None || currentTool == EditorTool.Brush)
        {
            if (lastCanvasRect.Contains(Event.current.mousePosition))
            {
                Repaint();
            }
        }
    }

    private void InitStyles()
    {
        if (whiteTex == null)
        {
            whiteTex = Texture2D.whiteTexture;
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
        }

        if (cardStyle == null)
        {
            cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 6, 6),
                margin = new RectOffset(2, 2, 2, 2)
            };
        }

        if (badgeStyle == null)
        {
            badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }

    #region Top Toolbar

    private void DrawTopToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.toolbar);

        // Row 1: Tools, Grid, Zoom, Edit Actions, Preview/Play, Save
        EditorGUILayout.BeginHorizontal();

        // Tool Buttons
        DrawToolToggle("Select (Q)", EditorTool.Select, 72);
        DrawToolToggle("Brush (B)", EditorTool.Brush, 68);
        DrawToolToggle("Erase (E)", EditorTool.Erase, 65);

        GUILayout.Space(8);

        // Snap & Grid
        snapToGrid = GUILayout.Toggle(snapToGrid, "Snap", EditorStyles.toolbarButton, GUILayout.Width(46));
        EditorGUI.BeginDisabledGroup(!snapToGrid);
        gridSize = EditorGUILayout.FloatField(gridSize, GUILayout.Width(40));
        gridSize = Mathf.Clamp(gridSize, 0.05f, 2f);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(8);

        // Zoom Controls
        if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(22))) SetZoom(zoom - 0.15f);
        zoom = GUILayout.HorizontalSlider(zoom, 0.4f, 3.0f, GUILayout.Width(70));
        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(22))) SetZoom(zoom + 0.15f);
        if (GUILayout.Button($"Fit ({Mathf.RoundToInt(zoom * 100)}%)", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            ResetCanvasView();
        }

        GUILayout.Space(8);

        // Edit Operations
        if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(44))) Undo.PerformUndo();
        if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(44))) Undo.PerformRedo();

        EditorGUI.BeginDisabledGroup(selectedIndices.Count == 0);
        if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(44))) CopySelected();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(clipboard.Count == 0 || currentLevel == null);
        if (GUILayout.Button("Paste", EditorStyles.toolbarButton, GUILayout.Width(46))) PasteClipboard();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(selectedIndices.Count == 0);
        if (GUILayout.Button("Dup", EditorStyles.toolbarButton, GUILayout.Width(38))) DuplicateSelected();
        if (GUILayout.Button("Del", EditorStyles.toolbarButton, GUILayout.Width(36))) DeleteSelected();
        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();

        // Scene Preview & Play Test
        EditorGUI.BeginDisabledGroup(currentLevel == null);
        if (GUILayout.Button("Preview Scene", EditorStyles.toolbarButton, GUILayout.Width(96)))
        {
            PreviewLevelInActiveScene();
        }
        if (GUILayout.Button("Clear Preview", EditorStyles.toolbarButton, GUILayout.Width(92)))
        {
            ClearScenePreview();
        }
        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.35f, 0.9f, 0.45f);
        if (GUILayout.Button("▶ Play Level", EditorStyles.toolbarButton, GUILayout.Width(86)))
        {
            PlayTestCurrentLevel();
        }
        GUI.backgroundColor = oldBg;
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(6);
        autoSave = GUILayout.Toggle(autoSave, "Auto-Save", EditorStyles.toolbarButton, GUILayout.Width(74));
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(44)))
        {
            SaveCurrentLevel(forceSaveAssetDatabase: true);
        }

        EditorGUILayout.EndHorizontal();

        // Row 2: Overlays + Align & Distribute (active when 2+ blocks selected)
        EditorGUILayout.BeginHorizontal();
        showGrid = GUILayout.Toggle(showGrid, "Grid", EditorStyles.toolbarButton, GUILayout.Width(42));
        showCameraFrame = GUILayout.Toggle(showCameraFrame, "9:16 Cam", EditorStyles.toolbarButton, GUILayout.Width(68));
        showMergeRadiusPreview = GUILayout.Toggle(showMergeRadiusPreview, "Blast Reach", EditorStyles.toolbarButton, GUILayout.Width(80));
        showHPBadges = GUILayout.Toggle(showHPBadges, "HP Tags", EditorStyles.toolbarButton, GUILayout.Width(62));

        GUILayout.Space(12);
        GUILayout.Label("Align:", EditorStyles.miniLabel, GUILayout.Width(36));

        EditorGUI.BeginDisabledGroup(selectedIndices.Count < 2);
        if (GUILayout.Button("Left", EditorStyles.toolbarButton, GUILayout.Width(38))) AlignSelected(AlignOp.Left);
        if (GUILayout.Button("CtrX", EditorStyles.toolbarButton, GUILayout.Width(38))) AlignSelected(AlignOp.CenterX);
        if (GUILayout.Button("Right", EditorStyles.toolbarButton, GUILayout.Width(42))) AlignSelected(AlignOp.Right);
        if (GUILayout.Button("Top", EditorStyles.toolbarButton, GUILayout.Width(34))) AlignSelected(AlignOp.Top);
        if (GUILayout.Button("MidY", EditorStyles.toolbarButton, GUILayout.Width(40))) AlignSelected(AlignOp.CenterY);
        if (GUILayout.Button("Bot", EditorStyles.toolbarButton, GUILayout.Width(32))) AlignSelected(AlignOp.Bottom);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(selectedIndices.Count < 3);
        if (GUILayout.Button("Dist H", EditorStyles.toolbarButton, GUILayout.Width(46))) DistributeSelected(horizontal: true);
        if (GUILayout.Button("Dist V", EditorStyles.toolbarButton, GUILayout.Width(46))) DistributeSelected(horizontal: false);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(selectedIndices.Count < 1);
        if (GUILayout.Button("Mirror X", EditorStyles.toolbarButton, GUILayout.Width(58))) MirrorSelectedHorizontal();
        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();
        if (currentLevel != null)
        {
            GUILayout.Label($"Editing: LVL {currentLevel.levelNumber} - {currentLevel.levelName}", EditorStyles.miniBoldLabel);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawToolToggle(string label, EditorTool tool, float width)
    {
        bool active = currentTool == tool;
        Color prev = GUI.backgroundColor;
        if (active) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Toggle(active, label, EditorStyles.toolbarButton, GUILayout.Width(width)) && !active)
        {
            currentTool = tool;
        }
        GUI.backgroundColor = prev;
    }

    #endregion

    #region Left Sidebar (Levels & Block Palette)

    private void DrawLeftSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));

        // --- SECTION 1: LEVEL MANAGEMENT ---
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Height(285));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Levels", headerStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(55))) RefreshLibrariesAndLevels();
        if (GUILayout.Button("Sync DB", EditorStyles.miniButton, GUILayout.Width(58)))
        {
            BlockLevelSetupUtility.SyncAllLevelsToDatabase();
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("LevelDatabase synced!"));
        }
        EditorGUILayout.EndHorizontal();

        // Action Buttons Row
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ New", EditorStyles.miniButtonLeft)) CreateNewLevel();
        EditorGUI.BeginDisabledGroup(currentLevel == null);
        if (GUILayout.Button("Dup", EditorStyles.miniButtonMid)) DuplicateCurrentLevel();
        if (GUILayout.Button("Rename", EditorStyles.miniButtonMid))
        {
            isRenamingLevel = !isRenamingLevel;
            renameDraft = currentLevel != null ? currentLevel.levelName : "";
        }
        if (GUILayout.Button("Del", EditorStyles.miniButtonRight)) DeleteCurrentLevel();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        // Inline Rename bar
        if (isRenamingLevel && currentLevel != null)
        {
            EditorGUILayout.BeginHorizontal();
            renameDraft = EditorGUILayout.TextField(renameDraft);
            if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(48)))
            {
                RenameCurrentLevel(renameDraft);
                isRenamingLevel = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        // JSON Import/Export Row
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(currentLevel == null);
        if (GUILayout.Button("Export JSON", EditorStyles.miniButtonLeft)) ExportCurrentLevelJson();
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Import JSON", EditorStyles.miniButtonRight)) ImportJsonIntoLevel();
        EditorGUILayout.EndHorizontal();

        // Search Filter
        EditorGUILayout.BeginHorizontal();
        levelSearchFilter = EditorGUILayout.TextField(levelSearchFilter, EditorStyles.toolbarSearchField);
        showEndlessLevels = GUILayout.Toggle(showEndlessLevels, "All", EditorStyles.miniButton, GUILayout.Width(34));
        EditorGUILayout.EndHorizontal();

        // Level Scroll List
        levelListScroll = EditorGUILayout.BeginScrollView(levelListScroll);
        if (allLevels.Count == 0)
        {
            EditorGUILayout.HelpBox("No levels found. Click '+ New' or Tools -> Ball Merge -> 4. Generate Block Assets & Sample Levels.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < allLevels.Count; i++)
            {
                LevelDataSO lvl = allLevels[i];
                if (lvl == null) continue;
                if (!showEndlessLevels && lvl.gameMode != GameMode.Level) continue;

                string label = $"LVL {lvl.levelNumber}: {lvl.levelName}  [{lvl.GetRequiredBlockCount(blockLibrary)} blk]";
                if (!string.IsNullOrEmpty(levelSearchFilter) &&
                    label.IndexOf(levelSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool isSelected = lvl == currentLevel;
                Color prevBg = GUI.backgroundColor;
                if (isSelected) GUI.backgroundColor = new Color(0.32f, 0.72f, 1f);
                if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(22)))
                {
                    SelectLevel(lvl);
                }
                GUI.backgroundColor = prevBg;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // --- SECTION 2: BLOCK PALETTE ---
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Block Palette (Drag or Click)", headerStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ Type", EditorStyles.miniButton, GUILayout.Width(52)))
        {
            CreateCustomBlockType();
        }
        EditorGUILayout.EndHorizontal();

        if (blockLibrary == null || blockLibrary.blocks == null || blockLibrary.blocks.Count == 0)
        {
            EditorGUILayout.HelpBox("BlockLibrary not found yet. Generate default blocks & sample levels in 1 click:", MessageType.Warning);
            if (GUILayout.Button("Generate Default Blocks & Levels", GUILayout.Height(30)))
            {
                BlockLevelSetupUtility.GenerateAllBlockAssetsAndSampleLevels();
                RefreshLibrariesAndLevels();
            }
        }
        else
        {
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);
            for (int i = 0; i < blockLibrary.blocks.Count; i++)
            {
                BlockDefinitionSO def = blockLibrary.blocks[i];
                if (def == null) continue;
                DrawPaletteItem(def);
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
    }

    private void DrawPaletteItem(BlockDefinitionSO def)
    {
        bool isBrushActive = activePaletteBlock == def && currentTool == EditorTool.Brush;
        bool isSelectedTemplate = activePaletteBlock == def;

        Rect itemRect = EditorGUILayout.BeginHorizontal(cardStyle, GUILayout.Height(50));
        Color highlight = isBrushActive
            ? new Color(0.25f, 0.65f, 0.95f, 0.3f)
            : (isSelectedTemplate ? new Color(1f, 1f, 1f, 0.1f) : Color.clear);
        if (highlight.a > 0f) EditorGUI.DrawRect(itemRect, highlight);

        // Thumbnail preview
        Rect iconRect = GUILayoutUtility.GetRect(38, 38, GUILayout.Width(38), GUILayout.Height(38));
        if (def.baseSprite != null)
        {
            DrawSpritePreviewInRect(iconRect, def.baseSprite, def.baseColor);
        }
        else
        {
            EditorGUI.DrawRect(iconRect, def.accentColor);
        }

        EditorGUILayout.BeginVertical();
        GUILayout.Label(def.displayName, EditorStyles.boldLabel);
        string meta = def.isDestructible
            ? $"{def.category}  |  HP: {def.defaultHP}  |  +{def.scoreReward} pts"
            : $"{def.category}  |  Static Obstacle";
        GUILayout.Label(meta, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // Handle Click -> activate Brush tool, Drag -> Drag&Drop onto Canvas
        Event e = Event.current;
        if (itemRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                activePaletteBlock = def;
                currentTool = EditorTool.Brush;
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                activePaletteBlock = def;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("BallMergeBlockDef", def);
                DragAndDrop.objectReferences = new UnityEngine.Object[] { def };
                DragAndDrop.StartDrag(def.displayName);
                e.Use();
            }
        }
    }

    #endregion

    #region Right Inspector Panel

    private void DrawRightInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(RIGHT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

        if (currentLevel == null)
        {
            EditorGUILayout.HelpBox("Select or create a level from the left panel.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        // --- SELECTED BLOCK(S) INSPECTOR ---
        if (selectedIndices.Count == 1)
        {
            DrawSingleBlockInspector(selectedIndices.First());
        }
        else if (selectedIndices.Count > 1)
        {
            DrawMultiBlockInspector();
        }

        // --- LEVEL PROPERTIES ---
        DrawLevelSettingsInspector();

        // --- LEVEL STATS FOOTER ---
        DrawLevelStatsSummary();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSingleBlockInspector(int index)
    {
        if (currentLevel.blocks == null || index < 0 || index >= currentLevel.blocks.Count) return;
        LevelBlockPlacement p = currentLevel.blocks[index];
        if (p == null) return;

        EditorGUILayout.BeginVertical(cardStyle);
        GUILayout.Label($"Selected Block (#{index + 1})", headerStyle);

        EditorGUI.BeginChangeCheck();
        BlockDefinitionSO currentDef = p.ResolveDefinition(blockLibrary);
        BlockDefinitionSO newDef = (BlockDefinitionSO)EditorGUILayout.ObjectField("Template", currentDef, typeof(BlockDefinitionSO), false);

        Vector2 newPos = EditorGUILayout.Vector2Field("Position", p.position);
        float newRot = EditorGUILayout.FloatField("Rotation (Deg)", p.rotation);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0°", EditorStyles.miniButtonLeft)) newRot = 0f;
        if (GUILayout.Button("-30°", EditorStyles.miniButtonMid)) newRot = -30f;
        if (GUILayout.Button("30°", EditorStyles.miniButtonMid)) newRot = 30f;
        if (GUILayout.Button("45°", EditorStyles.miniButtonMid)) newRot = 45f;
        if (GUILayout.Button("90°", EditorStyles.miniButtonRight)) newRot = 90f;
        EditorGUILayout.EndHorizontal();

        Vector2 newSize = EditorGUILayout.Vector2Field("Size (W x H)", p.size);
        newSize.x = Mathf.Max(0.15f, newSize.x);
        newSize.y = Mathf.Max(0.1f, newSize.y);

        bool isDestructible = newDef == null || newDef.isDestructible;
        int newHpOverride = p.hpOverride;
        bool newOverrideReq = p.overrideRequiredToClear;
        bool newReqToClear = p.requiredToClear;
        bool newAddShelf = p.addSupportShelf;

        if (isDestructible)
        {
            EditorGUILayout.Space(4);
            int effectiveHp = p.GetEffectiveHP(newDef);
            int editedHp = EditorGUILayout.IntSlider("Block HP", effectiveHp, 1, 20);
            if (editedHp != effectiveHp)
            {
                newHpOverride = editedHp;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Quick HP:", GUILayout.Width(65));
            if (GUILayout.Button("1", EditorStyles.miniButtonLeft)) newHpOverride = 1;
            if (GUILayout.Button("2", EditorStyles.miniButtonMid)) newHpOverride = 2;
            if (GUILayout.Button("3", EditorStyles.miniButtonMid)) newHpOverride = 3;
            if (GUILayout.Button("5", EditorStyles.miniButtonMid)) newHpOverride = 5;
            if (GUILayout.Button("Def", EditorStyles.miniButtonRight)) newHpOverride = 0;
            EditorGUILayout.EndHorizontal();

            newReqToClear = EditorGUILayout.Toggle("Required To Win", p.IsRequiredToClear(newDef));
            if (newReqToClear != (newDef == null || newDef.requiredToClear))
            {
                newOverrideReq = true;
            }

            newAddShelf = EditorGUILayout.Toggle("Attach Shelf Below", p.addSupportShelf);
        }

        bool newUseTint = EditorGUILayout.Toggle("Custom Tint", p.useCustomColor);
        Color newTint = p.customColor;
        if (newUseTint)
        {
            newTint = EditorGUILayout.ColorField("Tint Color", p.customColor);
        }

        // Special Behavior Overrides (Moving Path, Key/Lock Channel, Magnet)
        BlockBehaviorType bType = newDef != null ? newDef.behaviorType : BlockBehaviorType.Standard;
        bool newOverrideMove = p.overrideMovement;
        Vector2 newMoveOffset = p.GetEffectiveMoveOffset(newDef);
        float newMoveSpeed = p.GetEffectiveMoveSpeed(newDef);
        int newLockChannel = p.lockChannelOverride;
        float newMagRadius = p.magnetRadiusOverride;
        float newMagForce = p.magnetForceOverride;

        EditorGUILayout.Space(4);
        bool showMovingUI = bType == BlockBehaviorType.MovingPlatform || p.overrideMovement;
        newOverrideMove = EditorGUILayout.Toggle("Moving Platform Path", showMovingUI);
        if (newOverrideMove)
        {
            newMoveOffset = EditorGUILayout.Vector2Field("Travel Offset", newMoveOffset);
            newMoveSpeed = EditorGUILayout.Slider("Move Speed", newMoveSpeed, 0.2f, 5f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Horiz ±1.8", EditorStyles.miniButtonLeft)) newMoveOffset = new Vector2(1.8f, 0f);
            if (GUILayout.Button("Vert ±1.5", EditorStyles.miniButtonMid)) newMoveOffset = new Vector2(0f, 1.5f);
            if (GUILayout.Button("Flip", EditorStyles.miniButtonRight)) newMoveOffset = -newMoveOffset;
            EditorGUILayout.EndHorizontal();
        }

        if (bType == BlockBehaviorType.KeyTrigger || bType == BlockBehaviorType.LockedGate)
        {
            int curChan = p.GetEffectiveLockChannel(newDef);
            int editedChan = EditorGUILayout.IntSlider("Key/Lock Channel", curChan, 1, 5);
            if (editedChan != curChan) newLockChannel = editedChan;
        }

        if (bType == BlockBehaviorType.MagnetField)
        {
            float effRad = newMagRadius > 0.1f ? newMagRadius : (newDef != null ? newDef.magnetRadius : 2.4f);
            float effForce = Mathf.Abs(newMagForce) > 0.01f ? newMagForce : (newDef != null ? newDef.magnetForce : 8.5f);
            float rVal = EditorGUILayout.Slider("Magnet Radius", effRad, 0.8f, 4.5f);
            float fVal = EditorGUILayout.Slider("Force (+Pull/-Push)", effForce, -15f, 15f);
            if (!Mathf.Approximately(rVal, effRad)) newMagRadius = rVal;
            if (!Mathf.Approximately(fVal, effForce)) newMagForce = fVal;
        }

        if (EditorGUI.EndChangeCheck())
        {
            RecordLevelUndo("Edit Block");
            if (newDef != currentDef && newDef != null)
            {
                p.blockDefinition = newDef;
                p.blockId = newDef.blockId;
                p.size = newDef.defaultSize;
            }
            p.position = newPos;
            p.rotation = Mathf.Repeat(newRot + 180f, 360f) - 180f;
            p.size = newSize;
            p.hpOverride = newHpOverride;
            p.overrideRequiredToClear = newOverrideReq;
            p.requiredToClear = newReqToClear;
            p.addSupportShelf = newAddShelf;
            p.useCustomColor = newUseTint;
            p.customColor = newTint;
            p.overrideMovement = newOverrideMove;
            p.moveOffset = newMoveOffset;
            p.moveSpeed = newMoveSpeed;
            p.lockChannelOverride = newLockChannel;
            p.magnetRadiusOverride = newMagRadius;
            p.magnetForceOverride = newMagForce;
            MarkCurrentLevelDirty();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Center X", EditorStyles.miniButtonLeft))
        {
            RecordLevelUndo("Center Block X");
            p.position = new Vector2(0f, p.position.y);
            MarkCurrentLevelDirty();
        }
        if (GUILayout.Button("Reset Size", EditorStyles.miniButtonMid) && currentDef != null)
        {
            RecordLevelUndo("Reset Block Size");
            p.size = currentDef.defaultSize;
            MarkCurrentLevelDirty();
        }
        if (GUILayout.Button("+ Shelf Below", EditorStyles.miniButtonRight))
        {
            SpawnPlatformBelowSelected();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Duplicate (Ctrl+D)")) DuplicateSelected();
        if (GUILayout.Button("Delete (Del)")) DeleteSelected();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawMultiBlockInspector()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        GUILayout.Label($"Multi-Selection ({selectedIndices.Count} Blocks)", headerStyle);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Set All HP:", GUILayout.Width(68));
        if (GUILayout.Button("1", EditorStyles.miniButtonLeft)) SetSelectedHP(1);
        if (GUILayout.Button("2", EditorStyles.miniButtonMid)) SetSelectedHP(2);
        if (GUILayout.Button("3", EditorStyles.miniButtonMid)) SetSelectedHP(3);
        if (GUILayout.Button("5", EditorStyles.miniButtonRight)) SetSelectedHP(5);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Shelf Below All")) SpawnPlatformBelowSelected();
        if (GUILayout.Button("Duplicate")) DuplicateSelected();
        if (GUILayout.Button("Delete")) DeleteSelected();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawLevelSettingsInspector()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        GUILayout.Label("Level Configuration", headerStyle);

        EditorGUI.BeginChangeCheck();
        int lvlNum = EditorGUILayout.IntField("Level Number", currentLevel.levelNumber);
        string lvlName = EditorGUILayout.TextField("Level Name", currentLevel.levelName);
        GameMode mode = (GameMode)EditorGUILayout.EnumPopup("Game Mode", currentLevel.gameMode);
        LevelWinCondition winCond = (LevelWinCondition)EditorGUILayout.EnumPopup("Win Condition", currentLevel.winCondition);
        int maxDrops = EditorGUILayout.IntField("Max Drops (0=Unlim)", currentLevel.maxDrops);
        int targetScore = EditorGUILayout.IntField("Target Score", currentLevel.targetScore);

        EditorGUILayout.Space(4);
        GUILayout.Label("Merge & Blast Rules", EditorStyles.boldLabel);
        float blastRad = EditorGUILayout.Slider("Merge Blast Radius", currentLevel.mergeBlastRadius, 0.4f, 3.0f);
        int baseDmg = EditorGUILayout.IntSlider("Merge Base Damage", currentLevel.mergeBaseDamage, 1, 5);
        bool scaleDmg = EditorGUILayout.Toggle("Scale Dmg With Tier", currentLevel.scaleDamageWithTier);

        EditorGUILayout.Space(4);
        GUILayout.Label("Container & Boundaries", EditorStyles.boldLabel);
        float width = EditorGUILayout.Slider("Container Width", currentLevel.containerWidth, 3f, 10f);
        float height = EditorGUILayout.Slider("Container Height", currentLevel.containerHeight, 4f, 14f);
        float dangerY = EditorGUILayout.FloatField("Danger Line Y", currentLevel.dangerLineY);
        float dropY = EditorGUILayout.FloatField("Dropper Height Y", currentLevel.dropHeight);
        BallKitSO kit = (BallKitSO)EditorGUILayout.ObjectField("Ball Kit", currentLevel.defaultKit, typeof(BallKitSO), false);

        if (EditorGUI.EndChangeCheck())
        {
            RecordLevelUndo("Edit Level Settings");
            currentLevel.levelNumber = Mathf.Max(0, lvlNum);
            currentLevel.levelName = lvlName;
            currentLevel.gameMode = mode;
            currentLevel.winCondition = winCond;
            currentLevel.maxDrops = Mathf.Max(0, maxDrops);
            currentLevel.targetScore = Mathf.Max(0, targetScore);
            currentLevel.mergeBlastRadius = blastRad;
            currentLevel.mergeBaseDamage = baseDmg;
            currentLevel.scaleDamageWithTier = scaleDmg;
            currentLevel.containerWidth = width;
            currentLevel.containerHeight = height;
            currentLevel.dangerLineY = dangerY;
            currentLevel.dropHeight = dropY;
            if (kit != currentLevel.defaultKit && kit != null)
            {
                currentLevel.defaultKit = kit;
                currentLevel.allTiers = new List<BallDataSO>(kit.ballTiers);
                currentLevel.droppableTiers = new List<BallDataSO>(kit.droppableTiers);
            }
            MarkCurrentLevelDirty();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawLevelStatsSummary()
    {
        int totalPlaced = currentLevel.blocks != null ? currentLevel.blocks.Count : 0;
        int targets = 0;
        int totalHP = 0;
        int platforms = 0;

        if (currentLevel.blocks != null)
        {
            for (int i = 0; i < currentLevel.blocks.Count; i++)
            {
                var b = currentLevel.blocks[i];
                if (b == null) continue;
                var def = b.ResolveDefinition(blockLibrary);
                if (b.IsRequiredToClear(def))
                {
                    targets++;
                    totalHP += b.GetEffectiveHP(def);
                }
                else if (def != null && !def.isDestructible)
                {
                    platforms++;
                }
            }
        }

        EditorGUILayout.BeginVertical(cardStyle);
        GUILayout.Label("Level Summary", headerStyle);
        GUILayout.Label($"Placed Objects: {totalPlaced}  (Targets: {targets}, Ledges: {platforms})", EditorStyles.miniLabel);
        GUILayout.Label($"Total Target HP: {totalHP}  |  Suggested Min Drops: {Mathf.Max(5, totalHP * 3)}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Level Management Actions (Create / Dup / Rename / Delete / JSON / Preview / Play)

    private void CreateNewLevel()
    {
        BlockLevelSetupUtility.EnsureFolderExists(BlockLevelSetupUtility.LEVELS_FOLDER);

        int nextNumber = 1;
        for (int i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i] != null && allLevels[i].gameMode == GameMode.Level)
            {
                nextNumber = Mathf.Max(nextNumber, allLevels[i].levelNumber + 1);
            }
        }

        string path = AssetDatabase.GenerateUniqueAssetPath($"{BlockLevelSetupUtility.LEVELS_FOLDER}/Level_{nextNumber:00}.asset");
        LevelDataSO newLvl = ScriptableObject.CreateInstance<LevelDataSO>();
        newLvl.gameMode = GameMode.Level;
        newLvl.levelNumber = nextNumber;
        newLvl.levelName = $"Level {nextNumber}";
        newLvl.winCondition = LevelWinCondition.DestroyAllBlocks;
        newLvl.maxDrops = 25;
        newLvl.targetScore = 500;
        newLvl.containerWidth = 5.5f;
        newLvl.containerHeight = 8.5f;
        newLvl.wallThickness = 0.5f;
        newLvl.dropHeight = 3.8f;
        newLvl.dangerLineY = 2.5f;
        newLvl.mergeBlastRadius = 1.15f;
        newLvl.mergeBaseDamage = 1;

        BallKitSO kit = AssetDatabase.LoadAssetAtPath<BallKitSO>("Assets/Game/Data/Kits/BallKit_classic_kit.asset");
        if (kit != null)
        {
            newLvl.defaultKit = kit;
            newLvl.allTiers = new List<BallDataSO>(kit.ballTiers);
            newLvl.droppableTiers = new List<BallDataSO>(kit.droppableTiers);
        }

        AssetDatabase.CreateAsset(newLvl, path);
        BlockLevelSetupUtility.SyncAllLevelsToDatabase();
        AssetDatabase.SaveAssets();
        RefreshLibrariesAndLevels();
        SelectLevel(newLvl);
    }

    private void DuplicateCurrentLevel()
    {
        if (currentLevel == null) return;
        string srcPath = AssetDatabase.GetAssetPath(currentLevel);
        string dir = Path.GetDirectoryName(srcPath).Replace('\\', '/');

        int nextNumber = currentLevel.levelNumber + 1;
        for (int i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i] != null && allLevels[i].gameMode == GameMode.Level)
            {
                nextNumber = Mathf.Max(nextNumber, allLevels[i].levelNumber + 1);
            }
        }

        string destPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/Level_{nextNumber:00}.asset");
        AssetDatabase.CopyAsset(srcPath, destPath);
        LevelDataSO copy = AssetDatabase.LoadAssetAtPath<LevelDataSO>(destPath);
        if (copy != null)
        {
            copy.levelNumber = nextNumber;
            copy.levelName = $"{currentLevel.levelName} Copy";
            if (copy.blocks != null)
            {
                for (int i = 0; i < copy.blocks.Count; i++)
                {
                    if (copy.blocks[i] != null) copy.blocks[i].instanceGuid = Guid.NewGuid().ToString("N");
                }
            }
            EditorUtility.SetDirty(copy);
        }

        BlockLevelSetupUtility.SyncAllLevelsToDatabase();
        AssetDatabase.SaveAssets();
        RefreshLibrariesAndLevels();
        SelectLevel(copy);
    }

    private void RenameCurrentLevel(string newName)
    {
        if (currentLevel == null || string.IsNullOrWhiteSpace(newName)) return;
        RecordLevelUndo("Rename Level");
        currentLevel.levelName = newName.Trim();
        EditorUtility.SetDirty(currentLevel);
        AssetDatabase.SaveAssets();
        RefreshLibrariesAndLevels();
    }

    private void DeleteCurrentLevel()
    {
        if (currentLevel == null) return;
        string path = AssetDatabase.GetAssetPath(currentLevel);
        if (!EditorUtility.DisplayDialog("Delete Level?",
            $"Are you sure you want to permanently delete '{currentLevel.levelName}' ({path})?",
            "Delete", "Cancel"))
        {
            return;
        }

        currentLevel = null;
        selectedIndices.Clear();
        AssetDatabase.DeleteAsset(path);
        BlockLevelSetupUtility.SyncAllLevelsToDatabase();
        AssetDatabase.SaveAssets();
        RefreshLibrariesAndLevels();
    }

    private void ExportCurrentLevelJson()
    {
        if (currentLevel == null) return;
        string defaultName = $"Level_{currentLevel.levelNumber:00}.json";
        string filePath = EditorUtility.SaveFilePanel("Export Level JSON", Application.dataPath, defaultName, "json");
        if (string.IsNullOrEmpty(filePath)) return;

        string json = LevelJsonData.ToJson(currentLevel, prettyPrint: true);
        File.WriteAllText(filePath, json);
        ShowNotification(new GUIContent($"Exported {Path.GetFileName(filePath)}"));
    }

    private void ImportJsonIntoLevel()
    {
        string filePath = EditorUtility.OpenFilePanel("Import Level JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        int choice = EditorUtility.DisplayDialogComplex("Import Level JSON",
            "Create a brand new Level asset from this JSON, or overwrite the currently selected level?",
            "Create New Level", "Cancel", "Overwrite Current");

        if (choice == 1) return; // Cancel

        if (choice == 0 || currentLevel == null)
        {
            CreateNewLevel();
        }

        if (currentLevel != null)
        {
            RecordLevelUndo("Import Level JSON");
            if (LevelJsonData.ApplyJsonToLevel(json, currentLevel, blockLibrary))
            {
                MarkCurrentLevelDirty();
                SaveCurrentLevel(true);
                ShowNotification(new GUIContent("JSON imported successfully!"));
            }
            else
            {
                EditorUtility.DisplayDialog("Import Failed", "Could not parse the selected JSON file.", "OK");
            }
        }
    }

    private void PreviewLevelInActiveScene()
    {
        if (currentLevel == null) return;
        ContainerBoundary cb = FindAnyObjectByType<ContainerBoundary>();
        if (cb != null) cb.Setup(currentLevel);

        DangerLine dl = FindAnyObjectByType<DangerLine>();
        if (dl != null) dl.Setup(currentLevel);

        LevelBlockManager lbm = FindAnyObjectByType<LevelBlockManager>();
        if (lbm == null)
        {
            GameObject go = new GameObject("LevelBlockManager");
            lbm = go.AddComponent<LevelBlockManager>();
        }
        lbm.LoadLevelBlocks(currentLevel, cb);
        SceneView.RepaintAll();
    }

    private void ClearScenePreview()
    {
        LevelBlockManager lbm = FindAnyObjectByType<LevelBlockManager>();
        if (lbm != null)
        {
            lbm.ClearLevelBlocks();
            SceneView.RepaintAll();
        }
    }

    private void PlayTestCurrentLevel()
    {
        if (currentLevel == null) return;
        SaveCurrentLevel(forceSaveAssetDatabase: true);

        string levelAssetPath = AssetDatabase.GetAssetPath(currentLevel);
        EditorPrefs.SetString(BlockTestingController.PREF_PLAYTEST_LEVEL_GUID, levelAssetPath);

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (!File.Exists(BlockLevelSetupUtility.BLOCK_TESTING_SCENE_PATH))
        {
            BlockLevelSetupUtility.SetupBlockTestingScene();
        }
        else if (SceneManager.GetActiveScene().path != BlockLevelSetupUtility.BLOCK_TESTING_SCENE_PATH)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(BlockLevelSetupUtility.BLOCK_TESTING_SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                return;
            }
        }

        if (FindAnyObjectByType<BlockTestingController>() == null)
        {
            BlockLevelSetupUtility.SetupBlockTestingScene();
        }

        EditorApplication.isPlaying = true;
    }

    private void CreateCustomBlockType()
    {
        BlockLevelSetupUtility.EnsureFolderExists(BlockLevelSetupUtility.BLOCKS_FOLDER);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{BlockLevelSetupUtility.BLOCKS_FOLDER}/BlockDef_Custom.asset");
        BlockDefinitionSO def = ScriptableObject.CreateInstance<BlockDefinitionSO>();
        def.blockId = "custom_" + Guid.NewGuid().ToString("N").Substring(0, 6);
        def.displayName = "Custom Block";
        def.defaultHP = 2;
        def.baseSprite = BlockSpriteGenerator.LoadSprite("Block_Crate_Normal.png");

        AssetDatabase.CreateAsset(def, path);
        if (blockLibrary != null)
        {
            Undo.RecordObject(blockLibrary, "Add Custom Block Type");
            blockLibrary.blocks.Add(def);
            EditorUtility.SetDirty(blockLibrary);
        }
        AssetDatabase.SaveAssets();
        activePaletteBlock = def;
        Selection.activeObject = def;
        Repaint();
    }

    #endregion

    #region Undo, Selection & Editing Helpers

    private void RecordLevelUndo(string actionName)
    {
        if (currentLevel != null)
        {
            Undo.RecordObject(currentLevel, actionName);
        }
    }

    private void MarkCurrentLevelDirty()
    {
        if (currentLevel == null) return;
        EditorUtility.SetDirty(currentLevel);
        if (autoSave)
        {
            SaveCurrentLevel(forceSaveAssetDatabase: false);
        }
    }

    private void SaveCurrentLevel(bool forceSaveAssetDatabase)
    {
        if (currentLevel == null) return;
        EditorUtility.SetDirty(currentLevel);
        if (forceSaveAssetDatabase)
        {
            BlockLevelSetupUtility.SyncAllLevelsToDatabase();
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent($"Saved {currentLevel.levelName}"));
        }
    }

    private void PruneInvalidSelection()
    {
        if (currentLevel == null || currentLevel.blocks == null)
        {
            selectedIndices.Clear();
            return;
        }
        selectedIndices.RemoveWhere(idx => idx < 0 || idx >= currentLevel.blocks.Count);
    }

    private void CopySelected()
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count == 0) return;
        clipboard.Clear();
        foreach (int idx in selectedIndices.OrderBy(i => i))
        {
            if (idx >= 0 && idx < currentLevel.blocks.Count && currentLevel.blocks[idx] != null)
            {
                clipboard.Add(currentLevel.blocks[idx].Clone(newGuid: true));
            }
        }
    }

    private void PasteClipboard()
    {
        if (currentLevel == null || clipboard.Count == 0) return;
        RecordLevelUndo("Paste Blocks");
        if (currentLevel.blocks == null) currentLevel.blocks = new List<LevelBlockPlacement>();

        selectedIndices.Clear();
        for (int i = 0; i < clipboard.Count; i++)
        {
            LevelBlockPlacement copy = clipboard[i].Clone(newGuid: true);
            copy.position += new Vector2(0.25f, -0.25f);
            currentLevel.blocks.Add(copy);
            selectedIndices.Add(currentLevel.blocks.Count - 1);
            clipboard[i].position = copy.position; // Progressive offset on repeated paste
        }
        MarkCurrentLevelDirty();
        Repaint();
    }

    private void DuplicateSelected()
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count == 0) return;
        RecordLevelUndo("Duplicate Blocks");

        var toAdd = new List<LevelBlockPlacement>();
        foreach (int idx in selectedIndices.OrderBy(i => i))
        {
            if (idx >= 0 && idx < currentLevel.blocks.Count && currentLevel.blocks[idx] != null)
            {
                LevelBlockPlacement dup = currentLevel.blocks[idx].Clone(newGuid: true);
                dup.position += new Vector2(0.35f, -0.35f);
                toAdd.Add(dup);
            }
        }

        selectedIndices.Clear();
        for (int i = 0; i < toAdd.Count; i++)
        {
            currentLevel.blocks.Add(toAdd[i]);
            selectedIndices.Add(currentLevel.blocks.Count - 1);
        }
        MarkCurrentLevelDirty();
        Repaint();
    }

    private void DeleteSelected()
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count == 0) return;
        RecordLevelUndo("Delete Blocks");

        foreach (int idx in selectedIndices.OrderByDescending(i => i))
        {
            if (idx >= 0 && idx < currentLevel.blocks.Count)
            {
                currentLevel.blocks.RemoveAt(idx);
            }
        }

        selectedIndices.Clear();
        MarkCurrentLevelDirty();
        Repaint();
    }

    private void SetSelectedHP(int hp)
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count == 0) return;
        RecordLevelUndo("Set Selected HP");
        foreach (int idx in selectedIndices)
        {
            if (idx >= 0 && idx < currentLevel.blocks.Count && currentLevel.blocks[idx] != null)
            {
                currentLevel.blocks[idx].hpOverride = hp;
            }
        }
        MarkCurrentLevelDirty();
    }

    private void SpawnPlatformBelowSelected()
    {
        if (currentLevel == null || currentLevel.blocks == null || selectedIndices.Count == 0) return;
        BlockDefinitionSO shelfDef = blockLibrary != null ? blockLibrary.GetById("wood_shelf") : null;
        if (shelfDef == null) return;

        RecordLevelUndo("Add Shelf Below");
        var indicesSnapshot = selectedIndices.ToList();
        for (int i = 0; i < indicesSnapshot.Count; i++)
        {
            int idx = indicesSnapshot[i];
            if (idx < 0 || idx >= currentLevel.blocks.Count) continue;
            var b = currentLevel.blocks[idx];
            if (b == null) continue;

            LevelBlockPlacement shelf = new LevelBlockPlacement
            {
                instanceGuid = Guid.NewGuid().ToString("N"),
                blockId = shelfDef.blockId,
                blockDefinition = shelfDef,
                position = new Vector2(b.position.x, b.position.y - b.size.y * 0.5f - shelfDef.defaultSize.y * 0.5f - 0.01f),
                size = new Vector2(b.size.x + 0.35f, shelfDef.defaultSize.y),
                rotation = 0f,
                requiredToClear = false
            };
            currentLevel.blocks.Add(shelf);
        }
        MarkCurrentLevelDirty();
        Repaint();
    }

    #endregion
}
#endif
