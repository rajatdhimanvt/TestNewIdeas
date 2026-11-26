using System;
using UnityEngine;

/// <summary>
/// Serializable data entry representing one placed block or static shelf inside a LevelDataSO.
/// Coordinates are stored in container space (X=0 is horizontal center, Y=-4.0 is default floor).
/// </summary>
[Serializable]
public class LevelBlockPlacement
{
    [Tooltip("Unique instance ID for selection and undo tracking")]
    public string instanceGuid;

    [Tooltip("Block type ID used as fallback lookup when importing JSON")]
    public string blockId;

    [Tooltip("Direct reference to the BlockDefinitionSO template")]
    public BlockDefinitionSO blockDefinition;

    [Header("Transform")]
    public Vector2 position = Vector2.zero;
    public float rotation = 0f;
    public Vector2 size = new Vector2(0.85f, 0.85f);

    [Header("Overrides")]
    [Tooltip("Set > 0 to override the block template's default HP for this placement")]
    public int hpOverride = 0;

    public bool overrideRequiredToClear = false;
    public bool requiredToClear = true;

    [Tooltip("Optional: spawns a thin static support ledge directly underneath this block")]
    public bool addSupportShelf = false;

    public bool useCustomColor = false;
    public Color customColor = Color.white;

    [Header("Special Behavior Overrides")]
    public bool overrideMovement = false;
    public Vector2 moveOffset = new Vector2(1.8f, 0f);
    public float moveSpeed = 1.4f;

    [Tooltip("Set > 0 to override lock/key channel; 0 uses template default")]
    public int lockChannelOverride = 0;

    [Tooltip("Set > 0 to override magnet pull radius; 0 uses template default")]
    public float magnetRadiusOverride = 0f;
    public float magnetForceOverride = 0f;

    public void EnsureGuid()
    {
        if (string.IsNullOrEmpty(instanceGuid))
        {
            instanceGuid = Guid.NewGuid().ToString("N");
        }
    }

    public BlockDefinitionSO ResolveDefinition(BlockLibrarySO library = null)
    {
        if (blockDefinition != null) return blockDefinition;
        BlockLibrarySO lib = library != null ? library : BlockLibrarySO.LoadDefault();
        if (lib != null && !string.IsNullOrEmpty(blockId))
        {
            blockDefinition = lib.GetById(blockId);
        }
        return blockDefinition;
    }

    public int GetEffectiveHP(BlockDefinitionSO resolvedDef = null)
    {
        BlockDefinitionSO def = resolvedDef != null ? resolvedDef : ResolveDefinition();
        if (def != null && !def.isDestructible) return 0;
        if (hpOverride > 0) return hpOverride;
        return def != null ? Mathf.Max(1, def.defaultHP) : 1;
    }

    public bool IsRequiredToClear(BlockDefinitionSO resolvedDef = null)
    {
        BlockDefinitionSO def = resolvedDef != null ? resolvedDef : ResolveDefinition();
        if (def != null && !def.isDestructible) return false;
        if (overrideRequiredToClear) return requiredToClear;
        return def != null ? def.requiredToClear : true;
    }

    public bool IsDestructible(BlockDefinitionSO resolvedDef = null)
    {
        BlockDefinitionSO def = resolvedDef != null ? resolvedDef : ResolveDefinition();
        return def == null || def.isDestructible;
    }

    public Vector2 GetEffectiveMoveOffset(BlockDefinitionSO resolvedDef = null)
    {
        if (overrideMovement) return moveOffset;
        BlockDefinitionSO def = resolvedDef != null ? resolvedDef : ResolveDefinition();
        return def != null ? def.defaultMoveOffset : moveOffset;
    }

    public float GetEffectiveMoveSpeed(BlockDefinitionSO resolvedDef = null)
    {
        if (overrideMovement) return Mathf.Max(0.1f, moveSpeed);
        BlockDefinitionSO def = resolvedDef != null ? resolvedDef : ResolveDefinition();
        return def != null ? Mathf.Max(0.1f, def.defaultMoveSpeed) : Mathf.Max(0.1f, moveSpeed);
    }

    public int GetEffectiveLockChannel(BlockDefinitionSO resolvedDef = null)
    {
        if (lockChannelOverride > 0) return lockChannelOverride;
        BlockDefinitionSO def = resolvedDef != null ? resolvedDef : ResolveDefinition();
        return def != null ? def.defaultLockChannel : 1;
    }

    public LevelBlockPlacement Clone(bool newGuid = true)
    {
        return new LevelBlockPlacement
        {
            instanceGuid = newGuid ? Guid.NewGuid().ToString("N") : instanceGuid,
            blockId = blockId,
            blockDefinition = blockDefinition,
            position = position,
            rotation = rotation,
            size = size,
            hpOverride = hpOverride,
            overrideRequiredToClear = overrideRequiredToClear,
            requiredToClear = requiredToClear,
            addSupportShelf = addSupportShelf,
            useCustomColor = useCustomColor,
            customColor = customColor,
            overrideMovement = overrideMovement,
            moveOffset = moveOffset,
            moveSpeed = moveSpeed,
            lockChannelOverride = lockChannelOverride,
            magnetRadiusOverride = magnetRadiusOverride,
            magnetForceOverride = magnetForceOverride
        };
    }
}
