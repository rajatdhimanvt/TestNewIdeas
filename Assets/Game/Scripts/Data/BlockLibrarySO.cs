using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry of all BlockDefinitionSO templates available to the Visual Level Editor
/// and runtime JSON level loader.
/// </summary>
[CreateAssetMenu(fileName = "BlockLibrary", menuName = "BallMerge/Block Library")]
public class BlockLibrarySO : ScriptableObject
{
    public const string RESOURCE_PATH = "BallMerge/BlockLibrary";

    [Header("Registered Block Templates")]
    public List<BlockDefinitionSO> blocks = new List<BlockDefinitionSO>();

    private static BlockLibrarySO cachedDefault;

    public static BlockLibrarySO LoadDefault()
    {
        if (cachedDefault == null)
        {
            cachedDefault = Resources.Load<BlockLibrarySO>(RESOURCE_PATH);
        }
        return cachedDefault;
    }

    public static void ClearCache()
    {
        cachedDefault = null;
    }

    public BlockDefinitionSO GetById(string blockId)
    {
        if (string.IsNullOrEmpty(blockId) || blocks == null) return null;

        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] != null && blocks[i].blockId == blockId)
            {
                return blocks[i];
            }
        }
        return null;
    }

    public BlockDefinitionSO GetDefaultBlock()
    {
        if (blocks == null) return null;
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] != null) return blocks[i];
        }
        return null;
    }
}
