using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and manages all destructible blocks and static ledges for a level,
/// listens for ball merge events to apply proximity shockwave damage,
/// and tracks remaining clear targets.
/// </summary>
public class LevelBlockManager : Singleton<LevelBlockManager>
{
    private const float REFERENCE_FLOOR_Y = -4f;

    [Header("Configuration")]
    [SerializeField] private BlockLibrarySO blockLibrary;
    [SerializeField] private Transform blocksParent;

    [Header("Live Level State")]
    [SerializeField] private int totalRequiredBlocks = 0;
    [SerializeField] private int remainingRequiredBlocks = 0;

    private readonly List<DestructibleBlock> activeBlocks = new List<DestructibleBlock>();
    private LevelDataSO activeLevelData;

    public int TotalRequiredBlocks => totalRequiredBlocks;
    public int RemainingRequiredBlocks => remainingRequiredBlocks;
    public IReadOnlyList<DestructibleBlock> ActiveBlocks => activeBlocks;

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
        if (blockLibrary == null)
        {
            blockLibrary = BlockLibrarySO.LoadDefault();
        }
    }

    private void OnEnable()
    {
        GameEvents.OnBallsMerged += HandleBallsMerged;
    }

    private void OnDisable()
    {
        GameEvents.OnBallsMerged -= HandleBallsMerged;
    }

    public void LoadLevelBlocks(LevelDataSO levelData, ContainerBoundary container = null)
    {
        ClearLevelBlocks();
        activeLevelData = levelData;

        if (levelData == null || levelData.blocks == null || levelData.blocks.Count == 0)
        {
            totalRequiredBlocks = 0;
            remainingRequiredBlocks = 0;
            GameEvents.OnBlocksRemainingChanged?.Invoke(0, 0);
            return;
        }

        if (blockLibrary == null)
        {
            blockLibrary = BlockLibrarySO.LoadDefault();
        }

        Transform parentTransform = EnsureBlocksParent(container);
        Vector3 containerOrigin = container != null ? container.transform.position : Vector3.zero;
        float floorDeltaY = container != null ? (container.FloorY - REFERENCE_FLOOR_Y) : 0f;

        for (int i = 0; i < levelData.blocks.Count; i++)
        {
            LevelBlockPlacement placement = levelData.blocks[i];
            if (placement == null) continue;

            BlockDefinitionSO def = placement.ResolveDefinition(blockLibrary);
            Vector3 worldPos = containerOrigin + new Vector3(placement.position.x, placement.position.y + floorDeltaY, 0f);
            Quaternion rot = Quaternion.Euler(0f, 0f, placement.rotation);

            DestructibleBlock block = CreateBlockInstance(def, placement, worldPos, rot, parentTransform);
            if (block != null)
            {
                block.Initialize(placement, def, levelData);
                activeBlocks.Add(block);

                if (block.RequiredToClear)
                {
                    totalRequiredBlocks++;
                }
            }
        }

        remainingRequiredBlocks = totalRequiredBlocks;
        GameEvents.OnBlocksRemainingChanged?.Invoke(remainingRequiredBlocks, totalRequiredBlocks);
    }

    public void ClearLevelBlocks()
    {
        for (int i = 0; i < activeBlocks.Count; i++)
        {
            if (activeBlocks[i] != null)
            {
                DestroySafe(activeBlocks[i].gameObject);
            }
        }
        activeBlocks.Clear();

        if (blocksParent != null)
        {
            for (int i = blocksParent.childCount - 1; i >= 0; i--)
            {
                DestroySafe(blocksParent.GetChild(i).gameObject);
            }
        }

        totalRequiredBlocks = 0;
        remainingRequiredBlocks = 0;
    }

    private DestructibleBlock CreateBlockInstance(BlockDefinitionSO def, LevelBlockPlacement placement, Vector3 worldPos, Quaternion rot, Transform parent)
    {
        GameObject obj;
        if (def != null && def.customPrefab != null)
        {
            obj = Instantiate(def.customPrefab, worldPos, rot, parent);
        }
        else
        {
            string label = def != null ? def.displayName : (!string.IsNullOrEmpty(placement.blockId) ? placement.blockId : "Block");
            obj = new GameObject($"Block_{label}");
            obj.transform.SetParent(parent, false);
            obj.transform.position = worldPos;
            obj.transform.rotation = rot;
        }

        DestructibleBlock block = obj.GetComponent<DestructibleBlock>();
        if (block == null)
        {
            block = obj.AddComponent<DestructibleBlock>();
        }
        return block;
    }

    private Transform EnsureBlocksParent(ContainerBoundary container)
    {
        if (blocksParent != null) return blocksParent;

        string parentName = "[LevelBlocks]";
        GameObject existing = GameObject.Find(parentName);
        if (existing != null)
        {
            blocksParent = existing.transform;
            return blocksParent;
        }

        GameObject holder = new GameObject(parentName);
        if (container != null)
        {
            holder.transform.SetParent(container.transform, false);
        }
        else
        {
            holder.transform.SetParent(transform, false);
        }
        blocksParent = holder.transform;
        return blocksParent;
    }

    private void HandleBallsMerged(Vector3 midpoint, float mergedBallRadius, int mergedTier)
    {
        if (activeLevelData == null || activeBlocks.Count == 0) return;

        float baseRadius = activeLevelData.mergeBlastRadius > 0.1f ? activeLevelData.mergeBlastRadius : 1.15f;
        float effectiveRadius = Mathf.Max(baseRadius, mergedBallRadius * 1.55f + 0.35f);

        int damage = Mathf.Max(1, activeLevelData.mergeBaseDamage);
        if (activeLevelData.scaleDamageWithTier)
        {
            damage += mergedTier / 2;
        }

        // Visual shockwave ring showing merge blast reach
        BlockRuntimeVisuals.SpawnShockwaveRing(midpoint, effectiveRadius, new Color(1f, 0.85f, 0.25f, 0.78f), 0.28f);

        // Snapshot active blocks so chain explosions can safely modify activeBlocks
        List<DestructibleBlock> snapshot = new List<DestructibleBlock>(activeBlocks);
        for (int i = 0; i < snapshot.Count; i++)
        {
            DestructibleBlock block = snapshot[i];
            if (block != null && !block.IsDestroyed && block.CanTakeDamageFromMerge(midpoint, effectiveRadius))
            {
                block.TakeDamage(damage, midpoint);
            }
        }
    }

    public void TriggerBlockExplosion(Vector3 center, float radius, int damage, DestructibleBlock sourceBlock)
    {
        BlockRuntimeVisuals.SpawnShockwaveRing(center, radius, new Color(1f, 0.35f, 0.15f, 0.9f), 0.35f);

        List<DestructibleBlock> snapshot = new List<DestructibleBlock>(activeBlocks);
        for (int i = 0; i < snapshot.Count; i++)
        {
            DestructibleBlock block = snapshot[i];
            if (block == null || block == sourceBlock || block.IsDestroyed || !block.IsDestructible) continue;
            if (block.Collider == null) continue;

            Vector2 closest = block.Collider.ClosestPoint(center);
            if (Vector2.Distance(closest, center) <= radius)
            {
                block.TakeDamage(damage, center, isExplosion: true);
            }
        }
    }

    public void TriggerKeyUnlock(Vector3 keyWorldPos, int channel)
    {
        bool unlockedAny = false;
        List<DestructibleBlock> snapshot = new List<DestructibleBlock>(activeBlocks);
        for (int i = 0; i < snapshot.Count; i++)
        {
            DestructibleBlock block = snapshot[i];
            if (block == null || block.IsDestroyed || !block.IsLocked) continue;

            if (channel == 0 || block.LockChannel == 0 || block.LockChannel == channel)
            {
                unlockedAny = true;
                DestructibleBlock targetBlock = block;
                BlockRuntimeVisuals.SpawnFlyingKeyOrb(
                    keyWorldPos,
                    targetBlock.transform.position,
                    new Color(1f, 0.88f, 0.25f, 1f),
                    () =>
                    {
                        if (targetBlock != null && !targetBlock.IsDestroyed)
                        {
                            targetBlock.Unlock();
                        }
                    }
                );
            }
        }

        if (unlockedAny)
        {
            GameEvents.OnPlaySFX?.Invoke("KeyUnlock");
        }
    }

    public void NotifyBlockDestroyed(DestructibleBlock block)
    {
        activeBlocks.Remove(block);

        if (block != null && block.RequiredToClear)
        {
            remainingRequiredBlocks = Mathf.Max(0, remainingRequiredBlocks - 1);
            GameEvents.OnBlocksRemainingChanged?.Invoke(remainingRequiredBlocks, totalRequiredBlocks);

            if (GameplayController.HasInstance)
            {
                GameplayController.Instance.NotifyBlockProgressChanged();
            }
        }
    }

    private static void DestroySafe(GameObject obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
