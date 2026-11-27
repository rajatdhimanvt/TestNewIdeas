using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Reads Android/iOS accelerometer tilt (via both legacy Input.acceleration and InputSystem
    /// 60Hz Accelerometer/GravitySensor) with auto-calibration on first sample, comfort calibration,
    /// focus-resume sensor re-enabling, plus Unity Editor Keyboard & Touchscreen drag fallback.
    /// </summary>
    public class GeometryMazeTiltInput : MonoBehaviour
    {
        private const string PREF_SENSITIVITY = "GeometryMaze_TiltSensitivity";
        private const float DEFAULT_SENSITIVITY = 1.25f;

        [Header("Tilt Tuning")]
        [SerializeField] private float maxTiltDegrees = 24f;
        [SerializeField] private float deadZone = 0.025f;
        [SerializeField] private float smoothingSpeed = 15f;
        [SerializeField] private float keyboardRampSpeed = 5.5f;

        private Vector3 neutralAccel = new Vector3(0f, -0.45f, -0.89f); // ~27 deg comfortable handheld angle
        private bool hasInitialNeutralSample;
        private Vector2 filteredTilt;
        private Vector2 keyboardTilt;
        private Vector2 dragTilt;
        private Vector2 dragStartScreenPos;
        private bool isDraggingTilt;
        private float sensitivity = DEFAULT_SENSITIVITY;

        public Vector2 CurrentTilt => filteredTilt;
        public Vector3 LastRawAcceleration { get; private set; }
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
            EnsureMobileSensorsEnabled(forceToggleGyro: true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // Android sensors often freeze on resume unless re-enabled explicitly
                EnsureMobileSensorsEnabled(forceToggleGyro: true);
            }
        }

        private void EnsureMobileSensorsEnabled(bool forceToggleGyro)
        {
            if (Accelerometer.current != null)
            {
                if (!Accelerometer.current.enabled)
                {
                    InputSystem.EnableDevice(Accelerometer.current);
                }
                Accelerometer.current.samplingFrequency = 60f;
            }

            if (GravitySensor.current != null)
            {
                if (!GravitySensor.current.enabled)
                {
                    InputSystem.EnableDevice(GravitySensor.current);
                }
                GravitySensor.current.samplingFrequency = 60f;
            }

            if (Application.isMobilePlatform && SystemInfo.supportsGyroscope)
            {
                if (forceToggleGyro)
                {
                    Input.gyro.enabled = false;
                }
                if (!Input.gyro.enabled)
                {
                    Input.gyro.enabled = true;
                }
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
                hasInitialNeutralSample = true;
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
            // Continuously ensure late-registered Android InputSystem sensors get enabled at 60Hz
            if ((Accelerometer.current != null && !Accelerometer.current.enabled) ||
                (GravitySensor.current != null && !GravitySensor.current.enabled))
            {
                EnsureMobileSensorsEnabled(forceToggleGyro: false);
            }

            Vector2 rawTilt = Vector2.zero;
            IsUsingMobileSensor = false;

            if (TryReadRawAcceleration(out Vector3 accel) && accel.sqrMagnitude > 0.05f)
            {
                LastRawAcceleration = accel;
                IsUsingMobileSensor = true;

                // Auto-calibrate once to the player's holding angle on the very first stable 1G sample
                if (!hasInitialNeutralSample && accel.sqrMagnitude > 0.35f)
                {
                    neutralAccel = accel.normalized;
                    hasInitialNeutralSample = true;
                }

                rawTilt = ComputeRelativeTilt(accel.normalized);
            }

            // Combine keyboard & touch/mouse drag fallback seamlessly
            Vector2 simKeyboard = ReadKeyboardSimulation();
            Vector2 simDrag = ReadPointerOrTouchDrag();
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
            // 1. Legacy Input.acceleration (consistent Unity -Z convention + screen-orientation compensated)
            if (SystemInfo.supportsAccelerometer || Application.isMobilePlatform)
            {
                Vector3 legacy = Input.acceleration;
                if (legacy.sqrMagnitude > 0.05f)
                {
                    accel = legacy;
                    return true;
                }
            }

            // 2. New Input System Accelerometer (60Hz)
            if (Accelerometer.current != null && Accelerometer.current.enabled)
            {
                Vector3 a = Accelerometer.current.acceleration.ReadValue();
                if (a.sqrMagnitude > 0.05f)
                {
                    accel = a;
                    return true;
                }
            }

            // 3. New Input System GravitySensor fallback (scaled m/s^2 -> G)
            if (GravitySensor.current != null && GravitySensor.current.enabled)
            {
                Vector3 g = GravitySensor.current.gravity.ReadValue();
                if (g.sqrMagnitude > 0.05f)
                {
                    accel = g / 9.81f;
                    return true;
                }
            }

            accel = Vector3.zero;
            return false;
        }

        private Vector2 ComputeRelativeTilt(Vector3 accelNorm)
        {
            // Roll (tilt left/right)
            float angleXDeg = Mathf.Asin(Mathf.Clamp(accelNorm.x, -1f, 1f)) * Mathf.Rad2Deg
                              - Mathf.Asin(Mathf.Clamp(neutralAccel.x, -1f, 1f)) * Mathf.Rad2Deg;

            // Pitch (tilt top away/toward user relative to calibrated holding pose)
            Vector2 refYZ = new Vector2(neutralAccel.y, neutralAccel.z);
            Vector2 curYZ = new Vector2(accelNorm.y, accelNorm.z);
            float angleYDeg = 0f;
            if (refYZ.sqrMagnitude > 0.02f && curYZ.sqrMagnitude > 0.02f)
            {
                angleYDeg = Vector2.SignedAngle(refYZ.normalized, curYZ.normalized);
                // If hardware reports +Z when face up instead of -Z, invert pitch sign so top-down stays +Y
                if (neutralAccel.z > 0.1f)
                {
                    angleYDeg = -angleYDeg;
                }
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

        private Vector2 ReadPointerOrTouchDrag()
        {
            // Touchscreen drag fallback works even on Android emulators / sensor-less phones
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var touch = Touchscreen.current.primaryTouch;
                Vector2 touchPos = touch.position.ReadValue();
                if (touch.press.wasPressedThisFrame)
                {
                    int touchId = touch.touchId.ReadValue();
                    bool overUi = EventSystem.current != null &&
                                  (EventSystem.current.IsPointerOverGameObject(touchId) || EventSystem.current.IsPointerOverGameObject());
                    if (!overUi)
                    {
                        isDraggingTilt = true;
                        dragStartScreenPos = touchPos;
                    }
                    else
                    {
                        isDraggingTilt = false;
                    }
                }
                else if (isDraggingTilt)
                {
                    dragTilt = Vector2.ClampMagnitude((touchPos - dragStartScreenPos) / 140f, 1f);
                }
                return dragTilt;
            }

            if (Mouse.current == null)
            {
                isDraggingTilt = false;
                dragTilt = Vector2.MoveTowards(dragTilt, Vector2.zero, keyboardRampSpeed * 2f * Time.unscaledDeltaTime);
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
