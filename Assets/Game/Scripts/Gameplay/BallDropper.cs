using System.Collections;
using UnityEngine;

/// <summary>
/// Controls horizontal aiming and dropping of balls into the container.
/// Follows player mouse/touch input, clamps boundaries, and handles drop cooldown.
/// </summary>
public class BallDropper : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LevelDataSO levelData;
    [SerializeField] private Camera mainCamera;

    [Header("Dropper State")]
    [SerializeField] private Ball currentBall;
    [SerializeField] private BallDataSO nextBallData;
    [SerializeField] private bool canDrop = true;
    [SerializeField] private bool isInputActive = true;

    private float minX = -2f;
    private float maxX = 2f;
    private float currentAimX = 0f;

    public Ball CurrentBall => currentBall;
    public BallDataSO NextBallData => nextBallData;
    public bool CanDrop => canDrop;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        UpdateBoundaries();
        PrepareInitialBalls();
    }

    public void Setup(LevelDataSO data)
    {
        levelData = data;
        UpdateBoundaries();
        PrepareInitialBalls();
    }

    private void UpdateBoundaries()
    {
        if (levelData != null)
        {
            float halfWidth = levelData.containerWidth * 0.5f;
            // Subtract ball margin so the ball doesn't clip the side walls while aiming
            float margin = 0.4f;
            minX = -halfWidth + margin;
            maxX = halfWidth - margin;
            transform.position = new Vector3(0f, levelData.dropHeight, 0f);
        }
    }

    private void PrepareInitialBalls()
    {
        if (levelData == null) return;

        nextBallData = levelData.GetRandomDroppableTier();
        SpawnNextBallToDropper();
    }

    private void SpawnNextBallToDropper()
    {
        if (levelData == null || !BallMergeManager.HasInstance) return;

        BallDataSO dataToSpawn = nextBallData ?? levelData.GetRandomDroppableTier();
        nextBallData = levelData.GetRandomDroppableTier();

        Vector3 spawnPos = new Vector3(currentAimX, levelData.dropHeight, 0f);
        currentBall = BallMergeManager.Instance.InstantiateBall(dataToSpawn, spawnPos);
        currentBall.HoldInDropper(spawnPos);

        canDrop = true;
    }

    private void Update()
    {
        if (!isInputActive || levelData == null) return;

        HandleAimingInput();
        HandleDropInput();
    }

    private void HandleAimingInput()
    {
        if (mainCamera == null) return;

        Vector3 pointerScreenPos = Input.mousePosition;
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(pointerScreenPos);

        currentAimX = Mathf.Clamp(worldPoint.x, minX, maxX);
        transform.position = new Vector3(currentAimX, levelData.dropHeight, 0f);

        // Keep held ball aligned with dropper position
        if (currentBall != null && !currentBall.IsDropped)
        {
            currentBall.transform.position = transform.position;
        }
    }

    private void HandleDropInput()
    {
        // Drop on mouse click or screen tap release
        if (Input.GetMouseButtonUp(0) && canDrop && currentBall != null)
        {
            DropBall();
        }
    }

    /// <summary>
    /// Releases the held ball and initiates cooldown for the next ball.
    /// </summary>
    public void DropBall()
    {
        if (!canDrop || currentBall == null) return;

        canDrop = false;
        currentBall.Drop();
        currentBall = null;

        GameEvents.OnPlaySFX?.Invoke("Drop");

        StartCoroutine(DropCooldownRoutine());
    }

    private IEnumerator DropCooldownRoutine()
    {
        float cooldown = levelData != null ? levelData.dropCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);

        SpawnNextBallToDropper();
    }

    public void SetInputActive(bool active)
    {
        isInputActive = active;
    }
}
