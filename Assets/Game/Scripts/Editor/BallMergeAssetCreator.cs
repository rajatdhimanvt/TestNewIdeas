#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to generate default BallDataSO and LevelDataSO assets for quick playtesting.
/// Accessible via Unity Editor menu: Tools -> Ball Merge -> Generate Default Level Assets.
/// </summary>
public static class BallMergeAssetCreator
{
    private const string DATA_FOLDER = "Assets/Game/Data";

    [InitializeOnLoadMethod]
    [MenuItem("Tools/Ball Merge/Generate Default Level Assets")]
    public static void GenerateDefaultAssets()
    {
        string levelPath = $"{DATA_FOLDER}/LevelData_Default.asset";
        if (File.Exists(levelPath)) return;

        if (!AssetDatabase.IsValidFolder(DATA_FOLDER))
        {
            Directory.CreateDirectory(DATA_FOLDER);
            AssetDatabase.Refresh();
        }

        // Color palette for 6 tiers
        var tiersConfig = new[]
        {
            new { name = "Cherry", radius = 0.35f, color = new Color(0.95f, 0.2f, 0.25f), score = 2, mass = 0.8f },
            new { name = "Plum", radius = 0.50f, color = new Color(0.65f, 0.25f, 0.85f), score = 4, mass = 1.0f },
            new { name = "Orange", radius = 0.70f, color = new Color(1.0f, 0.6f, 0.1f), score = 8, mass = 1.4f },
            new { name = "Apple", radius = 0.95f, color = new Color(0.3f, 0.85f, 0.3f), score = 16, mass = 1.9f },
            new { name = "Melon", radius = 1.25f, color = new Color(0.95f, 0.9f, 0.2f), score = 32, mass = 2.6f },
            new { name = "Watermelon", radius = 1.60f, color = new Color(0.1f, 0.75f, 0.5f), score = 64, mass = 3.5f }
        };

        var allTiers = new System.Collections.Generic.List<BallDataSO>();

        for (int i = 0; i < tiersConfig.Length; i++)
        {
            var cfg = tiersConfig[i];
            string assetPath = $"{DATA_FOLDER}/BallData_Tier_{i}_{cfg.name}.asset";

            BallDataSO ballData = AssetDatabase.LoadAssetAtPath<BallDataSO>(assetPath);
            if (ballData == null)
            {
                ballData = ScriptableObject.CreateInstance<BallDataSO>();
                AssetDatabase.CreateAsset(ballData, assetPath);
            }

            ballData.tier = i;
            ballData.ballName = cfg.name;
            ballData.radius = cfg.radius;
            ballData.ballColor = cfg.color;
            ballData.scoreValue = cfg.score;
            ballData.mass = cfg.mass;
            ballData.bounciness = 0.2f;
            ballData.friction = 0.4f;

            EditorUtility.SetDirty(ballData);
            allTiers.Add(ballData);
        }

        // Create or update default LevelDataSO
        string levelPath = $"{DATA_FOLDER}/LevelData_Default.asset";
        LevelDataSO levelData = AssetDatabase.LoadAssetAtPath<LevelDataSO>(levelPath);
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(levelData, levelPath);
        }

        levelData.levelName = "Classic Mode";
        levelData.allTiers = allTiers;
        levelData.droppableTiers = new System.Collections.Generic.List<BallDataSO>
        {
            allTiers[0], // Cherry
            allTiers[1], // Plum
            allTiers[2]  // Orange
        };
        levelData.containerWidth = 5.5f;
        levelData.containerHeight = 8.5f;
        levelData.wallThickness = 0.5f;
        levelData.dropHeight = 3.8f;
        levelData.dropCooldown = 0.5f;

        EditorUtility.SetDirty(levelData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BallMergeAssetCreator] Successfully created 6 BallDataSO tiers and LevelData_Default!");
        Selection.activeObject = levelData;
    }
}
#endif
