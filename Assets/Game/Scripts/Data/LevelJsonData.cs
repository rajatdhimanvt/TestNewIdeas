using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JSON Data Transfer Objects for exporting and importing LevelDataSO configurations.
/// Uses Unity's built-in JsonUtility with zero external dependencies.
/// </summary>
[Serializable]
public class LevelJsonData
{
    public int formatVersion = 1;
    public int levelNumber = 1;
    public string levelName = "Level 1";
    public int gameMode = (int)GameMode.Level;
    public int winCondition = (int)LevelWinCondition.DestroyAllBlocks;
    public int targetScore = 500;
    public int maxDrops = 25;

    public float dropCooldown = 0.5f;
    public float dropHeight = 3.8f;
    public float containerWidth = 5.5f;
    public float containerHeight = 8.5f;
    public float wallThickness = 0.5f;
    public float dangerLineY = 2.5f;
    public float dangerTimeLimit = 3.0f;

    public float mergeBlastRadius = 1.15f;
    public int mergeBaseDamage = 1;
    public bool scaleDamageWithTier = false;

    public List<BlockJsonData> blocks = new List<BlockJsonData>();

    public static string ToJson(LevelDataSO level, bool prettyPrint = true)
    {
        if (level == null) return string.Empty;

        LevelJsonData dto = new LevelJsonData
        {
            levelNumber = level.levelNumber,
            levelName = level.levelName,
            gameMode = (int)level.gameMode,
            winCondition = (int)level.winCondition,
            targetScore = level.targetScore,
            maxDrops = level.maxDrops,
            dropCooldown = level.dropCooldown,
            dropHeight = level.dropHeight,
            containerWidth = level.containerWidth,
            containerHeight = level.containerHeight,
            wallThickness = level.wallThickness,
            dangerLineY = level.dangerLineY,
            dangerTimeLimit = level.dangerTimeLimit,
            mergeBlastRadius = level.mergeBlastRadius,
            mergeBaseDamage = level.mergeBaseDamage,
            scaleDamageWithTier = level.scaleDamageWithTier,
            blocks = new List<BlockJsonData>()
        };

        if (level.blocks != null)
        {
            for (int i = 0; i < level.blocks.Count; i++)
            {
                LevelBlockPlacement p = level.blocks[i];
                if (p == null) continue;

                BlockDefinitionSO def = p.ResolveDefinition();
                dto.blocks.Add(new BlockJsonData
                {
                    instanceGuid = p.instanceGuid,
                    blockId = !string.IsNullOrEmpty(p.blockId) ? p.blockId : (def != null ? def.blockId : "crate_normal"),
                    posX = p.position.x,
                    posY = p.position.y,
                    rotation = p.rotation,
                    sizeX = p.size.x,
                    sizeY = p.size.y,
                    hpOverride = p.hpOverride,
                    overrideRequiredToClear = p.overrideRequiredToClear,
                    requiredToClear = p.requiredToClear,
                    addSupportShelf = p.addSupportShelf,
                    useCustomColor = p.useCustomColor,
                    customColorHex = "#" + ColorUtility.ToHtmlStringRGBA(p.customColor),
                    overrideMovement = p.overrideMovement,
                    moveOffsetX = p.moveOffset.x,
                    moveOffsetY = p.moveOffset.y,
                    moveSpeed = p.moveSpeed,
                    lockChannelOverride = p.lockChannelOverride,
                    magnetRadiusOverride = p.magnetRadiusOverride,
                    magnetForceOverride = p.magnetForceOverride
                });
            }
        }

        return JsonUtility.ToJson(dto, prettyPrint);
    }

    public static bool ApplyJsonToLevel(string json, LevelDataSO targetLevel, BlockLibrarySO library = null)
    {
        if (string.IsNullOrEmpty(json) || targetLevel == null) return false;

        LevelJsonData dto = JsonUtility.FromJson<LevelJsonData>(json);
        if (dto == null) return false;

        BlockLibrarySO lib = library != null ? library : BlockLibrarySO.LoadDefault();

        targetLevel.levelNumber = dto.levelNumber;
        targetLevel.levelName = dto.levelName;
        targetLevel.gameMode = (GameMode)dto.gameMode;
        targetLevel.winCondition = (LevelWinCondition)dto.winCondition;
        targetLevel.targetScore = dto.targetScore;
        targetLevel.maxDrops = dto.maxDrops;
        targetLevel.dropCooldown = dto.dropCooldown;
        targetLevel.dropHeight = dto.dropHeight;
        targetLevel.containerWidth = dto.containerWidth;
        targetLevel.containerHeight = dto.containerHeight;
        targetLevel.wallThickness = dto.wallThickness;
        targetLevel.dangerLineY = dto.dangerLineY;
        targetLevel.dangerTimeLimit = dto.dangerTimeLimit;
        targetLevel.mergeBlastRadius = dto.mergeBlastRadius > 0.1f ? dto.mergeBlastRadius : 1.15f;
        targetLevel.mergeBaseDamage = Mathf.Max(1, dto.mergeBaseDamage);
        targetLevel.scaleDamageWithTier = dto.scaleDamageWithTier;

        if (targetLevel.blocks == null)
        {
            targetLevel.blocks = new List<LevelBlockPlacement>();
        }
        else
        {
            targetLevel.blocks.Clear();
        }

        if (dto.blocks != null)
        {
            for (int i = 0; i < dto.blocks.Count; i++)
            {
                BlockJsonData b = dto.blocks[i];
                if (b == null) continue;

                BlockDefinitionSO def = lib != null ? lib.GetById(b.blockId) : null;
                Color parsedColor = Color.white;
                if (!string.IsNullOrEmpty(b.customColorHex))
                {
                    ColorUtility.TryParseHtmlString(b.customColorHex, out parsedColor);
                }

                LevelBlockPlacement placement = new LevelBlockPlacement
                {
                    instanceGuid = string.IsNullOrEmpty(b.instanceGuid) ? Guid.NewGuid().ToString("N") : b.instanceGuid,
                    blockId = b.blockId,
                    blockDefinition = def,
                    position = new Vector2(b.posX, b.posY),
                    rotation = b.rotation,
                    size = new Vector2(Mathf.Max(0.1f, b.sizeX), Mathf.Max(0.1f, b.sizeY)),
                    hpOverride = b.hpOverride,
                    overrideRequiredToClear = b.overrideRequiredToClear,
                    requiredToClear = b.requiredToClear,
                    addSupportShelf = b.addSupportShelf,
                    useCustomColor = b.useCustomColor,
                    customColor = parsedColor,
                    overrideMovement = b.overrideMovement,
                    moveOffset = new Vector2(b.moveOffsetX, b.moveOffsetY),
                    moveSpeed = b.moveSpeed > 0.05f ? b.moveSpeed : 1.4f,
                    lockChannelOverride = b.lockChannelOverride,
                    magnetRadiusOverride = b.magnetRadiusOverride,
                    magnetForceOverride = b.magnetForceOverride
                };
                targetLevel.blocks.Add(placement);
            }
        }

        return true;
    }
}

[Serializable]
public class BlockJsonData
{
    public string instanceGuid;
    public string blockId;
    public float posX;
    public float posY;
    public float rotation;
    public float sizeX;
    public float sizeY;
    public int hpOverride;
    public bool overrideRequiredToClear;
    public bool requiredToClear;
    public bool addSupportShelf;
    public bool useCustomColor;
    public string customColorHex;
    public bool overrideMovement;
    public float moveOffsetX;
    public float moveOffsetY;
    public float moveSpeed;
    public int lockChannelOverride;
    public float magnetRadiusOverride;
    public float magnetForceOverride;
}
