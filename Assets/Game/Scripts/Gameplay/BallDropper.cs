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

    [Header("Aim Guide Line")]
    [SerializeField] private bool showAimGuide = true;
    [SerializeField] private float aimGuideWidth = 0.045f;

    [Header("Dropper State")]
    [SerializeField] private Ball currentBall;
    [SerializeField] private BallDataSO nextBallData;
    [SerializeField] private bool canDrop = true;
    [SerializeField] private bool isInputActive = true;
    private float ignoreInputTimer = 0f;

    private float minX = -2f;
    private float maxX = 2f;
    private float currentAimX = 0f;
    private LineRenderer aimLineRenderer;
    private Coroutine cooldownCoroutine;

    public Ball CurrentBall => currentBall;
    public BallDataSO NextBallData => nextBallData;
    public bool CanDrop => canDrop;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        EnsureAimLine();
    }

    private void Start()
    {
        if (levelData != null && currentBall == null && (!GameplayController.HasInstance || GameplayController.Instance.IsSessionActive))
        {
            UpdateBoundaries();
            PrepareInitialBalls();
        }
    }

    private void OnEnable()
    {
        if (isInputActive && currentBall == null && cooldownCoroutine == null && levelData != null &&
            (!GameplayController.HasInstance || GameplayController.Instance.IsSessionActive))
        {
            UpdateBoundaries();
            PrepareInitialBalls();
        }
    }

    public void Setup(LevelDataSO data)
    {
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        ClearHeldBall();
        levelData = data;
        ignoreInputTimer = 0.25f;
        UpdateBoundaries();
        PrepareInitialBalls();
    }

    public void ClearHeldBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
            currentBall = null;
        }
        canDrop = false;
        UpdateAimGuide();
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
        if (!isInputActive) return;

        if (GameplayController.HasInstance)
        {
            var gc = GameplayController.Instance;
            if (gc.LevelCompleted) return;
            if (levelData.gameMode == GameMode.Level && levelData.maxDrops > 0 && gc.RemainingDrops <= 0)
            {
                canDrop = false;
                UpdateAimGuide();
                return;
            }
        }

        BallDataSO dataToSpawn = nextBallData ?? levelData.GetRandomDroppableTier();
        nextBallData = levelData.GetRandomDroppableTier();
        if (dataToSpawn == null)
        {
            Debug.LogWarning("[BallDropper] No droppable BallDataSO found on active LevelDataSO!");
            canDrop = false;
            UpdateAimGuide();
            return;
        }

        Vector3 spawnPos = new Vector3(currentAimX, levelData.dropHeight, 0f);
        currentBall = BallMergeManager.Instance.InstantiateBall(dataToSpawn, spawnPos);
        currentBall.HoldInDropper(spawnPos);

        canDrop = true;
        UpdateAimGuide();
        GameEvents.OnNextBallUpdated?.Invoke();
    }

    private void Update()
    {
        if (!isInputActive || levelData == null)
        {
            if (aimLineRenderer != null && aimLineRenderer.enabled) aimLineRenderer.enabled = false;
            return;
        }
        if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.InGame)
        {
            if (aimLineRenderer != null && aimLineRenderer.enabled) aimLineRenderer.enabled = false;
            return;
        }

        if (ignoreInputTimer > 0f)
        {
            ignoreInputTimer -= Time.unscaledDeltaTime;
            HandleAimingInput();
            return;
        }

        HandleAimingInput();
        HandleDropInput();
    }

    private void HandleAimingInput()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 pointerScreenPos = Input.mousePosition;
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(pointerScreenPos);

        float ballRadius = currentBall != null && currentBall.Data != null ? currentBall.Data.radius : 0.4f;
        float halfWidth = levelData.containerWidth * 0.5f;
        float clampedMin = -halfWidth + ballRadius + 0.04f;
        float clampedMax = halfWidth - ballRadius - 0.04f;

        currentAimX = Mathf.Clamp(worldPoint.x, clampedMin, clampedMax);
        transform.position = new Vector3(currentAimX, levelData.dropHeight, 0f);

        // Keep held ball aligned with dropper position
        if (currentBall != null && !currentBall.IsDropped)
        {
            currentBall.transform.position = transform.position;
        }

        UpdateAimGuide();
    }

    private void HandleDropInput()
    {
        // Ignore drop input if pointer or touch is interacting with UI elements
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        }

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
        UpdateAimGuide();

        GameEvents.OnPlaySFX?.Invoke("Drop");
        GameEvents.OnBallDropped?.Invoke();

        if (gameObject.activeInHierarchy)
        {
            cooldownCoroutine = StartCoroutine(DropCooldownRoutine());
        }
    }

    private IEnumerator DropCooldownRoutine()
    {
        float cooldown = levelData != null ? levelData.dropCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);
        cooldownCoroutine = null;

        SpawnNextBallToDropper();
    }

    public void SetInputActive(bool active)
    {
        isInputActive = active;
        if (active)
        {
            ignoreInputTimer = 0.25f;
            if (currentBall == null && cooldownCoroutine == null && levelData != null && gameObject.activeInHierarchy)
            {
                SpawnNextBallToDropper();
            }
        }
        UpdateAimGuide();
    }

    private void EnsureAimLine()
    {
        if (aimLineRenderer != null) return;
        aimLineRenderer = GetComponent<LineRenderer>();
        if (aimLineRenderer == null)
        {
            aimLineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        aimLineRenderer.useWorldSpace = true;
        aimLineRenderer.positionCount = 2;
        aimLineRenderer.startWidth = aimGuideWidth;
        aimLineRenderer.endWidth = aimGuideWidth * 0.5f;
        aimLineRenderer.sortingOrder = -2;
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            aimLineRenderer.sharedMaterial = new Material(spriteShader);
        }
    }

    private void UpdateAimGuide()
    {
        if (aimLineRenderer == null) EnsureAimLine();
        if (aimLineRenderer == null) return;

        if (!showAimGuide || !isInputActive || currentBall == null || currentBall.IsDropped || levelData == null)
        {
            aimLineRenderer.enabled = false;
            return;
        }

        float ballRadius = currentBall.Data != null ? currentBall.Data.radius : 0.35f;
        Vector2 startPoint = new Vector2(currentAimX, levelData.dropHeight - ballRadius - 0.05f);
        float fallbackBottomY = -4f;
        float endY = fallbackBottomY;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(startPoint, ballRadius * 0.9f, Vector2.down, 14f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null || col.isTrigger) continue;
            if (col.GetComponentInParent<Ball>() == currentBall) continue;
            endY = hits[i].point.y;
            break;
        }

        aimLineRenderer.enabled = true;
        aimLineRenderer.SetPosition(0, new Vector3(currentAimX, startPoint.y, 0.1f));
        aimLineRenderer.SetPosition(1, new Vector3(currentAimX, endY, 0.1f));

        Color c = currentBall.Data != null ? currentBall.Data.ballColor : Color.white;
        aimLineRenderer.startColor = new Color(c.r, c.g, c.b, 0.45f);
        aimLineRenderer.endColor = new Color(c.r, c.g, c.b, 0.05f);
    }
}
