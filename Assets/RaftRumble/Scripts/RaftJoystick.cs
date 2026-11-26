using UnityEngine;
using UnityEngine.EventSystems;

namespace RaftRumble
{
    public class RaftJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static RaftJoystick Instance { get; private set; }

        [Header("Joystick Setup")]
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 60f;

        private RectTransform background;
        private Canvas canvas;
        private Vector2 inputVector = Vector2.zero;

        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;
        public Vector2 Direction => inputVector;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            background = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position = RectTransformUtility.WorldToScreenPoint(canvas != null ? canvas.worldCamera : null, background.position);
            Vector2 radius = background.sizeDelta / 2f;
            inputVector = (eventData.position - position) / (radius * (canvas != null ? canvas.scaleFactor : 1f));

            if (inputVector.magnitude > 1f)
            {
                inputVector = inputVector.normalized;
            }

            if (handle != null)
            {
                handle.anchoredPosition = inputVector * handleRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
