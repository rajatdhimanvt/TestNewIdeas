using UnityEngine;

public class EyesControl : MonoBehaviour
{
    public Transform eyes;   // Center bone
    public Transform target;

    public float speed = 10f;
    public float intensity = 0.08f;
    public Vector2 idleDirection = Vector2.down;
    public float idleIntensity = -1f;

    private Vector3 defaultLocalPos;
    private static Transform sharedTarget;
    private bool forceLookDown = false;

    void Start()
    {
        if (eyes == null)
        {
            return;
        }

        defaultLocalPos = eyes.localPosition;
    }

    void LateUpdate()
    {
        if (eyes == null || eyes.parent == null)
        {
            return;
        }

        Transform currentTarget = ResolveTarget();
        Vector3 desiredLocalPosition;

        if (forceLookDown)
        {
            desiredLocalPosition = defaultLocalPos + new Vector3(-1.7f, -0.7f, 0f) * intensity;
        }
        else
        {
            desiredLocalPosition = defaultLocalPos + GetIdleOffset();
        }

        if (!forceLookDown && currentTarget != null)
        {
            Vector3 localTargetPos = eyes.parent.InverseTransformPoint(currentTarget.position);
            Vector3 direction = localTargetPos - defaultLocalPos;

            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                desiredLocalPosition = defaultLocalPos + new Vector3(direction.x, direction.y, 0f) * intensity;
            }
        }

        eyes.localPosition = Vector3.Lerp(
            eyes.localPosition,
            desiredLocalPosition,
            speed * Time.deltaTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public Transform GetTrackingAnchor()
    {
        if (eyes != null && eyes.parent != null)
        {
            return eyes.parent;
        }

        return transform;
    }

    public static void SetSharedTarget(Transform newTarget)
    {
        sharedTarget = newTarget;
    }

    private Transform ResolveTarget()
    {
        Transform resolvedTarget = target != null ? target : sharedTarget;

        if (resolvedTarget == null)
        {
            return null;
        }

        if (resolvedTarget == transform || resolvedTarget.IsChildOf(transform))
        {
            return null;
        }

        return resolvedTarget;
    }


    public void SetForceLookDown(bool state)
    {
        forceLookDown = state;
    }
    private Vector3 GetIdleOffset()
    {
        Vector2 resolvedIdleDirection = idleDirection == Vector2.zero ? Vector2.down : idleDirection.normalized;
        float resolvedIdleIntensity = idleIntensity > 0f ? idleIntensity : intensity * 0.35f;

        return new Vector3(resolvedIdleDirection.x, resolvedIdleDirection.y, 0f) * resolvedIdleIntensity;
    }
}
