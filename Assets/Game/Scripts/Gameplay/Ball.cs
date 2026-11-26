using DG.Tweening;
using UnityEngine;

/// <summary>
/// Represents a physics-driven ball (single-body or bone-rigged softbody slime) in the merge game.
/// Handles visual setup, physical properties, drop state, and collision merge triggers.
/// </summary>
public class Ball : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private BallDataSO ballData;
    [SerializeField] private bool isDropped = false;
    [SerializeField] private bool isMerging = false;

    private static int nextBallId = 1;
    private int ballId;

    private CircleCollider2D circleCollider;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float portalCooldownUntil = -10f;

    // Softbody / bone-rigged slime support
    private bool isSoftBodyRig = false;
    private float baseVisualRadius = 0.5f;
    private Rigidbody2D[] allRigidbodies;
    private Collider2D[] allColliders;
    private SpringJoint2D[] allSpringJoints;
    private float[] unscaledSpringDistances;
    private float[] baseSpringFrequencies;
    private Transform simpleFace;
    private Transform matchFace;

    private static Sprite defaultCircleSprite;

    public int BallId => ballId;
    public BallDataSO Data => ballData;
    public bool IsDropped => isDropped;
    public bool IsMerging => isMerging;
    public bool IsSoftBodyRig => isSoftBodyRig;
    public Rigidbody2D Rigidbody => rb;

    public bool CanUsePortal() => isDropped && !isMerging && Time.time >= portalCooldownUntil;

    public void NotifyTeleported(float cooldownSeconds = 0.45f)
    {
        portalCooldownUntil = Time.time + cooldownSeconds;
    }

    private void Awake()
    {
        ballId = nextBallId++;
        circleCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        allRigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
        allColliders = GetComponentsInChildren<Collider2D>(true);

        // If root has no Rigidbody2D but children do, this is a bone-rigged softbody slime
        if (rb == null && allRigidbodies != null && allRigidbodies.Length > 0)
        {
            isSoftBodyRig = true;
            Transform centerTf = transform.Find("Center");
            rb = centerTf != null ? centerTf.GetComponent<Rigidbody2D>() : null;
            if (rb == null) rb = allRigidbodies[0];

            // Compute unscaled radius from sprite bounds (e.g. BlackSlime ~2.65 units)
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Vector3 ext = spriteRenderer.sprite.bounds.extents;
                baseVisualRadius = Mathf.Max(0.5f, Mathf.Min(ext.x, ext.y));
            }
            else
            {
                baseVisualRadius = 2.65f;
            }

            // Ensure Default layer (0) so bones collide with container walls and blocks
            if (gameObject.layer == 6)
            {
                SetLayerRecursively(gameObject, 0);
            }

            // Attach collision relay to every bone/center collider and keep on Default layer
            for (int i = 0; i < allColliders.Length; i++)
            {
                Collider2D col = allColliders[i];
                if (col == null || col.gameObject == gameObject) continue;
                if (col.gameObject.layer == 6)
                {
                    col.gameObject.layer = 0;
                }
                SlimeBoneRelay relay = col.GetComponent<SlimeBoneRelay>();
                if (relay == null) relay = col.gameObject.AddComponent<SlimeBoneRelay>();
                relay.Init(this);
            }

            simpleFace = FindChildRecursive(transform, "Simple");
            matchFace = FindChildRecursive(transform, "Match");

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 10;
            }
            Transform faceRoot = FindChildRecursive(transform, "BallFace");
            if (faceRoot != null)
            {
                SpriteRenderer[] faceRenderers = faceRoot.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < faceRenderers.Length; i++)
                {
                    if (faceRenderers[i] != null) faceRenderers[i].sortingOrder = 11;
                }
            }

            // Cache unscaled distance between each joint's actual anchors and authored spring frequencies before tier scaling
            allSpringJoints = GetComponentsInChildren<SpringJoint2D>(true);
            if (allSpringJoints != null && allSpringJoints.Length > 0)
            {
                unscaledSpringDistances = new float[allSpringJoints.Length];
                baseSpringFrequencies = new float[allSpringJoints.Length];
                for (int i = 0; i < allSpringJoints.Length; i++)
                {
                    SpringJoint2D sj = allSpringJoints[i];
                    if (sj == null) continue;
                    baseSpringFrequencies[i] = sj.frequency > 0f ? sj.frequency : 6.5f;
                    if (sj.connectedBody != null)
                    {
                        Vector3 worldAnchorA = sj.transform.TransformPoint(sj.anchor);
                        Vector3 worldAnchorB = sj.connectedBody.transform.TransformPoint(sj.connectedAnchor);
                        Vector3 pA = transform.InverseTransformPoint(worldAnchorA);
                        Vector3 pB = transform.InverseTransformPoint(worldAnchorB);
                        unscaledSpringDistances[i] = Vector2.Distance(pA, pB);
                    }
                }
            }
        }

        // High precision collision detection prevents balls slipping through walls
        if (allRigidbodies != null)
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] == null) continue;
                allRigidbodies[i].collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                allRigidbodies[i].interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
        }
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName) return child;
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    public float GetTargetScale(BallDataSO data)
    {
        if (data == null) return 1f;
        return isSoftBodyRig ? (data.radius / baseVisualRadius) : (data.radius * 2f);
    }

    public Vector3 GetWorldCenter()
    {
        if (isSoftBodyRig && rb != null)
        {
            return rb.transform.position;
        }
        return transform.position;
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

        // Visual setup (preserve rigged slime sprite; apply tier color tint)
        if (!isSoftBodyRig && spriteRenderer != null)
        {
            spriteRenderer.sprite = data.ballSprite != null ? data.ballSprite : GetOrCreateCircleSprite();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = data.ballColor;
        }

        if (simpleFace != null) simpleFace.gameObject.SetActive(true);
        if (matchFace != null) matchFace.gameObject.SetActive(false);

        // Scale transform uniformly based on radius
        float targetScale = GetTargetScale(data);
        transform.localScale = Vector3.one * targetScale;

        // Lock SpringJoint2D anchors/rest distances and tune frequency for radius so small and large balls squish cleanly
        if (isSoftBodyRig && allSpringJoints != null)
        {
            float freqMultiplier = Mathf.Clamp(Mathf.Sqrt(0.65f / Mathf.Max(0.2f, data.radius)), 0.85f, 1.45f);
            for (int i = 0; i < allSpringJoints.Length; i++)
            {
                SpringJoint2D sj = allSpringJoints[i];
                if (sj == null) continue;
                sj.autoConfigureConnectedAnchor = false;
                sj.autoConfigureDistance = false;
                if (unscaledSpringDistances != null && i < unscaledSpringDistances.Length)
                {
                    sj.distance = Mathf.Max(0.005f, unscaledSpringDistances[i] * targetScale);
                }
                if (baseSpringFrequencies != null && i < baseSpringFrequencies.Length && baseSpringFrequencies[i] > 0f)
                {
                    sj.frequency = baseSpringFrequencies[i] * freqMultiplier;
                }
            }
        }

        // Preserve bouncy softbody outer material (Fruit-Merge style) while using BallDataSO material for rigid balls
        if (isSoftBodyRig && allColliders != null)
        {
            PhysicsMaterial2D fallbackSoftMat = null;
            for (int i = 0; i < allColliders.Length; i++)
            {
                Collider2D col = allColliders[i];
                if (col == null) continue;
                if (col.attachedRigidbody == rb) continue; // Keep Center non-bouncy like Fruit-Merge
                if (col.sharedMaterial == null)
                {
                    if (fallbackSoftMat == null)
                    {
                        fallbackSoftMat = new PhysicsMaterial2D($"{data.ballName}_SoftMat")
                        {
                            bounciness = 1.0f,
                            friction = 0.5f
                        };
                    }
                    col.sharedMaterial = fallbackSoftMat;
                }
            }
        }
        else if (circleCollider != null)
        {
            circleCollider.sharedMaterial = new PhysicsMaterial2D($"{data.ballName}_Mat")
            {
                bounciness = data.bounciness,
                friction = data.friction
            };
        }

        // Uniform per-bone mass on softbody rigs keeps outer ring & Center balanced without crushing
        if (isSoftBodyRig && allRigidbodies != null && allRigidbodies.Length > 0)
        {
            float uniformBoneMass = Mathf.Lerp(0.95f, 1.5f, Mathf.Clamp01(data.tier / 5f));
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] != null)
                {
                    allRigidbodies[i].mass = uniformBoneMass;
                }
            }
        }
        else if (rb != null)
        {
            rb.mass = data.mass;
        }

        HoldInDropper(transform.position);
    }

    /// <summary>
    /// Holds the ball in place while player is aiming (kinematic, no gravity).
    /// </summary>
    public void HoldInDropper(Vector2 position)
    {
        isDropped = false;
        transform.position = position;

        if (allRigidbodies != null && allRigidbodies.Length > 0)
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                Rigidbody2D body = allRigidbodies[i];
                if (body == null) continue;
                body.bodyType = RigidbodyType2D.Kinematic;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }
        else if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        Physics2D.SyncTransforms();
    }

    /// <summary>
    /// Releases the ball into active physics simulation.
    /// </summary>
    public void Drop()
    {
        isDropped = true;

        if (allRigidbodies != null && allRigidbodies.Length > 0)
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                Rigidbody2D body = allRigidbodies[i];
                if (body == null) continue;
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = isSoftBodyRig ? 1f : 2f;
            }
        }
        else if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f;
        }
    }

    public void TriggerJellyPopImpulse(float strength = 1f)
    {
        if (!isSoftBodyRig || allRigidbodies == null) return;

        Vector3 centerPos = GetWorldCenter();
        for (int i = 0; i < allRigidbodies.Length; i++)
        {
            Rigidbody2D body = allRigidbodies[i];
            if (body == null || !body.simulated) continue;

            if (body != rb)
            {
                Vector2 radial = (Vector2)(body.transform.position - centerPos);
                if (radial.sqrMagnitude > 0.0001f)
                {
                    body.AddForce(radial.normalized * (0.75f * strength), ForceMode2D.Impulse);
                }
            }
            body.AddForce(Vector2.up * (0.35f * strength), ForceMode2D.Impulse);
        }
    }

    public void MarkAsMerging()
    {
        isMerging = true;

        if (simpleFace != null) simpleFace.gameObject.SetActive(false);
        if (matchFace != null) matchFace.gameObject.SetActive(true);

        if (allRigidbodies != null && allRigidbodies.Length > 0)
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] != null)
                {
                    allRigidbodies[i].simulated = false;
                }
            }
        }
        else if (rb != null)
        {
            rb.simulated = false; // Disable physics interaction immediately to prevent double collision
        }
    }

    private void RecenterRootToWorldCenter()
    {
        if (!isSoftBodyRig) return;

        Vector3 worldCenter = GetWorldCenter();
        if ((worldCenter - transform.position).sqrMagnitude <= 0.000001f) return;

        int childCount = transform.childCount;
        Vector3[] childPositions = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
        {
            childPositions[i] = transform.GetChild(i).position;
        }

        transform.position = worldCenter;

        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).position = childPositions[i];
        }
    }

    public Tween AnimateMergeInto(Vector3 midpoint, float duration)
    {
        // Re-align root pivot onto the physics center because child bones fall while root stays at drop height
        RecenterRootToWorldCenter();
        Vector3 offset = midpoint - GetWorldCenter();
        transform.DOMove(transform.position + offset, duration).SetLink(gameObject);
        return transform.DOScale(Vector3.zero, duration).SetLink(gameObject);
    }

    public void TeleportTo(Vector3 destinationWorldPos)
    {
        RecenterRootToWorldCenter();
        Vector3 delta = destinationWorldPos - GetWorldCenter();
        delta.z = 0f;
        transform.position += delta;
        Physics2D.SyncTransforms();

        if (allRigidbodies != null && allRigidbodies.Length > 0)
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                Rigidbody2D body = allRigidbodies[i];
                if (body == null) continue;
                Vector2 vel = body.linearVelocity;
                vel.y = Mathf.Min(vel.y, -2.2f);
                body.linearVelocity = vel;
            }
        }
        else if (rb != null)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = Mathf.Min(vel.y, -2.2f);
            rb.linearVelocity = vel;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision2D(collision);
    }

    public void HandleCollision2D(Collision2D collision)
    {
        if (!isDropped || isMerging) return;

        Ball otherBall = collision.gameObject.GetComponentInParent<Ball>();
        if (otherBall == null || otherBall == this || !otherBall.IsDropped || otherBall.IsMerging) return;

        // Check if both balls are of the same tier
        if (this.ballData != null && otherBall.Data != null && this.ballData.tier == otherBall.Data.tier)
        {
            // Atomic lock using unique ballId to prevent both balls triggering merge simultaneously
            if (this.ballId < otherBall.BallId)
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
