using System;
using System.Collections;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Controls the metallic steel ball rolling inside the Geometry Box Maze.
    /// Applies smooth tilt acceleration, realistic plastic-wall bounce SFX,
    /// dynamic shadow offset, hole-falling sink animation, and goal pocket settle.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeBallController : MonoBehaviour
    {
        [Header("Physics Tuning")]
        [SerializeField] private float accelerationStrength = 30f;
        [SerializeField] private float maxSpeed = 8.8f;
        [SerializeField] private float linearDrag = 1.6f;

        [Header("Visuals")]
        [SerializeField] private Transform shadowTransform;
        [SerializeField] private Transform glintTransform;
        [SerializeField] private Sprite rippleSprite;
        [SerializeField] private Vector2 baseShadowOffset = new Vector2(0.055f, -0.07f);

        private Rigidbody2D rb;
        private CircleCollider2D circleCol;
        private SpriteRenderer ballRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer glintRenderer;
        private GeometryMazeTiltInput tiltInput;
        private PhysicsMaterial2D steelPhysicsMat;

        private bool isControlledActive;
        private bool isTransitioning;
        private float lastTapSoundTime;
        private float trailSpawnTimer;
        private float ballRadius = 0.29f;
        private Vector3 initialLocalScale = Vector3.one;
        private Vector3 camRestPos;
        private bool hasCamRestPos;
        private Coroutine activeTransition;
        private Coroutine camKickRoutine;

        public bool IsFrozenOrTransitioning => !isControlledActive || isTransitioning;
        public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            circleCol = GetComponent<CircleCollider2D>();
            ballRenderer = GetComponent<SpriteRenderer>();
            initialLocalScale = transform.localScale;

            rb.gravityScale = 0f;
            rb.linearDamping = linearDrag;
            rb.angularDamping = 2.0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            steelPhysicsMat = new PhysicsMaterial2D("GeometryBoxSteelBallMat")
            {
                bounciness = 0.28f,
                friction = 0.01f
            };
            circleCol.sharedMaterial = steelPhysicsMat;

            if (shadowTransform == null)
            {
                Transform found = transform.Find("BallShadow");
                if (found != null) shadowTransform = found;
            }
            if (shadowTransform != null)
            {
                shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            }

            if (glintTransform == null)
            {
                Transform foundGlint = transform.Find("BallGlint");
                if (foundGlint != null) glintTransform = foundGlint;
            }
            if (glintTransform != null)
            {
                glintRenderer = glintTransform.GetComponent<SpriteRenderer>();
            }

            if (Camera.main != null)
            {
                camRestPos = Camera.main.transform.position;
                hasCamRestPos = true;
            }
        }

        private void OnDestroy()
        {
            if (steelPhysicsMat != null)
            {
                Destroy(steelPhysicsMat);
            }
        }

        public void Initialize(GeometryMazeTiltInput inputSource, float radius, Vector3 spawnPosition)
        {
            tiltInput = inputSource;
            ballRadius = radius;

            if (activeTransition != null)
            {
                StopCoroutine(activeTransition);
                activeTransition = null;
            }

            isTransitioning = false;
            isControlledActive = true;

            float diameterScale = (radius * 2f) / 1.24f; // Chrome_Steel_Ball.png is 128px @ 100 PPU (~1.24 visual sphere units)
            initialLocalScale = new Vector3(diameterScale, diameterScale, 1f);
            transform.localScale = initialLocalScale;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;

            if (circleCol != null)
            {
                circleCol.enabled = true;
                circleCol.radius = diameterScale > 0.01f ? (radius / diameterScale) : radius;
            }

            if (rb != null)
            {
                rb.simulated = true;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (ballRenderer != null)
            {
                Color c = ballRenderer.color;
                c.a = 1f;
                ballRenderer.color = c;
            }

            if (shadowTransform != null)
            {
                shadowTransform.localPosition = baseShadowOffset;
                shadowTransform.gameObject.SetActive(true);
            }
            if (shadowRenderer != null)
            {
                Color sc = shadowRenderer.color;
                sc.a = 1f;
                shadowRenderer.color = sc;
            }

            if (glintTransform != null)
            {
                glintTransform.localPosition = Vector3.zero;
                glintTransform.gameObject.SetActive(true);
            }
            if (glintRenderer != null)
            {
                Color gc = glintRenderer.color;
                gc.a = 1f;
                glintRenderer.color = gc;
            }
        }

        public void SetSimulationActive(bool active)
        {
            isControlledActive = active;
            if (!active && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!isControlledActive || isTransitioning || rb == null) return;

            Vector2 tilt = tiltInput != null ? tiltInput.CurrentTilt : Vector2.zero;
            if (tilt.sqrMagnitude > 0.0001f)
            {
                rb.AddForce(tilt * accelerationStrength, ForceMode2D.Force);
            }

            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        private void LateUpdate()
        {
            if (isTransitioning) return;

            Vector2 tilt = tiltInput != null ? tiltInput.CurrentTilt : Vector2.zero;
            if (shadowTransform != null)
            {
                shadowTransform.localPosition = baseShadowOffset - tilt * 0.06f;
            }

            if (glintTransform != null)
            {
                // Shift overhead specular highlight slightly with tilt so chrome depth pops
                glintTransform.localPosition = tilt * 0.055f;
            }

            if (isControlledActive && rb != null)
            {
                float speed = rb.linearVelocity.magnitude;
                if (speed > 2.6f)
                {
                    trailSpawnTimer += Time.deltaTime;
                    if (trailSpawnTimer >= 0.055f)
                    {
                        trailSpawnTimer = 0f;
                        SpawnRollSheenDot(speed);
                    }
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isTransitioning || !isControlledActive) return;

            float impact = collision.relativeVelocity.magnitude;
            if (impact > 1.35f && Time.unscaledTime - lastTapSoundTime > 0.065f)
            {
                lastTapSoundTime = Time.unscaledTime;
                float vol = Mathf.Clamp01((impact - 1.2f) / 6.5f);
                GeometryMazeAudio.PlayWallTap(vol);

                if (impact > 2.3f && collision.contactCount > 0)
                {
                    SpawnImpactRipple(collision.GetContact(0).point, vol);
                    TriggerMicroCameraKick(collision.GetContact(0).normal, vol * 0.045f);
                }
            }
        }

        private void SpawnImpactRipple(Vector2 worldPoint, float intensity)
        {
            Sprite spr = rippleSprite != null ? rippleSprite : (ballRenderer != null ? ballRenderer.sprite : null);
            if (spr == null) return;

            GameObject ring = new GameObject("WallHitRipple");
            ring.transform.position = new Vector3(worldPoint.x, worldPoint.y, 0f);
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.32f, 0.7f, intensity));
            sr.sortingOrder = 7;
            StartCoroutine(AnimateRippleRoutine(ring, sr, 0.24f, 0.14f, 0.55f + intensity * 0.25f));
        }

        private void SpawnRollSheenDot(float speed)
        {
            Sprite spr = rippleSprite != null ? rippleSprite : (ballRenderer != null ? ballRenderer.sprite : null);
            if (spr == null) return;

            GameObject dot = new GameObject("RollSheen");
            dot.transform.position = transform.position;
            SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp(speed / maxSpeed, 0.2f, 1f) * 0.22f);
            sr.sortingOrder = 2;
            StartCoroutine(AnimateRippleRoutine(dot, sr, 0.22f, 0.28f, 0.06f));
        }

        private static IEnumerator AnimateRippleRoutine(GameObject obj, SpriteRenderer sr, float duration, float startScale, float endScale)
        {
            float startAlpha = sr != null ? sr.color.a : 0.5f;
            float elapsed = 0f;
            while (elapsed < duration && obj != null)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Lerp(startScale, endScale, u);
                obj.transform.localScale = new Vector3(s, s, 1f);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(startAlpha, 0f, u);
                    sr.color = c;
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        private void TriggerMicroCameraKick(Vector2 normal, float amplitude)
        {
            if (!hasCamRestPos || Camera.main == null || amplitude < 0.005f) return;
            if (camKickRoutine != null) StopCoroutine(camKickRoutine);
            camKickRoutine = StartCoroutine(CameraKickRoutine(-normal.normalized * amplitude));
        }

        private IEnumerator CameraKickRoutine(Vector2 offset)
        {
            Transform camTf = Camera.main != null ? Camera.main.transform : null;
            if (camTf == null) yield break;

            float elapsed = 0f;
            const float dur = 0.10f;
            while (elapsed < dur && camTf != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                float kick = Mathf.Sin(u * Mathf.PI);
                camTf.position = camRestPos + new Vector3(offset.x * kick, offset.y * kick, 0f);
                yield return null;
            }
            if (camTf != null) camTf.position = camRestPos;
            camKickRoutine = null;
        }

        public void FallIntoHole(Vector3 holeCenter, Action onCompleted)
        {
            if (isTransitioning) return;
            isTransitioning = true;
            isControlledActive = false;

            GeometryMazeAudio.PlayHoleFall();
            activeTransition = StartCoroutine(HoleFallRoutine(holeCenter, onCompleted));
        }

        public void CaptureInGoal(Vector3 goalCenter, Action onCompleted)
        {
            if (isTransitioning) return;
            isTransitioning = true;
            isControlledActive = false;

            GeometryMazeAudio.PlayGoalWin();
            activeTransition = StartCoroutine(GoalCaptureRoutine(goalCenter, onCompleted));
        }

        private IEnumerator HoleFallRoutine(Vector3 holeCenter, Action onCompleted)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            if (circleCol != null) circleCol.enabled = false;
            if (shadowTransform != null) shadowTransform.gameObject.SetActive(false);

            Vector3 startPos = transform.position;
            float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t;

                transform.position = Vector3.Lerp(startPos, holeCenter, Mathf.SmoothStep(0f, 1f, t));
                transform.localScale = Vector3.Lerp(initialLocalScale, initialLocalScale * 0.14f, smooth);

                if (ballRenderer != null)
                {
                    Color c = ballRenderer.color;
                    c.a = Mathf.Lerp(1f, 0f, t * t);
                    ballRenderer.color = c;
                }
                if (glintRenderer != null)
                {
                    Color gc = glintRenderer.color;
                    gc.a = Mathf.Lerp(1f, 0f, t * t);
                    glintRenderer.color = gc;
                }
                yield return null;
            }

            activeTransition = null;
            onCompleted?.Invoke();
        }

        private IEnumerator GoalCaptureRoutine(Vector3 goalCenter, Action onCompleted)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            if (circleCol != null) circleCol.enabled = false;

            Vector3 startPos = transform.position;
            float settleDuration = 0.28f;
            float elapsed = 0f;

            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                transform.position = Vector3.Lerp(startPos, goalCenter, easeOut);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                transform.localScale = initialLocalScale * pulse;
                yield return null;
            }

            transform.position = goalCenter;
            transform.localScale = initialLocalScale;
            yield return new WaitForSecondsRealtime(0.18f);

            activeTransition = null;
            onCompleted?.Invoke();
        }
    }
}
