using System;
using UnityEngine;

namespace RaftRumble
{
    [RequireComponent(typeof(Rigidbody))]
    public class RaftController : MonoBehaviour
    {
        [Header("Raft Geometry")]
        [Tooltip("Radius of the circular raft for rim calculations")]
        [SerializeField] private float raftRadius = 1.8f;

        [Header("Water & Buoyancy Physics")]
        [SerializeField] private float waterLevelY = 0.5f;
        [SerializeField] private float buoyancyForce = 25f;
        [SerializeField] private float waterDrag = 1.8f;
        [SerializeField] private float waterAngularDrag = 2.5f;
        [SerializeField] private float uprightTorque = 15f;

        [Header("Collision Tags")]
        [SerializeField] private string obstacleTag = "Obstacle";
        [SerializeField] private string finishLineTag = "Finish";

        private Rigidbody rb;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public float RaftRadius => raftRadius;
        public Rigidbody Rigidbody => rb;
        public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

        public event Action OnObstacleHit;
        public event Action OnFinishReached;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }

        private void FixedUpdate()
        {
            ApplyWaterPhysics();
        }

        private void ApplyWaterPhysics()
        {
            // Buoyancy: Push up when sinking below water level
            float diffY = waterLevelY - transform.position.y;
            if (diffY > 0)
            {
                rb.AddForce(Vector3.up * (diffY * buoyancyForce), ForceMode.Acceleration);
            }

            // Upright stabilization: Prevent boat from flipping upside down
            Vector3 currentUp = transform.up;
            Vector3 targetUp = Vector3.up;
            Vector3 tiltAxis = Vector3.Cross(currentUp, targetUp);
            rb.AddTorque(tiltAxis * uprightTorque, ForceMode.Acceleration);
        }

        /// <summary>
        /// Applies fart/thrust force at the specified world position in the given direction.
        /// </summary>
        public void ApplyThrust(Vector3 worldPosition, Vector3 direction, float forceMagnitude)
        {
            if (rb == null) return;

            // Apply impulse force at the rim contact position.
            // This naturally produces linear acceleration AND turning torque.
            Vector3 forceVector = direction.normalized * forceMagnitude;
            rb.AddForceAtPosition(forceVector, worldPosition, ForceMode.Impulse);

            // Give a tiny fun wobble/pop effect
            rb.AddForce(Vector3.up * (forceMagnitude * 0.15f), ForceMode.Impulse);
        }

        public void ResetPosition()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(obstacleTag) || collision.gameObject.name.Contains("Obstacle"))
            {
                OnObstacleHit?.Invoke();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(obstacleTag) || other.name.Contains("Obstacle"))
            {
                OnObstacleHit?.Invoke();
            }
            else if (other.CompareTag(finishLineTag) || other.name.Contains("Finish"))
            {
                OnFinishReached?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            // Draw circle on the rim
            const int segments = 36;
            Vector3 prev = transform.position + new Vector3(raftRadius, 0.2f, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * (360f / segments) * Mathf.Deg2Rad;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(angle) * raftRadius, 0.2f, Mathf.Sin(angle) * raftRadius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
