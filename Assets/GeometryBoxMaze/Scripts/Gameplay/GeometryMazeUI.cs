using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Manages all UI screens for the Geometry Box Tilt Maze scene:
    /// Main Menu, 5-Level Select Grid, Live Gameplay HUD (with Spirit-Level Bubble & Calibrate button),
    /// Pause Settings (Sensitivity + Mute), Level Complete (1-3 Star display), and Game Over.
    /// </summary>
    public class GeometryMazeUI : MonoBehaviour
    {
        [Header("Root Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject gameplayHudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Main Menu Elements")]
        [SerializeField] private TMP_Text mainMenuStarsText;
        [SerializeField] private Button mainPlayButton;
        [SerializeField] private Button mainLevelsButton;
        [SerializeField] private Button mainRandomMazeButton;
        [SerializeField] private Button mainSwitchGameButton;
        [SerializeField] private Slider menuSensitivitySlider;
        [SerializeField] private TMP_Text menuSensitivityText;

        [Header("Level Select Elements")]
        [SerializeField] private Transform levelGridContainer;
        [SerializeField] private Button levelSelectBackButton;
        [SerializeField] private Sprite uiCardSprite;
        [SerializeField] private Sprite uiButtonSprite;
        [SerializeField] private Sprite starSprite;

        [Header("Gameplay HUD Elements")]
        [SerializeField] private TMP_Text hudLevelTitleText;
        [SerializeField] private TMP_Text hudModeBadgeText;
        [SerializeField] private TMP_Text hudTimerText;
        [SerializeField] private TMP_Text hudPinsText;
        [SerializeField] private TMP_Text hudControlsHintText;
        [SerializeField] private TMP_Text hudCalibrationToastText;
        [SerializeField] private RectTransform spiritLevelBubble;
        [SerializeField] private Button hudCalibrateButton;
        [SerializeField] private Button hudRestartButton;
        [SerializeField] private Button hudPauseButton;

        [Header("Pause Menu Elements")]
        [SerializeField] private Slider pauseSensitivitySlider;
        [SerializeField] private TMP_Text pauseSensitivityText;
        [SerializeField] private Button pauseMuteButton;
        [SerializeField] private TMP_Text pauseMuteText;
        [SerializeField] private Button pauseCalibrateButton;
        [SerializeField] private Button pauseResumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseSwitchGameButton;
        [SerializeField] private Button pauseHomeButton;

        [Header("Level Complete Elements")]
        [SerializeField] private TMP_Text winTitleText;
        [SerializeField] private TMP_Text winTimeText;
        [SerializeField] private Image[] winStarIcons;
        [SerializeField] private Button winNextButton;
        [SerializeField] private Button winReplayButton;
        [SerializeField] private Button winLevelsButton;

        [Header("Game Over Elements")]
        [SerializeField] private TMP_Text gameOverTitleText;
        [SerializeField] private TMP_Text gameOverHintText;
        [SerializeField] private Button gameOverRetryButton;
        [SerializeField] private Button gameOverMenuButton;

        private GeometryMazeManager manager;
        private GeometryMazeTiltInput tiltInput;
        private Coroutine toastRoutine;
        private bool hooksBound;

        public void Bind(GeometryMazeManager mgr, GeometryMazeTiltInput tilt)
        {
            manager = mgr;
            tiltInput = tilt;
            if (!hooksBound)
            {
                hooksBound = true;
                WireButtonAndSliderCallbacks();
            }

            SyncSensitivityWidgets();
            UpdateMuteLabel();
            UpdateControlsHint();
            if (hudCalibrationToastText != null) hudCalibrationToastText.gameObject.SetActive(false);
        }

        private void WireButtonAndSliderCallbacks()
        {
            if (mainPlayButton != null) mainPlayButton.onClick.AddListener(() => manager?.PlayContinueOrLevel1());
            if (mainLevelsButton != null) mainLevelsButton.onClick.AddListener(() => manager?.OpenLevelSelect());
            if (mainRandomMazeButton != null) mainRandomMazeButton.onClick.AddListener(() => manager?.PlayRandomMaze());
            if (mainSwitchGameButton != null) mainSwitchGameButton.onClick.AddListener(() => manager?.SwitchToOtherGame());
            if (levelSelectBackButton != null) levelSelectBackButton.onClick.AddListener(() => manager?.OpenMainMenu());

            if (hudCalibrateButton != null) hudCalibrateButton.onClick.AddListener(() => manager?.CalibrateTilt());
            if (hudRestartButton != null) hudRestartButton.onClick.AddListener(() => manager?.RetryLevel());
            if (hudPauseButton != null) hudPauseButton.onClick.AddListener(() => manager?.PauseGame());

            if (pauseCalibrateButton != null) pauseCalibrateButton.onClick.AddListener(() => manager?.CalibrateTilt());
            if (pauseResumeButton != null) pauseResumeButton.onClick.AddListener(() => manager?.ResumeGame());
            if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(() => manager?.RetryLevel());
            if (pauseSwitchGameButton != null) pauseSwitchGameButton.onClick.AddListener(() => manager?.SwitchToOtherGame());
            if (pauseHomeButton != null) pauseHomeButton.onClick.AddListener(() => manager?.OpenMainMenu());
            if (pauseMuteButton != null) pauseMuteButton.onClick.AddListener(ToggleAudioMute);

            if (winNextButton != null) winNextButton.onClick.AddListener(() => manager?.LoadNextLevel());
            if (winReplayButton != null) winReplayButton.onClick.AddListener(() => manager?.RetryLevel());
            if (winLevelsButton != null) winLevelsButton.onClick.AddListener(() => manager?.OpenLevelSelect());

            if (gameOverRetryButton != null) gameOverRetryButton.onClick.AddListener(() => manager?.RetryLevel());
            if (gameOverMenuButton != null) gameOverMenuButton.onClick.AddListener(() => manager?.OpenMainMenu());

            if (menuSensitivitySlider != null) menuSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            if (pauseSensitivitySlider != null) pauseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        public void ApplyStateVisibility(GeometryMazeManager.GameState state, GeometryMazeLevelCatalog catalog, int unlockedCount)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GeometryMazeManager.GameState.MainMenu);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(state == GeometryMazeManager.GameState.LevelSelect);
            if (gameplayHudPanel != null) gameplayHudPanel.SetActive(state == GeometryMazeManager.GameState.Playing);
            if (pausePanel != null) pausePanel.SetActive(state == GeometryMazeManager.GameState.Paused);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(state == GeometryMazeManager.GameState.LevelComplete);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GeometryMazeManager.GameState.GameOver);

            if (state == GeometryMazeManager.GameState.MainMenu && mainMenuStarsText != null && manager != null)
            {
                int totalPossible = Mathf.Max(1, manager.LevelCount) * 3;
                mainMenuStarsText.text = $"STARS COLLECTED: {manager.GetTotalStarsEarned()} / {totalPossible}";
            }

            if (state == GeometryMazeManager.GameState.LevelSelect)
            {
                RebuildLevelGrid(catalog, unlockedCount);
            }

            if (state == GeometryMazeManager.GameState.Paused || state == GeometryMazeManager.GameState.MainMenu)
            {
                SyncSensitivityWidgets();
                UpdateMuteLabel();
            }
        }

        public void UpdateGameplayHUD(GeometryMazeLevelData level, float elapsedSeconds, int pinsCollected, int totalPins, Vector2 tilt)
        {
            if (level != null)
            {
                if (hudLevelTitleText != null) hudLevelTitleText.text = $"LVL {level.levelNumber}: {level.levelName.ToUpper()}";
                if (hudModeBadgeText != null) hudModeBadgeText.text = $"{level.difficultyLabel} • {level.modeLabel}";

                if (hudPinsText != null)
                {
                    hudPinsText.text = $"PINS: {pinsCollected}/{totalPins}";
                    hudPinsText.color = (totalPins > 0 && pinsCollected >= totalPins)
                        ? new Color(1f, 0.90f, 0.25f)
                        : new Color(0.95f, 0.85f, 0.45f);
                }

                if (hudTimerText != null)
                {
                    if (level.timeTrialLimit > 0f)
                    {
                        float remaining = Mathf.Max(0f, level.timeTrialLimit - elapsedSeconds);
                        hudTimerText.text = $"TIME LEFT: {remaining:0.0}s";
                        hudTimerText.color = remaining <= 7f ? new Color(1f, 0.35f, 0.35f) : Color.white;
                    }
                    else
                    {
                        hudTimerText.text = $"TIME: {elapsedSeconds:0.0}s";
                        hudTimerText.color = Color.white;
                    }
                }
            }

            if (spiritLevelBubble != null)
            {
                Vector2 clamped = Vector2.ClampMagnitude(tilt, 1.15f);
                spiritLevelBubble.anchoredPosition = clamped * 28f;
            }

            UpdateControlsHint();
        }

        public void ShowLevelComplete(GeometryMazeLevelData level, float timeTaken, int stars, int pinsCollected, int totalPins, bool hasNextLevel, bool isRandomMaze = false)
        {
            if (winTitleText != null && level != null)
            {
                if (isRandomMaze)
                    winTitleText.text = $"{level.levelName.ToUpper()} CLEARED!";
                else
                    winTitleText.text = hasNextLevel ? $"LEVEL {level.levelNumber} CLEARED!" : "ALL LEVELS MASTERED!";
            }
            if (winTimeText != null)
            {
                winTimeText.text = totalPins > 0
                    ? $"TIME: {timeTaken:0.0}s  •  PINS: {pinsCollected}/{totalPins}  •  {stars}/3 STARS"
                    : $"TIME: {timeTaken:0.0}s  •  {stars} / 3 STARS";
            }

            if (winNextButton != null)
            {
                TMP_Text nextLbl = winNextButton.GetComponentInChildren<TMP_Text>(true);
                if (nextLbl != null) nextLbl.text = isRandomMaze ? "NEXT RANDOM MAZE" : "NEXT LEVEL";
            }

            if (winStarIcons != null)
            {
                for (int i = 0; i < winStarIcons.Length; i++)
                {
                    if (winStarIcons[i] != null)
                    {
                        winStarIcons[i].color = i < stars
                            ? new Color(1f, 0.88f, 0.22f, 1f)
                            : new Color(0.20f, 0.24f, 0.34f, 0.65f);
                    }
                }
            }

            if (winNextButton != null)
            {
                winNextButton.gameObject.SetActive(hasNextLevel);
            }
        }

        public void ShowGameOver(string title, string hint)
        {
            if (gameOverTitleText != null) gameOverTitleText.text = title;
            if (gameOverHintText != null) gameOverHintText.text = hint;
        }

        public void ShowCalibrationToast()
        {
            if (hudCalibrationToastText == null) return;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            hudCalibrationToastText.gameObject.SetActive(true);
            hudCalibrationToastText.text = "TILT ZEROED! HOLD STEADY";
            yield return new WaitForSecondsRealtime(1.4f);
            hudCalibrationToastText.gameObject.SetActive(false);
            toastRoutine = null;
        }

        private void OnSensitivityChanged(float value)
        {
            if (tiltInput != null)
            {
                tiltInput.Sensitivity = value;
            }
            SyncSensitivityWidgets();
        }

        private void SyncSensitivityWidgets()
        {
            float sens = tiltInput != null ? tiltInput.Sensitivity : 1.15f;
            if (menuSensitivitySlider != null) menuSensitivitySlider.SetValueWithoutNotify(sens);
            if (pauseSensitivitySlider != null) pauseSensitivitySlider.SetValueWithoutNotify(sens);

            string label = $"TILT SENSITIVITY: {sens:0.0}x";
            if (menuSensitivityText != null) menuSensitivityText.text = label;
            if (pauseSensitivityText != null) pauseSensitivityText.text = label;
        }

        private void ToggleAudioMute()
        {
            GeometryMazeAudio.IsMuted = !GeometryMazeAudio.IsMuted;
            UpdateMuteLabel();
            GeometryMazeAudio.PlayClick();
        }

        private void UpdateMuteLabel()
        {
            if (pauseMuteText != null)
            {
                pauseMuteText.text = GeometryMazeAudio.IsMuted ? "SOUND: OFF" : "SOUND: ON";
            }
        }

        private void UpdateControlsHint()
        {
            if (hudControlsHintText == null) return;
            if (tiltInput != null && tiltInput.IsUsingMobileSensor)
            {
                hudControlsHintText.text = "TILT PHONE TO GUIDE BALL  •  CALIBRATE SETTLES NEUTRAL";
            }
            else
            {
                hudControlsHintText.text = "TILT ON MOBILE  |  WASD / ARROWS / MOUSE DRAG IN EDITOR  |  R = RETRY";
            }
        }

        private void RebuildLevelGrid(GeometryMazeLevelCatalog catalog, int unlockedCount)
        {
            if (levelGridContainer == null || catalog == null) return;

            for (int i = levelGridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(levelGridContainer.GetChild(i).gameObject);
            }

            TMP_FontAsset sharedFont = hudLevelTitleText != null ? hudLevelTitleText.font : null;

            for (int i = 0; i < catalog.LevelCount; i++)
            {
                int idx = i;
                GeometryMazeLevelData lvl = catalog.GetLevelByIndex(idx);
                if (lvl == null) continue;

                bool isUnlocked = (idx + 1) <= unlockedCount;
                int stars = manager != null ? manager.GetSavedStars(lvl.levelNumber) : 0;
                float bestTime = manager != null ? manager.GetBestTime(lvl.levelNumber) : 0f;

                GameObject card = new GameObject($"LevelCard_{lvl.levelNumber}");
                card.transform.SetParent(levelGridContainer, false);
                RectTransform crt = card.AddComponent<RectTransform>();
                crt.sizeDelta = new Vector2(820f, 156f);

                Image cardBg = card.AddComponent<Image>();
                if (uiCardSprite != null)
                {
                    cardBg.sprite = uiCardSprite;
                    cardBg.type = Image.Type.Sliced;
                }
                cardBg.color = isUnlocked
                    ? Color.Lerp(lvl.outerFrameColor, new Color(0.12f, 0.16f, 0.25f), 0.55f)
                    : new Color(0.14f, 0.15f, 0.20f, 0.85f);

                // Level Title & Subtitle
                TMP_Text titleTxt = CreateGridText(card.transform, "Title",
                    isUnlocked ? $"LEVEL {lvl.levelNumber}: {lvl.levelName.ToUpper()}" : $"LEVEL {lvl.levelNumber} (LOCKED)",
                    new Vector2(-120f, 24f), new Vector2(520f, 48f), 30f, Color.white, TextAlignmentOptions.Left, sharedFont);

                string sub = isUnlocked
                    ? (bestTime > 0.01f ? $"{lvl.difficultyLabel} • {lvl.modeLabel} • BEST {bestTime:0.0}s" : $"{lvl.difficultyLabel} • {lvl.modeLabel}")
                    : "Complete previous level to unlock";
                CreateGridText(card.transform, "Sub", sub,
                    new Vector2(-120f, -24f), new Vector2(520f, 36f), 20f,
                    isUnlocked ? new Color(0.82f, 0.90f, 1f) : new Color(0.6f, 0.65f, 0.72f), TextAlignmentOptions.Left, sharedFont);

                // Stars row
                for (int s = 0; s < 3; s++)
                {
                    GameObject starObj = new GameObject($"Star_{s}");
                    starObj.transform.SetParent(card.transform, false);
                    RectTransform srt = starObj.AddComponent<RectTransform>();
                    srt.anchoredPosition = new Vector2(150f + (s - 1) * 34f, 32f);
                    srt.sizeDelta = new Vector2(28f, 28f);
                    Image sImg = starObj.AddComponent<Image>();
                    sImg.sprite = starSprite;
                    sImg.raycastTarget = false;
                    sImg.color = isUnlocked && s < stars
                        ? new Color(1f, 0.88f, 0.22f, 1f)
                        : new Color(0.12f, 0.14f, 0.20f, 0.65f);
                }

                // Play button inside card
                GameObject playObj = new GameObject("PlayBtn");
                playObj.transform.SetParent(card.transform, false);
                RectTransform prt = playObj.AddComponent<RectTransform>();
                prt.anchoredPosition = new Vector2(295f, -10f);
                prt.sizeDelta = new Vector2(165f, 64f);

                Image pBg = playObj.AddComponent<Image>();
                if (uiButtonSprite != null)
                {
                    pBg.sprite = uiButtonSprite;
                    pBg.type = Image.Type.Sliced;
                }
                pBg.color = isUnlocked ? new Color(0.18f, 0.78f, 0.44f, 1f) : new Color(0.28f, 0.30f, 0.36f, 0.85f);

                Button btn = playObj.AddComponent<Button>();
                btn.targetGraphic = pBg;
                btn.interactable = isUnlocked;
                btn.onClick.AddListener(() => manager?.StartLevel(idx));

                CreateGridText(playObj.transform, "Text", isUnlocked ? "PLAY" : "LOCK",
                    Vector2.zero, prt.sizeDelta, 24f, Color.white, TextAlignmentOptions.Center, sharedFont);
            }
        }

        private static TMP_Text CreateGridText(
            Transform parent,
            string name,
            string text,
            Vector2 pos,
            Vector2 size,
            float fontSize,
            Color color,
            TextAlignmentOptions align,
            TMP_FontAsset font)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
