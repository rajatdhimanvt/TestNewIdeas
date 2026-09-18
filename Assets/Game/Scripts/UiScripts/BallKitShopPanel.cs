using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-grade Ball Kit Shop Panel.
/// Allows player to select and equip different Ball Kits (Classic Fruits, Ocean World, Sky High).
/// </summary>
public class BallKitShopPanel : UIBasePanel
{
    [Header("Current Equipped Display")]
    [SerializeField] private TMP_Text currentKitNameText;

    [Header("Kit Buttons")]
    [SerializeField] private Button classicKitButton;
    [SerializeField] private Button oceanKitButton;
    [SerializeField] private Button skyKitButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (classicKitButton != null) classicKitButton.onClick.AddListener(() => EquipKit("classic_kit"));
        if (oceanKitButton != null) oceanKitButton.onClick.AddListener(() => EquipKit("ocean_kit"));
        if (skyKitButton != null) skyKitButton.onClick.AddListener(() => EquipKit("sky_kit"));

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (BallKitManager.HasInstance && currentKitNameText != null)
        {
            BallKitSO active = BallKitManager.Instance.CurrentEquippedKit;
            currentKitNameText.text = active != null ? $"ACTIVE: {active.kitName.ToUpper()}" : "ACTIVE: CLASSIC FRUITS";
        }
    }

    private void EquipKit(string kitId)
    {
        if (BallKitManager.HasInstance)
        {
            BallKitManager.Instance.EquipKit(kitId);
            RefreshDisplay();
            if (uiManager != null)
            {
                uiManager.ShowPopup("Kit Equipped!", $"Successfully equipped ball kit '{kitId}'.");
            }
        }
    }

    private void OnBackClicked()
    {
        if (uiManager != null)
        {
            uiManager.GoBack();
        }
    }
}
