using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Production-grade base class for all UI Panels.
/// Handles lifecycle callbacks, CanvasGroup fading, and raycast locking.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIBasePanel : MonoBehaviour
{
    [Header("Panel Animation Settings")]
    [SerializeField] protected bool showBackgroundDim = false;
    [SerializeField] protected float fadeDuration = 0.25f;
    [SerializeField] protected bool animateScale = false;

    protected CanvasGroup canvasGroup;
    protected UIManager uiManager;
    protected Tween activeTween;

    public Action onHide;
    public bool IsVisible => gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0.05f;

    public virtual void Init(UIManager manager)
    {
        uiManager = manager;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public virtual void InitData()
    {
        // Optional override for panels that load persistent or dynamic state
    }

    /// <summary>
    /// Shows the panel with an animated fade-in and scale if enabled.
    /// </summary>
    public virtual void OnShow()
    {
        if (uiManager != null)
        {
            uiManager.ShowBg(showBackgroundDim);
        }

        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            activeTween?.Kill();

            if (animateScale)
            {
                transform.localScale = Vector3.one * 0.85f;
                transform.DOScale(1f, fadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }

            activeTween = canvasGroup.DOFade(1f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.interactable = true;
                    }
                    OnShown();
                });
        }
        else
        {
            OnShown();
        }
    }

    /// <summary>
    /// Invoked immediately after show animation completes.
    /// </summary>
    protected virtual void OnShown()
    {
    }

    /// <summary>
    /// Hides the panel with animation and deactivates the GameObject.
    /// </summary>
    public virtual void OnHide()
    {
        activeTween?.Kill();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;

            activeTween = canvasGroup.DOFade(0f, fadeDuration * 0.8f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    CompleteHide();
                });
        }
        else
        {
            CompleteHide();
        }
    }

    private void CompleteHide()
    {
        gameObject.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        OnHidden();
        onHide?.Invoke();
        onHide = null;
    }

    /// <summary>
    /// Invoked immediately after panel is fully deactivated.
    /// </summary>
    protected virtual void OnHidden()
    {
    }

    protected virtual void OnDestroy()
    {
        activeTween?.Kill();
    }
}
