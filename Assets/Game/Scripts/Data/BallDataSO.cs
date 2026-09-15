using UnityEngine;

/// <summary>
/// ScriptableObject defining properties for a single ball tier in the merge game.
/// </summary>
[CreateAssetMenu(fileName = "BallData_Tier_", menuName = "BallMerge/Ball Data")]
public class BallDataSO : ScriptableObject
{
    [Header("Tier Information")]
    [Tooltip("Tier index (0 = smallest, higher numbers = higher merge tiers)")]
    public int tier;
    public string ballName = "Ball";

    [Header("Visual Properties")]
    [Tooltip("Visual and physical radius/scale multiplier")]
    [Range(0.2f, 5f)]
    public float radius = 0.5f;
    public Color ballColor = Color.white;
    public Sprite ballSprite;

    [Header("Score & Progression")]
    [Tooltip("Score awarded when two lower-tier balls merge into this ball")]
    public int scoreValue = 10;

    [Header("Physics Properties")]
    [Range(0.1f, 10f)]
    public float mass = 1f;
    [Range(0f, 1f)]
    public float bounciness = 0.2f;
    [Range(0f, 1f)]
    public float friction = 0.4f;
}
