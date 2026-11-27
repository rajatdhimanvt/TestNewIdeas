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
    [SerializeField] private TMP_Text coinBalanceText;

    [Header("Kit Buttons")]
    [SerializeField] private Button classicKitButton;
    [SerializeField] private Button oceanKitButton;
    [SerializeField] private Button skyKitButton;

    [Header("Inspect Kit Buttons")]
    [SerializeField] private Button inspectClassicButton;
    [SerializeField] private Button inspectOceanButton;
    [SerializeField] private Button inspectSkyButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (classicKitButton != null) classicKitButton.onClick.AddListener(() => EquipKit("classic_kit"));
        if (oceanKitButton != null) oceanKitButton.onClick.AddListener(() => EquipKit("ocean_kit"));
        if (skyKitButton != null) skyKitButton.onClick.AddListener(() => EquipKit("sky_kit"));

        if (inspectClassicButton != null) inspectClassicButton.onClick.AddListener(() => OpenKitPreview("classic_kit"));
        if (inspectOceanButton != null) inspectOceanButton.onClick.AddListener(() => OpenKitPreview("ocean_kit"));
        if (inspectSkyButton != null) inspectSkyButton.onClick.AddListener(() => OpenKitPreview("sky_kit"));

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        PlayerDataManager.OnCoinsChanged += UpdateCoinDisplay;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PlayerDataManager.OnCoinsChanged -= UpdateCoinDisplay;
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

        UpdateCoinDisplay(PlayerDataManager.HasInstance ? PlayerDataManager.Instance.Coins : 0);
        UpdateKitButtonState("classic_kit", classicKitButton, 0);
        UpdateKitButtonState("ocean_kit", oceanKitButton, 500);
        UpdateKitButtonState("sky_kit", skyKitButton, 1000);
    }

    private void UpdateKitButtonState(string kitId, Button btn, int coinCost)
    {
        if (btn == null) return;

        bool isUnlocked = BallKitManager.HasInstance && BallKitManager.Instance.IsKitUnlocked(kitId);
        bool isEquipped = BallKitManager.HasInstance && BallKitManager.Instance.CurrentEquippedKit != null && BallKitManager.Instance.CurrentEquippedKit.kitId == kitId;

        TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            if (isEquipped) txt.text = "EQUIPPED";
            else if (isUnlocked) txt.text = "EQUIP";
            else txt.text = $"UNLOCK\n{coinCost} COINS";
        }
    }

    public void OpenKitPreview(string kitId)
    {
        if (!BallKitManager.HasInstance || uiManager == null) return;

        BallKitSO kit = BallKitManager.Instance.AvailableKits.Find(k => k != null && k.kitId == kitId);
        if (kit != null)
        {
            BallKitPreviewPanel previewPanel = uiManager.GetPanel<BallKitPreviewPanel>();
            if (previewPanel != null)
            {
                previewPanel.SetupPreview(kit);
                uiManager.ShowPanel<BallKitPreviewPanel>(false);
            }
        }
    }

    private void EquipKit(string kitId)
    {
        if (!BallKitManager.HasInstance) return;

        bool isUnlocked = BallKitManager.Instance.IsKitUnlocked(kitId);
        int cost = kitId == "ocean_kit" ? 500 : (kitId == "sky_kit" ? 1000 : 0);

        if (!isUnlocked)
        {
            if (BallKitManager.Instance.UnlockKit(kitId, cost))
            {
                BallKitManager.Instance.EquipKit(kitId);
                RefreshDisplay();
                if (uiManager != null) uiManager.ShowPopup("Kit Unlocked!", $"Unlocked and equipped '{kitId}'!");
            }
            else
            {
                if (uiManager != null) uiManager.ShowPopup("Insufficient Coins", $"You need {cost} Coins to unlock this kit!");
            }
            return;
        }

        BallKitManager.Instance.EquipKit(kitId);
        RefreshDisplay();
    }

    private void OnBackClicked()
    {
        if (uiManager != null)
        {
            uiManager.GoBack();
        }
    }

    private void UpdateCoinDisplay(int coins)
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = $"{coins:N0} COINS";
        }
    }
}
