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

    public LevelDataSO CurrentLevelData => currentLevelData;

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

    /// <summary>
    /// Executes merge between two identical tier balls.
    /// Spawns the next tier ball at their midpoint with an animated pop.
    /// </summary>
    public void MergeBalls(Ball ballA, Ball ballB)
    {
        if (ballA == null || ballB == null) return;

        Vector3 midpoint = (ballA.transform.position + ballB.transform.position) * 0.5f;
        BallDataSO currentTier = ballA.Data;

        // Animate balls shrinking into midpoint
        ballA.transform.DOMove(midpoint, mergeAnimationDuration);
        ballA.transform.DOScale(Vector3.zero, mergeAnimationDuration);

        ballB.transform.DOMove(midpoint, mergeAnimationDuration);
        ballB.transform.DOScale(Vector3.zero, mergeAnimationDuration)
            .OnComplete(() =>
            {
                Destroy(ballA.gameObject);
                Destroy(ballB.gameObject);
                SpawnMergedTierBall(currentTier, midpoint);
            });
    }

    private void SpawnMergedTierBall(BallDataSO currentTier, Vector3 spawnPosition)
    {
        if (currentLevelData == null)
        {
            Debug.LogWarning("[BallMergeManager] No LevelDataSO assigned! Cannot resolve next tier.");
            return;
        }

        BallDataSO nextTier = currentLevelData.GetNextTier(currentTier);
        if (nextTier != null)
        {
            Ball newBall = InstantiateBall(nextTier, spawnPosition);
            newBall.Drop();

            // Pop bounce animation using DOTween
            float targetDiameter = nextTier.radius * 2f;
            newBall.transform.localScale = Vector3.one * (targetDiameter * 0.5f);
            newBall.transform.DOScale(targetDiameter, 0.2f).SetEase(Ease.OutBack);

            // Trigger score & audio events
            GameEvents.OnScoreChanged?.Invoke(nextTier.scoreValue);
            GameEvents.OnScoreAddedAtPosition?.Invoke(nextTier.scoreValue, spawnPosition);
            GameEvents.OnPlaySFX?.Invoke(mergeSoundKey);
        }
        else
        {
            // Max tier merged! Award jackpot score
            int jackpotScore = currentTier.scoreValue * 2;
            Debug.Log("[BallMergeManager] Max tier merged!");
            GameEvents.OnScoreChanged?.Invoke(jackpotScore);
            GameEvents.OnScoreAddedAtPosition?.Invoke(jackpotScore, spawnPosition);
            GameEvents.OnPlaySFX?.Invoke(mergeSoundKey);
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
