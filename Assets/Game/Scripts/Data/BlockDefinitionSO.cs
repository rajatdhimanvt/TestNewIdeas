using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Categorizes block types in the Visual Level Editor palette and gameplay.
/// </summary>
public enum BlockCategory
{
    Weak = 0,
    Normal = 1,
    Strong = 2,
    Special = 3,
    Platform = 4
}

/// <summary>
/// Gameplay mechanic specialization for a block.
/// </summary>
public enum BlockBehaviorType
{
    Standard = 0,
    ExplosiveBomb = 1,
    BonusDrops = 2,
    IceFrozen = 3,
    MovingPlatform = 4,
    KeyTrigger = 5,
    LockedGate = 6,
    MagnetField = 7,
    BumperPad = 8
}

/// <summary>
/// ScriptableObject defining a reusable block template/type (HP, sprites, cracks,
/// destruction effects, collision rules, and special behaviors).
/// </summary>
[CreateAssetMenu(fileName = "BlockDef_", menuName = "BallMerge/Block Definition")]
public class BlockDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier used by JSON import/export and block lookup")]
    public string blockId = "crate_normal";
    public string displayName = "Normal Crate";
    public BlockCategory category = BlockCategory.Normal;
    [TextArea(1, 3)]
    public string description = "Standard wooden crate broken by nearby ball merges.";

    [Header("Health & Destruction Rules")]
    [Tooltip("Can this block be damaged and destroyed? Uncheck for static platforms/ledges.")]
    public bool isDestructible = true;

    [Tooltip("Must this block be destroyed to win the level?")]
    public bool requiredToClear = true;

    [Min(1)]
    public int defaultHP = 2;

    [Tooltip("Take damage when balls merge near this block")]
    public bool damageOnNearbyMerge = true;

    [Tooltip("Also take 1 damage on direct ball collision (off by default: merges break blocks)")]
    public bool damageOnBallCollision = false;

    [Tooltip("Extra reach added to merge blast radius checks around this block")]
    [Range(-0.5f, 2f)]
    public float extraMergeHitRadius = 0.0f;

    [Header("Special Behavior Configuration")]
    public BlockBehaviorType behaviorType = BlockBehaviorType.Standard;

    [Tooltip("Score awarded when this block is destroyed")]
    public int scoreReward = 50;

    [Tooltip("Extra ball drops granted when destroyed (e.g. Bonus Drop Crate)")]
    [Min(0)]
    public int bonusDropsOnDestroy = 0;

    [Tooltip("If enabled, triggers an area blast on destruction damaging nearby blocks")]
    public bool explodeOnDestroy = false;

    [Range(0.5f, 5f)]
    public float explosionRadius = 1.8f;

    [Min(1)]
    public int explosionDamage = 2;

    [Header("Moving / Key-Lock / Magnet / Bumper Defaults")]
    [Tooltip("Default ping-pong travel vector for Moving Platforms")]
    public Vector2 defaultMoveOffset = new Vector2(1.8f, 0f);
    [Range(0.2f, 6f)]
    public float defaultMoveSpeed = 1.4f;

    [Tooltip("Channel linking KeyTrigger and LockedGate blocks (0 = matches all)")]
    public int defaultLockChannel = 1;
    [Tooltip("Optional padlock overlay sprite shown while LockedGate is locked")]
    public Sprite lockOverlaySprite;

    [Tooltip("Magnetic pull radius for MagnetField blocks")]
    [Range(0.5f, 6f)]
    public float magnetRadius = 2.4f;
    [Tooltip("Pull force (>0 pulls balls in, <0 pushes away)")]
    public float magnetForce = 8.5f;

    [Tooltip("Impulse kick applied to balls bouncing off BumperPad blocks")]
    [Range(1f, 20f)]
    public float bumperImpulse = 7.5f;

    [Header("Default Transform & Physics")]
    public Vector2 defaultSize = new Vector2(0.85f, 0.85f);
    public float defaultRotation = 0f;

    [Range(0f, 1f)]
    public float bounciness = 0.25f;
    [Range(0f, 1f)]
    public float friction = 0.4f;

    [Header("Visuals & Crack Stages")]
    public Sprite baseSprite;
    [Tooltip("Crack overlays ordered from light damage to heavy damage")]
    public List<Sprite> crackSprites = new List<Sprite>();

    public Color baseColor = Color.white;
    public Color accentColor = new Color(0.95f, 0.45f, 0.15f, 1f);
    public Color hitParticleColor = new Color(1f, 0.78f, 0.35f, 1f);
    public Color destroyParticleColor = new Color(1f, 0.52f, 0.18f, 1f);

    [Tooltip("Displays a crisp HP counter badge on multi-HP or destructible blocks")]
    public bool showHPBadge = true;

    [Header("Audio & Optional Custom Prefab")]
    public string hitSoundKey = "BlockHit";
    public string destroySoundKey = "BlockBreak";
    [Tooltip("Optional custom prefab containing a DestructibleBlock component")]
    public GameObject customPrefab;

    /// <summary>
    /// Selects the appropriate crack sprite based on current vs maximum HP.
    /// Returns null if block is undamaged or no crack sprites exist.
    /// </summary>
    public Sprite GetCrackSpriteForHealth(int currentHP, int maxHP)
    {
        if (crackSprites == null || crackSprites.Count == 0) return null;
        if (currentHP >= maxHP || maxHP <= 0) return null;

        float damageFraction = 1f - Mathf.Clamp01((float)currentHP / maxHP);
        int index = Mathf.Clamp(
            Mathf.FloorToInt(damageFraction * crackSprites.Count),
            0,
            crackSprites.Count - 1
        );

        // Guarantee at least stage 0 crack shows as soon as block takes any damage
        if (currentHP < maxHP && index == 0 && crackSprites[0] == null) return null;
        return crackSprites[index];
    }
}
