using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interactive Milestone Treasure Chest popup displayed every 5 levels (5, 10, 15...).
/// Self-builds cleanly onto any active Canvas so it works in both GameTest and Block testing scenes.
/// </summary>
public class MilestoneChestModal : MonoBehaviour
{
    private static MilestoneChestModal activeModal;

    private GameObject overlayObj;
    private Transform cardTransform;
    private Image chestImage;
    private Transform glowRaysTransform;
    private TMP_Text headerText;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text coinCounterText;
    private GameObject swatchRow;
    private Button openChestButton;
    private Button equipButton;
    private Button collectButton;

    private int targetLevelNumber;
    private MilestoneChestReward currentReward;
    private Action onClosedCallback;
    private bool isOpened = false;

    public static void Show(Transform canvasRoot, int levelNumber, Action onClosed = null)
    {
        if (canvasRoot == null)
        {
            Canvas anyCanvas = FindAnyObjectByType<Canvas>();
            if (anyCanvas == null) return;
            canvasRoot = anyCanvas.transform;
        }

        if (activeModal == null)
        {
            GameObject modalHost = new GameObject("MilestoneChestModal");
            modalHost.transform.SetParent(canvasRoot, false);
            RectTransform rt = modalHost.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            activeModal = modalHost.AddComponent<MilestoneChestModal>();
            activeModal.BuildUI(modalHost);
        }
        else if (activeModal.transform.parent != canvasRoot)
        {
            activeModal.transform.SetParent(canvasRoot, false);
        }

        activeModal.transform.SetAsLastSibling();
        activeModal.PresentClosedChest(levelNumber, onClosed);
    }

    private void BuildUI(GameObject root)
    {
        overlayObj = root;

        Image dimBg = root.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.84f);

        GameObject cardObj = new GameObject("ChestCard");
        cardObj.transform.SetParent(root.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(840f, 900f);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.13f, 0.15f, 0.23f, 0.98f);
        cardTransform = cardObj.transform;

        headerText = CreateText(cardObj, "Header", "MILESTONE CHEST!", new Vector2(0, 360), new Vector2(760, 80), 48, new Color(1f, 0.84f, 0.22f));
        titleText = CreateText(cardObj, "Title", "LEVEL 5 REWARD", new Vector2(0, 290), new Vector2(760, 52), 30, new Color(0.45f, 0.9f, 1f));

        // Rotating Glow Aura behind chest
        GameObject raysObj = new GameObject("GlowRays");
        raysObj.transform.SetParent(cardObj.transform, false);
        RectTransform raysRt = raysObj.AddComponent<RectTransform>();
        raysRt.anchoredPosition = new Vector2(0, 60);
        raysRt.sizeDelta = new Vector2(360f, 360f);
        Image raysImg = raysObj.AddComponent<Image>();
        raysImg.sprite = BlockRuntimeVisuals.GetSoftGlowSprite();
        raysImg.color = new Color(1f, 0.82f, 0.2f, 0.45f);
        glowRaysTransform = raysObj.transform;

        // Chest Icon Button (tapping the chest itself also opens it!)
        GameObject chestObj = new GameObject("ChestSprite");
        chestObj.transform.SetParent(cardObj.transform, false);
        RectTransform chestRt = chestObj.AddComponent<RectTransform>();
        chestRt.anchoredPosition = new Vector2(0, 60);
        chestRt.sizeDelta = new Vector2(260f, 260f);
        chestImage = chestObj.AddComponent<Image>();
        Button chestTapBtn = chestObj.AddComponent<Button>();
        chestTapBtn.targetGraphic = chestImage;
        chestTapBtn.onClick.AddListener(OnOpenChestClicked);

        // Swatch Preview Row (for unlocked Kit or Theme)
        swatchRow = new GameObject("SwatchRow");
        swatchRow.transform.SetParent(cardObj.transform, false);
        RectTransform swRt = swatchRow.AddComponent<RectTransform>();
        swRt.anchoredPosition = new Vector2(0, -105);
        swRt.sizeDelta = new Vector2(320, 48);

        subtitleText = CreateText(cardObj, "Subtitle", "Tap to open your Milestone Reward!", new Vector2(0, -170), new Vector2(760, 64), 28, Color.white);
        coinCounterText = CreateText(cardObj, "CoinCounter", "", new Vector2(0, -235), new Vector2(760, 56), 36, new Color(1f, 0.88f, 0.25f));

        openChestButton = CreateButton(cardObj, "OpenChestBtn", "OPEN CHEST!", new Vector2(0, -325), new Vector2(460, 96), new Color(0.95f, 0.65f, 0.12f));
        openChestButton.onClick.AddListener(OnOpenChestClicked);

        equipButton = CreateButton(cardObj, "EquipCollectBtn", "EQUIP & COLLECT", new Vector2(0, -310), new Vector2(480, 88), new Color(0.18f, 0.78f, 0.44f));
        equipButton.onClick.AddListener(OnEquipAndClose);

        collectButton = CreateButton(cardObj, "CollectBtn", "COLLECT", new Vector2(0, -402), new Vector2(340, 74), new Color(0.24f, 0.56f, 0.88f));
        collectButton.onClick.AddListener(CloseModal);
    }

    private void PresentClosedChest(int levelNumber, Action onClosed)
    {
        targetLevelNumber = levelNumber;
        onClosedCallback = onClosed;
        currentReward = null;
        isOpened = false;

        overlayObj.SetActive(true);
        headerText.text = "MILESTONE CHEST!";
        titleText.text = $"LEVEL {levelNumber} COMPLETED";
        subtitleText.text = "Tap the chest to reveal your special reward!";
        coinCounterText.text = string.Empty;
        swatchRow.SetActive(false);

        openChestButton.gameObject.SetActive(true);
        equipButton.gameObject.SetActive(false);
        collectButton.gameObject.SetActive(false);

        Sprite closedSprite = Resources.Load<Sprite>("BallMerge/UI/Chest_Closed");
        if (closedSprite != null) chestImage.sprite = closedSprite;
        chestImage.color = Color.white;

        cardTransform.DOKill();
        cardTransform.localScale = Vector3.one * 0.7f;
        cardTransform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);

        chestImage.transform.DOKill();
        chestImage.transform.localScale = Vector3.one;
        chestImage.transform.localRotation = Quaternion.identity;
        chestImage.transform.DOScale(1.08f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gameObject);
        chestImage.transform.DOShakeRotation(0.8f, new Vector3(0, 0, 7f), 8, 90f, true).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetLink(gameObject);

        glowRaysTransform.DOKill();
        glowRaysTransform.localScale = Vector3.one;
        glowRaysTransform.DOScale(1.15f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
    }

    private void OnOpenChestClicked()
    {
        if (isOpened) return;
        isOpened = true;

        GameEvents.OnPlaySFX?.Invoke("ChestOpen");
        CameraShake2D.TriggerShake(0.18f, 0.24f);

        currentReward = PlayerDataManager.HasInstance
            ? PlayerDataManager.Instance.ClaimMilestoneChest(targetLevelNumber)
            : new MilestoneChestReward
            {
                levelNumber = targetLevelNumber,
                bonusCoins = 250,
                rewardTitle = "TREASURE JACKPOT!",
                rewardSubtitle = "+250 Bonus Coins!"
            };

        Sprite openSprite = Resources.Load<Sprite>("BallMerge/UI/Chest_Open");
        if (openSprite != null) chestImage.sprite = openSprite;

        chestImage.transform.DOKill();
        chestImage.transform.localRotation = Quaternion.identity;
        chestImage.transform.localScale = Vector3.one * 0.65f;
        chestImage.transform.DOScale(1.15f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);

        glowRaysTransform.DOKill();
        glowRaysTransform.DOScale(1.45f, 0.45f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);

        headerText.text = currentReward.rewardTitle;
        subtitleText.text = currentReward.rewardSubtitle;

        BuildRewardSwatches();

        openChestButton.gameObject.SetActive(false);
        bool hasEquipItem = currentReward.unlockedKit != null || currentReward.unlockedTheme != null;
        equipButton.gameObject.SetActive(hasEquipItem);
        collectButton.gameObject.SetActive(true);

        if (!hasEquipItem)
        {
            collectButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -335);
        }
        else
        {
            equipButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -305);
            collectButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -395);
        }

        // Animated Coins Roll-Up Counter
        int displayed = 0;
        int targetCoins = currentReward.bonusCoins;
        int lastTickStep = -1;
        coinCounterText.text = "+0 COINS";
        DOTween.To(() => displayed, v =>
        {
            displayed = v;
            coinCounterText.text = $"+{displayed:N0} COINS";
            int step = displayed / 35;
            if (step > lastTickStep)
            {
                lastTickStep = step;
                GameEvents.OnPlaySFX?.Invoke("CoinTick");
            }
        }, targetCoins, 0.75f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);

        coinCounterText.transform.DOKill();
        coinCounterText.transform.localScale = Vector3.one * 0.8f;
        coinCounterText.transform.DOScale(1.1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
    }

    private void BuildRewardSwatches()
    {
        for (int i = swatchRow.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(swatchRow.transform.GetChild(i).gameObject);
        }

        if (currentReward == null)
        {
            swatchRow.SetActive(false);
            return;
        }

        if (currentReward.unlockedKit != null && currentReward.unlockedKit.ballTiers != null)
        {
            swatchRow.SetActive(true);
            int count = Mathf.Min(4, currentReward.unlockedKit.ballTiers.Count);
            float startX = -((count - 1) * 44f) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var tier = currentReward.unlockedKit.ballTiers[i];
                if (tier != null) CreateSwatchDot(startX + i * 44f, tier.ballColor);
            }
        }
        else if (currentReward.unlockedTheme != null)
        {
            swatchRow.SetActive(true);
            CreateSwatchDot(-44f, currentReward.unlockedTheme.backgroundColor);
            CreateSwatchDot(0f, currentReward.unlockedTheme.wallColor);
            CreateSwatchDot(44f, currentReward.unlockedTheme.dangerLineColor);
        }
        else
        {
            swatchRow.SetActive(false);
        }
    }

    private void CreateSwatchDot(float xOffset, Color col)
    {
        GameObject dot = new GameObject("Swatch");
        dot.transform.SetParent(swatchRow.transform, false);
        RectTransform rt = dot.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(xOffset, 0f);
        rt.sizeDelta = new Vector2(34f, 34f);
        Image img = dot.AddComponent<Image>();
        img.sprite = BlockRuntimeVisuals.GetSoftGlowSprite();
        col.a = 1f;
        img.color = col;
    }

    private void OnEquipAndClose()
    {
        GameEvents.OnPlaySFX?.Invoke("Click");
        if (currentReward != null)
        {
            if (currentReward.unlockedKit != null && BallKitManager.HasInstance)
            {
                BallKitManager.Instance.EquipKit(currentReward.unlockedKit);
            }
            if (currentReward.unlockedTheme != null && ThemeManager.HasInstance)
            {
                ThemeManager.Instance.EquipTheme(currentReward.unlockedTheme);
            }
        }
        CloseModal();
    }

    private void CloseModal()
    {
        GameEvents.OnPlaySFX?.Invoke("Click");
        overlayObj.SetActive(false);
        onClosedCallback?.Invoke();
    }

    private static TMP_Text CreateText(GameObject parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        BallMergeUITheme.ApplyFont(tmp);
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color bgCol)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = bgCol;
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        CreateText(obj, "Label", label, Vector2.zero, size, size.y * 0.36f, Color.white);
        return btn;
    }
}
