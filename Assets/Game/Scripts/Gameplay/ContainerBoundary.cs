using UnityEngine;

/// <summary>
/// Generates and manages the 3 boundary colliders (Left Wall, Right Wall, Bottom Floor)
/// forming the physical container for falling and settling balls.
/// </summary>
public class ContainerBoundary : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LevelDataSO levelData;
    [SerializeField] private float floorY = -4f;
    [SerializeField] private Color wallColor = new Color(0.2f, 0.6f, 1f, 0.8f);

    [Header("Collider References")]
    [SerializeField] private BoxCollider2D leftWallCollider;
    [SerializeField] private BoxCollider2D rightWallCollider;
    [SerializeField] private BoxCollider2D floorCollider;

    public float FloorY => floorY;
    public float ContainerWidth => levelData != null ? levelData.containerWidth : 5.5f;
    public float ContainerHeight => levelData != null ? levelData.containerHeight : 8.5f;
    public float WallThickness => levelData != null ? levelData.wallThickness : 0.5f;

    private void Awake()
    {
        BuildContainer();
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged(ThemeDataSO newTheme)
    {
        if (newTheme != null)
        {
            SetWallColor(newTheme.wallColor);
        }
    }

    public void SetWallColor(Color color)
    {
        wallColor = color;
        ApplyWallColor(floorCollider);
        ApplyWallColor(leftWallCollider);
        ApplyWallColor(rightWallCollider);
    }

    private void ApplyWallColor(BoxCollider2D col)
    {
        if (col != null)
        {
            SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = wallColor;
            }
        }
    }

    public void Setup(LevelDataSO data)
    {
        levelData = data;
        BuildContainer();

        if (ThemeManager.HasInstance && ThemeManager.Instance.CurrentEquippedTheme != null)
        {
            SetWallColor(ThemeManager.Instance.CurrentEquippedTheme.wallColor);
        }
    }

    /// <summary>
    /// Constructs or resizes the 3 colliders to match level data dimensions.
    /// </summary>
    [ContextMenu("Rebuild Container")]
    public void BuildContainer()
    {
        float width = levelData != null ? levelData.containerWidth : 5.5f;
        float height = levelData != null ? levelData.containerHeight : 8.5f;
        float thickness = levelData != null ? levelData.wallThickness : 0.5f;

        float halfWidth = width * 0.5f;
        float wallCenterY = floorY + (height * 0.5f);

        // 1. Bottom Floor
        if (floorCollider == null)
        {
            floorCollider = CreateWallSegment("Floor_Collider");
        }
        ConfigureSegment(floorCollider, new Vector2(width + (thickness * 2f), thickness), new Vector3(0f, floorY - (thickness * 0.5f), 0f));

        // 2. Left Wall
        if (leftWallCollider == null)
        {
            leftWallCollider = CreateWallSegment("LeftWall_Collider");
        }
        ConfigureSegment(leftWallCollider, new Vector2(thickness, height), new Vector3(-halfWidth - (thickness * 0.5f), wallCenterY, 0f));

        // 3. Right Wall
        if (rightWallCollider == null)
        {
            rightWallCollider = CreateWallSegment("RightWall_Collider");
        }
        ConfigureSegment(rightWallCollider, new Vector2(thickness, height), new Vector3(halfWidth + (thickness * 0.5f), wallCenterY, 0f));
    }

    private void ConfigureSegment(BoxCollider2D col, Vector2 worldSize, Vector3 localPos)
    {
        if (col == null) return;
        // Keep BoxCollider2D size at (1,1) so localScale controls both collider and 1x1 sprite uniformly
        col.size = Vector2.one;
        col.transform.localPosition = localPos;
        col.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);

        SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            sr.sprite = GetOrCreateBoxSprite();
        }
    }

    private BoxCollider2D CreateWallSegment(string segmentName)
    {
        Transform existing = transform.Find(segmentName);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(segmentName);
        obj.transform.SetParent(transform);

        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = obj.AddComponent<BoxCollider2D>();
        }

        // Add visual representation using a simple white sprite
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateBoxSprite();
            sr.color = wallColor;
        }

        return col;
    }

    private static Sprite boxSprite;
    private static Sprite GetOrCreateBoxSprite()
    {
        if (boxSprite != null) return boxSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        boxSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return boxSprite;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        float width = ContainerWidth;
        float height = ContainerHeight;
        Vector3 center = transform.position + new Vector3(0f, floorY + height * 0.5f, 0f);
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.45f);
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0f));
    }
#endif
}
