using System.Collections;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Interactive pressure button inside the Geometry Box. Rolling over it depresses the plate
    /// and slides open its linked cyan Locked Gate barrier.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class GeometryMazeSwitchGate : MonoBehaviour
    {
        private GameObject gateObj;
        private BoxCollider2D gateCollider;
        private SpriteRenderer gateRenderer;
        private SpriteRenderer switchRenderer;
        private bool triggered;

        public void Initialize(GameObject linkedGate, Sprite switchSprite)
        {
            gateObj = linkedGate;
            if (gateObj != null)
            {
                gateCollider = gateObj.GetComponent<BoxCollider2D>();
                gateRenderer = gateObj.GetComponent<SpriteRenderer>();
            }

            switchRenderer = GetComponent<SpriteRenderer>();
            if (switchRenderer != null && switchSprite != null)
            {
                switchRenderer.sprite = switchSprite;
                switchRenderer.sortingOrder = 3;
            }
            transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.56f;
            triggered = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            GeometryMazeBallController ball = other.GetComponent<GeometryMazeBallController>();
            if (ball == null || ball.IsFrozenOrTransitioning) return;

            triggered = true;
            GeometryMazeAudio.PlaySwitchOpen();
            StartCoroutine(OpenGateRoutine());
        }

        private IEnumerator OpenGateRoutine()
        {
            transform.localScale = new Vector3(0.64f, 0.64f, 1f);
            if (switchRenderer != null)
            {
                switchRenderer.color = new Color(0.32f, 1f, 0.62f, 0.75f);
            }

            if (gateCollider != null) gateCollider.enabled = false;
            if (gateObj == null) yield break;

            Vector3 startPos = gateObj.transform.localPosition;
            Vector3 slideDir = gateObj.transform.right * 1.4f;
            float elapsed = 0f;
            while (elapsed < 0.34f && gateObj != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.34f));
                gateObj.transform.localPosition = Vector3.Lerp(startPos, startPos + slideDir, u);
                if (gateRenderer != null)
                {
                    Color c = gateRenderer.color;
                    c.a = Mathf.Lerp(1f, 0f, u);
                    gateRenderer.color = c;
                }
                yield return null;
            }
            if (gateObj != null) gateObj.SetActive(false);
        }
    }
}
