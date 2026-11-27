using System;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Represents a sunken pit hole inside the Geometry Box tray.
    /// Applies a slight realistic edge-lip pull near the rim and triggers ball capture
    /// once the ball's center crosses into the hole core.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeHole : MonoBehaviour
    {
        private float holeWorldRadius = 0.42f;
        private Action<GeometryMazeBallController, Vector3> onBallCaptured;
        private bool triggered;

        public void Initialize(float radius, Action<GeometryMazeBallController, Vector3> captureCallback)
        {
            holeWorldRadius = Mathf.Max(0.22f, radius);
            onBallCaptured = captureCallback;
            triggered = false;

            // Hole_Pit.png is 128px @ 100 PPU (~1.24 unit circle)
            float scale = (holeWorldRadius * 2f) / 1.24f;
            transform.localScale = new Vector3(scale, scale, 1f);

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = scale > 0.01f ? ((holeWorldRadius * 1.1f) / scale) : 0.62f;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (triggered) return;

            GeometryMazeBallController ball = other.GetComponent<GeometryMazeBallController>();
            if (ball == null || ball.IsFrozenOrTransitioning) return;

            Vector2 toHole = (Vector2)(transform.position - ball.transform.position);
            float dist = toHole.magnitude;

            // Subtle rim dip: realistic pull only when marble genuinely reaches inner pit rim
            if (dist < holeWorldRadius * 0.80f && dist > 0.001f)
            {
                Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
                if (ballRb != null)
                {
                    ballRb.AddForce(toHole.normalized * 6.5f, ForceMode2D.Force);
                }
            }

            if (dist <= holeWorldRadius * 0.50f)
            {
                triggered = true;
                onBallCaptured?.Invoke(ball, transform.position);
            }
        }
    }
}
