using System;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Finish target dimple cup in the Geometry Box Maze.
    /// Pulses softly to draw attention and settles the steel ball when reached.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeGoal : MonoBehaviour
    {
        [SerializeField] private Transform pulseRing;
        private float goalRadius = 0.50f;
        private Action<GeometryMazeBallController, Vector3> onGoalReached;
        private bool completed;

        public void Initialize(float radius, Sprite ringSprite, Action<GeometryMazeBallController, Vector3> goalCallback)
        {
            goalRadius = Mathf.Max(0.32f, radius);
            onGoalReached = goalCallback;
            completed = false;

            float scale = (goalRadius * 2f) / 1.24f;
            transform.localScale = new Vector3(scale, scale, 1f);

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = scale > 0.01f ? ((goalRadius * 1.15f) / scale) : 0.65f;

            if (pulseRing == null)
            {
                Transform existing = transform.Find("PulseRing");
                if (existing != null) pulseRing = existing;
            }

            if (pulseRing == null && ringSprite != null)
            {
                GameObject ringObj = new GameObject("PulseRing");
                ringObj.transform.SetParent(transform, false);
                ringObj.transform.localPosition = Vector3.zero;
                SpriteRenderer sr = ringObj.AddComponent<SpriteRenderer>();
                sr.sprite = ringSprite;
                sr.color = new Color(0.25f, 1f, 0.55f, 0.45f);
                sr.sortingOrder = 5;
                pulseRing = ringObj.transform;
            }
        }

        private void Update()
        {
            if (pulseRing != null)
            {
                float s = 1.15f + Mathf.PingPong(Time.unscaledTime * 0.7f, 0.35f);
                pulseRing.localScale = new Vector3(s, s, 1f);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (completed) return;

            GeometryMazeBallController ball = other.GetComponent<GeometryMazeBallController>();
            if (ball == null || ball.IsFrozenOrTransitioning) return;

            float dist = Vector2.Distance(ball.transform.position, transform.position);
            if (dist <= goalRadius * 0.78f)
            {
                completed = true;
                onGoalReached?.Invoke(ball, transform.position);
            }
        }
    }
}
