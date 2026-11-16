using System.Collections;
using System.Collections.Generic;
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
    private Coroutine activeScrollRoutine;

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

        InitializeMap();
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

        if (autoScrollToCurrentLevel)
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

        // Outer Glow / Ring
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(nodeObj.transform, false);
        RectTransform ringRt = ringObj.AddComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.sizeDelta = new Vector2(16f, 16f);
        Image ringImg = ringObj.AddComponent<Image>();
        if (nodeBackgroundSprite != null) ringImg.sprite = nodeBackgroundSprite;
        ringImg.color = isCurrent ? new Color(1f, 0.9f, 0.35f, 0.9f) : new Color(0f, 0f, 0f, 0.32f);
        ringImg.raycastTarget = false;

        // Main Button Background
        Image bgImg = nodeObj.AddComponent<Image>();
        if (nodeBackgroundSprite != null) bgImg.sprite = nodeBackgroundSprite;
        if (isCurrent)
            bgImg.color = currentLevelColor;
        else if (isUnlocked)
            bgImg.color = unlockedColor;
        else
            bgImg.color = lockedColor;

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
        textRt.anchoredPosition = new Vector2(0, isUnlocked ? 8f : 0f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = isUnlocked ? levelNum.ToString() : "🔒";
        tmp.fontSize = isUnlocked ? (levelNum >= 100 ? 36 : 42) : 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // Stars display (for unlocked levels)
        if (isUnlocked)
        {
            GameObject starsObj = new GameObject("Stars");
            starsObj.transform.SetParent(nodeObj.transform, false);
            RectTransform starsRt = starsObj.AddComponent<RectTransform>();
            starsRt.anchorMin = new Vector2(0.5f, 0f);
            starsRt.anchorMax = new Vector2(0.5f, 0f);
            starsRt.pivot = new Vector2(0.5f, 0f);
            starsRt.anchoredPosition = new Vector2(0, 10f);
            starsRt.sizeDelta = new Vector2(110f, 24f);

            TextMeshProUGUI starsTmp = starsObj.AddComponent<TextMeshProUGUI>();
            string starStr = "";
            for (int s = 0; s < 3; s++)
            {
                starStr += s < stars ? "★" : "☆";
            }
            starsTmp.text = starStr;
            starsTmp.fontSize = 18;
            starsTmp.alignment = TextAlignmentOptions.Center;
            starsTmp.color = new Color(1f, 0.9f, 0.2f);
            starsTmp.raycastTarget = false;
        }

        return nodeObj;
    }

    private void ConfigurePrefabNode(GameObject nodeObj, int levelNum, bool isUnlocked, bool isCurrent, int stars)
    {
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
            string lower = t.gameObject.name.ToLower();
            if (lower.Contains("number") || lower.Contains("level") || lower.Contains("label") || texts.Length == 1)
            {
                t.text = isUnlocked ? levelNum.ToString() : "🔒";
            }
            else if (lower.Contains("star"))
            {
                string starStr = "";
                for (int s = 0; s < 3; s++) starStr += s < stars ? "★" : "☆";
                t.text = isUnlocked ? starStr : "";
            }
        }
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
            if (pathDotSprite != null) img.sprite = pathDotSprite;
            img.color = pathDotColor;
            img.raycastTarget = false;
        }
    }

    private void OnLevelClicked(int levelNum)
    {
        Debug.Log($"[LevelMapController] Selected Level: {levelNum}");

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
        }

        activeScrollRoutine = StartCoroutine(SmoothScrollRoutine(targetLevel, duration));
    }

    private IEnumerator SmoothScrollRoutine(int targetLevel, float duration)
    {
        if (scrollRect == null || contentRect == null) yield break;

        yield return null; // Wait for layout updates
        Canvas.ForceUpdateCanvases();

        float maxScrollHeight = contentRect.sizeDelta.y - scrollRect.viewport.rect.height;
        if (maxScrollHeight <= 0) yield break;

        float targetY = cachedCumulativeY.TryGetValue(targetLevel, out float y) ? y : (startPadding + (targetLevel - 1) * stepY);
        // Center the level in the viewport
        float viewportHalf = scrollRect.viewport.rect.height * 0.5f;
        float desiredOffset = Mathf.Clamp(targetY - viewportHalf, 0f, maxScrollHeight);
        float targetNormalized = Mathf.Clamp01(desiredOffset / maxScrollHeight);

        float finalNormalized = (scrollDirection == ScrollDirection.BottomToTop) ? targetNormalized : (1f - targetNormalized);

        if (duration <= 0.05f)
        {
            scrollRect.verticalNormalizedPosition = finalNormalized;
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
