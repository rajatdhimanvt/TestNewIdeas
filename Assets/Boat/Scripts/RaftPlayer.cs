using System;
using UnityEngine;

namespace RaftRumble
{
    public class RaftPlayer : MonoBehaviour
    {
        [Header("Player Identity")]
        [SerializeField] private int playerIndex = 1;
        [SerializeField] private Color playerColor = Color.green;
        [SerializeField] private bool isLocalControlled = true;

        [Header("Rim Positioning")]
        [Tooltip("Current angle in degrees along the raft circular rim (0 to 360)")]
        [SerializeField] private float currentAngleDegrees = 270f; // 270 degrees = back of the raft by default
        [SerializeField] private float rimMoveSpeed = 120f; // degrees per second
        [SerializeField] private float heightOffset = 0.4f;

        [Header("Fart / Propulsion Physics")]
        [SerializeField] private float thrustForce = 15f;
        [SerializeField] private float cooldownTime = 0.8f;
        [SerializeField] private float maxGas = 100f;
        [SerializeField] private float gasCost = 25f;
        [SerializeField] private float gasRechargeRate = 20f; // per second

        [Header("Feedback Effects")]
        [SerializeField] private ParticleSystem fartParticleEffect;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fartClip;

        private RaftController raft;
        private float currentGas;
        private float cooldownTimer;

        public int PlayerIndex => playerIndex;
        public Color PlayerColor => playerColor;
        public float CurrentAngle => currentAngleDegrees;
        public float GasNormalized => maxGas > 0 ? Mathf.Clamp01(currentGas / maxGas) : 1f;
        public float CooldownNormalized => cooldownTime > 0 ? Mathf.Clamp01(cooldownTimer / cooldownTime) : 0f;
        public bool CanFart => cooldownTimer <= 0f && currentGas >= gasCost;
        public bool IsLocalControlled { get => isLocalControlled; set => isLocalControlled = value; }

        public event Action<RaftPlayer> OnFartExecuted;

        private void Awake()
        {
            currentGas = maxGas;
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (raft == null)
            {
                raft = GetComponentInParent<RaftController>();
            }
            UpdateRimTransform();
        }

        public void Initialize(RaftController raftController, int index, float startAngle, Color color, bool localControl)
        {
            raft = raftController;
            playerIndex = index;
            currentAngleDegrees = startAngle;
            playerColor = color;
            isLocalControlled = localControl;

            // Apply color to renderer if available
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            UpdateRimTransform();
        }

        private void Update()
        {
            // Recharge gas and tick cooldown
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (currentGas < maxGas)
            {
                currentGas = Mathf.Min(maxGas, currentGas + gasRechargeRate * Time.deltaTime);
            }

            if (isLocalControlled)
            {
                HandleInput();
            }

            UpdateRimTransform();
        }

        private void HandleInput()
        {
            // Read input from Joystick or keyboard
            float moveInput = 0f;

            if (RaftJoystick.Instance != null && Mathf.Abs(RaftJoystick.Instance.Horizontal) > 0.05f)
            {
                moveInput = RaftJoystick.Instance.Horizontal;
            }
            else
            {
                moveInput = Input.GetAxisRaw("Horizontal");
            }

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                // Positive input moves clockwise, negative moves counter-clockwise
                currentAngleDegrees -= moveInput * rimMoveSpeed * Time.deltaTime;
                // Keep within 0 - 360
                currentAngleDegrees = (currentAngleDegrees % 360f + 360f) % 360f;
            }

            // Keyboard shortcut for testing in editor
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerFart();
            }
        }

        /// <summary>
        /// Positions the player on the raft rim and faces them toward the raft center.
        /// </summary>
        private void UpdateRimTransform()
        {
            if (raft == null) return;

            float rad = currentAngleDegrees * Mathf.Deg2Rad;
            float radius = raft.RaftRadius;

            // Compute local position on the rim
            Vector3 localPos = new Vector3(Mathf.Cos(rad) * radius, heightOffset, Mathf.Sin(rad) * radius);
            transform.localPosition = localPos;

            // Face inward toward the center (0, heightOffset, 0) in local space
            Vector3 toCenter = new Vector3(-localPos.x, 0f, -localPos.z).normalized;
            if (toCenter.sqrMagnitude > 0.001f)
            {
                transform.localRotation = Quaternion.LookRotation(toCenter, Vector3.up);
            }
        }

        /// <summary>
        /// Executes the fart propulsion action.
        /// </summary>
        public bool TriggerFart()
        {
            if (!CanFart || raft == null) return false;

            currentGas -= gasCost;
            cooldownTimer = cooldownTime;

            // Reaction force pushes the raft inward along the player's facing forward direction
            Vector3 thrustDir = transform.forward;
            raft.ApplyThrust(transform.position, thrustDir, thrustForce);

            // Play VFX behind character
            if (fartParticleEffect != null)
            {
                fartParticleEffect.Play();
            }
            else
            {
                SpawnProceduralFartPuff();
            }

            // Play Audio
            if (audioSource != null && fartClip != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.25f);
                audioSource.PlayOneShot(fartClip);
            }

            OnFartExecuted?.Invoke(this);
            return true;
        }

        private void SpawnProceduralFartPuff()
        {
            // Procedural gas puff behind character
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "FartPuff";
            puff.transform.position = transform.position - transform.forward * 0.45f + Vector3.up * 0.15f;
            puff.transform.localScale = Vector3.one * 0.4f;

            var puffCol = puff.GetComponent<Collider>();
            if (puffCol != null) Destroy(puffCol);

            var rend = puff.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.65f, 0.85f, 0.2f, 0.8f); // Funny yellowish-green puff
            }

            Destroy(puff, 0.35f);
        }

        public void SetAngle(float angle)
        {
            currentAngleDegrees = (angle % 360f + 360f) % 360f;
            UpdateRimTransform();
        }
    }
}
