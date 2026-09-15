using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade UI Manager.
/// Manages screen/panel stacks, modal popups, background dimming, and screen transitions.
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("Core UI Elements")]
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private GameObject backgroundDim;
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TMP_Text versionText;

    [Header("Sound Settings")]
    [SerializeField] private bool playSoundOnAllButtons = true;
    [SerializeField] private string defaultButtonSound = "Click";

    private readonly Dictionary<Type, UIBasePanel> panels = new Dictionary<Type, UIBasePanel>();
    private readonly Stack<Type> panelHistory = new Stack<Type>();

    private Type currentPanelType = null;

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
    }

    public void Init()
    {
        SetupCanvasScaler();
        SetupVersionText();
        InitializeAllPanels();
        SetupButtonAudioHooks();
        HideAllPanels();
    }

    private void SetupCanvasScaler()
    {
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            float canvasAspectRatio = scaler.referenceResolution.x / scaler.referenceResolution.y;
            float screenAspectRatio = (float)Screen.width / Screen.height;
            scaler.matchWidthOrHeight = screenAspectRatio < canvasAspectRatio ? 0f : 1f;
        }
    }

    private void SetupVersionText()
    {
        if (versionText != null)
        {
            versionText.text = $"v{Application.version}";
        }
    }

    private void InitializeAllPanels()
    {
        panels.Clear();
        var foundPanels = GetComponentsInChildren<UIBasePanel>(true);
        for (int i = 0; i < foundPanels.Length; i++)
        {
            var panel = foundPanels[i];
            Type type = panel.GetType();
            if (!panels.ContainsKey(type))
            {
                panels.Add(type, panel);
                panel.Init(this);
            }
        }
    }

    private void SetupButtonAudioHooks()
    {
        if (!playSoundOnAllButtons) return;

        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].onClick.AddListener(() =>
            {
                if (AudioManager.HasInstance)
                {
                    AudioManager.Instance.PlaySFX(defaultButtonSound);
                }
                else
                {
                    GameEvents.OnPlaySFX?.Invoke(defaultButtonSound);
                }
            });
        }
    }

    public void InitData()
    {
        foreach (var panel in panels.Values)
        {
            panel.InitData();
        }
    }

    #region Panel Navigation & History

    /// <summary>
    /// Shows a panel of type T.
    /// </summary>
    /// <param name="hideOld">If true, hides the previous active panel and pushes it to navigation stack.</param>
    public T ShowPanel<T>(bool hideOld = true) where T : UIBasePanel
    {
        return ShowPanel(typeof(T), hideOld) as T;
    }

    public UIBasePanel ShowPanel(Type panelType, bool hideOld = true)
    {
        ShowLoader(false);

        if (!panels.TryGetValue(panelType, out UIBasePanel targetPanel))
        {
            Debug.LogWarning($"[UIManager] Panel of type '{panelType.Name}' not found in registered panels.");
            return null;
        }

        if (hideOld && currentPanelType != null && panels.TryGetValue(currentPanelType, out UIBasePanel oldPanel))
        {
            oldPanel.OnHide();
            if (currentPanelType != panelType)
            {
                panelHistory.Push(currentPanelType);
            }
        }

        currentPanelType = panelType;
        targetPanel.OnShow();
        return targetPanel;
    }

    /// <summary>
    /// Navigates back to the previous panel in the history stack.
    /// </summary>
    public void GoBack()
    {
        if (panelHistory.Count > 0)
        {
            Type previous = panelHistory.Pop();
            ShowPanel(previous, true);
        }
    }

    public void HidePanel<T>() where T : UIBasePanel
    {
        HidePanel(typeof(T));
    }

    public void HidePanel(Type panelType)
    {
        if (panels.TryGetValue(panelType, out UIBasePanel panel))
        {
            panel.OnHide();
            if (currentPanelType == panelType)
            {
                currentPanelType = null;
            }
        }
    }

    public T GetPanel<T>() where T : UIBasePanel
    {
        if (panels.TryGetValue(typeof(T), out UIBasePanel panel))
        {
            return panel as T;
        }
        return null;
    }

    public void HideAllPanels()
    {
        foreach (var panel in panels.Values)
        {
            panel.gameObject.SetActive(false);
        }
        currentPanelType = null;
    }

    #endregion

    #region UI Overlays (Loader, Dim, Popup)

    public void ShowLoader(bool show)
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(show);
        }
    }

    public void ShowBg(bool show)
    {
        if (backgroundDim != null)
        {
            backgroundDim.SetActive(show);
        }
    }

    /// <summary>
    /// Shows a generic confirmation/alert modal popup.
    /// </summary>
    public void ShowPopup(string title, string message, Action onConfirm = null, Action onCancel = null, string confirmText = "OK", string cancelText = "Cancel")
    {
        GenericPopupPanel popup = GetPanel<GenericPopupPanel>();
        if (popup != null)
        {
            popup.SetupPopup(title, message, onConfirm, onCancel, confirmText, cancelText);
            ShowPanel<GenericPopupPanel>(false);
        }
        else
        {
            Debug.Log($"[UIManager Modal] {title}: {message}");
            onConfirm?.Invoke();
        }
    }

    #endregion
}
