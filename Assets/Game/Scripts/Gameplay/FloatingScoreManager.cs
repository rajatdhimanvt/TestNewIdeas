using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Spawns animated floating score popups (+10, +20, etc.) in world space whenever balls merge.
/// Uses DOTween for smooth float-up, pop-scale, and fade-out animations.
/// </summary>
public class FloatingScoreManager : Singleton<FloatingScoreManager>
{
    [Header("Popup Settings")]
    [SerializeField] private Color textColor = new Color(1f, 0.84f, 0f, 1f); // Vibrant Gold
    [SerializeField] private float fontSize = 5.5f;
    [SerializeField] private float floatDistance = 1.2f;
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private int sortingOrder = 100;

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnScoreAddedAtPosition += SpawnFloatingScore;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreAddedAtPosition -= SpawnFloatingScore;
    }

    /// <summary>
    /// Spawns a floating score popup at the specified world position.
    /// </summary>
    public void SpawnFloatingScore(int points, Vector3 worldPosition)
    {
        if (points <= 0) return;

        // Offset position slightly on Z so it renders in front of 2D sprites
        Vector3 spawnPos = new Vector3(worldPosition.x, worldPosition.y, -1f);

        GameObject popupObj = new GameObject($"ScorePopup_+{points}");
        popupObj.transform.position = spawnPos;

        // Add TextMeshPro (World Space component)
        TextMeshPro tmpText = popupObj.AddComponent<TextMeshPro>();
        tmpText.text = $"+{points}";
        tmpText.fontSize = fontSize;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = textColor;
        tmpText.sortingOrder = sortingOrder;

        // Execute DOTween Animations
        // 1. Move upwards
        popupObj.transform.DOMoveY(spawnPos.y + floatDistance, duration).SetEase(Ease.OutCubic);

        // 2. Punch scale pop effect
        popupObj.transform.localScale = Vector3.one * 0.7f;
        popupObj.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

        // 3. Fade out text
        tmpText.DOFade(0f, duration * 0.45f)
            .SetDelay(duration * 0.55f)
            .OnComplete(() =>
            {
                Destroy(popupObj);
            });
    }
}
