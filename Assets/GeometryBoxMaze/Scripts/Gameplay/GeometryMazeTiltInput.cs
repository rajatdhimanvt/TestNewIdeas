using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Reads mobile accelerometer / gravity tilt with comfort calibration, smoothing,
    /// sensitivity scaling, and full Unity Editor keyboard (WASD / Arrow) + drag simulation.
    /// </summary>
    public class GeometryMazeTiltInput : MonoBehaviour
    {
        private const string PREF_SENSITIVITY = "GeometryMaze_TiltSensitivity";
        private const float DEFAULT_SENSITIVITY = 1.15f;

        [Header("Tilt Tuning")]
        [SerializeField] private float maxTiltDegrees = 28f;
        [SerializeField] private float deadZone = 0.03f;
        [SerializeField] private float smoothingSpeed = 14f;
        [SerializeField] private float keyboardRampSpeed = 5.5f;

        private Vector3 neutralAccel = new Vector3(0f, -0.45f, -0.89f); // ~27 deg comfortable handheld angle
        private Vector2 filteredTilt;
        private Vector2 keyboardTilt;
        private Vector2 dragTilt;
        private Vector2 dragStartScreenPos;
        private bool isDraggingTilt;
        private float sensitivity = DEFAULT_SENSITIVITY;
        private bool sensorInitialized;

        public Vector2 CurrentTilt => filteredTilt;
        public float Sensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = Mathf.Clamp(value, 0.5f, 2.5f);
                PlayerPrefs.SetFloat(PREF_SENSITIVITY, sensitivity);
            }
        }

        public bool IsUsingMobileSensor { get; private set; }

        private void Awake()
        {
            sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(PREF_SENSITIVITY, DEFAULT_SENSITIVITY), 0.5f, 2.5f);
        }

        private void OnEnable()
        {
            TryEnableSensors();
        }

        private void TryEnableSensors()
        {
            if (sensorInitialized) return;
            sensorInitialized = true;

            if (Accelerometer.current != null && !Accelerometer.current.enabled)
            {
                InputSystem.EnableDevice(Accelerometer.current);
            }
            if (GravitySensor.current != null && !GravitySensor.current.enabled)
            {
                InputSystem.EnableDevice(GravitySensor.current);
            }
        }

        /// <summary>
        /// Captures the player's current comfortable phone holding angle as zero-tilt neutral.
        /// </summary>
        public void CalibrateNeutral()
        {
            if (TryReadRawAcceleration(out Vector3 raw) && raw.sqrMagnitude > 0.05f)
            {
                neutralAccel = raw.normalized;
            }
            else
            {
                neutralAccel = new Vector3(0f, -0.45f, -0.89f);
            }
            filteredTilt = Vector2.zero;
            keyboardTilt = Vector2.zero;
            dragTilt = Vector2.zero;
        }

        public void ResetInputState()
        {
            filteredTilt = Vector2.zero;
            keyboardTilt = Vector2.zero;
            dragTilt = Vector2.zero;
            isDraggingTilt = false;
        }

        private void Update()
        {
            Vector2 rawTilt = Vector2.zero;
            IsUsingMobileSensor = false;

            if (TryReadRawAcceleration(out Vector3 accel) && accel.sqrMagnitude > 0.05f)
            {
                IsUsingMobileSensor = true;
                rawTilt = ComputeRelativeTilt(accel.normalized);
            }

            // Combine keyboard & pointer-drag simulation so testing inside Unity Editor is immediate
            Vector2 simKeyboard = ReadKeyboardSimulation();
            Vector2 simDrag = ReadPointerDragSimulation();
            if (simKeyboard.sqrMagnitude > 0.001f || simDrag.sqrMagnitude > 0.001f)
            {
                Vector2 combinedSim = simKeyboard + simDrag;
                if (combinedSim.sqrMagnitude > 1f) combinedSim.Normalize();
                rawTilt = IsUsingMobileSensor
                    ? Vector2.ClampMagnitude(rawTilt + combinedSim, 1f)
                    : combinedSim;
            }

            if (rawTilt.magnitude < deadZone)
            {
                rawTilt = Vector2.zero;
            }

            Vector2 targetTilt = Vector2.ClampMagnitude(rawTilt * sensitivity, 1.35f);
            filteredTilt = Vector2.Lerp(filteredTilt, targetTilt, 1f - Mathf.Exp(-smoothingSpeed * Time.unscaledDeltaTime));
        }

        private bool TryReadRawAcceleration(out Vector3 accel)
        {
            if (GravitySensor.current != null && GravitySensor.current.enabled)
            {
                Vector3 g = GravitySensor.current.gravity.ReadValue();
                if (g.sqrMagnitude > 0.05f)
                {
                    accel = g / 9.81f;
                    return true;
                }
            }

            if (Accelerometer.current != null && Accelerometer.current.enabled)
            {
                Vector3 a = Accelerometer.current.acceleration.ReadValue();
                if (a.sqrMagnitude > 0.05f)
                {
                    accel = a;
                    return true;
                }
            }

            if (SystemInfo.supportsAccelerometer)
            {
                Vector3 legacy = Input.acceleration;
                if (legacy.sqrMagnitude > 0.05f)
                {
                    accel = legacy;
                    return true;
                }
            }

            accel = Vector3.zero;
            return false;
        }

        private Vector2 ComputeRelativeTilt(Vector3 accelNorm)
        {
            // Horizontal tilt (phone tilted left / right)
            float angleXDeg = Mathf.Asin(Mathf.Clamp(accelNorm.x, -1f, 1f)) * Mathf.Rad2Deg
                              - Mathf.Asin(Mathf.Clamp(neutralAccel.x, -1f, 1f)) * Mathf.Rad2Deg;

            // Pitch tilt relative to calibrated neutral holding angle
            Vector2 refYZ = new Vector2(neutralAccel.y, neutralAccel.z);
            Vector2 curYZ = new Vector2(accelNorm.y, accelNorm.z);
            float angleYDeg = 0f;
            if (refYZ.sqrMagnitude > 0.02f && curYZ.sqrMagnitude > 0.02f)
            {
                angleYDeg = Vector2.SignedAngle(refYZ.normalized, curYZ.normalized);
            }

            float nx = Mathf.Clamp(angleXDeg / Mathf.Max(5f, maxTiltDegrees), -1.2f, 1.2f);
            float ny = Mathf.Clamp(angleYDeg / Mathf.Max(5f, maxTiltDegrees), -1.2f, 1.2f);
            return new Vector2(nx, ny);
        }

        private Vector2 ReadKeyboardSimulation()
        {
            Vector2 dir = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) dir.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) dir.x += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) dir.y -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) dir.y += 1f;
            }
            else
            {
                dir.x = Input.GetAxisRaw("Horizontal");
                dir.y = Input.GetAxisRaw("Vertical");
            }

            if (dir.sqrMagnitude > 1f) dir.Normalize();
            keyboardTilt = Vector2.MoveTowards(keyboardTilt, dir, keyboardRampSpeed * Time.unscaledDeltaTime);
            return keyboardTilt;
        }

        private Vector2 ReadPointerDragSimulation()
        {
            if (Mouse.current == null || Application.isMobilePlatform && IsUsingMobileSensor)
            {
                dragTilt = Vector2.zero;
                return dragTilt;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    isDraggingTilt = false;
                }
                else
                {
                    isDraggingTilt = true;
                    dragStartScreenPos = Mouse.current.position.ReadValue();
                }
            }
            else if (Mouse.current.leftButton.isPressed && isDraggingTilt)
            {
                Vector2 delta = Mouse.current.position.ReadValue() - dragStartScreenPos;
                dragTilt = Vector2.ClampMagnitude(delta / 140f, 1f);
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame || !Mouse.current.leftButton.isPressed)
            {
                isDraggingTilt = false;
                dragTilt = Vector2.MoveTowards(dragTilt, Vector2.zero, keyboardRampSpeed * 2f * Time.unscaledDeltaTime);
            }

            return dragTilt;
        }
    }
}
