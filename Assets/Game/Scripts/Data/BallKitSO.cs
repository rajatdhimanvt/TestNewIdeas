using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a complete Ball Kit (theme set of ball tiers).
/// Allows switching between different ball visual/physics themes (e.g. Classic, Ocean, Sky).
/// </summary>
[CreateAssetMenu(fileName = "BallKit_", menuName = "BallMerge/Ball Kit Data")]
public class BallKitSO : ScriptableObject
{
    [Header("Kit Metadata")]
    public string kitId = "classic_kit";
    public string kitName = "Classic Kit";
    public Sprite kitIcon;

    [Header("Ball Tiers in this Kit")]
    [Tooltip("All ball tiers for this kit ordered from smallest (Tier 0) to largest")]
    public List<BallDataSO> ballTiers = new List<BallDataSO>();

    [Tooltip("Subset of lower tiers from this kit that can be dropped by the player")]
    public List<BallDataSO> droppableTiers = new List<BallDataSO>();

    /// <summary>
    /// Returns the next tier BallDataSO within this kit.
    /// Returns null if current tier is the max tier.
    /// </summary>
    public BallDataSO GetNextTier(BallDataSO currentTier)
    {
        if (currentTier == null || ballTiers == null) return null;

        int index = ballTiers.IndexOf(currentTier);
        if (index >= 0 && index + 1 < ballTiers.Count)
        {
            return ballTiers[index + 1];
        }

        // Fallback match by tier index
        for (int i = 0; i < ballTiers.Count; i++)
        {
            if (ballTiers[i] != null && ballTiers[i].tier == currentTier.tier + 1)
            {
                return ballTiers[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a random droppable tier from this kit.
    /// </summary>
    public BallDataSO GetRandomDroppableTier()
    {
        if (droppableTiers != null && droppableTiers.Count > 0)
        {
            int randomIndex = Random.Range(0, droppableTiers.Count);
            return droppableTiers[randomIndex];
        }

        if (ballTiers != null && ballTiers.Count > 0)
        {
            return ballTiers[0];
        }

        return null;
    }
}
