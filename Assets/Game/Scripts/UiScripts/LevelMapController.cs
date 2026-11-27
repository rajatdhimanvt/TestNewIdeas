using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Candy Crush style Level Map with Organic Randomized Zigzag & Smooth Dynamic Scrolling.
/// Supports up to 1000+ levels with both initial full load and dynamic chunk expansion,
/// and smooth jumping between Current Level and Top.
/// </summary>
public class LevelMapController : MonoBehaviour
{
    public enum ScrollDirection
    {
        BottomToTop, // Candy Crush style: Level 1 at bottom, scroll upwards to 1000
        TopToBottom  // Level 1 at top, scroll downwards
    }

    [Header("UI References")]
    [Tooltip("Leave empty to auto-detect on this GameObject or children")]
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("Leave empty to auto-detect from ScrollRect.content")]
    [SerializeField] private RectTransform contentRect;
    [Tooltip("Optional custom prefab for level nodes. If null, a polished procedural button will be used.")]
    [SerializeField] private GameObject levelNodePrefab;

    [Header("Level Configuration")]
    [SerializeField] private int totalLevels = 1000;
    [Tooltip("If true, all levels (1 to 1000) are loaded at start so you can freely scroll to top and back.")]
    [SerializeField] private bool loadAllInitially = true;
    [Tooltip("Used when loadAllInitially is false: how many levels spawn at start")]
    [SerializeField] private int initialLoadCount = 30;
    [Tooltip("Used when loadAllInitially is false: how many spawn per scroll batch")]
    [SerializeField] private int batchLoadCount = 20;
    [SerializeField] private ScrollDirection scrollDirection = ScrollDirection.BottomToTop;
    [SerializeField] private bool autoScrollToCurrentLevel = true;

    [Header("Organic Randomized Zigzag Layout")]
    [SerializeField] private float stepY = 190f;             // Average vertical distance between levels
    [SerializeField] private float waveAmplitude = 240f;     // Primary left-right swing
    [SerializeField] private float waveFrequency = 0.52f;    // Primary frequency
    [Range(0f, 150f)]
    [SerializeField] private float randomJitterX = 85f;      // Non-uniform variation in horizontal positions
    [Range(0f, 40f)]
    [SerializeField] private float randomStepYJitter = 22f;  // Subtle variation in vertical distance
    [SerializeField] private float maxHorizontalSpread = 320f;// Screen edge boundary clamp
    [SerializeField] private float noiseSeed = 73.194f;      // Deterministic seed for repeatable organic map
    [SerializeField] private float startPadding = 180f;      // Padding before Level 1
    [SerializeField] private float endPadding = 250f;        // Padding after last level
    [SerializeField] private bool drawConnectingPath = true; // Draw dotted trail between level nodes

    [Header("Visual Styling (Procedural Fallback)")]
    [SerializeField] private Vector2 nodeSize = new Vector2(130f, 130f);
    [SerializeField] private Sprite nodeBackgroundSprite;
    [SerializeField] private Sprite pathDotSprite;
    [SerializeField] private Color unlockedColor = new Color(0.18f, 0.72f, 0.42f, 1f);     // Vibrant Green
    [SerializeField] private Color currentLevelColor = new Color(1f, 0.76f, 0.15f, 1f);   // Bright Gold
    [SerializeField] private Color lockedColor = new Color(0.28f, 0.32f, 0.38f, 0.95f);    // Muted Slate
    [SerializeField] private Color pathDotColor = new Color(1f, 1f, 1f, 0.35f);

    private int currentlyLoadedLevels = 0;
    private bool isLoadingMore = false;
    private bool hasInitialized = false;
    private Coroutine activeScrollRoutine;
    private static Sprite defaultCircleSprite;

    // Cache precomputed positions for all 1000 levels
    private readonly Dictionary<int, Vector2> cachedNodePositions = new Dictionary<int, Vector2>();
    private readonly Dictionary<int, float> cachedCumulativeY = new Dictionary<int, float>();
    private readonly Dictionary<int, GameObject> spawnedNodes = new Dictionary<int, GameObject>();

    private void Awake()
    {
        // Auto-detect ScrollRect
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            }
        }

        // Auto-detect Content
        if (scrollRect != null && contentRect == null)
        {
            contentRect = scrollRect.content;
        }

        SetupContentAlignment();
        PrecalculateAllPositions();
    }

    private void Start()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollPositionChanged);
        }

        PlayerDataManager.OnLevelStarsUpdated += OnLevelStarsUpdated;

        if (!hasInitialized)
        {
            InitializeMap();
            hasInitialized = true;
        }
    }

    private void OnEnable()
    {
        if (hasInitialized)
        {
            RefreshMap();
        }
    }

    public void SetScrollChunkConfig(bool loadAll, int initialCount, int batchCount)
    {
        loadAllInitially = loadAll;
        initialLoadCount = Mathf.Max(10, initialCount);
        batchLoadCount = Mathf.Max(10, batchCount);
        UpdateContentHeight();
    }

    public void RefreshMap()
    {
        if (!hasInitialized)
        {
            if (contentRect == null && scrollRect != null) contentRect = scrollRect.content;
            if (cachedNodePositions.Count == 0)
            {
                SetupContentAlignment();
                PrecalculateAllPositions();
            }
            InitializeMap();
            hasInitialized = true;
            return;
        }

        int highestUnlocked = GetHighestUnlockedLevel();
        if (!loadAllInitially && currentlyLoadedLevels < Mathf.Min(totalLevels, highestUnlocked + 10))
        {
            LoadLevelsUpTo(highestUnlocked + 15);
        }

        foreach (var kv in spawnedNodes)
        {
            int lvl = kv.Key;
            GameObject node = kv.Value;
            if (node == null) continue;

            bool isUnlocked = lvl <= highestUnlocked;
            bool isCurrent = lvl == highestUnlocked;
            int stars = GetLevelStars(lvl);
            ConfigurePrefabNode(node, lvl, isUnlocked, isCurrent, stars);
        }

        if (autoScrollToCurrentLevel && gameObject.activeInHierarchy)
        {
            ScrollToLevel(highestUnlocked, 0.2f);
        }
    }

    private void OnDestroy()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollPositionChanged);
        }

        PlayerDataManager.OnLevelStarsUpdated -= OnLevelStarsUpdated;
    }

    /// <summary>
    /// Configures anchors & pivots for content container according to scroll direction.
    /// </summary>
    private void SetupContentAlignment()
    {
        if (contentRect == null) return;

        if (scrollDirection == ScrollDirection.BottomToTop)
        {
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// Precalculates non-uniform, organic zigzag positions for all levels 1..totalLevels.
    /// Combines primary wave, secondary harmonic, and Perlin noise for an organic adventure trail.
    /// </summary>
    private void PrecalculateAllPositions()
    {
        cachedCumulativeY.Clear();
        cachedNodePositions.Clear();

        float curY = startPadding;
        cachedCumulativeY[1] = curY;

        for (int lvl = 1; lvl <= totalLevels; lvl++)
        {
            if (lvl > 1)
            {
                // Subtle organic variation in vertical step
                float stepNoise = (Mathf.PerlinNoise(noiseSeed + 19.3f, lvl * 0.48f) - 0.5f) * 2f;
                float currentStep = stepY + (stepNoise * randomStepYJitter);
                curY += Mathf.Max(110f, currentStep);
                cachedCumulativeY[lvl] = curY;
            }

            // Organic Horizontal Zigzag Calculation:
            // 1. Primary sine wave
            float baseWave = Mathf.Sin(lvl * waveFrequency) * waveAmplitude;
            // 2. Secondary harmonic wave (breaks uniformity of alternating turns)
            float harmonicWave = Mathf.Sin(lvl * 1.63f + 1.1f) * (waveAmplitude * 0.32f);
            // 3. Perlin noise organic jitter
            float noiseX = (Mathf.PerlinNoise(lvl * 0.39f, noiseSeed) - 0.5f) * 2f * randomJitterX;

            float x = baseWave + harmonicWave + noiseX;
            x = Mathf.Clamp(x, -maxHorizontalSpread, maxHorizontalSpread);

            float y = (scrollDirection == ScrollDirection.BottomToTop) ? curY : -curY;
            cachedNodePositions[lvl] = new Vector2(x, y);
        }

        UpdateContentHeight();
    }

    /// <summary>
    /// Initializes map loading based on settings (either all levels or initial chunk).
    /// </summary>
    public void InitializeMap()
    {
        if (contentRect == null) return;

        // Clear previous children
        for (int i = contentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRect.GetChild(i).gameObject);
        }

        currentlyLoadedLevels = 0;
        spawnedNodes.Clear();

        int highestUnlocked = GetHighestUnlockedLevel();

        if (loadAllInitially)
        {
            // Load all 1000 levels so player can scroll freely from bottom to top and back
            LoadLevelsUpTo(totalLevels);
        }
        else
        {
            // Ensure at least enough levels are loaded for current level + buffer
            int toLoad = Mathf.Max(initialLoadCount, highestUnlocked + 10);
            toLoad = Mathf.Min(toLoad, totalLevels);
            LoadLevelsUpTo(toLoad);
        }

        if (autoScrollToCurrentLevel && gameObject.activeInHierarchy)
        {
            ScrollToLevel(highestUnlocked, 0.1f);
        }
    }

    private void OnScrollPositionChanged(Vector2 scrollPos)
    {
        if (isLoadingMore || currentlyLoadedLevels >= totalLevels) return;

        // Dynamic expansion when scrolling near the end
        if (scrollDirection == ScrollDirection.BottomToTop)
        {
            if (scrollPos.y >= 0.85f)
            {
                LoadMoreLevels(batchLoadCount);
            }
        }
        else
        {
            if (scrollPos.y <= 0.15f)
            {
                LoadMoreLevels(batchLoadCount);
            }
        }
    }

    /// <summary>
    /// Loads the next batch of levels.
    /// </summary>
    public void LoadMoreLevels(int count)
    {
        LoadLevelsUpTo(currentlyLoadedLevels + count);
    }

    /// <summary>
    /// Loads all levels up to targetLevel.
    /// </summary>
    public void LoadLevelsUpTo(int targetLevel)
    {
        if (isLoadingMore) return;

        targetLevel = Mathf.Min(targetLevel, totalLevels);
        if (targetLevel <= currentlyLoadedLevels) return;

        isLoadingMore = true;
        int highestUnlocked = GetHighestUnlockedLevel();

        for (int lvl = currentlyLoadedLevels + 1; lvl <= targetLevel; lvl++)
        {
            Vector2 pos = cachedNodePositions[lvl];

            // Draw dotted connecting path between previous node and current node
            if (drawConnectingPath && lvl > 1 && cachedNodePositions.ContainsKey(lvl - 1))
            {
                Vector2 prevPos = cachedNodePositions[lvl - 1];
                CreatePathConnector(prevPos, pos, lvl);
            }

            // Create Level Node
            GameObject nodeObj = CreateLevelNode(lvl, pos, highestUnlocked);
            spawnedNodes[lvl] = nodeObj;
        }

        currentlyLoadedLevels = targetLevel;
        UpdateContentHeight();

        isLoadingMore = false;
    }

    private void UpdateContentHeight()
    {
        if (contentRect == null) return;

        float lastY;
        if (loadAllInitially)
        {
            lastY = cachedCumulativeY.TryGetValue(totalLevels, out float fullY) ? fullY : (startPadding + totalLevels * stepY);
        }
        else
        {
            int refLvl = Mathf.Max(1, currentlyLoadedLevels);
            lastY = cachedCumulativeY.TryGetValue(refLvl, out float y) ? y : (startPadding + refLvl * stepY);
        }

        float totalHeight = lastY + endPadding;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
    }

    private GameObject CreateLevelNode(int levelNum, Vector2 anchoredPos, int highestUnlocked)
    {
        bool isUnlocked = levelNum <= highestUnlocked;
        bool isCurrent = levelNum == highestUnlocked;
        int stars = GetLevelStars(levelNum);

        GameObject nodeObj;
        if (levelNodePrefab != null)
        {
            nodeObj = Instantiate(levelNodePrefab, contentRect);
            ConfigurePrefabNode(nodeObj, levelNum, isUnlocked, isCurrent, stars);
        }
        else
        {
            nodeObj = CreateProceduralNode(levelNum, isUnlocked, isCurrent, stars);
        }

        nodeObj.name = $"LevelNode_{levelNum:D3}";
        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (scrollDirection == ScrollDirection.BottomToTop)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
            }

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
        }

        return nodeObj;
    }

    private GameObject CreateProceduralNode(int levelNum, bool isUnlocked, bool isCurrent, int stars)
    {
        GameObject nodeObj = new GameObject($"LevelNode_{levelNum}");
        nodeObj.transform.SetParent(contentRect, false);

        RectTransform rt = nodeObj.AddComponent<RectTransform>();
        rt.sizeDelta = nodeSize;

        Sprite circleSprite = nodeBackgroundSprite != null
            ? nodeBackgroundSprite
            : (BallMergeUITheme.GetCircleSprite() != null ? BallMergeUITheme.GetCircleSprite() : GetDefaultCircleSprite());

        // Outer Glow / Ring
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(nodeObj.transform, false);
        RectTransform ringRt = ringObj.AddComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.sizeDelta = isCurrent ? new Vector2(22f, 22f) : new Vector2(16f, 16f);
        Image ringImg = ringObj.AddComponent<Image>();
        ringImg.sprite = circleSprite;
        ringImg.color = isCurrent ? new Color(1f, 0.9f, 0.35f, 0.95f) : new Color(0f, 0f, 0f, 0.38f);
        ringImg.raycastTarget = false;

        if (isCurrent && Application.isPlaying)
        {
            ringRt.DOScale(1.1f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(ringObj);
        }

        // Main Button Background
        Image bgImg = nodeObj.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        if (isCurrent)
            bgImg.color = currentLevelColor;
        else if (isUnlocked)
            bgImg.color = unlockedColor;
        else
            bgImg.color = lockedColor;

        // Top-left glossy specular highlight
        if (isUnlocked)
        {
            GameObject shineObj = new GameObject("Shine");
            shineObj.transform.SetParent(nodeObj.transform, false);
            RectTransform shineRt = shineObj.AddComponent<RectTransform>();
            shineRt.anchoredPosition = new Vector2(-nodeSize.x * 0.22f, nodeSize.y * 0.22f);
            shineRt.sizeDelta = new Vector2(nodeSize.x * 0.24f, nodeSize.y * 0.24f);
            Image shineImg = shineObj.AddComponent<Image>();
            shineImg.sprite = circleSprite;
            shineImg.color = new Color(1f, 1f, 1f, 0.42f);
            shineImg.raycastTarget = false;
        }

        Button btn = nodeObj.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        btn.interactable = isUnlocked;
        btn.onClick.AddListener(() => OnLevelClicked(levelNum));

        // Level Number / Lock text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(nodeObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = new Vector2(0, isUnlocked ? 10f : 0f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        BallMergeUITheme.ApplyFont(tmp);
        tmp.text = isUnlocked ? levelNum.ToString() : "LOCK";
        tmp.fontSize = isUnlocked ? (levelNum >= 100 ? 34 : 42) : 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // Visual 5-point Star Icons (no missing font glyphs)
        ApplyNodeStarsVisual(nodeObj.transform, isUnlocked, stars);

        // Top tag: Milestone Chest (every 5 levels) or Block Stage count
        RefreshNodeBadge(nodeObj, levelNum, isUnlocked);

        return nodeObj;
    }

    private void ConfigurePrefabNode(GameObject nodeObj, int levelNum, bool isUnlocked, bool isCurrent, int stars)
    {
        Transform ringTf = nodeObj.transform.Find("Ring");
        if (ringTf != null)
        {
            ringTf.DOKill();
            ringTf.localScale = Vector3.one;
            Image ringImg = ringTf.GetComponent<Image>();
            if (ringImg != null)
            {
                ringImg.color = isCurrent ? new Color(1f, 0.9f, 0.35f, 0.95f) : new Color(0f, 0f, 0f, 0.38f);
            }
            if (isCurrent && Application.isPlaying)
            {
                ringTf.DOScale(1.1f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(ringTf.gameObject);
            }
        }

        Image mainBg = nodeObj.GetComponent<Image>();
        if (mainBg != null)
        {
            if (isCurrent) mainBg.color = currentLevelColor;
            else if (isUnlocked) mainBg.color = unlockedColor;
            else mainBg.color = lockedColor;
        }

        Button btn = nodeObj.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.interactable = isUnlocked;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnLevelClicked(levelNum));
        }

        TextMeshProUGUI[] texts = nodeObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            BallMergeUITheme.ApplyFont(t);
            string lower = t.gameObject.name.ToLower();
            if (lower.Contains("number") || lower.Contains("level") || lower.Contains("label") || texts.Length == 1)
            {
                t.text = isUnlocked ? levelNum.ToString() : "LOCK";
                t.fontSize = isUnlocked ? (levelNum >= 100 ? 34 : 42) : 22;
            }
            else if (lower.Contains("star"))
            {
                t.gameObject.SetActive(false);
            }
        }

        ApplyNodeStarsVisual(nodeObj.transform, isUnlocked, stars);
        RefreshNodeBadge(nodeObj, levelNum, isUnlocked);
    }

    private void ApplyNodeStarsVisual(Transform nodeTransform, bool isUnlocked, int stars)
    {
        Transform starsRowTf = nodeTransform.Find("StarIcons");
        if (!isUnlocked)
        {
            if (starsRowTf != null) starsRowTf.gameObject.SetActive(false);
            return;
        }

        Sprite starSprite = BallMergeUITheme.GetStarSprite();
        GameObject rowObj;
        if (starsRowTf == null)
        {
            rowObj = new GameObject("StarIcons");
            rowObj.transform.SetParent(nodeTransform, false);
            RectTransform rowRt = rowObj.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0f);
            rowRt.anchorMax = new Vector2(0.5f, 0f);
            rowRt.pivot = new Vector2(0.5f, 0f);
            rowRt.anchoredPosition = new Vector2(0f, 10f);
            rowRt.sizeDelta = new Vector2(96f, 26f);

            for (int i = 0; i < 3; i++)
            {
                GameObject sObj = new GameObject($"Star_{i}");
                sObj.transform.SetParent(rowObj.transform, false);
                RectTransform srt = sObj.AddComponent<RectTransform>();
                srt.anchoredPosition = new Vector2((i - 1) * 26f, i == 1 ? 3f : 0f);
                srt.sizeDelta = i == 1 ? new Vector2(22f, 22f) : new Vector2(19f, 19f);
                Image img = sObj.AddComponent<Image>();
                img.sprite = starSprite;
                img.raycastTarget = false;
            }
        }
        else
        {
            rowObj = starsRowTf.gameObject;
            rowObj.SetActive(true);
        }

        for (int i = 0; i < 3 && i < rowObj.transform.childCount; i++)
        {
            Image sImg = rowObj.transform.GetChild(i).GetComponent<Image>();
            if (sImg != null)
            {
                sImg.color = i < stars ? new Color(1f, 0.9f, 0.22f, 1f) : new Color(0.14f, 0.17f, 0.24f, 0.55f);
            }
        }
    }

    private void RefreshNodeBadge(GameObject nodeObj, int levelNum, bool isUnlocked)
    {
        Transform badgeTf = nodeObj.transform.Find("StageBadge");
        string badgeText = string.Empty;
        Color badgeColor = new Color(1f, 0.78f, 0.28f);

        bool isMilestone = PlayerDataManager.HasInstance ? PlayerDataManager.Instance.IsMilestoneLevel(levelNum) : (levelNum > 0 && levelNum % 5 == 0);
        bool chestClaimed = PlayerDataManager.HasInstance && PlayerDataManager.Instance.IsMilestoneChestClaimed(levelNum);

        if (isMilestone)
        {
            badgeText = chestClaimed ? "CHEST DONE" : "CHEST";
            badgeColor = chestClaimed ? new Color(0.55f, 0.92f, 0.58f) : new Color(1f, 0.85f, 0.22f);
        }
        else if (isUnlocked)
        {
            LevelDatabaseSO db = LevelDatabaseSO.LoadDefault();
            LevelDataSO data = db != null ? db.GetLevel(levelNum) : null;
            if (data != null && data.HasBlocks)
            {
                int count = data.GetRequiredBlockCount();
                if (count > 0)
                {
                    badgeText = $"{count} BLK";
                    badgeColor = new Color(1f, 0.68f, 0.3f);
                }
            }
        }

        if (string.IsNullOrEmpty(badgeText))
        {
            if (badgeTf != null) badgeTf.gameObject.SetActive(false);
            return;
        }

        GameObject badgeObj;
        TextMeshProUGUI tmp;
        if (badgeTf == null)
        {
            badgeObj = new GameObject("StageBadge");
            badgeObj.transform.SetParent(nodeObj.transform, false);
            RectTransform brt = badgeObj.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 6f);
            brt.sizeDelta = new Vector2(126f, 28f);

            Image pillBg = badgeObj.AddComponent<Image>();
            pillBg.color = new Color(0.08f, 0.1f, 0.16f, 0.92f);
            pillBg.raycastTarget = false;

            GameObject txtObj = new GameObject("BadgeText");
            txtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform trt = txtObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            tmp = txtObj.AddComponent<TextMeshProUGUI>();
            BallMergeUITheme.ApplyFont(tmp);
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
        else
        {
            badgeObj = badgeTf.gameObject;
            badgeObj.SetActive(true);
            tmp = badgeObj.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (tmp != null)
        {
            BallMergeUITheme.ApplyFont(tmp);
            tmp.text = badgeText;
            tmp.color = badgeColor;
        }
    }

    private static Sprite GetDefaultCircleSprite()
    {
        if (defaultCircleSprite != null) return defaultCircleSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01((radius - d) / 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        defaultCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return defaultCircleSprite;
    }

    /// <summary>
    /// Spawns dotted path connection between consecutive level nodes.
    /// </summary>
    private void CreatePathConnector(Vector2 fromPos, Vector2 toPos, int toLevel)
    {
        int dotsCount = 4;
        GameObject pathParent = new GameObject($"Path_{toLevel - 1}_to_{toLevel}");
        pathParent.transform.SetParent(contentRect, false);
        pathParent.transform.SetAsFirstSibling(); // Render behind buttons

        for (int d = 1; d <= dotsCount; d++)
        {
            float t = (float)d / (dotsCount + 1);
            Vector2 dotPos = Vector2.Lerp(fromPos, toPos, t);

            GameObject dot = new GameObject($"Dot_{d}");
            dot.transform.SetParent(pathParent.transform, false);

            RectTransform rt = dot.AddComponent<RectTransform>();
            if (scrollDirection == ScrollDirection.BottomToTop)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
            }

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = dotPos;
            rt.sizeDelta = new Vector2(16f, 16f);

            Image img = dot.AddComponent<Image>();
            img.sprite = pathDotSprite != null ? pathDotSprite : GetDefaultCircleSprite();
            img.color = pathDotColor;
            img.raycastTarget = false;
        }
    }

    private void OnLevelClicked(int levelNum)
    {
        Debug.Log($"[LevelMapController] Selected Level: {levelNum}");
        GameEvents.OnPlaySFX?.Invoke("Click");

        if (GameplayController.HasInstance)
        {
            GameplayController.Instance.StartLevelMode(levelNum);
        }

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnLevelStarsUpdated(int levelNum, int stars)
    {
        if (spawnedNodes.TryGetValue(levelNum, out GameObject node) && node != null)
        {
            int highestUnlocked = GetHighestUnlockedLevel();
            bool isUnlocked = levelNum <= highestUnlocked;
            bool isCurrent = levelNum == highestUnlocked;
            ConfigurePrefabNode(node, levelNum, isUnlocked, isCurrent, stars);
        }
    }

    private int GetHighestUnlockedLevel()
    {
        if (PlayerDataManager.HasInstance)
        {
            return PlayerDataManager.Instance.HighestUnlockedLevel;
        }
        return 1;
    }

    private int GetLevelStars(int levelNum)
    {
        if (PlayerDataManager.HasInstance)
        {
            return PlayerDataManager.Instance.GetLevelStars(levelNum);
        }
        return 0;
    }

    #region Navigation & Smooth Scrolling

    /// <summary>
    /// Smoothly scrolls to the player's current unlocked level.
    /// </summary>
    [ContextMenu("Scroll To Current Level")]
    public void ScrollToCurrentLevel()
    {
        ScrollToLevel(GetHighestUnlockedLevel(), 0.5f);
    }

    /// <summary>
    /// Smoothly scrolls to the top of the map (highest level).
    /// </summary>
    [ContextMenu("Scroll To Top")]
    public void ScrollToTop()
    {
        ScrollToLevel(totalLevels, 0.6f);
    }

    /// <summary>
    /// Smoothly scrolls to the bottom of the map (Level 1).
    /// </summary>
    [ContextMenu("Scroll To Bottom")]
    public void ScrollToBottom()
    {
        ScrollToLevel(1, 0.5f);
    }

    /// <summary>
    /// Smoothly scrolls to any level number (1..totalLevels).
    /// Automatically ensures levels up to target are loaded before scrolling.
    /// </summary>
    public void ScrollToLevel(int targetLevel, float duration = 0.5f)
    {
        targetLevel = Mathf.Clamp(targetLevel, 1, totalLevels);

        // Ensure levels up to target are loaded
        if (targetLevel > currentlyLoadedLevels)
        {
            LoadLevelsUpTo(targetLevel);
        }

        if (activeScrollRoutine != null)
        {
            StopCoroutine(activeScrollRoutine);
            activeScrollRoutine = null;
        }

        if (!gameObject.activeInHierarchy) return;
        activeScrollRoutine = StartCoroutine(SmoothScrollRoutine(targetLevel, duration));
    }

    private IEnumerator SmoothScrollRoutine(int targetLevel, float duration)
    {
        if (scrollRect == null || contentRect == null) yield break;

        yield return null; // Wait for layout updates
        Canvas.ForceUpdateCanvases();

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (scrollRect.transform as RectTransform);
        float viewportHeight = viewport != null ? viewport.rect.height : 900f;
        float maxScrollHeight = contentRect.sizeDelta.y - viewportHeight;
        if (maxScrollHeight <= 0)
        {
            activeScrollRoutine = null;
            yield break;
        }

        float targetY = cachedCumulativeY.TryGetValue(targetLevel, out float y) ? y : (startPadding + (targetLevel - 1) * stepY);
        // Center the level in the viewport
        float viewportHalf = viewportHeight * 0.5f;
        float desiredOffset = Mathf.Clamp(targetY - viewportHalf, 0f, maxScrollHeight);
        float targetNormalized = Mathf.Clamp01(desiredOffset / maxScrollHeight);

        float finalNormalized = (scrollDirection == ScrollDirection.BottomToTop) ? targetNormalized : (1f - targetNormalized);

        if (duration <= 0.05f)
        {
            scrollRect.verticalNormalizedPosition = finalNormalized;
            activeScrollRoutine = null;
            yield break;
        }

        float startNormalized = scrollRect.verticalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startNormalized, finalNormalized, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = finalNormalized;
        activeScrollRoutine = null;
    }

    #endregion
}
