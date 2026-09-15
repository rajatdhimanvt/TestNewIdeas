using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable modal popup panel for alerts, confirmations, and warnings.
/// </summary>
public class GenericPopupPanel : UIBasePanel
{
    [Header("Popup UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text cancelButtonText;
    [SerializeField] private Button closeButton;

    private Action onConfirmCallback;
    private Action onCancelCallback;

    public override void Init(UIManager manager)
    {
        base.Init(manager);
        showBackgroundDim = true;
        animateScale = true;

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClick);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCancelClick);
        }
    }

    public void SetupPopup(string title, string message, Action onConfirm = null, Action onCancel = null, string confirmLabel = "OK", string cancelLabel = "Cancel")
    {
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            if (confirmButtonText != null) confirmButtonText.text = confirmLabel;
        }

        if (cancelButton != null)
        {
            bool hasCancel = onCancel != null;
            cancelButton.gameObject.SetActive(hasCancel);
            if (cancelButtonText != null) cancelButtonText.text = cancelLabel;
        }
    }

    private void OnConfirmClick()
    {
        OnHide();
        onConfirmCallback?.Invoke();
    }

    private void OnCancelClick()
    {
        OnHide();
        onCancelCallback?.Invoke();
    }
}
