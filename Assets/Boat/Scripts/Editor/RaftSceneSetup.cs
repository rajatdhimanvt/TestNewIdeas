#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaftRumble.Editor
{
    public static class RaftSceneSetup
    {
        [MenuItem("Raft Rumble/Setup Raft Scene", false, 1)]
        public static void SetupScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.name.Contains("RaftRumble"))
            {
                if (EditorUtility.DisplayDialog("Open Scene", "Do you want to open RaftRumble scene first?", "Yes", "No"))
                {
                    EditorSceneManager.OpenScene("Assets/Boat/Scene/RaftRumble.unity");
                }
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Setup Raft GameObject
            GameObject raftObj = GameObject.Find("Raft");
            if (raftObj == null)
            {
                raftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                raftObj.name = "Raft";
                raftObj.transform.position = new Vector3(0f, 0.8f, -12f);
                Undo.RegisterCreatedObjectUndo(raftObj, "Create Raft");
            }

            // Ensure circular cylinder look
            var meshFilter = raftObj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var cylinderPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                meshFilter.sharedMesh = cylinderPrimitive.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(cylinderPrimitive);
            }

            raftObj.transform.localScale = new Vector3(3.6f, 0.35f, 3.6f);

            var rb = raftObj.GetComponent<Rigidbody>();
            if (rb == null) rb = raftObj.AddComponent<Rigidbody>();
            rb.mass = 4f;
            rb.linearDamping = 1.8f;
            rb.angularDamping = 2.5f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var raftController = raftObj.GetComponent<RaftController>();
            if (raftController == null) raftController = raftObj.AddComponent<RaftController>();

            // 2. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camFollow = mainCam.GetComponent<RaftCameraFollow>();
                if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<RaftCameraFollow>();
                camFollow.SetTarget(raftObj.transform);
            }

            // 3. Setup Obstacle tag/collider
            GameObject obstacle = GameObject.Find("Obstacle");
            if (obstacle != null)
            {
                obstacle.name = "Obstacle_Hurdle";
            }

            // 4. Setup Finish Line
            GameObject finishLine = GameObject.Find("FinishLine");
            if (finishLine == null)
            {
                finishLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finishLine.name = "FinishLine";
                finishLine.transform.position = new Vector3(0f, 0.6f, 18f);
                finishLine.transform.localScale = new Vector3(7.5f, 1f, 0.5f);
                var col = finishLine.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                // Transparent or distinct material if available
                var rend = finishLine.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    rend.sharedMaterial.color = new Color(0.2f, 1f, 0.3f, 0.6f);
                }
                Undo.RegisterCreatedObjectUndo(finishLine, "Create FinishLine");
            }

            // 5. Ensure EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // 6. Setup Canvas UI
            GameObject canvasObj = GameObject.Find("Raft_UI_Canvas");
            if (canvasObj != null)
            {
                Object.DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("Raft_UI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create UI Canvas");

            var canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Portrait mobile reference
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var gameplayUI = canvasObj.GetComponent<RaftGameplayUI>();
            if (gameplayUI == null) gameplayUI = canvasObj.AddComponent<RaftGameplayUI>();

            // Setup UI Elements inside Canvas
            SetupUIHierarchy(canvasObj, gameplayUI);

            // 7. Setup RaftGameManager
            GameObject gmObj = GameObject.Find("RaftGameManager");
            if (gmObj == null)
            {
                gmObj = new GameObject("RaftGameManager");
                Undo.RegisterCreatedObjectUndo(gmObj, "Create RaftGameManager");
            }

            var gm = gmObj.GetComponent<RaftGameManager>();
            if (gm == null) gm = gmObj.AddComponent<RaftGameManager>();

            // Wire references via SerializedObject
            var serializedGM = new SerializedObject(gm);
            serializedGM.FindProperty("raft").objectReferenceValue = raftController;
            serializedGM.FindProperty("gameplayUI").objectReferenceValue = gameplayUI;
            if (mainCam != null)
            {
                serializedGM.FindProperty("cameraFollow").objectReferenceValue = mainCam.GetComponent<RaftCameraFollow>();
            }
            if (finishLine != null)
            {
                serializedGM.FindProperty("trackFinish").objectReferenceValue = finishLine.transform;
            }
            serializedGM.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=green><b>[Raft Rumble] Scene setup successfully completed and saved!</b></color>");
        }

        private static void SetupUIHierarchy(GameObject canvasObj, RaftGameplayUI gameplayUI)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // --- JOYSTICK ---
            GameObject joystickBase = GetOrCreateChild(canvasObj, "Joystick_Base");
            var joyRect = joystickBase.GetComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(220f, 260f);
            joyRect.sizeDelta = new Vector2(200f, 200f);

            var joyImage = GetOrAddComponent<Image>(joystickBase);
            joyImage.color = new Color(1f, 1f, 1f, 0.25f);

            var joyScript = GetOrAddComponent<RaftJoystick>(joystickBase);

            GameObject joyHandle = GetOrCreateChild(joystickBase, "Handle");
            var handleRect = joyHandle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80f, 80f);

            var handleImage = GetOrAddComponent<Image>(joyHandle);
            handleImage.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);

            var serializedJoy = new SerializedObject(joyScript);
            serializedJoy.FindProperty("handle").objectReferenceValue = handleRect;
            serializedJoy.FindProperty("handleRange").floatValue = 70f;
            serializedJoy.ApplyModifiedProperties();

            // --- FART BUTTON ---
            GameObject fartBtnObj = GetOrCreateChild(canvasObj, "Fart_Button");
            var fartRect = fartBtnObj.GetComponent<RectTransform>();
            fartRect.anchorMin = new Vector2(1f, 0f);
            fartRect.anchorMax = new Vector2(1f, 0f);
            fartRect.pivot = new Vector2(0.5f, 0.5f);
            fartRect.anchoredPosition = new Vector2(-220f, 260f);
            fartRect.sizeDelta = new Vector2(180f, 180f);

            var fartBtnImage = GetOrAddComponent<Image>(fartBtnObj);
            fartBtnImage.color = new Color(0.95f, 0.45f, 0.1f, 0.95f);
            var fartBtn = GetOrAddComponent<Button>(fartBtnObj);

            // Fart label
            GameObject fartLabel = GetOrCreateChild(fartBtnObj, "Text");
            var fartText = GetOrAddComponent<TextMeshProUGUI>(fartLabel);
            if (font != null) fartText.font = font;
            fartText.text = "💨\nFART!";
            fartText.fontSize = 36;
            fartText.alignment = TextAlignmentOptions.Center;
            fartText.color = Color.white;
            fartText.GetComponent<RectTransform>().sizeDelta = fartRect.sizeDelta;

            // Fart Cooldown Image overlay
            GameObject cdObj = GetOrCreateChild(fartBtnObj, "CooldownOverlay");
            var cdRect = cdObj.GetComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;
            var cdImg = GetOrAddComponent<Image>(cdObj);
            cdImg.color = new Color(0f, 0f, 0f, 0.55f);
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillAmount = 0f;
            cdImg.raycastTarget = false;

            // --- GAS SLIDER ---
            GameObject gasObj = GetOrCreateChild(canvasObj, "Gas_Slider");
            var gasRect = gasObj.GetComponent<RectTransform>();
            gasRect.anchorMin = new Vector2(1f, 0f);
            gasRect.anchorMax = new Vector2(1f, 0f);
            gasRect.pivot = new Vector2(0.5f, 0.5f);
            gasRect.anchoredPosition = new Vector2(-220f, 400f);
            gasRect.sizeDelta = new Vector2(180f, 24f);

            var gasSlider = GetOrAddComponent<Slider>(gasObj);
            gasSlider.interactable = false;
            gasSlider.minValue = 0f;
            gasSlider.maxValue = 1f;
            gasSlider.value = 1f;

            var gasBg = GetOrAddComponent<Image>(gasObj);
            gasBg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            GameObject gasFillArea = GetOrCreateChild(gasObj, "Fill Area");
            var fillAreaRect = gasFillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject gasFill = GetOrCreateChild(gasFillArea, "Fill");
            var fillRect = gasFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = GetOrAddComponent<Image>(gasFill);
            fillImg.color = new Color(0.2f, 0.85f, 0.3f);
            gasSlider.fillRect = fillRect;

            // --- TOP HUD: PROGRESS BAR & SPEED ---
            GameObject progressObj = GetOrCreateChild(canvasObj, "Track_Progress");
            var progRect = progressObj.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.5f, 1f);
            progRect.anchorMax = new Vector2(0.5f, 1f);
            progRect.pivot = new Vector2(0.5f, 1f);
            progRect.anchoredPosition = new Vector2(0f, -60f);
            progRect.sizeDelta = new Vector2(600f, 30f);

            var progSlider = GetOrAddComponent<Slider>(progressObj);
            progSlider.interactable = false;
            progSlider.minValue = 0f;
            progSlider.maxValue = 1f;
            var progBg = GetOrAddComponent<Image>(progressObj);
            progBg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            GameObject progFillArea = GetOrCreateChild(progressObj, "Fill Area");
            var progFillAreaRect = progFillArea.GetComponent<RectTransform>();
            progFillAreaRect.anchorMin = Vector2.zero;
            progFillAreaRect.anchorMax = Vector2.one;
            progFillAreaRect.offsetMin = Vector2.zero;
            progFillAreaRect.offsetMax = Vector2.zero;

            GameObject progFill = GetOrCreateChild(progFillArea, "Fill");
            var progFillRect = progFill.GetComponent<RectTransform>();
            progFillRect.anchorMin = Vector2.zero;
            progFillRect.anchorMax = Vector2.one;
            progFillRect.offsetMin = Vector2.zero;
            progFillRect.offsetMax = Vector2.zero;
            var progFillImg = GetOrAddComponent<Image>(progFill);
            progFillImg.color = new Color(0.15f, 0.6f, 1f);
            progSlider.fillRect = progFillRect;

            // Speed Text
            GameObject speedObj = GetOrCreateChild(canvasObj, "Speed_Text");
            var speedRect = speedObj.GetComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(0.5f, 1f);
            speedRect.anchorMax = new Vector2(0.5f, 1f);
            speedRect.pivot = new Vector2(0.5f, 1f);
            speedRect.anchoredPosition = new Vector2(0f, -110f);
            speedRect.sizeDelta = new Vector2(300f, 50f);

            var speedTxt = GetOrAddComponent<TextMeshProUGUI>(speedObj);
            if (font != null) speedTxt.font = font;
            speedTxt.text = "0.0 km/h";
            speedTxt.fontSize = 32;
            speedTxt.alignment = TextAlignmentOptions.Center;
            speedTxt.color = Color.white;

            // Player Indicator Text
            GameObject playerIndObj = GetOrCreateChild(canvasObj, "Player_Indicator");
            var pIndRect = playerIndObj.GetComponent<RectTransform>();
            pIndRect.anchorMin = new Vector2(0f, 1f);
            pIndRect.anchorMax = new Vector2(0f, 1f);
            pIndRect.pivot = new Vector2(0f, 1f);
            pIndRect.anchoredPosition = new Vector2(50f, -60f);
            pIndRect.sizeDelta = new Vector2(250f, 50f);

            var pIndTxt = GetOrAddComponent<TextMeshProUGUI>(playerIndObj);
            if (font != null) pIndTxt.font = font;
            pIndTxt.text = "Player 1 (Solo)";
            pIndTxt.fontSize = 28;
            pIndTxt.color = new Color(0.2f, 0.85f, 0.3f);

            // --- GAME OVER PANEL ---
            GameObject goPanel = GetOrCreateChild(canvasObj, "GameOver_Panel");
            var goRect = goPanel.GetComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;
            var goImg = GetOrAddComponent<Image>(goPanel);
            goImg.color = new Color(0.1f, 0f, 0f, 0.85f);

            GameObject goTitle = GetOrCreateChild(goPanel, "Title");
            var goTitleTxt = GetOrAddComponent<TextMeshProUGUI>(goTitle);
            if (font != null) goTitleTxt.font = font;
            goTitleTxt.text = "💥 GAME OVER!\nBOAT CRASHED!";
            goTitleTxt.fontSize = 52;
            goTitleTxt.alignment = TextAlignmentOptions.Center;
            goTitleTxt.color = new Color(1f, 0.3f, 0.3f);
            goTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 100f);

            GameObject goBtn = GetOrCreateChild(goPanel, "Retry_Button");
            var goBtnRect = goBtn.GetComponent<RectTransform>();
            goBtnRect.anchoredPosition = new Vector2(0f, -100f);
            goBtnRect.sizeDelta = new Vector2(280f, 80f);
            var goBtnImg = GetOrAddComponent<Image>(goBtn);
            goBtnImg.color = new Color(0.9f, 0.25f, 0.25f);
            var goButtonComp = GetOrAddComponent<Button>(goBtn);

            GameObject goBtnTxtObj = GetOrCreateChild(goBtn, "Text");
            var goBtnTxt = GetOrAddComponent<TextMeshProUGUI>(goBtnTxtObj);
            if (font != null) goBtnTxt.font = font;
            goBtnTxt.text = "RETRY";
            goBtnTxt.fontSize = 36;
            goBtnTxt.alignment = TextAlignmentOptions.Center;
            goBtnTxt.color = Color.white;
            goBtnTxtObj.GetComponent<RectTransform>().sizeDelta = goBtnRect.sizeDelta;

            // --- VICTORY PANEL ---
            GameObject vicPanel = GetOrCreateChild(canvasObj, "Victory_Panel");
            var vicRect = vicPanel.GetComponent<RectTransform>();
            vicRect.anchorMin = Vector2.zero;
            vicRect.anchorMax = Vector2.one;
            vicRect.offsetMin = Vector2.zero;
            vicRect.offsetMax = Vector2.zero;
            var vicImg = GetOrAddComponent<Image>(vicPanel);
            vicImg.color = new Color(0f, 0.1f, 0.05f, 0.85f);

            GameObject vicTitle = GetOrCreateChild(vicPanel, "Title");
            var vicTitleTxt = GetOrAddComponent<TextMeshProUGUI>(vicTitle);
            if (font != null) vicTitleTxt.font = font;
            vicTitleTxt.text = "🏆 VICTORY!\nFINISH LINE REACHED!";
            vicTitleTxt.fontSize = 52;
            vicTitleTxt.alignment = TextAlignmentOptions.Center;
            vicTitleTxt.color = new Color(0.3f, 1f, 0.4f);
            vicTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 100f);

            GameObject vicBtn = GetOrCreateChild(vicPanel, "PlayAgain_Button");
            var vicBtnRect = vicBtn.GetComponent<RectTransform>();
            vicBtnRect.anchoredPosition = new Vector2(0f, -100f);
            vicBtnRect.sizeDelta = new Vector2(280f, 80f);
            var vicBtnImg = GetOrAddComponent<Image>(vicBtn);
            vicBtnImg.color = new Color(0.2f, 0.8f, 0.3f);
            var vicButtonComp = GetOrAddComponent<Button>(vicBtn);

            GameObject vicBtnTxtObj = GetOrCreateChild(vicBtn, "Text");
            var vicBtnTxt = GetOrAddComponent<TextMeshProUGUI>(vicBtnTxtObj);
            if (font != null) vicBtnTxt.font = font;
            vicBtnTxt.text = "PLAY AGAIN";
            vicBtnTxt.fontSize = 36;
            vicBtnTxt.alignment = TextAlignmentOptions.Center;
            vicBtnTxt.color = Color.white;
            vicBtnTxtObj.GetComponent<RectTransform>().sizeDelta = vicBtnRect.sizeDelta;

            // Hide panels initially
            goPanel.SetActive(false);
            vicPanel.SetActive(false);

            // Bind into RaftGameplayUI
            var serializedUI = new SerializedObject(gameplayUI);
            serializedUI.FindProperty("fartButton").objectReferenceValue = fartBtn;
            serializedUI.FindProperty("fartCooldownImage").objectReferenceValue = cdImg;
            serializedUI.FindProperty("gasSlider").objectReferenceValue = gasSlider;
            serializedUI.FindProperty("speedText").objectReferenceValue = speedTxt;
            serializedUI.FindProperty("progressBar").objectReferenceValue = progSlider;
            serializedUI.FindProperty("playerIndicatorText").objectReferenceValue = pIndTxt;
            serializedUI.FindProperty("gameOverPanel").objectReferenceValue = goPanel;
            serializedUI.FindProperty("victoryPanel").objectReferenceValue = vicPanel;
            serializedUI.FindProperty("restartButton").objectReferenceValue = goButtonComp;
            serializedUI.FindProperty("victoryRestartButton").objectReferenceValue = vicButtonComp;
            serializedUI.ApplyModifiedProperties();
        }

        private static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            if (child != null)
            {
                var rect = child.GetComponent<RectTransform>();
                if (rect != null) return child.gameObject;
                Object.DestroyImmediate(child.gameObject);
            }

            GameObject newChild = new GameObject(name, typeof(RectTransform));
            newChild.transform.SetParent(parent.transform, false);
            return newChild;
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }
    }
}
#endif
