using UnityEngine;

/// <summary>
/// Represents a single physics-driven ball in the merge game.
/// Handles visual setup, physical properties, drop state, and collision merge triggers.
/// </summary>
[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private BallDataSO ballData;
    [SerializeField] private bool isDropped = false;
    [SerializeField] private bool isMerging = false;

    private CircleCollider2D circleCollider;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private static Sprite defaultCircleSprite;

    public BallDataSO Data => ballData;
    public bool IsDropped => isDropped;
    public bool IsMerging => isMerging;
    public Rigidbody2D Rigidbody => rb;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // High precision collision detection prevents balls slipping through walls
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>
    /// Initializes this ball instance with configuration from BallDataSO.
    /// </summary>
    public void Initialize(BallDataSO data)
    {
        ballData = data;
        isMerging = false;
        isDropped = false;

        if (data == null) return;

        // Visual setup
        if (data.ballSprite != null)
        {
            spriteRenderer.sprite = data.ballSprite;
        }
        else
        {
            spriteRenderer.sprite = GetOrCreateCircleSprite();
        }

        spriteRenderer.color = data.ballColor;

        // Scale transform based on radius
        float diameter = data.radius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);

        // Physics material setup
        PhysicsMaterial2D mat = new PhysicsMaterial2D($"{data.ballName}_Mat")
        {
            bounciness = data.bounciness,
            friction = data.friction
        };
        circleCollider.sharedMaterial = mat;

        // Rigidbody setup
        rb.mass = data.mass;
        HoldInDropper(transform.position);
    }

    /// <summary>
    /// Holds the ball in place while player is aiming (kinematic, no gravity).
    /// </summary>
    public void HoldInDropper(Vector2 position)
    {
        isDropped = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = position;
    }

    /// <summary>
    /// Releases the ball into active physics simulation.
    /// </summary>
    public void Drop()
    {
        isDropped = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
    }

    public void MarkAsMerging()
    {
        isMerging = true;
        rb.simulated = false; // Disable physics interaction immediately to prevent double collision
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDropped || isMerging) return;

        Ball otherBall = collision.gameObject.GetComponent<Ball>();
        if (otherBall == null || !otherBall.IsDropped || otherBall.IsMerging) return;

        // Check if both balls are of the same tier
        if (this.ballData != null && otherBall.Data != null && this.ballData.tier == otherBall.Data.tier)
        {
            // Atomic lock using unique InstanceID to prevent both balls triggering merge simultaneously
            if (this.GetInstanceID() < otherBall.GetInstanceID())
            {
                this.MarkAsMerging();
                otherBall.MarkAsMerging();

                if (BallMergeManager.HasInstance)
                {
                    BallMergeManager.Instance.MergeBalls(this, otherBall);
                }
            }
        }
    }

    /// <summary>
    /// Generates a simple circular sprite if no custom art is assigned yet.
    /// </summary>
    private static Sprite GetOrCreateCircleSprite()
    {
        if (defaultCircleSprite != null) return defaultCircleSprite;

        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius - 1.5f)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        defaultCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return defaultCircleSprite;
    }
}
