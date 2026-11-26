using DG.Tweening;
using UnityEngine;

/// <summary>
/// Manages ball merge resolution, next tier spawning at collision midpoint,
/// pop animations, and score dispatching.
/// </summary>
public class BallMergeManager : Singleton<BallMergeManager>
{
    [Header("Level Configuration")]
    [SerializeField] private LevelDataSO currentLevelData;

    [Header("Ball Prefab")]
    [Tooltip("Optional prefab for Ball. If null, a Ball is created procedurally with all components.")]
    [SerializeField] private GameObject ballPrefab;

    [Header("Merge Feedback")]
    [SerializeField] private float mergeAnimationDuration = 0.12f;
    [SerializeField] private string mergeSoundKey = "Merge";
    [SerializeField] private float comboWindowSeconds = 1.35f;

    private int activeMergeCount = 0;
    private int mergeSessionToken = 0;
    private int currentComboCount = 0;
    private float lastMergeTime = -99f;

    public LevelDataSO CurrentLevelData => currentLevelData;
    public int ActiveMergeCount => activeMergeCount;
    public int CurrentComboCount => currentComboCount;

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
    }

    public void SetLevelData(LevelDataSO levelData)
    {
        currentLevelData = levelData;
    }

    public void ResetMergeState()
    {
        mergeSessionToken++;
        activeMergeCount = 0;
        currentComboCount = 0;
        lastMergeTime = -99f;
    }

    /// <summary>
    /// Executes merge between two identical tier balls.
    /// Spawns the next tier ball at their midpoint with an animated pop.
    /// </summary>
    public void MergeBalls(Ball ballA, Ball ballB)
    {
        if (ballA == null || ballB == null) return;

        Vector3 midpoint = (ballA.transform.position + ballB.transform.position) * 0.5f;
        BallDataSO currentTier = ballA.Data;
        int tokenAtStart = mergeSessionToken;
        bool handled = false;
        activeMergeCount++;

        // Animate balls shrinking into midpoint
        ballA.transform.DOMove(midpoint, mergeAnimationDuration).SetLink(ballA.gameObject);
        ballA.transform.DOScale(Vector3.zero, mergeAnimationDuration).SetLink(ballA.gameObject);

        ballB.transform.DOMove(midpoint, mergeAnimationDuration).SetLink(ballB.gameObject);
        ballB.transform.DOScale(Vector3.zero, mergeAnimationDuration)
            .SetLink(ballB.gameObject)
            .OnComplete(() =>
            {
                if (handled) return;
                handled = true;
                activeMergeCount = Mathf.Max(0, activeMergeCount - 1);
                if (tokenAtStart != mergeSessionToken) return;

                if (ballA != null) Destroy(ballA.gameObject);
                if (ballB != null) Destroy(ballB.gameObject);
                SpawnMergedTierBall(currentTier, midpoint);
            })
            .OnKill(() =>
            {
                if (!handled)
                {
                    handled = true;
                    activeMergeCount = Mathf.Max(0, activeMergeCount - 1);
                }
            });
    }

    private void SpawnMergedTierBall(BallDataSO currentTier, Vector3 spawnPosition)
    {
        if (currentLevelData == null)
        {
            Debug.LogWarning("[BallMergeManager] No LevelDataSO assigned! Cannot resolve next tier.");
            return;
        }

        if (Time.time - lastMergeTime <= comboWindowSeconds)
        {
            currentComboCount++;
        }
        else
        {
            currentComboCount = 1;
        }
        lastMergeTime = Time.time;

        float comboPitch = 1f + Mathf.Min(currentComboCount - 1, 7) * 0.08f;

        BallDataSO nextTier = currentLevelData.GetNextTier(currentTier);
        if (nextTier != null)
        {
            Ball newBall = InstantiateBall(nextTier, spawnPosition);
            newBall.Drop();

            // Pop bounce animation using DOTween
            float targetDiameter = nextTier.radius * 2f;
            newBall.transform.localScale = Vector3.one * (targetDiameter * 0.5f);
            newBall.transform.DOScale(targetDiameter, 0.2f).SetEase(Ease.OutBack).SetLink(newBall.gameObject);

            // Trigger score, block merge-shockwave, & pitched audio events
            GameEvents.OnScoreChanged?.Invoke(nextTier.scoreValue);
            GameEvents.OnScoreAddedAtPosition?.Invoke(nextTier.scoreValue, spawnPosition);
            GameEvents.OnBallsMerged?.Invoke(spawnPosition, nextTier.radius, nextTier.tier);
            GameEvents.OnPlaySFXPitched?.Invoke(mergeSoundKey, comboPitch);
        }
        else
        {
            // Max tier merged! Award jackpot score & blast nearby blocks
            int jackpotScore = currentTier != null ? currentTier.scoreValue * 2 : 100;
            float blastRadius = currentTier != null ? currentTier.radius * 1.25f : 1.5f;
            int tierVal = currentTier != null ? currentTier.tier + 1 : 6;

            Debug.Log("[BallMergeManager] Max tier merged!");
            GameEvents.OnScoreChanged?.Invoke(jackpotScore);
            GameEvents.OnScoreAddedAtPosition?.Invoke(jackpotScore, spawnPosition);
            GameEvents.OnBallsMerged?.Invoke(spawnPosition, blastRadius, tierVal);
            GameEvents.OnPlaySFXPitched?.Invoke(mergeSoundKey, comboPitch);
        }

        if (currentComboCount >= 2)
        {
            GameEvents.OnComboAchieved?.Invoke(currentComboCount, spawnPosition);
            string comboText = currentComboCount >= 4 ? $"MEGA x{currentComboCount}!" : $"COMBO x{currentComboCount}!";
            Color comboCol = currentComboCount >= 4 ? new Color(1f, 0.45f, 0.95f) : new Color(0.35f, 0.95f, 1f);
            BlockRuntimeVisuals.SpawnFloatingTag(comboText, spawnPosition + Vector3.up * 0.5f, comboCol, 4.8f);
            CameraShake2D.TriggerShake(0.06f, 0.12f);
        }
    }

    /// <summary>
    /// Instantiates and initializes a Ball entity.
    /// </summary>
    public Ball InstantiateBall(BallDataSO data, Vector3 position)
    {
        GameObject obj;
        if (ballPrefab != null)
        {
            obj = Instantiate(ballPrefab, position, Quaternion.identity);
        }
        else
        {
            obj = new GameObject(data != null ? data.ballName : "Ball");
            obj.transform.position = position;
            obj.AddComponent<SpriteRenderer>();
            obj.AddComponent<CircleCollider2D>();
            obj.AddComponent<Rigidbody2D>();
            obj.AddComponent<Ball>();
        }

        Ball ballComponent = obj.GetComponent<Ball>();
        ballComponent.Initialize(data);
        return ballComponent;
    }
}
