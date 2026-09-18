using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining level configuration, tier progressions, dropper pool, and container bounds.
/// </summary>
[CreateAssetMenu(fileName = "LevelData_", menuName = "BallMerge/Level Data")]
public class LevelDataSO : ScriptableObject
{
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
