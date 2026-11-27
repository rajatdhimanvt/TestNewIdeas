using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryBoxMaze
{
    [Serializable]
    public struct MazeWallDefinition
    {
        public Vector2 position;
        public Vector2 size;
        public float rotationDeg;

        public MazeWallDefinition(Vector2 position, Vector2 size, float rotationDeg = 0f)
        {
            this.position = position;
            this.size = size;
            this.rotationDeg = rotationDeg;
        }
    }

    [Serializable]
    public struct MazeHoleDefinition
    {
        public Vector2 position;
        public float radius;

        public MazeHoleDefinition(Vector2 position, float radius = 0.42f)
        {
            this.position = position;
            this.radius = radius;
        }
    }

    [Serializable]
    public struct MazeObstacleDefinition
    {
        public bool isSpinner;
        public Vector2 startPosition;
        public Vector2 endPosition;
        public Vector2 size;
        public float speed;

        public static MazeObstacleDefinition CreateSlider(Vector2 start, Vector2 end, Vector2 size, float speed = 1.6f)
        {
            return new MazeObstacleDefinition
            {
                isSpinner = false,
                startPosition = start,
                endPosition = end,
                size = size,
                speed = speed
            };
        }

        public static MazeObstacleDefinition CreateSpinner(Vector2 center, Vector2 barSize, float angularSpeedDeg = 75f)
        {
            return new MazeObstacleDefinition
            {
                isSpinner = true,
                startPosition = center,
                endPosition = center,
                size = barSize,
                speed = angularSpeedDeg
            };
        }
    }

    [Serializable]
    public struct MazeBumperDefinition
    {
        public Vector2 position;
        public Vector2 size;
        public float rotationDeg;
        public float bounceForce;

        public MazeBumperDefinition(Vector2 position, Vector2 size, float rotationDeg = 0f, float bounceForce = 8.5f)
        {
            this.position = position;
            this.size = size;
            this.rotationDeg = rotationDeg;
            this.bounceForce = bounceForce;
        }
    }

    [Serializable]
    public struct MazeSwitchGateDefinition
    {
        public Vector2 switchPosition;
        public Vector2 gatePosition;
        public Vector2 gateSize;
        public float gateRotationDeg;

        public MazeSwitchGateDefinition(Vector2 switchPosition, Vector2 gatePosition, Vector2 gateSize, float gateRotationDeg = 0f)
        {
            this.switchPosition = switchPosition;
            this.gatePosition = gatePosition;
            this.gateSize = gateSize;
            this.gateRotationDeg = gateRotationDeg;
        }
    }

    /// <summary>
    /// Data-driven definition for a single Geometry Box Tilt Maze level.
    /// Stores board dimensions, nostalgic plastic tray colors, walls, pit holes,
    /// moving/rotating obstacles, start/goal spots, and star time targets.
    /// </summary>
    [CreateAssetMenu(fileName = "GeometryMazeLevel_01", menuName = "Geometry Box Maze/Level Data")]
    public class GeometryMazeLevelData : ScriptableObject
    {
        [Header("Level Identity")]
        public int levelNumber = 1;
        public string levelName = "First Roll";
        public string difficultyLabel = "EASY";
        public string modeLabel = "CLASSIC";

        [Header("Geometry Box Board Styling")]
        public Vector2 trayInnerSize = new Vector2(7.6f, 11.6f);
        public Color trayColor = new Color(0.79f, 0.91f, 0.98f, 1f);
        public Color outerFrameColor = new Color(0.18f, 0.47f, 0.76f, 1f);
        public Color wallColor = new Color(0.95f, 0.98f, 1.0f, 1f);

        [Header("Progression & Star Targets")]
        public float threeStarTime = 16f;
        public float twoStarTime = 30f;
        [Tooltip("If > 0, player must finish within this many seconds or fail (Time Trial). 0 = Unlimited.")]
        public float timeTrialLimit = 0f;

        [Header("Gameplay Layout")]
        public Vector2 startPosition = new Vector2(-2.4f, -4.6f);
        public Vector2 goalPosition = new Vector2(2.4f, 4.6f);
        public float ballRadius = 0.29f;

        public List<MazeWallDefinition> walls = new List<MazeWallDefinition>();
        public List<MazeHoleDefinition> holes = new List<MazeHoleDefinition>();
        public List<MazeObstacleDefinition> obstacles = new List<MazeObstacleDefinition>();
        public List<Vector2> bonusPins = new List<Vector2>();
        public List<MazeBumperDefinition> eraserBumpers = new List<MazeBumperDefinition>();
        public List<MazeSwitchGateDefinition> switchGates = new List<MazeSwitchGateDefinition>();
    }
}
