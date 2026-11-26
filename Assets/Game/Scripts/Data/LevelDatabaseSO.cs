using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime catalog of all campaign LevelDataSO assets, ordered by level number.
/// Stored in Resources/BallMerge/LevelDatabase for mobile build compatibility.
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "BallMerge/Level Database")]
public class LevelDatabaseSO : ScriptableObject
{
    public const string RESOURCE_PATH = "BallMerge/LevelDatabase";

    [Header("Campaign Levels")]
    public List<LevelDataSO> levels = new List<LevelDataSO>();

    private static LevelDatabaseSO cachedDefault;

    public static LevelDatabaseSO LoadDefault()
    {
        if (cachedDefault == null)
        {
            cachedDefault = Resources.Load<LevelDatabaseSO>(RESOURCE_PATH);
        }
        return cachedDefault;
    }

    public static void ClearCache()
    {
        cachedDefault = null;
    }

    public LevelDataSO GetLevel(int levelNumber)
    {
        if (levels == null) return null;

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].levelNumber == levelNumber)
            {
                return levels[i];
            }
        }

        // Fallback by 1-based index
        int idx = levelNumber - 1;
        if (idx >= 0 && idx < levels.Count)
        {
            return levels[idx];
        }

        return null;
    }

    public int TotalLevels => levels != null ? levels.Count : 0;

    public void SortByLevelNumber()
    {
        if (levels == null) return;
        levels.RemoveAll(l => l == null);
        levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
    }
}
