using UnityEngine;
using UnityEngine.EventSystems;

namespace RaftRumble
{
    public class RaftCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float heightOffset = 1.0f;

        [Header("Distance & Angles")]
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float defaultPitch = 25f;
        [SerializeField] private float minPitch = 10f;
        [SerializeField] private float maxPitch = 70f;

        [Header("Sensitivity")]
        [SerializeField] private float touchSensitivityX = 0.2f;
        [SerializeField] private float touchSensitivityY = 0.2f;
        [SerializeField] private float mouseSensitivityX = 3f;
        [SerializeField] private float mouseSensitivityY = 3f;

        [Header("Smoothing")]
        [SerializeField] private float followSmoothSpeed = 10f;
        [SerializeField] private float rotationSmoothSpeed = 12f;

        private float currentYaw = 0f;
        private float currentPitch = 25f;
        private Vector3 currentVelocity;

        public Transform Target { get => target; set => target = value; }

        private void Start()
        {
            currentPitch = defaultPitch;
            if (target != null)
            {
                currentYaw = target.eulerAngles.y;
            }
        }

        private void Update()
        {
            HandleSwipeInput();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateCameraTransform();
        }

        private void HandleSwipeInput()
        {
            // Mobile touch input
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    // Ignore touch if touching UI elements (joystick, fart button, etc.)
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        continue;
                    }

                    if (touch.phase == TouchPhase.Moved)
                    {
                        currentYaw += touch.deltaPosition.x * touchSensitivityX;
                        currentPitch -= touch.deltaPosition.y * touchSensitivityY;
                        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
                        break; // Process one camera touch at a time
                    }
                }
            }
            // Mouse drag input (Editor & Standalone testing)
            else if (Input.GetMouseButton(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    float mouseX = Input.GetAxis("Mouse X");
                    float mouseY = Input.GetAxis("Mouse Y");

                    currentYaw += mouseX * mouseSensitivityX;
                    currentPitch -= mouseY * mouseSensitivityY;
                    currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
                }
            }
        }

        private void UpdateCameraTransform()
        {
            Vector3 targetPivot = target.position + Vector3.up * heightOffset;

            // Compute desired camera rotation
            Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // Compute desired camera position behind target
            Vector3 targetPosition = targetPivot - (targetRotation * Vector3.forward * distance);

            // Smooth position and rotation
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                currentYaw = target.eulerAngles.y;
            }
        }
    }
}
