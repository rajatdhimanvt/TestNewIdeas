using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines how a Level Mode stage is completed.
/// </summary>
public enum LevelWinCondition
{
    TargetScore = 0,
    DestroyAllBlocks = 1,
    BlocksAndScore = 2
}

/// <summary>
/// ScriptableObject defining level configuration, tier progressions, dropper pool,
/// container bounds, and placed destructible blocks/obstacles.
/// </summary>
[CreateAssetMenu(fileName = "LevelData_", menuName = "BallMerge/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Game Mode Configuration")]
    [Tooltip("Endless Mode vs Level Mode")]
    public GameMode gameMode = GameMode.Endless;

    [Tooltip("Level number for campaign progression (Level mode)")]
    public int levelNumber = 1;

    [Tooltip("How this level is won (Destroy All Blocks, Target Score, or Both)")]
    public LevelWinCondition winCondition = LevelWinCondition.TargetScore;

    [Tooltip("Target score required to complete the level (when winCondition uses score)")]
    public int targetScore = 500;

    [Tooltip("Maximum allowed ball drops (Level mode). Set to 0 for unlimited.")]
    public int maxDrops = 30;

    [Header("Level Info")]
    public string levelName = "Default Level";


    [Header("Dropper Configuration")]
    [Tooltip("Cooldown delay in seconds before next ball can be dropped")]
    [Range(0.1f, 2f)]
    public float dropCooldown = 0.5f;

    [Tooltip("Vertical Y position where the dropper aims and drops")]
    public float dropHeight = 3.8f;

    [Header("Container Dimensions")]
    [Tooltip("Interior horizontal width of the container")]
    [Range(3f, 12f)]
    public float containerWidth = 5.5f;

    [Tooltip("Height of container side walls")]
    [Range(4f, 15f)]
    public float containerHeight = 8.5f;

    [Tooltip("Thickness of wall and floor colliders")]
    [Range(0.2f, 2f)]
    public float wallThickness = 0.5f;

    [Header("Active Ball Kit")]
    [Tooltip("Default Ball Kit theme for this level. If BallKitManager is active, equipped kit is prioritized.")]
    public BallKitSO defaultKit;

    [Header("Fallback Ball Tiers (Legacy)")]
    [Tooltip("Fallback list of ball tiers if no BallKit is active")]
    public List<BallDataSO> allTiers = new List<BallDataSO>();

    [Tooltip("Fallback pool of droppable ball tiers if no BallKit is active")]
    public List<BallDataSO> droppableTiers = new List<BallDataSO>();

    [Header("Danger Line / Overflow Limit")]
    [Tooltip("Vertical Y position where overflow warning triggers")]
    public float dangerLineY = 2.5f;

    [Tooltip("Time in seconds a ball can stay above danger line before Game Over triggers")]
    [Range(1f, 10f)]
    public float dangerTimeLimit = 3.0f;

    [Header("Block Obstacles & Merge Destruction")]
    [Tooltip("Minimum radius around a merge midpoint that damages nearby blocks")]
    [Range(0.4f, 3.5f)]
    public float mergeBlastRadius = 1.15f;

    [Tooltip("Base HP damage dealt to each block hit by a nearby merge")]
    [Min(1)]
    public int mergeBaseDamage = 1;

    [Tooltip("If true, higher-tier ball merges deal extra damage (1 + tier/2)")]
    public bool scaleDamageWithTier = false;

    [Tooltip("Blocks and static ledges placed in this level")]
    public List<LevelBlockPlacement> blocks = new List<LevelBlockPlacement>();

    public bool HasBlocks => blocks != null && blocks.Count > 0;

    /// <summary>
    /// Counts how many blocks in this level are required to be destroyed for completion.
    /// </summary>
    public int GetRequiredBlockCount(BlockLibrarySO library = null)
    {
        if (blocks == null) return 0;

        int count = 0;
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] != null && blocks[i].IsRequiredToClear(blocks[i].ResolveDefinition(library)))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Returns the active kit (Equipped kit from BallKitManager or defaultKit).
    /// </summary>
    public BallKitSO GetActiveKit()
    {
        if (BallKitManager.HasInstance && BallKitManager.Instance.CurrentEquippedKit != null)
        {
            return BallKitManager.Instance.CurrentEquippedKit;
        }
        return defaultKit;
    }

    /// <summary>
    /// Returns the next tier ball data for a successful merge.
    /// Returns null if the current ball is already at maximum tier.
    /// </summary>
    public BallDataSO GetNextTier(BallDataSO currentTier)
    {
        BallKitSO activeKit = GetActiveKit();
        if (activeKit != null)
        {
            return activeKit.GetNextTier(currentTier);
        }

        if (currentTier == null || allTiers == null) return null;

        int index = allTiers.IndexOf(currentTier);
        if (index >= 0 && index + 1 < allTiers.Count)
        {
            return allTiers[index + 1];
        }

        // Fallback by tier index comparison
        for (int i = 0; i < allTiers.Count; i++)
        {
            if (allTiers[i] != null && allTiers[i].tier == currentTier.tier + 1)
            {
                return allTiers[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Picks a random droppable tier for the player's next ball.
    /// </summary>
    public BallDataSO GetRandomDroppableTier()
    {
        BallKitSO activeKit = GetActiveKit();
        if (activeKit != null)
        {
            return activeKit.GetRandomDroppableTier();
        }

        if (droppableTiers != null && droppableTiers.Count > 0)
        {
            int randomIndex = Random.Range(0, droppableTiers.Count);
            return droppableTiers[randomIndex];
        }

        if (allTiers != null && allTiers.Count > 0)
        {
            return allTiers[0];
        }

        return null;
    }
}
