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

    private void Awake()
    {
        BuildContainer();
    }

    public void Setup(LevelDataSO data)
    {
        levelData = data;
        BuildContainer();
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
        floorCollider.size = new Vector2(width + (thickness * 2f), thickness);
        floorCollider.transform.localPosition = new Vector3(0f, floorY - (thickness * 0.5f), 0f);

        // 2. Left Wall
        if (leftWallCollider == null)
        {
            leftWallCollider = CreateWallSegment("LeftWall_Collider");
        }
        leftWallCollider.size = new Vector2(thickness, height);
        leftWallCollider.transform.localPosition = new Vector3(-halfWidth - (thickness * 0.5f), wallCenterY, 0f);

        // 3. Right Wall
        if (rightWallCollider == null)
        {
            rightWallCollider = CreateWallSegment("RightWall_Collider");
        }
        rightWallCollider.size = new Vector2(thickness, height);
        rightWallCollider.transform.localPosition = new Vector3(halfWidth + (thickness * 0.5f), wallCenterY, 0f);
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

    private void Update()
    {
        // Keep sprite visual transform scaled to box collider size
        UpdateWallVisual(floorCollider);
        UpdateWallVisual(leftWallCollider);
        UpdateWallVisual(rightWallCollider);
    }

    private void UpdateWallVisual(BoxCollider2D col)
    {
        if (col != null)
        {
            col.transform.localScale = new Vector3(col.size.x, col.size.y, 1f);
        }
    }
}
