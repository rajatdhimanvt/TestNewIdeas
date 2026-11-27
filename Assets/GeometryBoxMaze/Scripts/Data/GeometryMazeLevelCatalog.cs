using System.Collections.Generic;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Ordered catalog of all Geometry Box Tilt Maze levels stored under Assets/GeometryBoxMaze/Data.
    /// </summary>
    [CreateAssetMenu(fileName = "GeometryMazeLevelCatalog", menuName = "Geometry Box Maze/Level Catalog")]
    public class GeometryMazeLevelCatalog : ScriptableObject
    {
        public List<GeometryMazeLevelData> levels = new List<GeometryMazeLevelData>();

        public int LevelCount => levels != null ? levels.Count : 0;

        public GeometryMazeLevelData GetLevelByIndex(int index)
        {
            if (levels == null || levels.Count == 0) return null;
            int clamped = Mathf.Clamp(index, 0, levels.Count - 1);
            return levels[clamped];
        }
    }
}
