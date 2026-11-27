using System.Collections;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Nostalgic two-tone rubber Eraser block placed inside the Geometry Box.
    /// Bounces the metallic marble playfully with an elastic squash-stretch reaction & boing SFX.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeBumper : MonoBehaviour
    {
        private float bounceImpulse = 8.5f;
        private Vector3 restScale = Vector3.one;
        private Coroutine bounceAnim;

        public void Initialize(Vector2 size, float rotationDeg, float bounceForce, Sprite eraserSprite)
        {
            bounceImpulse = Mathf.Max(3f, bounceForce);
            transform.localRotation = Quaternion.Euler(0f, 0f, rotationDeg);

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (eraserSprite != null)
            {
                sr.sprite = eraserSprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
                sr.color = Color.white;
            }
            sr.sortingOrder = 6;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = false;
            restScale = transform.localScale;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            GeometryMazeBallController ball = collision.collider.GetComponent<GeometryMazeBallController>();
            if (ball == null || ball.IsFrozenOrTransitioning) return;

            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null && collision.contactCount > 0)
            {
                Vector2 pushDir = (ball.transform.position - transform.position).normalized;
                if (collision.GetContact(0).normal.sqrMagnitude > 0.01f)
                {
                    pushDir = -collision.GetContact(0).normal;
                }
                ballRb.linearVelocity = pushDir * bounceImpulse;
            }

            GeometryMazeAudio.PlayEraserBounce();
            if (bounceAnim != null) StopCoroutine(bounceAnim);
            bounceAnim = StartCoroutine(SquashRoutine());
        }

        private IEnumerator SquashRoutine()
        {
            float elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / 0.22f);
                float bump = 1f + Mathf.Sin(u * Mathf.PI) * 0.16f;
                transform.localScale = new Vector3(restScale.x * bump, restScale.y / Mathf.Sqrt(bump), 1f);
                yield return null;
            }
            transform.localScale = restScale;
            bounceAnim = null;
        }
    }
}
