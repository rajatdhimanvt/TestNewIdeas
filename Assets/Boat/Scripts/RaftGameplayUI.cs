using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaftRumble
{
    public class RaftGameplayUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Button fartButton;
        [SerializeField] private Image fartCooldownImage;
        [SerializeField] private Slider gasSlider;

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI playerIndicatorText;

        [Header("Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button victoryRestartButton;

        private RaftPlayer localPlayer;
        private RaftController raft;

        private void Start()
        {
            if (fartButton != null)
            {
                fartButton.onClick.AddListener(OnFartButtonClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (victoryRestartButton != null)
            {
                victoryRestartButton.onClick.AddListener(OnRestartClicked);
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);

            if (RaftGameManager.Instance != null)
            {
                RaftGameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (RaftGameManager.Instance != null)
            {
                RaftGameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        public void BindPlayer(RaftPlayer player, RaftController raftController)
        {
            localPlayer = player;
            raft = raftController;

            if (playerIndicatorText != null && localPlayer != null)
            {
                playerIndicatorText.text = $"Player {localPlayer.PlayerIndex}";
                playerIndicatorText.color = localPlayer.PlayerColor;
            }
        }

        private void Update()
        {
            if (localPlayer != null)
            {
                // Update cooldown overlay
                if (fartCooldownImage != null)
                {
                    fartCooldownImage.fillAmount = localPlayer.CooldownNormalized;
                }

                // Update gas bar
                if (gasSlider != null)
                {
                    gasSlider.value = localPlayer.GasNormalized;
                }

                // Interactability
                if (fartButton != null)
                {
                    fartButton.interactable = localPlayer.CanFart;
                }
            }

            if (raft != null)
            {
                // Update Speed text
                if (speedText != null)
                {
                    float kmh = raft.CurrentSpeed * 3.6f;
                    speedText.text = $"{kmh:0.0} km/h";
                }

                // Update Track progress
                if (progressBar != null && RaftGameManager.Instance != null)
                {
                    progressBar.value = RaftGameManager.Instance.TrackProgress;
                }
            }
        }

        private void OnFartButtonClicked()
        {
            if (localPlayer != null)
            {
                localPlayer.TriggerFart();
            }
        }

        private void OnRestartClicked()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);

            if (RaftGameManager.Instance != null)
            {
                RaftGameManager.Instance.RestartGame();
            }
        }

        private void HandleGameStateChanged(RaftGameState newState)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(newState == RaftGameState.GameOver);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(newState == RaftGameState.Victory);
            }
        }

        /// <summary>
        /// Ensures a full gameplay HUD canvas exists even if the scene was not pre-baked in Editor.
        /// </summary>
        public static RaftGameplayUI EnsureUIExists()
        {
            var existing = FindAnyObjectByType<RaftGameplayUI>();
            if (existing != null) return existing;

            // Ensure EventSystem
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Create Canvas with RectTransform
            GameObject canvasObj = new GameObject("Raft_UI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var ui = canvasObj.AddComponent<RaftGameplayUI>();

            // Setup Font
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // 1. Joystick
            GameObject joyBase = new GameObject("Joystick_Base", typeof(RectTransform));
            joyBase.transform.SetParent(canvasObj.transform, false);
            var joyRect = joyBase.GetComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(220f, 260f);
            joyRect.sizeDelta = new Vector2(200f, 200f);
            var joyImg = joyBase.AddComponent<Image>();
            joyImg.color = new Color(1f, 1f, 1f, 0.25f);
            var joystick = joyBase.AddComponent<RaftJoystick>();

            GameObject joyHandle = new GameObject("Handle", typeof(RectTransform));
            joyHandle.transform.SetParent(joyBase.transform, false);
            var handleRect = joyHandle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80f, 80f);
            var handleImg = joyHandle.AddComponent<Image>();
            handleImg.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);

            // 2. Fart Button
            GameObject fartObj = new GameObject("Fart_Button", typeof(RectTransform));
            fartObj.transform.SetParent(canvasObj.transform, false);
            var fartRect = fartObj.GetComponent<RectTransform>();
            fartRect.anchorMin = new Vector2(1f, 0f);
            fartRect.anchorMax = new Vector2(1f, 0f);
            fartRect.pivot = new Vector2(0.5f, 0.5f);
            fartRect.anchoredPosition = new Vector2(-220f, 260f);
            fartRect.sizeDelta = new Vector2(180f, 180f);
            var fartImg = fartObj.AddComponent<Image>();
            fartImg.color = new Color(0.95f, 0.45f, 0.1f, 0.95f);
            var fartBtn = fartObj.AddComponent<Button>();

            GameObject fartTxtObj = new GameObject("Text", typeof(RectTransform));
            fartTxtObj.transform.SetParent(fartObj.transform, false);
            var fartTxtRect = fartTxtObj.GetComponent<RectTransform>();
            fartTxtRect.sizeDelta = fartRect.sizeDelta;
            var fartTxt = fartTxtObj.AddComponent<TextMeshProUGUI>();
            if (font != null) fartTxt.font = font;
            fartTxt.text = "💨\nFART!";
            fartTxt.fontSize = 36;
            fartTxt.alignment = TextAlignmentOptions.Center;
            fartTxt.color = Color.white;

            GameObject cdObj = new GameObject("CooldownOverlay", typeof(RectTransform));
            cdObj.transform.SetParent(fartObj.transform, false);
            var cdRect = cdObj.GetComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;
            var cdImg = cdObj.AddComponent<Image>();
            cdImg.color = new Color(0f, 0f, 0f, 0.55f);
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillAmount = 0f;
            cdImg.raycastTarget = false;

            // 3. Gas Bar Slider
            GameObject gasObj = new GameObject("Gas_Slider", typeof(RectTransform));
            gasObj.transform.SetParent(canvasObj.transform, false);
            var gasRect = gasObj.GetComponent<RectTransform>();
            gasRect.anchorMin = new Vector2(1f, 0f);
            gasRect.anchorMax = new Vector2(1f, 0f);
            gasRect.pivot = new Vector2(0.5f, 0.5f);
            gasRect.anchoredPosition = new Vector2(-220f, 400f);
            gasRect.sizeDelta = new Vector2(180f, 24f);
            var gasSlider = gasObj.AddComponent<Slider>();
            gasSlider.interactable = false;
            var gasBg = gasObj.AddComponent<Image>();
            gasBg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            GameObject gasFillArea = new GameObject("Fill Area", typeof(RectTransform));
            gasFillArea.transform.SetParent(gasObj.transform, false);
            var gfaRect = gasFillArea.GetComponent<RectTransform>();
            gfaRect.anchorMin = Vector2.zero;
            gfaRect.anchorMax = Vector2.one;
            gfaRect.offsetMin = Vector2.zero;
            gfaRect.offsetMax = Vector2.zero;

            GameObject gasFill = new GameObject("Fill", typeof(RectTransform));
            gasFill.transform.SetParent(gasFillArea.transform, false);
            var gfRect = gasFill.GetComponent<RectTransform>();
            gfRect.anchorMin = Vector2.zero;
            gfRect.anchorMax = Vector2.one;
            gfRect.offsetMin = Vector2.zero;
            gfRect.offsetMax = Vector2.zero;
            var gfImg = gasFill.AddComponent<Image>();
            gfImg.color = new Color(0.2f, 0.85f, 0.3f);
            gasSlider.fillRect = gfRect;

            // 4. Progress Bar
            GameObject progObj = new GameObject("Track_Progress", typeof(RectTransform));
            progObj.transform.SetParent(canvasObj.transform, false);
            var progRect = progObj.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.5f, 1f);
            progRect.anchorMax = new Vector2(0.5f, 1f);
            progRect.pivot = new Vector2(0.5f, 1f);
            progRect.anchoredPosition = new Vector2(0f, -60f);
            progRect.sizeDelta = new Vector2(600f, 30f);
            var progSlider = progObj.AddComponent<Slider>();
            progSlider.interactable = false;
            var progBg = progObj.AddComponent<Image>();
            progBg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            GameObject progFillArea = new GameObject("Fill Area", typeof(RectTransform));
            progFillArea.transform.SetParent(progObj.transform, false);
            var pfaRect = progFillArea.AddComponent<RectTransform>();
            pfaRect.anchorMin = Vector2.zero;
            pfaRect.anchorMax = Vector2.one;
            pfaRect.offsetMin = Vector2.zero;
            pfaRect.offsetMax = Vector2.zero;

            GameObject progFill = new GameObject("Fill", typeof(RectTransform));
            progFill.transform.SetParent(progFillArea.transform, false);
            var pfRect = progFill.GetComponent<RectTransform>();
            pfRect.anchorMin = Vector2.zero;
            pfRect.anchorMax = Vector2.one;
            pfRect.offsetMin = Vector2.zero;
            pfRect.offsetMax = Vector2.zero;
            var pfImg = progFill.AddComponent<Image>();
            pfImg.color = new Color(0.15f, 0.6f, 1f);
            progSlider.fillRect = pfRect;

            // 5. Speed Text
            GameObject speedObj = new GameObject("Speed_Text", typeof(RectTransform));
            speedObj.transform.SetParent(canvasObj.transform, false);
            var speedRect = speedObj.GetComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(0.5f, 1f);
            speedRect.anchorMax = new Vector2(0.5f, 1f);
            speedRect.pivot = new Vector2(0.5f, 1f);
            speedRect.anchoredPosition = new Vector2(0f, -110f);
            speedRect.sizeDelta = new Vector2(300f, 50f);
            var speedTxt = speedObj.AddComponent<TextMeshProUGUI>();
            if (font != null) speedTxt.font = font;
            speedTxt.text = "0.0 km/h";
            speedTxt.fontSize = 32;
            speedTxt.alignment = TextAlignmentOptions.Center;
            speedTxt.color = Color.white;

            // 6. Player Indicator
            GameObject indObj = new GameObject("Player_Indicator", typeof(RectTransform));
            indObj.transform.SetParent(canvasObj.transform, false);
            var indRect = indObj.GetComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 1f);
            indRect.anchorMax = new Vector2(0f, 1f);
            indRect.pivot = new Vector2(0f, 1f);
            indRect.anchoredPosition = new Vector2(50f, -60f);
            indRect.sizeDelta = new Vector2(250f, 50f);
            var indTxt = indObj.AddComponent<TextMeshProUGUI>();
            if (font != null) indTxt.font = font;
            indTxt.text = "Player 1";
            indTxt.fontSize = 28;
            indTxt.color = new Color(0.2f, 0.85f, 0.3f);

            // Assign fields
            ui.fartButton = fartBtn;
            ui.fartCooldownImage = cdImg;
            ui.gasSlider = gasSlider;
            ui.progressBar = progSlider;
            ui.speedText = speedTxt;
            ui.playerIndicatorText = indTxt;

            return ui;
        }
    }
}
