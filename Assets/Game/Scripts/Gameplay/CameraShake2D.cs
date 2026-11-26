using UnityEngine;

/// <summary>
/// Lightweight, zero-dependency 2D camera micro-shake for juicy impacts,
/// block destruction, bomb blasts, and merge combos.
/// </summary>
public class CameraShake2D : MonoBehaviour
{
    private static CameraShake2D activeInstance;

    private Vector3 baseLocalPosition;
    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;
    private bool isShaking = false;

    private void Awake()
    {
        activeInstance = this;
        baseLocalPosition = transform.localPosition;
    }

    public static void TriggerShake(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;

        if (activeInstance == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            activeInstance = cam.GetComponent<CameraShake2D>();
            if (activeInstance == null)
            {
                activeInstance = cam.gameObject.AddComponent<CameraShake2D>();
            }
        }

        activeInstance.StartShake(intensity, duration);
    }

    public void StartShake(float intensity, float duration)
    {
        if (!isShaking)
        {
            baseLocalPosition = transform.localPosition;
        }

        // Preserve strongest ongoing shake if overlapping
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = shakeDuration;
        isShaking = true;
    }

    private void LateUpdate()
    {
        if (!isShaking) return;

        shakeTimer -= Time.unscaledDeltaTime;
        if (shakeTimer <= 0f)
        {
            isShaking = false;
            shakeIntensity = 0f;
            shakeDuration = 0f;
            transform.localPosition = baseLocalPosition;
            return;
        }

        float damper = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
        float offsetX = (Random.value * 2f - 1f) * shakeIntensity * damper;
        float offsetY = (Random.value * 2f - 1f) * shakeIntensity * damper;
        transform.localPosition = baseLocalPosition + new Vector3(offsetX, offsetY, 0f);
    }
}
