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

    [Header("Ball Tiers Progression")]
    [Tooltip("All available ball tiers ordered from smallest to largest")]
    public List<BallDataSO> allTiers = new List<BallDataSO>();

    [Tooltip("Pool of ball tiers that can be randomly given to the player to drop")]
    public List<BallDataSO> droppableTiers = new List<BallDataSO>();

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

    /// <summary>
    /// Returns the next tier ball data for a successful merge.
    /// Returns null if the current ball is already at maximum tier.
    /// </summary>
    public BallDataSO GetNextTier(BallDataSO currentTier)
    {
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
