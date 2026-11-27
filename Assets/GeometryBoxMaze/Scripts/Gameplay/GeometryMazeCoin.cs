using System;
using System.Collections;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Collectible brass pin / gold token inside the Geometry Box Maze.
    /// Gently breathes & glints until the metallic ball rolls over it.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeCoin : MonoBehaviour
    {
        private Sprite ringSprite;
        private Action onCollected;
        private bool collected;
        private Vector3 baseScale;
        private float phaseOffset;

        public void Initialize(Sprite flashRingSprite, Action collectedCallback)
        {
            ringSprite = flashRingSprite;
            onCollected = collectedCallback;
            collected = false;
            baseScale = new Vector3(0.46f, 0.46f, 1f);
            transform.localScale = baseScale;
            phaseOffset = transform.localPosition.x * 1.3f + transform.localPosition.y * 0.7f;

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.65f;
        }

        private void Update()
        {
            if (collected) return;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.4f + phaseOffset) * 0.08f;
            transform.localScale = baseScale * pulse;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            GeometryMazeBallController ball = other.GetComponent<GeometryMazeBallController>();
            if (ball == null || ball.IsFrozenOrTransitioning) return;

            collected = true;
            GeometryMazeAudio.PlayPinPickup();
            onCollected?.Invoke();
            StartCoroutine(CollectRoutine());
        }

        private IEnumerator CollectRoutine()
        {
            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;

            if (ringSprite != null)
            {
                GameObject flash = new GameObject("PinFlash");
                flash.transform.SetParent(transform.parent, false);
                flash.transform.position = transform.position;
                SpriteRenderer fsr = flash.AddComponent<SpriteRenderer>();
                fsr.sprite = ringSprite;
                fsr.color = new Color(1f, 0.90f, 0.30f, 0.75f);
                fsr.sortingOrder = 9;
                StartCoroutine(ExpandFadeFlash(flash, fsr));
            }

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / 0.18f);
                transform.localScale = Vector3.Lerp(start, start * 1.55f, u);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f - u;
                    sr.color = c;
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private static IEnumerator ExpandFadeFlash(GameObject obj, SpriteRenderer sr)
        {
            float elapsed = 0f;
            while (elapsed < 0.28f && obj != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / 0.28f);
                float s = Mathf.Lerp(0.35f, 1.35f, u);
                obj.transform.localScale = new Vector3(s, s, 1f);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(0.75f, 0f, u);
                    sr.color = c;
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }
    }
}
