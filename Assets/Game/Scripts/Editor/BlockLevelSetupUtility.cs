#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility that generates block sprites, BlockDefinitionSO templates, BlockLibrarySO,
/// 5 sample levels (with Level 1 matching the sketch layout), LevelDatabaseSO, and configures
/// Assets/Scenes/Block testing.unity for immediate Play Mode testing.
/// </summary>
[InitializeOnLoad]
public static class BlockLevelSetupUtility
{
    public const string RESOURCES_ROOT = "Assets/Resources/BallMerge";
    public const string BLOCKS_FOLDER = "Assets/Resources/BallMerge/Blocks";
    public const string LEVELS_FOLDER = "Assets/Resources/BallMerge/Levels";
    public const string BLOCK_LIBRARY_PATH = "Assets/Resources/BallMerge/BlockLibrary.asset";
    public const string LEVEL_DATABASE_PATH = "Assets/Resources/BallMerge/LevelDatabase.asset";
    public const string BLOCK_TESTING_SCENE_PATH = "Assets/Scenes/Block testing.unity";
    public const string GAME_TEST_SCENE_PATH = "Assets/Game/Scene/GameTest.unity";
    public const string LEVEL_SCROLL_PREFAB_PATH = "Assets/New Ideas/Scroll/Prefab/LevelScrollView.prefab";

    static BlockLevelSetupUtility()
    {
        EditorApplication.delayCall += AutoInitializeBlockSystemOnce;
    }

    private static void AutoInitializeBlockSystemOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SessionState.GetBool("BallMerge_BlockInitV5Checked", false)) return;
        SessionState.SetBool("BallMerge_BlockInitV5Checked", true);

        BlockLibrarySO existingLib = AssetDatabase.LoadAssetAtPath<BlockLibrarySO>(BLOCK_LIBRARY_PATH);
        bool needsUpgrade = existingLib == null
                            || !File.Exists(LEVEL_DATABASE_PATH)
                            || existingLib.GetById("ice_frozen") == null
                            || existingLib.GetById("magnet_block") == null
                            || existingLib.GetById("portal_entry") == null
                            || !File.Exists($"{LEVELS_FOLDER}/Level_09.asset")
                            || !File.Exists($"{BlockSpriteGenerator.UI_RESOURCES_FOLDER}/Chest_Closed.png");

        if (needsUpgrade)
        {
            BallMergeAssetCreator.GenerateDefaultAssets();
            GenerateAllBlockAssetsAndSampleLevels();
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == BLOCK_TESTING_SCENE_PATH && !activeScene.isDirty &&
            (UnityEngine.Object.FindAnyObjectByType<BlockTestingController>() == null
             || UnityEngine.Object.FindAnyObjectByType<ThemeAnimatedBackground>() == null
             || GameObject.Find("ThemeCycleButton") == null))
        {
            SetupBlockTestingScene();
        }
        else if (activeScene.path == GAME_TEST_SCENE_PATH && !activeScene.isDirty && NeedsGameTestUpgrade())
        {
            UpgradeGameTestSceneInternal(saveScene: true);
        }
    }

    [MenuItem("Tools/Ball Merge/4. Generate Block Assets & Sample Levels")]
    public static void GenerateAllBlockAssetsAndSampleLevels()
    {
        BlockLibrarySO library = EnsureBlockLibraryAndDefinitions();
        EnsureSampleLevelsAndDatabase(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BlockLevelSetupUtility] Generated block sprites, BlockLibrary, 5 sample levels, and LevelDatabase!");
    }

    [MenuItem("Tools/Ball Merge/5. Setup Block Testing Scene")]
    public static void SetupBlockTestingScene()
    {
        // Ensure kits/themes & block assets exist first
        BallMergeAssetCreator.GenerateDefaultAssets();
        BlockLibrarySO library = EnsureBlockLibraryAndDefinitions();
        LevelDatabaseSO database = EnsureSampleLevelsAndDatabase(library);

        if (!File.Exists(BLOCK_TESTING_SCENE_PATH))
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, BLOCK_TESTING_SCENE_PATH);
        }
        else
        {
            EditorSceneManager.OpenScene(BLOCK_TESTING_SCENE_PATH, OpenSceneMode.Single);
        }

        BuildBlockTestingSceneHierarchy(database);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), BLOCK_TESTING_SCENE_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log("[BlockLevelSetupUtility] Block testing.unity setup complete! Press Play to test immediately.");
    }

    public static BlockLibrarySO EnsureBlockLibraryAndDefinitions()
    {
        EnsureFolderExists(RESOURCES_ROOT);
        EnsureFolderExists(BLOCKS_FOLDER);
        EnsureFolderExists(LEVELS_FOLDER);

        BlockSpriteGenerator.EnsureAllBlockSpritesExist();

        Sprite crack1 = BlockSpriteGenerator.LoadSprite("Crack_Stage1.png");
        Sprite crack2 = BlockSpriteGenerator.LoadSprite("Crack_Stage2.png");
        Sprite crack3 = BlockSpriteGenerator.LoadSprite("Crack_Stage3.png");
        var defaultCracks = new List<Sprite>();
        if (crack1 != null) defaultCracks.Add(crack1);
        if (crack2 != null) defaultCracks.Add(crack2);
        if (crack3 != null) defaultCracks.Add(crack3);

        var definitions = new List<BlockDefinitionSO>
        {
            CreateOrUpdateBlockDef("crate_weak", "Weak Crate", BlockCategory.Weak,
                "Light wooden crate. Breaks in 1 nearby merge.",
                isDestructible: true, required: true, hp: 1, score: 30, bonusDrops: 0, explode: false,
                size: new Vector2(0.82f, 0.82f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Crate_Weak.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.98f, 0.72f, 0.32f),
                particleCol: new Color(0.95f, 0.65f, 0.25f)),

            CreateOrUpdateBlockDef("crate_normal", "Normal Crate", BlockCategory.Normal,
                "Standard reinforced crate from the level sketch. Takes 2 nearby merges.",
                isDestructible: true, required: true, hp: 2, score: 60, bonusDrops: 0, explode: false,
                size: new Vector2(0.9f, 0.9f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Crate_Normal.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.95f, 0.56f, 0.16f),
                particleCol: new Color(0.95f, 0.5f, 0.15f)),

            CreateOrUpdateBlockDef("stone_strong", "Strong Stone", BlockCategory.Strong,
                "Heavy stone block requiring 3 nearby merges to shatter.",
                isDestructible: true, required: true, hp: 3, score: 100, bonusDrops: 0, explode: false,
                size: new Vector2(0.95f, 0.95f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Stone_Strong.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.55f, 0.65f, 0.78f),
                particleCol: new Color(0.6f, 0.68f, 0.78f)),

            CreateOrUpdateBlockDef("iron_heavy", "Armored Iron", BlockCategory.Strong,
                "Armored steel block with 5 HP. Ideal for boss levels or near bomb crates.",
                isDestructible: true, required: true, hp: 5, score: 180, bonusDrops: 0, explode: false,
                size: new Vector2(1.0f, 1.0f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Iron_Heavy.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.45f, 0.52f, 0.62f),
                particleCol: new Color(0.75f, 0.82f, 0.9f)),

            CreateOrUpdateBlockDef("bomb_special", "Bomb Crate", BlockCategory.Special,
                "Explosive TNT crate! Deals 2 area damage to nearby blocks when destroyed.",
                isDestructible: true, required: true, hp: 1, score: 90, bonusDrops: 0, explode: true,
                size: new Vector2(0.88f, 0.88f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Bomb_Special.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.95f, 0.25f, 0.15f),
                particleCol: new Color(1f, 0.4f, 0.15f)),

            CreateOrUpdateBlockDef("bonus_drop", "Bonus Drop Crate", BlockCategory.Special,
                "Golden reward crate! Grants +2 extra ball drops when destroyed.",
                isDestructible: true, required: true, hp: 2, score: 120, bonusDrops: 2, explode: false,
                size: new Vector2(0.85f, 0.85f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Bonus_Special.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.25f, 0.88f, 0.55f),
                particleCol: new Color(1f, 0.88f, 0.3f)),

            CreateOrUpdateBlockDef("wood_shelf", "Wood Shelf / Ledge", BlockCategory.Platform,
                "Indestructible static shelf ledge that supports crates and deflects falling balls.",
                isDestructible: false, required: false, hp: 1, score: 0, bonusDrops: 0, explode: false,
                size: new Vector2(1.35f, 0.16f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Shelf_Platform.png"),
                cracks: null,
                baseCol: Color.white,
                accentCol: new Color(0.62f, 0.38f, 0.20f),
                particleCol: new Color(0.58f, 0.35f, 0.18f)),

            CreateOrUpdateBlockDef("ice_frozen", "Ice Block", BlockCategory.Special,
                "Slippery frozen crystal block! Low friction lets balls slide smoothly across its surface.",
                isDestructible: true, required: true, hp: 2, score: 85, bonusDrops: 0, explode: false,
                size: new Vector2(0.88f, 0.88f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Ice_Frozen.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.55f, 0.88f, 0.98f),
                particleCol: new Color(0.60f, 0.92f, 1.0f),
                behavior: BlockBehaviorType.IceFrozen, friction: 0.02f, bounciness: 0.35f,
                hitSfx: "IceCrack", destroySfx: "IceCrack"),

            CreateOrUpdateBlockDef("moving_platform", "Moving Platform", BlockCategory.Platform,
                "Gliding platform that smoothly carries and deflects balls along a customizable path.",
                isDestructible: false, required: false, hp: 1, score: 0, bonusDrops: 0, explode: false,
                size: new Vector2(1.40f, 0.20f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Moving_Platform.png"),
                cracks: null,
                baseCol: Color.white,
                accentCol: new Color(0.25f, 0.72f, 0.95f),
                particleCol: new Color(0.35f, 0.90f, 1.0f),
                behavior: BlockBehaviorType.MovingPlatform),

            CreateOrUpdateBlockDef("key_block", "Key Crate", BlockCategory.Special,
                "Golden Key Crate! Breaking it launches a glowing key orb that unlocks matching Locked Vaults.",
                isDestructible: true, required: true, hp: 1, score: 110, bonusDrops: 0, explode: false,
                size: new Vector2(0.84f, 0.84f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Key_Special.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.96f, 0.78f, 0.22f),
                particleCol: new Color(1.0f, 0.88f, 0.28f),
                behavior: BlockBehaviorType.KeyTrigger),

            CreateOrUpdateBlockDef("locked_block", "Locked Vault", BlockCategory.Strong,
                "Padlocked block immune to merges until a matching Key Crate is broken!",
                isDestructible: true, required: true, hp: 2, score: 150, bonusDrops: 0, explode: false,
                size: new Vector2(0.92f, 0.92f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Locked_Gate.png"),
                cracks: defaultCracks,
                baseCol: Color.white,
                accentCol: new Color(0.55f, 0.45f, 0.22f),
                particleCol: new Color(0.85f, 0.72f, 0.28f),
                behavior: BlockBehaviorType.LockedGate,
                lockOverlay: BlockSpriteGenerator.LoadSprite("Block_Padlock_Overlay.png")),

            CreateOrUpdateBlockDef("magnet_block", "Magnet Core", BlockCategory.Special,
                "Magnetic field core that gently pulls (or repels) nearby balls to set up trick merges!",
                isDestructible: false, required: false, hp: 1, score: 0, bonusDrops: 0, explode: false,
                size: new Vector2(0.76f, 0.76f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Magnet_Special.png"),
                cracks: null,
                baseCol: Color.white,
                accentCol: new Color(0.35f, 0.72f, 1.0f),
                particleCol: new Color(0.45f, 0.88f, 1.0f),
                behavior: BlockBehaviorType.MagnetField),

            CreateOrUpdateBlockDef("bumper_pad", "Bounce Bumper", BlockCategory.Special,
                "Pinball-style spring pad that launches hitting balls dynamically across the container!",
                isDestructible: false, required: false, hp: 1, score: 0, bonusDrops: 0, explode: false,
                size: new Vector2(0.78f, 0.78f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Bumper_Pad.png"),
                cracks: null,
                baseCol: Color.white,
                accentCol: new Color(0.95f, 0.28f, 0.82f),
                particleCol: new Color(1.0f, 0.45f, 0.95f),
                behavior: BlockBehaviorType.BumperPad, friction: 0.05f, bounciness: 0.95f),

            CreateOrUpdateBlockDef("portal_entry", "Portal Entry", BlockCategory.Special,
                "Swirling teleport vortex! Dropped balls entering it warp directly to matching Portal Exit.",
                isDestructible: false, required: false, hp: 1, score: 0, bonusDrops: 0, explode: false,
                size: new Vector2(0.86f, 0.86f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Portal_Entry.png"),
                cracks: null,
                baseCol: Color.white,
                accentCol: new Color(0.15f, 0.82f, 1.0f),
                particleCol: new Color(0.2f, 0.85f, 1.0f),
                behavior: BlockBehaviorType.PortalEntry),

            CreateOrUpdateBlockDef("portal_exit", "Portal Exit", BlockCategory.Special,
                "Exit vortex where teleported balls emerge (ideal behind shelves or in hidden pockets!).",
                isDestructible: false, required: false, hp: 1, score: 0, bonusDrops: 0, explode: false,
                size: new Vector2(0.86f, 0.86f),
                sprite: BlockSpriteGenerator.LoadSprite("Block_Portal_Exit.png"),
                cracks: null,
                baseCol: Color.white,
                accentCol: new Color(0.96f, 0.42f, 0.92f),
                particleCol: new Color(1.0f, 0.45f, 0.92f),
                behavior: BlockBehaviorType.PortalExit)
        };

        BlockLibrarySO library = AssetDatabase.LoadAssetAtPath<BlockLibrarySO>(BLOCK_LIBRARY_PATH);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<BlockLibrarySO>();
            AssetDatabase.CreateAsset(library, BLOCK_LIBRARY_PATH);
        }

        library.blocks = definitions;
        EditorUtility.SetDirty(library);
        BlockLibrarySO.ClearCache();
        return library;
    }

    private static BlockDefinitionSO CreateOrUpdateBlockDef(
        string id,
        string displayName,
        BlockCategory category,
        string description,
        bool isDestructible,
        bool required,
        int hp,
        int score,
        int bonusDrops,
        bool explode,
        Vector2 size,
        Sprite sprite,
        List<Sprite> cracks,
        Color baseCol,
        Color accentCol,
        Color particleCol,
        BlockBehaviorType behavior = BlockBehaviorType.Standard,
        float friction = 0.38f,
        float bounciness = -1f,
        string hitSfx = "BlockHit",
        string destroySfx = "BlockBreak",
        Sprite lockOverlay = null)
    {
        string path = $"{BLOCKS_FOLDER}/BlockDef_{id}.asset";
        BlockDefinitionSO def = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<BlockDefinitionSO>();
            AssetDatabase.CreateAsset(def, path);
        }

        if (behavior == BlockBehaviorType.Standard)
        {
            if (explode) behavior = BlockBehaviorType.ExplosiveBomb;
            else if (bonusDrops > 0) behavior = BlockBehaviorType.BonusDrops;
        }

        def.blockId = id;
        def.displayName = displayName;
        def.category = category;
        def.description = description;
        def.behaviorType = behavior;
        def.isDestructible = isDestructible;
        def.requiredToClear = required;
        def.defaultHP = hp;
        def.damageOnNearbyMerge = isDestructible;
        def.damageOnBallCollision = false;
        def.scoreReward = score;
        def.bonusDropsOnDestroy = bonusDrops;
        def.explodeOnDestroy = explode;
        def.explosionRadius = 1.9f;
        def.explosionDamage = 2;
        def.defaultMoveOffset = new Vector2(1.8f, 0f);
        def.defaultMoveSpeed = 1.4f;
        def.defaultLockChannel = 1;
        def.lockOverlaySprite = lockOverlay;
        def.magnetRadius = 2.4f;
        def.magnetForce = 8.5f;
        def.bumperImpulse = 7.5f;
        def.defaultSize = size;
        def.bounciness = bounciness >= 0f ? bounciness : (isDestructible ? 0.25f : 0.32f);
        def.friction = friction;
        def.baseSprite = sprite;
        def.crackSprites = cracks != null ? new List<Sprite>(cracks) : new List<Sprite>();
        def.baseColor = baseCol;
        def.accentColor = accentCol;
        def.hitParticleColor = particleCol;
        def.destroyParticleColor = particleCol;
        def.hitSoundKey = hitSfx;
        def.destroySoundKey = destroySfx;
        def.showHPBadge = isDestructible;

        EditorUtility.SetDirty(def);
        return def;
    }

    public static LevelDatabaseSO EnsureSampleLevelsAndDatabase(BlockLibrarySO library)
    {
        EnsureFolderExists(LEVELS_FOLDER);

        BallKitSO classicKit = AssetDatabase.LoadAssetAtPath<BallKitSO>("Assets/Game/Data/Kits/BallKit_classic_kit.asset");

        BlockDefinitionSO weakCrate = library.GetById("crate_weak");
        BlockDefinitionSO normalCrate = library.GetById("crate_normal");
        BlockDefinitionSO stoneBlock = library.GetById("stone_strong");
        BlockDefinitionSO ironBlock = library.GetById("iron_heavy");
        BlockDefinitionSO bombCrate = library.GetById("bomb_special");
        BlockDefinitionSO bonusCrate = library.GetById("bonus_drop");
        BlockDefinitionSO woodShelf = library.GetById("wood_shelf");
        BlockDefinitionSO iceBlock = library.GetById("ice_frozen");
        BlockDefinitionSO movingPlatform = library.GetById("moving_platform");
        BlockDefinitionSO keyBlock = library.GetById("key_block");
        BlockDefinitionSO lockedBlock = library.GetById("locked_block");
        BlockDefinitionSO magnetBlock = library.GetById("magnet_block");
        BlockDefinitionSO bumperPad = library.GetById("bumper_pad");
        BlockDefinitionSO portalEntry = library.GetById("portal_entry");
        BlockDefinitionSO portalExit = library.GetById("portal_exit");

        // Level 1: Matches the user's sketch layout (4 crates resting on brown ledges!)
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_01.asset", 1, "Crate Valley", 20, 400, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(weakCrate, new Vector2(-1.35f, -0.45f), new Vector2(0.85f, 1.05f), 0f, hp: 1, shelf: true),
            MakeBlock(weakCrate, new Vector2(1.45f, 0.35f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: true),
            MakeBlock(normalCrate, new Vector2(0.60f, -1.25f), new Vector2(0.88f, 0.82f), 0f, hp: 2, shelf: true),
            MakeBlock(weakCrate, new Vector2(-0.35f, -2.35f), new Vector2(0.88f, 0.72f), 0f, hp: 1, shelf: true)
        });

        // Level 2: Stepped Pyramid with center deflecting shelf
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_02.asset", 2, "Stepped Crates", 24, 600, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(normalCrate, new Vector2(0.0f, 0.45f), new Vector2(0.88f, 0.88f), 0f, hp: 2, shelf: false),
            MakeBlock(woodShelf, new Vector2(0.0f, -0.10f), new Vector2(1.45f, 0.16f), 0f, hp: 0, shelf: false),
            MakeBlock(weakCrate, new Vector2(-1.45f, -1.25f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: true),
            MakeBlock(weakCrate, new Vector2(1.45f, -1.25f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: true),
            MakeBlock(normalCrate, new Vector2(0.0f, -2.55f), new Vector2(0.92f, 0.92f), 0f, hp: 2, shelf: true)
        });

        // Level 3: Blast Chamber (Bomb Crate + Strong Stone Blocks)
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_03.asset", 3, "Blast Chamber", 24, 800, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(stoneBlock, new Vector2(-1.25f, -0.65f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(bombCrate, new Vector2(0.0f, -0.65f), new Vector2(0.86f, 0.86f), 0f, hp: 1, shelf: true),
            MakeBlock(stoneBlock, new Vector2(1.25f, -0.65f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(normalCrate, new Vector2(-0.75f, -2.25f), new Vector2(0.85f, 0.85f), 0f, hp: 2, shelf: true),
            MakeBlock(normalCrate, new Vector2(0.75f, -2.25f), new Vector2(0.85f, 0.85f), 0f, hp: 2, shelf: true)
        });

        // Level 4: Twin Towers + Bonus Drops + Angled Ledges
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_04.asset", 4, "Twin Towers", 22, 950, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(woodShelf, new Vector2(-1.40f, 0.65f), new Vector2(1.45f, 0.16f), -16f, hp: 0, shelf: false),
            MakeBlock(woodShelf, new Vector2(1.40f, 0.65f), new Vector2(1.45f, 0.16f), 16f, hp: 0, shelf: false),
            MakeBlock(stoneBlock, new Vector2(-1.35f, -1.05f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(stoneBlock, new Vector2(1.35f, -1.05f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(bonusCrate, new Vector2(0.0f, -0.35f), new Vector2(0.82f, 0.82f), 0f, hp: 2, shelf: true),
            MakeBlock(normalCrate, new Vector2(-0.85f, -2.65f), new Vector2(0.85f, 0.85f), 0f, hp: 2, shelf: false),
            MakeBlock(normalCrate, new Vector2(0.85f, -2.65f), new Vector2(0.85f, 0.85f), 0f, hp: 2, shelf: false)
        });

        // Level 5: Iron Fortress Boss Level
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_05.asset", 5, "Iron Fortress", 28, 1200, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(ironBlock, new Vector2(0.0f, -0.45f), new Vector2(1.05f, 1.05f), 0f, hp: 5, shelf: true),
            MakeBlock(bombCrate, new Vector2(-1.45f, -0.45f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: true),
            MakeBlock(bombCrate, new Vector2(1.45f, -0.45f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: true),
            MakeBlock(bonusCrate, new Vector2(0.0f, 0.95f), new Vector2(0.80f, 0.80f), 0f, hp: 2, shelf: true),
            MakeBlock(stoneBlock, new Vector2(-1.15f, -2.25f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(stoneBlock, new Vector2(1.15f, -2.25f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true)
        });

        // Level 6: Frost & Glide (Ice Blocks + Moving Platform)
        var lvl6Mover = MakeBlock(movingPlatform, new Vector2(-0.90f, -0.35f), new Vector2(1.40f, 0.20f), 0f, hp: 0, shelf: false);
        lvl6Mover.overrideMovement = true;
        lvl6Mover.moveOffset = new Vector2(1.80f, 0f);
        lvl6Mover.moveSpeed = 1.25f;

        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_06.asset", 6, "Frost & Glide", 26, 950, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(iceBlock, new Vector2(-1.35f, 0.75f), new Vector2(0.88f, 0.88f), -12f, hp: 2, shelf: false),
            MakeBlock(iceBlock, new Vector2(1.35f, 0.75f), new Vector2(0.88f, 0.88f), 12f, hp: 2, shelf: false),
            lvl6Mover,
            MakeBlock(iceBlock, new Vector2(-0.85f, -2.05f), new Vector2(0.88f, 0.88f), 0f, hp: 2, shelf: true),
            MakeBlock(iceBlock, new Vector2(0.85f, -2.05f), new Vector2(0.88f, 0.88f), 0f, hp: 2, shelf: true)
        });

        // Level 7: Key & Vault (Break Key Crate to unlock Vaults!)
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_07.asset", 7, "Key & Vault", 28, 1100, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(keyBlock, new Vector2(0.0f, 0.75f), new Vector2(0.86f, 0.86f), 0f, hp: 1, shelf: true),
            MakeBlock(lockedBlock, new Vector2(-1.35f, -1.15f), new Vector2(0.92f, 0.92f), 0f, hp: 2, shelf: true),
            MakeBlock(lockedBlock, new Vector2(1.35f, -1.15f), new Vector2(0.92f, 0.92f), 0f, hp: 2, shelf: true),
            MakeBlock(weakCrate, new Vector2(0.0f, -2.45f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: true)
        });

        // Level 8: Magnetic Pinball (Magnet Core + Bounce Bumpers + Bomb + Stone)
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_08.asset", 8, "Magnetic Pinball", 30, 1400, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(magnetBlock, new Vector2(0.0f, -0.40f), new Vector2(0.76f, 0.76f), 0f, hp: 0, shelf: false),
            MakeBlock(bumperPad, new Vector2(-1.70f, 0.45f), new Vector2(0.75f, 0.75f), 0f, hp: 0, shelf: false),
            MakeBlock(bumperPad, new Vector2(1.70f, 0.45f), new Vector2(0.75f, 0.75f), 0f, hp: 0, shelf: false),
            MakeBlock(bombCrate, new Vector2(0.0f, 0.85f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: false),
            MakeBlock(stoneBlock, new Vector2(-1.10f, -1.95f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(stoneBlock, new Vector2(1.10f, -1.95f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: true),
            MakeBlock(bonusCrate, new Vector2(0.0f, -2.85f), new Vector2(0.82f, 0.82f), 0f, hp: 2, shelf: false)
        });

        // Level 9: Warp Pocket (Teleport Portals bypass sealed shelf into hidden pocket!)
        CreateSampleLevelIfMissing($"{LEVELS_FOLDER}/Level_09.asset", 9, "Warp Pocket", 28, 1250, classicKit, new List<LevelBlockPlacement>
        {
            MakeBlock(portalEntry, new Vector2(1.45f, 1.35f), new Vector2(0.86f, 0.86f), 0f, hp: 0, shelf: false),
            MakeBlock(normalCrate, new Vector2(-1.25f, 1.15f), new Vector2(0.86f, 0.86f), 0f, hp: 2, shelf: true),
            MakeBlock(woodShelf, new Vector2(-0.45f, -0.15f), new Vector2(3.60f, 0.22f), 0f, hp: 0, shelf: false),
            MakeBlock(portalExit, new Vector2(-0.85f, -1.05f), new Vector2(0.86f, 0.86f), 0f, hp: 0, shelf: false),
            MakeBlock(bombCrate, new Vector2(0.15f, -2.55f), new Vector2(0.85f, 0.85f), 0f, hp: 1, shelf: false),
            MakeBlock(stoneBlock, new Vector2(-1.25f, -2.55f), new Vector2(0.90f, 0.90f), 0f, hp: 3, shelf: false),
            MakeBlock(normalCrate, new Vector2(1.35f, -2.55f), new Vector2(0.86f, 0.86f), 0f, hp: 2, shelf: false)
        });

        return SyncAllLevelsToDatabase();
    }

    private static LevelBlockPlacement MakeBlock(BlockDefinitionSO def, Vector2 pos, Vector2 size, float rot, int hp, bool shelf)
    {
        return new LevelBlockPlacement
        {
            instanceGuid = Guid.NewGuid().ToString("N"),
            blockId = def != null ? def.blockId : "crate_normal",
            blockDefinition = def,
            position = pos,
            size = size,
            rotation = rot,
            hpOverride = hp,
            addSupportShelf = shelf,
            requiredToClear = def == null || def.requiredToClear
        };
    }

    private static LevelDataSO CreateSampleLevelIfMissing(string assetPath, int levelNum, string levelName, int maxDrops, int targetScore, BallKitSO kit, List<LevelBlockPlacement> placements)
    {
        LevelDataSO existing = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
        if (existing != null)
        {
            // Keep user edits if level already has blocks; populate if empty
            if (existing.blocks != null && existing.blocks.Count > 0)
            {
                return existing;
            }
        }
        else
        {
            existing = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(existing, assetPath);
        }

        existing.gameMode = GameMode.Level;
        existing.levelNumber = levelNum;
        existing.levelName = levelName;
        existing.winCondition = LevelWinCondition.DestroyAllBlocks;
        existing.maxDrops = maxDrops;
        existing.targetScore = targetScore;
        existing.containerWidth = 5.5f;
        existing.containerHeight = 8.5f;
        existing.wallThickness = 0.5f;
        existing.dropHeight = 3.8f;
        existing.dropCooldown = 0.5f;
        existing.dangerLineY = 2.5f;
        existing.dangerTimeLimit = 3.0f;
        existing.mergeBlastRadius = 1.15f;
        existing.mergeBaseDamage = 1;
        existing.defaultKit = kit;
        if (kit != null)
        {
            existing.allTiers = new List<BallDataSO>(kit.ballTiers);
            existing.droppableTiers = new List<BallDataSO>(kit.droppableTiers);
        }
        existing.blocks = placements;

        EditorUtility.SetDirty(existing);
        return existing;
    }

    public static LevelDatabaseSO SyncAllLevelsToDatabase()
    {
        EnsureFolderExists(RESOURCES_ROOT);

        LevelDatabaseSO database = AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>(LEVEL_DATABASE_PATH);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<LevelDatabaseSO>();
            AssetDatabase.CreateAsset(database, LEVEL_DATABASE_PATH);
        }

        var levelsByNumber = new Dictionary<int, LevelDataSO>();
        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            LevelDataSO lvl = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (lvl == null || lvl.gameMode != GameMode.Level) continue;

            bool isCampaignAsset = path.StartsWith(LEVELS_FOLDER, StringComparison.OrdinalIgnoreCase);
            if (!levelsByNumber.ContainsKey(lvl.levelNumber) || isCampaignAsset)
            {
                levelsByNumber[lvl.levelNumber] = lvl;
            }
        }

        database.levels = new List<LevelDataSO>(levelsByNumber.Values);
        database.SortByLevelNumber();
        EditorUtility.SetDirty(database);
        LevelDatabaseSO.ClearCache();
        return database;
    }

    private static void BuildBlockTestingSceneHierarchy(LevelDatabaseSO database)
    {
        LevelDataSO firstLevel = (database != null && database.levels != null && database.levels.Count > 0)
            ? database.levels[0]
            : AssetDatabase.LoadAssetAtPath<LevelDataSO>($"{LEVELS_FOLDER}/Level_01.asset");

        // 1. Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.65f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.11f, 0.13f, 0.19f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        EnsureComp<CameraShake2D>(cam.gameObject);

        // 2. [Managers]
        GameObject managersObj = GameObject.Find("[Managers]");
        if (managersObj == null) managersObj = new GameObject("[Managers]");

        EnsureComp<DependencyManager>(managersObj);
        GameManager gm = EnsureComp<GameManager>(managersObj);
        SerializedObject gmSo = new SerializedObject(gm);
        SerializedProperty autoInitProp = gmSo.FindProperty("autoInitializeOnStart");
        if (autoInitProp != null) autoInitProp.boolValue = false;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        EnsureComp<AudioManager>(managersObj);
        BallMergeManager bmm = EnsureComp<BallMergeManager>(managersObj);
        if (firstLevel != null) bmm.SetLevelData(firstLevel);

        EnsureComp<FloatingScoreManager>(managersObj);
        EnsureComp<PlayerDataManager>(managersObj);

        BallKitManager bkm = EnsureComp<BallKitManager>(managersObj);
        BallKitSO classicKit = AssetDatabase.LoadAssetAtPath<BallKitSO>("Assets/Game/Data/Kits/BallKit_classic_kit.asset");
        BallKitSO oceanKit = AssetDatabase.LoadAssetAtPath<BallKitSO>("Assets/Game/Data/Kits/BallKit_ocean_kit.asset");
        BallKitSO skyKit = AssetDatabase.LoadAssetAtPath<BallKitSO>("Assets/Game/Data/Kits/BallKit_sky_kit.asset");
        var allKits = new List<BallKitSO>();
        if (classicKit != null) allKits.Add(classicKit);
        if (oceanKit != null) allKits.Add(oceanKit);
        if (skyKit != null) allKits.Add(skyKit);
        if (allKits.Count > 0)
        {
            bkm.RegisterKits(allKits, classicKit ?? allKits[0]);
        }

        ThemeManager tm = EnsureComp<ThemeManager>(managersObj);
        ThemeDataSO darkTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>("Assets/Game/Data/Themes/ThemeData_dark_theme.asset");
        ThemeDataSO oceanTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>("Assets/Game/Data/Themes/ThemeData_ocean_theme.asset");
        ThemeDataSO skyTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>("Assets/Game/Data/Themes/ThemeData_sky_theme.asset");
        ThemeDataSO sunsetTheme = AssetDatabase.LoadAssetAtPath<ThemeDataSO>("Assets/Game/Data/Themes/ThemeData_sunset_theme.asset");
        var allThemes = new List<ThemeDataSO>();
        if (darkTheme != null) allThemes.Add(darkTheme);
        if (oceanTheme != null) allThemes.Add(oceanTheme);
        if (skyTheme != null) allThemes.Add(skyTheme);
        if (sunsetTheme != null) allThemes.Add(sunsetTheme);
        if (allThemes.Count > 0)
        {
            tm.RegisterThemes(allThemes, darkTheme ?? allThemes[0]);
        }

        // 3. [Gameplay]
        GameObject gameplayObj = GameObject.Find("[Gameplay]");
        if (gameplayObj == null) gameplayObj = new GameObject("[Gameplay]");

        GameplayController gc = EnsureComp<GameplayController>(gameplayObj);

        GameObject bgAnimObj = EnsureChild(gameplayObj, "ThemeAnimatedBackground");
        EnsureComp<ThemeAnimatedBackground>(bgAnimObj);

        GameObject containerObj = EnsureChild(gameplayObj, "ContainerBoundary");
        ContainerBoundary cb = EnsureComp<ContainerBoundary>(containerObj);
        cb.Setup(firstLevel);

        GameObject dangerObj = EnsureChild(gameplayObj, "DangerLine");
        DangerLine dl = EnsureComp<DangerLine>(dangerObj);
        dl.Setup(firstLevel);

        GameObject dropperObj = EnsureChild(gameplayObj, "BallDropper");
        BallDropper bd = EnsureComp<BallDropper>(dropperObj);

        GameObject blockMgrObj = EnsureChild(gameplayObj, "LevelBlockManager");
        LevelBlockManager lbm = EnsureComp<LevelBlockManager>(blockMgrObj);

        SerializedObject gcSo = new SerializedObject(gc);
        SetProp(gcSo, "levelData", firstLevel);
        SetProp(gcSo, "dropper", bd);
        SetProp(gcSo, "container", cb);
        SetProp(gcSo, "dangerLine", dl);
        SetProp(gcSo, "blockManager", lbm);
        gcSo.ApplyModifiedPropertiesWithoutUndo();

        // Preview Level 1 blocks inside the scene right away so user sees them in Scene View
        lbm.LoadLevelBlocks(firstLevel, cb);

        // 4. EventSystem & Test HUD Canvas
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        BuildBlockTestingHUD(firstLevel, gc, cb, lbm, bd, dl);
    }

    private static void BuildBlockTestingHUD(
        LevelDataSO firstLevel,
        GameplayController gc,
        ContainerBoundary cb,
        LevelBlockManager lbm,
        BallDropper bd,
        DangerLine dl)
    {
        GameObject oldCanvas = GameObject.Find("BlockTestCanvas");
        if (oldCanvas != null) UnityEngine.Object.DestroyImmediate(oldCanvas);

        GameObject canvasObj = new GameObject("BlockTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        BlockTestingController btc = canvasObj.AddComponent<BlockTestingController>();

        Sprite[] uiPack2 = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Sprites/Full sprite sheet/UI-pack_Sprite_2.png").OfType<Sprite>().ToArray();
        Sprite spBtnGreen = uiPack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_16");
        Sprite spBtnYellow = uiPack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_9");
        Sprite spBtnBlue = uiPack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_10");
        Sprite spFrameBlue = uiPack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_0");
        Sprite spFrameCyan = uiPack2.FirstOrDefault(s => s.name == "UI-pack_Sprite_2_4");
        Sprite spWhiteBar = BlockSpriteGenerator.LoadSprite("UI_White_Bar.png");

        // Top HUD bar (sits cleanly above dropHeight)
        Image topBar = CreateSlicedImage(canvasObj, "TopBar", new Vector2(0, 862), new Vector2(1050, 185),
            spFrameBlue, new Color(0.10f, 0.12f, 0.18f, 0.94f));

        Button prevBtn = CreateStyledButton(topBar.gameObject, "PrevLevelButton", "< PREV", new Vector2(-415, 44), new Vector2(176, 62), spBtnBlue, new Color(0.22f, 0.28f, 0.42f));
        TMP_Text titleTxt = CreateUIText(topBar.gameObject, "LevelTitleText", "LVL 1: Crate Valley", new Vector2(-45, 44), new Vector2(510, 62), 34, new Color(0.35f, 0.88f, 1f), TextAlignmentOptions.Center);
        Button nextBtn = CreateStyledButton(topBar.gameObject, "NextLevelButton", "NEXT >", new Vector2(282, 44), new Vector2(150, 62), spBtnBlue, new Color(0.22f, 0.28f, 0.42f));
        Button restartBtn = CreateStyledButton(topBar.gameObject, "RestartButton", "RESET", new Vector2(442, 44), new Vector2(140, 62), spBtnYellow, new Color(0.85f, 0.45f, 0.18f));

        TMP_Text blocksTxt = CreateUIText(topBar.gameObject, "BlocksText", "BLOCKS: 4 / 4", new Vector2(-345, -24), new Vector2(320, 44), 27, new Color(1f, 0.76f, 0.28f), TextAlignmentOptions.Left);
        TMP_Text dropsTxt = CreateUIText(topBar.gameObject, "DropsText", "DROPS: 20 / 20", new Vector2(0, -24), new Vector2(320, 44), 27, new Color(1f, 0.52f, 0.52f), TextAlignmentOptions.Center);
        TMP_Text scoreTxt = CreateUIText(topBar.gameObject, "ScoreText", "SCORE: 0", new Vector2(345, -24), new Vector2(320, 44), 27, new Color(0.4f, 0.95f, 0.6f), TextAlignmentOptions.Right);

        // Blocks Cleared Progress Bar
        Image progBg = CreateUIImage(topBar.gameObject, "ProgressBg", new Vector2(0, -64), new Vector2(960, 16), new Color(0.05f, 0.06f, 0.09f, 0.9f));
        if (spWhiteBar != null) progBg.sprite = spWhiteBar;
        Image progFill = CreateUIImage(progBg.gameObject, "BlocksProgressFill", Vector2.zero, new Vector2(960, 16), new Color(0.24f, 0.86f, 0.54f, 1f));
        if (spWhiteBar != null) progFill.sprite = spWhiteBar;
        progFill.type = Image.Type.Filled;
        progFill.fillMethod = Image.FillMethod.Horizontal;
        progFill.fillAmount = 0f;

        // Small Next Ball card in top-left below bar
        Image nextBox = CreateSlicedImage(canvasObj, "NextBallBox", new Vector2(-435, 685), new Vector2(158, 136),
            spFrameCyan, new Color(0.12f, 0.14f, 0.2f, 0.9f));
        CreateUIText(nextBox.gameObject, "NextLabel", "NEXT", new Vector2(0, 44), new Vector2(140, 30), 20, new Color(0.7f, 0.8f, 0.92f), TextAlignmentOptions.Center);
        Image nextImg = CreateUIImage(nextBox.gameObject, "NextBallImg", new Vector2(0, 0), new Vector2(48, 48), Color.white);
        TMP_Text nextName = CreateUIText(nextBox.gameObject, "NextBallName", "Cherry", new Vector2(0, -45), new Vector2(145, 30), 20, Color.white, TextAlignmentOptions.Center);

        // Coins & Theme Switcher card in top-right below bar
        Image rightBox = CreateSlicedImage(canvasObj, "ThemeCoinsBox", new Vector2(422, 685), new Vector2(190, 136),
            spFrameCyan, new Color(0.12f, 0.14f, 0.2f, 0.9f));
        TMP_Text coinsTxt = CreateUIText(rightBox.gameObject, "CoinsBalanceText", "COINS: 200", new Vector2(0, 38), new Vector2(175, 34), 22, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center);
        Button themeBtn = CreateStyledButton(rightBox.gameObject, "ThemeCycleButton", "THEME", new Vector2(0, -14), new Vector2(160, 50), spBtnBlue, new Color(0.26f, 0.42f, 0.72f));
        TMP_Text themeLbl = themeBtn.GetComponentInChildren<TMP_Text>();
        if (themeLbl != null) themeLbl.fontSize = 18;

        // Bottom Tip Footer
        CreateUIText(canvasObj, "GameplayTip", "Aim & release to drop balls. Merge beside blocks to crack & break them!",
            new Vector2(0, -900), new Vector2(1020, 60), 23, new Color(0.72f, 0.78f, 0.88f, 0.85f), TextAlignmentOptions.Center);

        // Result Overlay
        Image overlay = CreateUIImage(canvasObj, "ResultOverlay", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0.78f));
        Image card = CreateSlicedImage(overlay.gameObject, "ResultCard", Vector2.zero, new Vector2(820, 690),
            spFrameBlue, new Color(0.13f, 0.16f, 0.23f, 1f));
        TMP_Text resTitle = CreateUIText(card.gameObject, "ResultTitle", "LEVEL CLEARED!", new Vector2(0, 250), new Vector2(740, 82), 52, new Color(0.25f, 0.95f, 0.55f), TextAlignmentOptions.Center);
        TMP_Text resStars = CreateUIText(card.gameObject, "ResultStars", "★ ★ ★", new Vector2(0, 175), new Vector2(700, 66), 48, new Color(1.0f, 0.86f, 0.24f), TextAlignmentOptions.Center);
        TMP_Text resSub = CreateUIText(card.gameObject, "ResultSubtitle", "Score: 0", new Vector2(0, 102), new Vector2(740, 54), 27, Color.white, TextAlignmentOptions.Center);
        TMP_Text resCoins = CreateUIText(card.gameObject, "ResultCoinBonusText", "+0 COINS EARNED!", new Vector2(0, 22), new Vector2(760, 84), 26, new Color(1.0f, 0.88f, 0.28f), TextAlignmentOptions.Center);
        Button resNext = CreateStyledButton(card.gameObject, "ResultNextButton", "NEXT LEVEL", new Vector2(0, -120), new Vector2(440, 92), spBtnGreen, new Color(0.18f, 0.78f, 0.44f));
        Button resRetry = CreateStyledButton(card.gameObject, "ResultRetryButton", "REPLAY LEVEL", new Vector2(0, -234), new Vector2(440, 92), spBtnBlue, new Color(0.22f, 0.56f, 0.88f));
        overlay.gameObject.SetActive(false);

        SerializedObject btcSo = new SerializedObject(btc);
        SetProp(btcSo, "startLevel", firstLevel);
        SetProp(btcSo, "gameplayController", gc);
        SetProp(btcSo, "containerBoundary", cb);
        SetProp(btcSo, "blockManager", lbm);
        SetProp(btcSo, "ballDropper", bd);
        SetProp(btcSo, "dangerLine", dl);
        SetProp(btcSo, "levelTitleText", titleTxt);
        SetProp(btcSo, "blocksText", blocksTxt);
        SetProp(btcSo, "blocksProgressFill", progFill);
        SetProp(btcSo, "dropsText", dropsTxt);
        SetProp(btcSo, "scoreText", scoreTxt);
        SetProp(btcSo, "coinsBalanceText", coinsTxt);
        SetProp(btcSo, "nextBallNameText", nextName);
        SetProp(btcSo, "nextBallImage", nextImg);
        SetProp(btcSo, "prevLevelButton", prevBtn);
        SetProp(btcSo, "nextLevelButton", nextBtn);
        SetProp(btcSo, "restartButton", restartBtn);
        SetProp(btcSo, "themeCycleButton", themeBtn);
        SetProp(btcSo, "themeLabelText", themeLbl);
        SetProp(btcSo, "resultOverlay", overlay.gameObject);
        SetProp(btcSo, "resultCard", card.transform);
        SetProp(btcSo, "resultTitleText", resTitle);
        SetProp(btcSo, "resultStarsText", resStars);
        SetProp(btcSo, "resultSubtitleText", resSub);
        SetProp(btcSo, "resultCoinBonusText", resCoins);
        SetProp(btcSo, "resultNextButton", resNext);
        SetProp(btcSo, "resultRetryButton", resRetry);
        btcSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T EnsureComp<T>(GameObject obj) where T : Component
    {
        T c = obj.GetComponent<T>();
        return c != null ? c : obj.AddComponent<T>();
    }

    private static GameObject EnsureChild(GameObject parent, string childName)
    {
        Transform t = parent.transform.Find(childName);
        if (t != null) return t.gameObject;
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void SetProp(SerializedObject so, string propName, UnityEngine.Object val)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.objectReferenceValue = val;
    }

    private static Image CreateUIImage(GameObject parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text CreateUIText(GameObject parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    private static Image CreateSlicedImage(GameObject parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        Image img = CreateUIImage(parent, name, pos, size, color);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            // Keep frames legible under white/gold HUD text
            img.color = name.Contains("TopBar") ? new Color(0.22f, 0.28f, 0.42f, 0.94f)
                      : (name.Contains("ResultCard") ? new Color(0.20f, 0.25f, 0.38f, 0.98f) : new Color(0.25f, 0.34f, 0.50f, 0.92f));
        }
        return img;
    }

    private static Button CreateStyledButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Sprite sprite, Color fallbackColor)
    {
        Image img = CreateUIImage(parent, name, pos, size, fallbackColor);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        TMP_Text txt = CreateUIText(img.gameObject, "Label", label, Vector2.zero, size, size.y * 0.36f, Color.white, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        return btn;
    }

    public static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folder = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, folder);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Ball Merge/6. Wire New Features Into GameTest Scene (Keep Existing Flow)")]
    public static void UpgradeGameTestSceneNonDestructive()
    {
        BallMergeAssetCreator.GenerateDefaultAssets();
        GenerateAllBlockAssetsAndSampleLevels();

        if (!File.Exists(GAME_TEST_SCENE_PATH))
        {
            Debug.LogWarning($"[BlockLevelSetupUtility] Scene not found at {GAME_TEST_SCENE_PATH}");
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        if (active.path != GAME_TEST_SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GAME_TEST_SCENE_PATH, OpenSceneMode.Single);
        }

        UpgradeGameTestSceneInternal(saveScene: true);
    }

    private static bool NeedsGameTestUpgrade()
    {
        if (UnityEngine.Object.FindAnyObjectByType<LevelBlockManager>(FindObjectsInactive.Include) == null) return true;
        if (UnityEngine.Object.FindAnyObjectByType<ThemeAnimatedBackground>(FindObjectsInactive.Include) == null) return true;
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraShake2D>() == null) return true;
        GameplayHUDPanel hud = UnityEngine.Object.FindAnyObjectByType<GameplayHUDPanel>(FindObjectsInactive.Include);
        if (hud != null && hud.transform.Find("BlocksContainer") == null) return true;
        LevelSelectionPanel lp = UnityEngine.Object.FindAnyObjectByType<LevelSelectionPanel>(FindObjectsInactive.Include);
        if (lp != null && lp.GetComponentInChildren<LevelMapController>(true) == null) return true;
        return false;
    }

    private static void UpgradeGameTestSceneInternal(bool saveScene)
    {
        int changes = 0;

        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraShake2D>() == null)
        {
            cam.gameObject.AddComponent<CameraShake2D>();
            changes++;
        }

        GameplayController gc = UnityEngine.Object.FindAnyObjectByType<GameplayController>(FindObjectsInactive.Include);
        if (gc != null)
        {
            LevelBlockManager bm = UnityEngine.Object.FindAnyObjectByType<LevelBlockManager>(FindObjectsInactive.Include);
            if (bm == null)
            {
                GameObject bmObj = new GameObject("LevelBlockManager");
                bmObj.transform.SetParent(gc.transform, false);
                bm = bmObj.AddComponent<LevelBlockManager>();
                changes++;
            }

            ThemeAnimatedBackground bg = UnityEngine.Object.FindAnyObjectByType<ThemeAnimatedBackground>(FindObjectsInactive.Include);
            if (bg == null)
            {
                GameObject bgObj = new GameObject("ThemeAnimatedBackground");
                bgObj.transform.SetParent(gc.transform, false);
                bg = bgObj.AddComponent<ThemeAnimatedBackground>();
                changes++;
            }

            SerializedObject gcSo = new SerializedObject(gc);
            SerializedProperty bmProp = gcSo.FindProperty("blockManager");
            if (bmProp != null && bmProp.objectReferenceValue == null)
            {
                bmProp.objectReferenceValue = bm;
                changes++;
            }
            SerializedProperty bgProp = gcSo.FindProperty("animatedBackground");
            if (bgProp != null && bgProp.objectReferenceValue == null)
            {
                bgProp.objectReferenceValue = bg;
                changes++;
            }
            gcSo.ApplyModifiedPropertiesWithoutUndo();
        }

        GameplayHUDPanel hud = UnityEngine.Object.FindAnyObjectByType<GameplayHUDPanel>(FindObjectsInactive.Include);
        if (hud != null)
        {
            SerializedObject hudSo = new SerializedObject(hud);
            SerializedProperty blocksContProp = hudSo.FindProperty("blocksContainer");
            SerializedProperty blocksTxtProp = hudSo.FindProperty("blocksText");
            if (blocksContProp != null && blocksContProp.objectReferenceValue == null)
            {
                Transform existing = hud.transform.Find("BlocksContainer");
                GameObject blocksObj = existing != null ? existing.gameObject : null;
                TMP_Text blocksTmp = blocksObj != null ? blocksObj.GetComponentInChildren<TMP_Text>(true) : null;

                if (blocksObj == null)
                {
                    SerializedProperty dropsContProp = hudSo.FindProperty("dropsContainer");
                    SerializedProperty dropsTxtProp = hudSo.FindProperty("dropsText");
                    GameObject dropsObj = dropsContProp != null ? dropsContProp.objectReferenceValue as GameObject : null;
                    TMP_Text dropsTmp = dropsTxtProp != null ? dropsTxtProp.objectReferenceValue as TMP_Text : null;

                    blocksObj = new GameObject("BlocksContainer");
                    blocksObj.transform.SetParent(hud.transform, false);
                    RectTransform rt = blocksObj.AddComponent<RectTransform>();
                    if (dropsObj != null)
                    {
                        RectTransform drt = dropsObj.GetComponent<RectTransform>();
                        rt.anchorMin = drt.anchorMin;
                        rt.anchorMax = drt.anchorMax;
                        rt.pivot = drt.pivot;
                        rt.anchoredPosition = drt.anchoredPosition + new Vector2(0f, -55f);
                        rt.sizeDelta = new Vector2(drt.sizeDelta.x, 50f);
                    }
                    else
                    {
                        rt.anchoredPosition = new Vector2(360f, 785f);
                        rt.sizeDelta = new Vector2(300f, 50f);
                    }

                    blocksTmp = CreateUIText(blocksObj, "BlocksText", "BLOCKS: 0 / 0", Vector2.zero, rt.sizeDelta, 26f, new Color(1f, 0.72f, 0.25f), TextAlignmentOptions.Right);
                    RectTransform txtRt = blocksTmp.rectTransform;
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.offsetMin = Vector2.zero;
                    txtRt.offsetMax = Vector2.zero;
                    if (dropsTmp != null && dropsTmp.font != null) blocksTmp.font = dropsTmp.font;
                }

                blocksContProp.objectReferenceValue = blocksObj;
                if (blocksTxtProp != null) blocksTxtProp.objectReferenceValue = blocksTmp;
                hudSo.ApplyModifiedPropertiesWithoutUndo();
                changes++;
            }
        }

        LevelWinPanel winPanel = UnityEngine.Object.FindAnyObjectByType<LevelWinPanel>(FindObjectsInactive.Include);
        if (winPanel != null)
        {
            SerializedObject winSo = new SerializedObject(winPanel);
            SerializedProperty coinProp = winSo.FindProperty("coinBonusText");
            if (coinProp != null && coinProp.objectReferenceValue == null)
            {
                Transform windowBox = winPanel.transform.Find("WindowBox");
                if (windowBox != null)
                {
                    Transform existing = windowBox.Find("CoinBonusText");
                    TMP_Text coinTmp = existing != null ? existing.GetComponent<TMP_Text>() : null;
                    if (coinTmp == null)
                    {
                        coinTmp = CreateUIText(windowBox.gameObject, "CoinBonusText", "", new Vector2(0f, 22f), new Vector2(760f, 56f), 23f, new Color(1f, 0.88f, 0.28f), TextAlignmentOptions.Center);
                        SerializedProperty scoreProp = winSo.FindProperty("scoreText");
                        TMP_Text scoreTmp = scoreProp != null ? scoreProp.objectReferenceValue as TMP_Text : null;
                        if (scoreTmp != null && scoreTmp.font != null) coinTmp.font = scoreTmp.font;
                    }
                    coinProp.objectReferenceValue = coinTmp;
                    winSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                }
            }
        }

        LevelSelectionPanel levelPanel = UnityEngine.Object.FindAnyObjectByType<LevelSelectionPanel>(FindObjectsInactive.Include);
        if (levelPanel != null && levelPanel.GetComponentInChildren<LevelMapController>(true) == null)
        {
            Transform mapHost = levelPanel.transform.Find("GridCard");
            if (mapHost == null) mapHost = levelPanel.transform;

            GameObject scrollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEVEL_SCROLL_PREFAB_PATH);
            if (scrollPrefab != null)
            {
                Transform legacyScroll = mapHost.Find("ScrollView");
                if (legacyScroll != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacyScroll.gameObject);
                }

                GameObject scrollInstance = PrefabUtility.InstantiatePrefab(scrollPrefab, mapHost) as GameObject;
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
                        srt.localScale = Vector3.one;
                    }

                    Image bgImg = scrollInstance.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.color = new Color(0.06f, 0.09f, 0.16f, 0.92f);
                    }

                    LevelMapController lmc = scrollInstance.GetComponent<LevelMapController>();
                    if (lmc != null)
                    {
                        SerializedObject lmcSo = new SerializedObject(lmc);
                        SerializedProperty loadAllProp = lmcSo.FindProperty("loadAllInitially");
                        if (loadAllProp != null) loadAllProp.boolValue = false;
                        SerializedProperty initCountProp = lmcSo.FindProperty("initialLoadCount");
                        if (initCountProp != null) initCountProp.intValue = 50;
                        SerializedProperty batchProp = lmcSo.FindProperty("batchLoadCount");
                        if (batchProp != null) batchProp.intValue = 25;
                        lmcSo.ApplyModifiedPropertiesWithoutUndo();
                    }

                    SerializedObject lpSo = new SerializedObject(levelPanel);
                    SerializedProperty lmcProp = lpSo.FindProperty("levelMapController");
                    if (lmcProp != null) lmcProp.objectReferenceValue = lmc;
                    SerializedProperty gridProp = lpSo.FindProperty("levelGridContainer");
                    if (gridProp != null) gridProp.objectReferenceValue = null;
                    lpSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                }
            }
        }

        if (changes > 0)
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[BlockLevelSetupUtility] Non-destructively upgraded GameTest.unity ({changes} addition(s)) while keeping existing UI & flow intact.");
        }
    }
}
#endif
