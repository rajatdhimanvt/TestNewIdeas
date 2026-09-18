using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monitors balls overflowing near/above the danger line height.
/// Triggers visual warnings and Game Over if any settled ball remains above the line for too long.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DangerLine : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LevelDataSO levelData;

    [Header("Visual Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 0.3f, 0.3f, 0.4f);
    [SerializeField] private Color warningColor = new Color(1f, 0.1f, 0.1f, 0.9f);

    [Header("State")]
    [SerializeField] private float currentTimer = 3.0f;
    [SerializeField] private bool isDangerActive = false;
    [SerializeField] private bool isSessionActive = true;

    private BoxCollider2D triggerCollider;
    private SpriteRenderer lineRenderer;
    private readonly HashSet<Ball> ballsInZone = new HashSet<Ball>();

    public float CurrentTimer => currentTimer;
    public bool IsDangerActive => isDangerActive;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        EnsureVisualRenderer();
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged(ThemeDataSO newTheme)
    {
        if (newTheme != null)
        {
            SetNormalColor(newTheme.dangerLineColor);
        }
    }

    public void SetNormalColor(Color color)
    {
        normalColor = color;
        if (!isDangerActive && lineRenderer != null)
        {
            lineRenderer.color = normalColor;
        }
    }

    public void Setup(LevelDataSO data)
    {
        levelData = data;
        RebuildDangerLine();
        ResetTimer();

        if (ThemeManager.HasInstance && ThemeManager.Instance.CurrentEquippedTheme != null)
        {
            SetNormalColor(ThemeManager.Instance.CurrentEquippedTheme.dangerLineColor);
        }
    }

    /// <summary>
    /// Constructs/positions the danger line visual and trigger collider according to level data.
    /// </summary>
    [ContextMenu("Rebuild Danger Line")]
    public void RebuildDangerLine()
    {
        float width = levelData != null ? levelData.containerWidth : 5.5f;
        float dangerY = levelData != null ? levelData.dangerLineY : 2.5f;
        float triggerHeight = 2.0f;

        transform.position = new Vector3(0f, dangerY, 0f);

        // Setup trigger collider extending upwards from danger Y
        if (triggerCollider == null) triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.size = new Vector2(width, triggerHeight);
        triggerCollider.offset = new Vector2(0f, triggerHeight * 0.5f);

        // Setup line visual
        EnsureVisualRenderer();
        lineRenderer.transform.localPosition = Vector3.zero;
        lineRenderer.transform.localScale = new Vector3(width, 0.06f, 1f);
        lineRenderer.color = normalColor;
    }

    private void EnsureVisualRenderer()
    {
        Transform visualChild = transform.Find("DangerLineVisual");
        GameObject visualObj = visualChild != null ? visualChild.gameObject : null;

        if (visualObj == null)
        {
            visualObj = new GameObject("DangerLineVisual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;
        }

        lineRenderer = visualObj.GetComponent<SpriteRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = visualObj.AddComponent<SpriteRenderer>();
            lineRenderer.sprite = GetOrCreateLineSprite();
        }
    }

    private static Sprite lineSprite;
    private static Sprite GetOrCreateLineSprite()
    {
        if (lineSprite != null) return lineSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        lineSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return lineSprite;
    }

    public void SetSessionActive(bool active)
    {
        isSessionActive = active;
        if (!active)
        {
            ResetTimer();
        }
    }

    public void ResetTimer()
    {
        ballsInZone.Clear();
        isDangerActive = false;
        currentTimer = levelData != null ? levelData.dangerTimeLimit : 3.0f;
        if (lineRenderer != null)
        {
            lineRenderer.color = normalColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball != null)
        {
            ballsInZone.Add(ball);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball != null)
        {
            ballsInZone.Remove(ball);
        }
    }

    private void Update()
    {
        if (!isSessionActive) return;

        // Clean up destroyed or merging balls
        ballsInZone.RemoveWhere(b => b == null || !b.gameObject.activeInHierarchy || b.IsMerging);

        // Check if any settled dropped ball is inside danger zone
        bool hasSettledBallInDanger = FalseHasSettledBallInDanger();

        if (hasSettledBallInDanger)
        {
            isDangerActive = true;
            currentTimer -= Time.deltaTime;

            // Animate warning flash on line renderer
            float pulse = Mathf.PingPong(Time.time * 6f, 1f);
            if (lineRenderer != null)
            {
                lineRenderer.color = Color.Lerp(normalColor, warningColor, pulse);
            }

            // Check for Game Over condition
            if (currentTimer <= 0f)
            {
                TriggerOverflowGameOver();
            }
        }
        else
        {
            // Recover timer gradually back to limit
            float maxLimit = levelData != null ? levelData.dangerTimeLimit : 3.0f;
            if (currentTimer < maxLimit)
            {
                currentTimer = Mathf.MoveTowards(currentTimer, maxLimit, Time.deltaTime * 2f);
                if (lineRenderer != null)
                {
                    float ratio = 1f - (currentTimer / maxLimit);
                    lineRenderer.color = Color.Lerp(normalColor, warningColor, ratio);
                }
            }
            else
            {
                isDangerActive = false;
                if (lineRenderer != null)
                {
                    lineRenderer.color = normalColor;
                }
            }
        }
    }

    private bool FalseHasSettledBallInDanger()
    {
        foreach (Ball ball in ballsInZone)
        {
            if (ball != null && ball.IsDropped && !ball.IsMerging)
            {
                // Only count balls that are not falling down fast (settled/resting above line)
                if (ball.Rigidbody != null && ball.Rigidbody.linearVelocity.y >= -1.0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void TriggerOverflowGameOver()
    {
        Debug.Log("[DangerLine] Overflow limit reached! Triggering Game Over.");
        ResetTimer();

        GameplayController controller = FindAnyObjectByType<GameplayController>();
        if (controller != null)
        {
            controller.TriggerGameOver();
        }
    }
}
