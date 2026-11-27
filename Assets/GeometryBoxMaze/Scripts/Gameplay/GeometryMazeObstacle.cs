using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Drives dynamic Geometry Box obstacles (oscillating plastic gates & rotating spinner bars)
    /// using kinematic Rigidbody2D movement so ball collisions stay rock-solid.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeObstacle : MonoBehaviour
    {
        private Rigidbody2D rb;
        private MazeObstacleDefinition definition;
        private bool isConfigured;
        private float elapsed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Initialize(MazeObstacleDefinition def, Sprite obstacleSprite)
        {
            definition = def;
            isConfigured = true;
            elapsed = 0f;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (obstacleSprite != null)
            {
                sr.sprite = obstacleSprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = def.size;
                sr.color = Color.white;
            }
            sr.sortingOrder = 6;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.size = def.size;
            col.offset = Vector2.zero;

            transform.localPosition = new Vector3(def.startPosition.x, def.startPosition.y, 0f);
            transform.localRotation = Quaternion.identity;
        }

        private void FixedUpdate()
        {
            if (!isConfigured || rb == null) return;

            elapsed += Time.fixedDeltaTime;
            if (definition.isSpinner)
            {
                float angle = elapsed * definition.speed;
                rb.MoveRotation(angle);
            }
            else
            {
                float ping = 0.5f - 0.5f * Mathf.Cos(elapsed * definition.speed);
                Vector2 localTarget = Vector2.Lerp(definition.startPosition, definition.endPosition, ping);
                Vector3 worldTarget = transform.parent != null
                    ? transform.parent.TransformPoint(localTarget)
                    : (Vector3)localTarget;
                rb.MovePosition(worldTarget);
            }
        }
    }
}
