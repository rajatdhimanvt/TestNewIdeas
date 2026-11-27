using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal Panel displaying complete tier details, visual colors, names, and scores
/// for a selected Ball Kit (e.g. Ocean World, Classic Fruits, Sky High).
/// </summary>
public class BallKitPreviewPanel : UIBasePanel
{
    [Header("Kit Header")]
    [SerializeField] private TMP_Text kitTitleText;
    [SerializeField] private TMP_Text kitSubtitleText;

    [Header("Tier List Container")]
    [SerializeField] private Transform tierListContainer;

    [Header("Action Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;
    [SerializeField] private Button closeButton;

    private BallKitSO currentPreviewKit;

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    /// <summary>
    /// Opens preview details for the specified BallKitSO.
    /// </summary>
    public void SetupPreview(BallKitSO kit)
    {
        currentPreviewKit = kit;
        if (currentPreviewKit == null) return;

        if (kitTitleText != null)
        {
            kitTitleText.text = currentPreviewKit.kitName.ToUpper();
        }

        if (kitSubtitleText != null)
        {
            int totalTiers = currentPreviewKit.ballTiers != null ? currentPreviewKit.ballTiers.Count : 0;
            int dropTiers = currentPreviewKit.droppableTiers != null ? currentPreviewKit.droppableTiers.Count : 0;
            kitSubtitleText.text = $"{totalTiers} BALL TIERS  •  DROPPABLE TIERS: 1 - {dropTiers}";
        }

        RefreshEquipButtonState();
        PopulateTierItems();
    }

    private void RefreshEquipButtonState()
    {
        if (currentPreviewKit == null || !BallKitManager.HasInstance) return;

        bool isEquipped = BallKitManager.Instance.CurrentEquippedKit == currentPreviewKit;

        if (equipButtonText != null)
        {
            equipButtonText.text = isEquipped ? "EQUIPPED" : "EQUIP THIS KIT";
        }

        if (equipButton != null)
        {
            equipButton.interactable = !isEquipped;
        }
    }

    private void PopulateTierItems()
    {
        if (tierListContainer == null || currentPreviewKit == null) return;

        for (int i = tierListContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = tierListContainer.GetChild(i);
            if (child != null && child.gameObject != null)
            {
                Destroy(child.gameObject);
            }
        }

        List<BallDataSO> tiers = currentPreviewKit.ballTiers;
        if (tiers == null) return;

        for (int i = 0; i < tiers.Count; i++)
        {
            BallDataSO data = tiers[i];
            if (data == null) continue;

            CreateTierItemElement(data, i);
        }
    }

    private void CreateTierItemElement(BallDataSO data, int index)
    {
        GameObject itemObj = new GameObject($"TierItem_{index}");
        itemObj.transform.SetParent(tierListContainer, false);

        RectTransform rect = itemObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(760, 95);
        rect.anchoredPosition = new Vector2(0, 240 - (index * 105));

        Image bg = itemObj.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.18f, 0.26f, 0.95f);

        // Color Swatch Circle
        GameObject dotObj = new GameObject("ColorDot");
        dotObj.transform.SetParent(itemObj.transform, false);
        RectTransform dotRect = dotObj.AddComponent<RectTransform>();
        dotRect.anchoredPosition = new Vector2(-320, 0);
        dotRect.sizeDelta = new Vector2(50, 50);
        Image dotImg = dotObj.AddComponent<Image>();
        dotImg.sprite = BallMergeUITheme.GetCircleSprite();
        dotImg.color = data.ballColor;

        // Tier Title & Name
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(itemObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(-80, 15);
        titleRect.sizeDelta = new Vector2(380, 40);
        TMP_Text titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        BallMergeUITheme.ApplyFont(titleTxt);
        titleTxt.text = $"T{data.tier + 1}: {data.ballName}";
        titleTxt.fontSize = 28;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = Color.white;

        // Subtitle (Score / Radius)
        GameObject subObj = new GameObject("SubText");
        subObj.transform.SetParent(itemObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(-80, -20);
        subRect.sizeDelta = new Vector2(380, 30);
        TMP_Text subTxt = subObj.AddComponent<TextMeshProUGUI>();
        BallMergeUITheme.ApplyFont(subTxt);
        subTxt.text = $"Score: +{data.scoreValue} Pts  •  Radius: {data.radius:F2}";
        subTxt.fontSize = 20;
        subTxt.color = Color.gray;

        // Status Badge (Droppable vs High Tier)
        bool isDroppable = currentPreviewKit.droppableTiers != null && currentPreviewKit.droppableTiers.Contains(data);
        GameObject badgeObj = new GameObject("BadgeText");
        badgeObj.transform.SetParent(itemObj.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchoredPosition = new Vector2(240, 0);
        badgeRect.sizeDelta = new Vector2(200, 40);
        TMP_Text badgeTxt = badgeObj.AddComponent<TextMeshProUGUI>();
        BallMergeUITheme.ApplyFont(badgeTxt);
        badgeTxt.text = isDroppable ? "DROPPABLE" : "MERGE ONLY";
        badgeTxt.fontSize = 20;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.alignment = TextAlignmentOptions.Right;
        badgeTxt.color = isDroppable ? new Color(0.18f, 0.8f, 0.44f) : new Color(0.95f, 0.75f, 0.2f);
    }

    private void OnEquipClicked()
    {
        if (currentPreviewKit != null && BallKitManager.HasInstance)
        {
            BallKitManager.Instance.EquipKit(currentPreviewKit);
            RefreshEquipButtonState();

            // Refresh parent Kit Shop Panel display
            BallKitShopPanel shopPanel = uiManager != null ? uiManager.GetPanel<BallKitShopPanel>() : null;
            if (shopPanel != null)
            {
                shopPanel.OnShow();
            }

            if (uiManager != null)
            {
                uiManager.ShowPopup("Kit Equipped!", $"Successfully equipped '{currentPreviewKit.kitName}'.");
            }
        }
    }

    private void OnCloseClicked()
    {
        if (uiManager != null)
        {
            uiManager.HidePanel<BallKitPreviewPanel>();
        }
    }
}
