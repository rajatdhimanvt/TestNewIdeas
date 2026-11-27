using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Top-level controller for the Geometry Box Tilt Maze game.
    /// Coordinates level loading, timer/star progression saved in PlayerPrefs,
    /// tilt calibration, hole fails, level completion, and UI state transitions.
    /// </summary>
    public class GeometryMazeManager : MonoBehaviour
    {
        public enum GameState
        {
            MainMenu,
            LevelSelect,
            Playing,
            Paused,
            LevelComplete,
            GameOver
        }

        private const string PREF_UNLOCKED_COUNT = "GeometryMaze_UnlockedCount";
        private const string PREF_STARS_PREFIX = "GeometryMaze_Stars_L";
        private const string PREF_BESTTIME_PREFIX = "GeometryMaze_BestTime_L";

        [Header("Scene References")]
        [SerializeField] private GeometryMazeLevelCatalog levelCatalog;
        [SerializeField] private GeometryMazeBoardBuilder boardBuilder;
        [SerializeField] private GeometryMazeTiltInput tiltInput;
        [SerializeField] private GeometryMazeUI mazeUI;

        private GameState currentState = GameState.MainMenu;
        private int activeLevelIndex;
        private GeometryMazeLevelData activeLevelData;
        private GeometryMazeLevelData runtimeGeneratedLevel;
        private bool isRandomMazeSession;
        private int randomDifficultyTier = 1;
        private float levelElapsedTime;
        private int pinsCollected;
        private int totalPinsInLevel;

        public GameState CurrentState => currentState;
        public int ActiveLevelIndex => activeLevelIndex;
        public int LevelCount => levelCatalog != null ? levelCatalog.LevelCount : 0;

        public int UnlockedLevelCount
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PREF_UNLOCKED_COUNT, 1), 1, Mathf.Max(1, LevelCount));
            private set => PlayerPrefs.SetInt(PREF_UNLOCKED_COUNT, Mathf.Clamp(value, 1, Mathf.Max(1, LevelCount)));
        }

        private void Awake()
        {
            EnsureLevelCatalogLoaded();
        }

        private void EnsureLevelCatalogLoaded()
        {
            if (levelCatalog != null && levelCatalog.LevelCount > 0) return;

            levelCatalog = Resources.Load<GeometryMazeLevelCatalog>("GeometryMazeLevelCatalog");
#if UNITY_EDITOR
            if (levelCatalog == null || levelCatalog.LevelCount == 0)
            {
                levelCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GeometryMazeLevelCatalog>(
                    "Assets/GeometryBoxMaze/Data/GeometryMazeLevelCatalog.asset");
            }
#endif
        }

        private void Start()
        {
            EnsureLevelCatalogLoaded();
            Time.timeScale = 1f;
            if (Application.isMobilePlatform)
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }
            if (mazeUI != null)
            {
                mazeUI.Bind(this, tiltInput);
            }

#if UNITY_EDITOR
            int testLevelNum = UnityEditor.EditorPrefs.GetInt("GeometryMaze_EditorPlayTestLevel", -1);
            if (testLevelNum >= 0 && levelCatalog != null && levelCatalog.levels != null)
            {
                UnityEditor.EditorPrefs.DeleteKey("GeometryMaze_EditorPlayTestLevel");
                int matchIdx = levelCatalog.levels.FindIndex(l => l != null && l.levelNumber == testLevelNum);
                if (matchIdx >= 0)
                {
                    StartLevel(matchIdx);
                    return;
                }
            }
#endif

            // Show Level 1 board softly in the background behind the Main Menu for instant nostalgia
            GeometryMazeLevelData preview = levelCatalog != null ? levelCatalog.GetLevelByIndex(0) : null;
            if (preview != null && boardBuilder != null)
            {
                boardBuilder.BuildLevel(preview, HandleHoleCaptured, HandleGoalCaptured, HandlePinCollected);
                if (boardBuilder.ActiveBall != null) boardBuilder.ActiveBall.SetSimulationActive(false);
            }

            SetState(GameState.MainMenu);
        }

        private void Update()
        {
            if (currentState != GameState.Playing) return;

            levelElapsedTime += Time.deltaTime;
            if (mazeUI != null)
            {
                mazeUI.UpdateGameplayHUD(activeLevelData, levelElapsedTime, pinsCollected, totalPinsInLevel, tiltInput != null ? tiltInput.CurrentTilt : Vector2.zero);
            }

            // Optional Time Trial limit expiry check
            if (activeLevelData != null && activeLevelData.timeTrialLimit > 0f && levelElapsedTime >= activeLevelData.timeTrialLimit)
            {
                HandleTimeExpired();
                return;
            }

            // Handy Editor/Desktop shortcuts: R = quick retry, Esc = Pause
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RetryLevel();
                }
                else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    PauseGame();
                }
            }
        }

        public void PlayContinueOrLevel1()
        {
            GeometryMazeAudio.PlayClick();
            int targetIdx = Mathf.Clamp(UnlockedLevelCount - 1, 0, Mathf.Max(0, LevelCount - 1));
            StartLevel(targetIdx);
        }

        public void OpenLevelSelect()
        {
            GeometryMazeAudio.PlayClick();
            SetState(GameState.LevelSelect);
        }

        public void OpenMainMenu()
        {
            GeometryMazeAudio.PlayClick();
            Time.timeScale = 1f;
            if (boardBuilder != null && boardBuilder.ActiveBall != null)
            {
                boardBuilder.ActiveBall.SetSimulationActive(false);
            }
            SetState(GameState.MainMenu);
        }

        public void StartLevel(int levelIndex)
        {
            EnsureLevelCatalogLoaded();
            if (levelCatalog == null || levelCatalog.LevelCount == 0)
            {
                Debug.LogError("[GeometryMazeManager] LevelCatalog is empty or unassigned!");
                return;
            }

            Time.timeScale = 1f;
            isRandomMazeSession = false;
            if (runtimeGeneratedLevel != null)
            {
                Destroy(runtimeGeneratedLevel);
                runtimeGeneratedLevel = null;
            }

            activeLevelIndex = Mathf.Clamp(levelIndex, 0, levelCatalog.LevelCount - 1);
            activeLevelData = levelCatalog.GetLevelByIndex(activeLevelIndex);
            LaunchLevelData(activeLevelData);
        }

        public void PlayRandomMaze(int difficultyTier = -1)
        {
            GeometryMazeAudio.PlayClick();
            if (difficultyTier >= 0) randomDifficultyTier = Mathf.Clamp(difficultyTier, 0, 2);
            else randomDifficultyTier = (randomDifficultyTier + 1) % 3;

            isRandomMazeSession = true;
            if (runtimeGeneratedLevel != null)
            {
                Destroy(runtimeGeneratedLevel);
            }

            int seed = (Environment.TickCount & 0x7FFFFFFF) + UnityEngine.Random.Range(1, 9999);
            GeometryMazeGenerator.GenerationConfig cfg = BuildRandomConfigForTier(randomDifficultyTier, seed);
            runtimeGeneratedLevel = GeometryMazeGenerator.CreateRuntimeLevel(cfg);
            activeLevelData = runtimeGeneratedLevel;
            LaunchLevelData(activeLevelData);
        }

        private void LaunchLevelData(GeometryMazeLevelData levelData)
        {
            Time.timeScale = 1f;
            levelElapsedTime = 0f;
            pinsCollected = 0;
            totalPinsInLevel = levelData != null && levelData.bonusPins != null ? levelData.bonusPins.Count : 0;

            if (tiltInput != null)
            {
                tiltInput.ResetInputState();
            }

            if (boardBuilder != null)
            {
                boardBuilder.BuildLevel(levelData, HandleHoleCaptured, HandleGoalCaptured, HandlePinCollected);
            }

            SetState(GameState.Playing);
            if (mazeUI != null)
            {
                mazeUI.UpdateGameplayHUD(levelData, 0f, pinsCollected, totalPinsInLevel, Vector2.zero);
            }
        }

        public void RetryLevel()
        {
            GeometryMazeAudio.PlayClick();
            if (isRandomMazeSession && runtimeGeneratedLevel != null)
            {
                LaunchLevelData(runtimeGeneratedLevel);
            }
            else
            {
                StartLevel(activeLevelIndex);
            }
        }

        public void LoadNextLevel()
        {
            GeometryMazeAudio.PlayClick();
            if (isRandomMazeSession)
            {
                PlayRandomMaze(randomDifficultyTier);
                return;
            }

            if (activeLevelIndex + 1 < LevelCount)
            {
                StartLevel(activeLevelIndex + 1);
            }
            else
            {
                OpenLevelSelect();
            }
        }

        public void SwitchToOtherGame()
        {
            GeometryMazeAudio.PlayClick();
            Time.timeScale = 1f;

            const string targetSceneName = "GameTest";
            const string targetScenePath = "Assets/Game/Scene/GameTest.unity";

            if (Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
                return;
            }

#if UNITY_EDITOR
            if (System.IO.File.Exists(targetScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    targetScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogWarning($"[GeometryMazeManager] Scene '{targetSceneName}' could not be loaded. Add '{targetScenePath}' to Build Settings.");
        }

        private static GeometryMazeGenerator.GenerationConfig BuildRandomConfigForTier(int tier, int seed)
        {
            switch (tier)
            {
                case 0:
                    return new GeometryMazeGenerator.GenerationConfig
                    {
                        levelNumber = 0,
                        levelName = "Random Easy",
                        difficultyLabel = "EASY",
                        modeLabel = "GENERATOR",
                        cols = 5,
                        rows = 7,
                        seed = seed,
                        holeCount = 2,
                        pinCount = 3,
                        includeEraserBumper = true,
                        includeSwitchGate = false,
                        includeMovingSlider = false,
                        includeSpinner = false,
                        threeStarTime = 18f,
                        twoStarTime = 34f,
                        timeTrialLimit = 0f,
                        trayColor = new Color(0.79f, 0.91f, 0.98f, 1f),
                        outerFrameColor = new Color(0.18f, 0.47f, 0.76f, 1f),
                        wallColor = new Color(0.95f, 0.98f, 1f, 1f),
                    };
                case 1:
                    return new GeometryMazeGenerator.GenerationConfig
                    {
                        levelNumber = 0,
                        levelName = "Random Classic",
                        difficultyLabel = "MEDIUM",
                        modeLabel = "GENERATOR",
                        cols = 6,
                        rows = 9,
                        seed = seed,
                        holeCount = 3,
                        pinCount = 3,
                        includeEraserBumper = true,
                        includeSwitchGate = true,
                        includeMovingSlider = false,
                        includeSpinner = false,
                        threeStarTime = 26f,
                        twoStarTime = 46f,
                        timeTrialLimit = 0f,
                        trayColor = new Color(0.78f, 0.96f, 0.89f, 1f),
                        outerFrameColor = new Color(0.12f, 0.56f, 0.42f, 1f),
                        wallColor = new Color(0.95f, 1f, 0.97f, 1f),
                    };
                default:
                    return new GeometryMazeGenerator.GenerationConfig
                    {
                        levelNumber = 0,
                        levelName = "Random Pro",
                        difficultyLabel = "HARD",
                        modeLabel = "GENERATOR",
                        cols = 7,
                        rows = 11,
                        seed = seed,
                        holeCount = 4,
                        pinCount = 3,
                        includeEraserBumper = true,
                        includeSwitchGate = true,
                        includeMovingSlider = true,
                        includeSpinner = false,
                        threeStarTime = 35f,
                        twoStarTime = 60f,
                        timeTrialLimit = 0f,
                        trayColor = new Color(0.84f, 0.83f, 0.97f, 1f),
                        outerFrameColor = new Color(0.32f, 0.28f, 0.64f, 1f),
                        wallColor = new Color(0.96f, 0.96f, 1f, 1f),
                    };
            }
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;
            GeometryMazeAudio.PlayClick();
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;
            GeometryMazeAudio.PlayClick();
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void CalibrateTilt()
        {
            if (tiltInput != null)
            {
                tiltInput.CalibrateNeutral();
            }
            GeometryMazeAudio.PlayCalibrate();
            if (mazeUI != null)
            {
                mazeUI.ShowCalibrationToast();
            }
        }

        public int GetSavedStars(int levelNumber)
        {
            return PlayerPrefs.GetInt($"{PREF_STARS_PREFIX}{levelNumber}", 0);
        }

        public float GetBestTime(int levelNumber)
        {
            return PlayerPrefs.GetFloat($"{PREF_BESTTIME_PREFIX}{levelNumber}", 0f);
        }

        public int GetTotalStarsEarned()
        {
            if (levelCatalog == null) return 0;
            int total = 0;
            for (int i = 0; i < levelCatalog.LevelCount; i++)
            {
                var lvl = levelCatalog.GetLevelByIndex(i);
                if (lvl != null) total += GetSavedStars(lvl.levelNumber);
            }
            return total;
        }

        private void HandleHoleCaptured(GeometryMazeBallController ball, Vector3 holePos)
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.GameOver;

            ball.FallIntoHole(holePos, () =>
            {
                if (mazeUI != null)
                {
                    mazeUI.ShowGameOver("FELL INTO PIT HOLE!", "Tilt gently near hole rims!");
                }
                SetState(GameState.GameOver);
            });
        }

        private void HandleTimeExpired()
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.GameOver;
            if (boardBuilder != null && boardBuilder.ActiveBall != null)
            {
                boardBuilder.ActiveBall.SetSimulationActive(false);
            }
            GeometryMazeAudio.PlayHoleFall();
            if (mazeUI != null)
            {
                mazeUI.ShowGameOver("TIME'S UP!", "Try slightly higher sensitivity or shorter turns.");
            }
            SetState(GameState.GameOver);
        }

        private void HandlePinCollected()
        {
            if (currentState != GameState.Playing) return;
            pinsCollected++;
            if (mazeUI != null)
            {
                mazeUI.UpdateGameplayHUD(activeLevelData, levelElapsedTime, pinsCollected, totalPinsInLevel, tiltInput != null ? tiltInput.CurrentTilt : Vector2.zero);
            }
        }

        private void HandleGoalCaptured(GeometryMazeBallController ball, Vector3 goalPos)
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.LevelComplete;

            int stars = ComputeEarnedStars(activeLevelData, levelElapsedTime);
            if (!isRandomMazeSession && activeLevelData != null && activeLevelData.levelNumber > 0)
            {
                SaveLevelProgress(activeLevelData.levelNumber, stars, levelElapsedTime);
            }

            ball.CaptureInGoal(goalPos, () =>
            {
                bool hasNext = isRandomMazeSession || (activeLevelIndex + 1 < LevelCount);
                if (mazeUI != null)
                {
                    mazeUI.ShowLevelComplete(activeLevelData, levelElapsedTime, stars, pinsCollected, totalPinsInLevel, hasNext, isRandomMazeSession);
                }
                SetState(GameState.LevelComplete);
            });
        }

        private static int ComputeEarnedStars(GeometryMazeLevelData level, float timeTaken)
        {
            if (level == null) return 3;
            if (timeTaken <= level.threeStarTime) return 3;
            if (timeTaken <= level.twoStarTime) return 2;
            return 1;
        }

        private void SaveLevelProgress(int levelNum, int stars, float timeTaken)
        {
            int prevStars = GetSavedStars(levelNum);
            if (stars > prevStars)
            {
                PlayerPrefs.SetInt($"{PREF_STARS_PREFIX}{levelNum}", stars);
            }

            float prevBest = GetBestTime(levelNum);
            if (prevBest <= 0.01f || timeTaken < prevBest)
            {
                PlayerPrefs.SetFloat($"{PREF_BESTTIME_PREFIX}{levelNum}", timeTaken);
            }

            if (activeLevelIndex + 2 > UnlockedLevelCount)
            {
                UnlockedLevelCount = activeLevelIndex + 2;
            }
            PlayerPrefs.Save();
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            if (mazeUI != null)
            {
                mazeUI.ApplyStateVisibility(newState, levelCatalog, UnlockedLevelCount);
            }
        }
    }
}
