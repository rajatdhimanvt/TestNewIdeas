using System;
using UnityEngine;

namespace GeometryBoxMaze
{
    /// <summary>
    /// Constructs the visual and physical Geometry Box Maze tray at runtime from LevelData,
    /// complete with warm classroom wooden desk backdrop, metallic hinges & clasp latch,
    /// embossed protractor & metric cm ruler markings, 2.5D wall drop shadows, pit holes,
    /// collectible Gold Pins, Rubber Eraser bounce pads, Switch Gates, and the chrome steel ball.
    /// </summary>
    public class GeometryMazeBoardBuilder : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite deskWoodSprite;
        [SerializeField] private Sprite trayBaseSprite;
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite holeSprite;
        [SerializeField] private Sprite goalSprite;
        [SerializeField] private Sprite obstacleSprite;
        [SerializeField] private Sprite eraserSprite;
        [SerializeField] private Sprite goldPinSprite;
        [SerializeField] private Sprite switchSprite;
        [SerializeField] private Sprite lockedGateSprite;
        [SerializeField] private Sprite hingeSprite;
        [SerializeField] private Sprite claspSprite;
        [SerializeField] private Sprite protractorSprite;
        [SerializeField] private Sprite rulerSprite;
        [SerializeField] private Sprite circleSprite;

        [Header("Prefabs & Tilt Source")]
        [SerializeField] private GeometryMazeBallController ballPrefab;
        [SerializeField] private GeometryMazeTiltInput tiltInput;

        private Transform visualTrayTransform;
        private GameObject deskBackdropObj;
        private GameObject spawnedBoardRoot;
        private GeometryMazeBallController activeBall;
        private GeometryMazeLevelData currentLevel;

        public GeometryMazeBallController ActiveBall => activeBall;

        public void SetSprites(
            Sprite deskWood,
            Sprite tray,
            Sprite wall,
            Sprite hole,
            Sprite goal,
            Sprite obstacle,
            Sprite eraser,
            Sprite goldPin,
            Sprite switchPlate,
            Sprite lockedGate,
            Sprite hinge,
            Sprite clasp,
            Sprite protractor,
            Sprite ruler,
            Sprite circle)
        {
            deskWoodSprite = deskWood;
            trayBaseSprite = tray;
            wallSprite = wall;
            holeSprite = hole;
            goalSprite = goal;
            obstacleSprite = obstacle;
            eraserSprite = eraser;
            goldPinSprite = goldPin;
            switchSprite = switchPlate;
            lockedGateSprite = lockedGate;
            hingeSprite = hinge;
            claspSprite = clasp;
            protractorSprite = protractor;
            rulerSprite = ruler;
            circleSprite = circle;
        }

        public void SetBallPrefab(GeometryMazeBallController prefab)
        {
            ballPrefab = prefab;
        }

        public void BuildLevel(
            GeometryMazeLevelData levelData,
            Action<GeometryMazeBallController, Vector3> onHoleFall,
            Action<GeometryMazeBallController, Vector3> onGoalReached,
            Action onPinCollected)
        {
            currentLevel = levelData;
            ClearCurrentBoard();
            EnsureDeskBackdrop();
            if (levelData == null) return;

            spawnedBoardRoot = new GameObject("ActiveMazeBoard");
            spawnedBoardRoot.transform.SetParent(transform, false);

            BuildTrayVisuals(levelData);
            BuildOuterBoundaryWalls(levelData);
            BuildMazeWalls(levelData);
            BuildHoles(levelData, onHoleFall);
            BuildEraserBumpers(levelData);
            BuildSwitchGates(levelData);
            BuildBonusPins(levelData, onPinCollected);
            BuildStartAndGoal(levelData, onGoalReached);
            BuildObstacles(levelData);
            SpawnBall(levelData);
        }

        public void ClearCurrentBoard()
        {
            if (spawnedBoardRoot != null)
            {
                Destroy(spawnedBoardRoot);
                spawnedBoardRoot = null;
            }
            visualTrayTransform = null;
            activeBall = null;
        }

        public void ResetBallToStart()
        {
            if (activeBall == null || currentLevel == null) return;
            activeBall.Initialize(tiltInput, currentLevel.ballRadius, new Vector3(currentLevel.startPosition.x, currentLevel.startPosition.y, 0f));
        }

        private void EnsureDeskBackdrop()
        {
            if (deskBackdropObj != null || deskWoodSprite == null) return;

            deskBackdropObj = new GameObject("ClassroomDeskBackdrop");
            deskBackdropObj.transform.SetParent(transform, false);
            deskBackdropObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            float scale = 19f / Mathf.Max(1f, deskWoodSprite.bounds.size.y);
            deskBackdropObj.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = deskBackdropObj.AddComponent<SpriteRenderer>();
            sr.sprite = deskWoodSprite;
            sr.sortingOrder = -10;
        }

        private void Update()
        {
            if (visualTrayTransform != null && tiltInput != null)
            {
                Vector2 tilt = tiltInput.CurrentTilt;
                Quaternion targetRot = Quaternion.Euler(-tilt.y * 5.2f, tilt.x * 5.2f, 0f);
                visualTrayTransform.localRotation = Quaternion.Slerp(visualTrayTransform.localRotation, targetRot, Time.unscaledDeltaTime * 10f);
            }
        }

        private void BuildTrayVisuals(GeometryMazeLevelData levelData)
        {
            GameObject visualHolder = new GameObject("TrayVisuals");
            visualHolder.transform.SetParent(spawnedBoardRoot.transform, false);
            visualTrayTransform = visualHolder.transform;

            Vector2 inner = levelData.trayInnerSize;

            // Desk shadow beneath Geometry Box + Metallic outer rim + Molded plastic bed
            CreateSlicedSprite("BoxDeskShadow", visualHolder.transform, new Vector2(0.22f, -0.26f), inner + new Vector2(1.20f, 1.20f), trayBaseSprite, new Color(0.02f, 0.01f, 0.01f, 0.60f), -4);
            CreateSlicedSprite("ChromeBoxRim", visualHolder.transform, Vector2.zero, inner + new Vector2(1.04f, 1.04f), trayBaseSprite, new Color(0.76f, 0.81f, 0.87f, 1f), -1);
            CreateSlicedSprite("OuterPlasticFrame", visualHolder.transform, Vector2.zero, inner + new Vector2(0.84f, 0.84f), trayBaseSprite, levelData.outerFrameColor, 0);
            CreateSlicedSprite("InnerTrayFloor", visualHolder.transform, Vector2.zero, inner + new Vector2(0.08f, 0.08f), trayBaseSprite, levelData.trayColor, 1);

            // Silver Geometry Box Hinges (left spine) & Clasp Latch (right edge)
            if (hingeSprite != null)
            {
                float leftX = -(inner.x * 0.5f + 0.56f);
                CreateSimpleSprite("Hinge_Top", visualHolder.transform, new Vector2(leftX, inner.y * 0.30f), new Vector2(0.38f, 0.55f), hingeSprite, Color.white, 7);
                CreateSimpleSprite("Hinge_Bottom", visualHolder.transform, new Vector2(leftX, -inner.y * 0.30f), new Vector2(0.38f, 0.55f), hingeSprite, Color.white, 7);
            }
            if (claspSprite != null)
            {
                float rightX = inner.x * 0.5f + 0.54f;
                CreateSimpleSprite("FrontClaspLatch", visualHolder.transform, new Vector2(rightX, 0f), new Vector2(0.55f, 0.55f), claspSprite, Color.white, 7);
            }

            // Delicate embossed Protractor watermark inside tray floor
            if (protractorSprite != null)
            {
                GameObject prot = new GameObject("ProtractorWatermark");
                prot.transform.SetParent(visualHolder.transform, false);
                prot.transform.localPosition = new Vector3(0f, -0.45f, 0f);
                float scale = (inner.x * 0.62f) / Mathf.Max(0.1f, protractorSprite.bounds.size.x);
                prot.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer sr = prot.AddComponent<SpriteRenderer>();
                sr.sprite = protractorSprite;
                sr.color = new Color(1f, 1f, 1f, 0.65f);
                sr.sortingOrder = 2;
            }

            // Geometry Box cm-ruler engraving along top & left rim
            if (rulerSprite != null)
            {
                GameObject topRuler = new GameObject("TopRulerStrip");
                topRuler.transform.SetParent(visualHolder.transform, false);
                topRuler.transform.localPosition = new Vector3(0f, inner.y * 0.5f + 0.22f, 0f);
                float rulerWidthWorld = rulerSprite.bounds.size.x;
                topRuler.transform.localScale = new Vector3((inner.x * 0.88f) / Mathf.Max(0.1f, rulerWidthWorld), 0.34f, 1f);
                SpriteRenderer srTop = topRuler.AddComponent<SpriteRenderer>();
                srTop.sprite = rulerSprite;
                srTop.color = new Color(1f, 1f, 1f, 0.78f);
                srTop.sortingOrder = 2;

                GameObject leftRuler = new GameObject("LeftRulerStrip");
                leftRuler.transform.SetParent(visualHolder.transform, false);
                leftRuler.transform.localPosition = new Vector3(-inner.x * 0.5f - 0.22f, 0f, 0f);
                leftRuler.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                leftRuler.transform.localScale = new Vector3((inner.y * 0.88f) / Mathf.Max(0.1f, rulerWidthWorld), 0.34f, 1f);
                SpriteRenderer srLeft = leftRuler.AddComponent<SpriteRenderer>();
                srLeft.sprite = rulerSprite;
                srLeft.color = new Color(1f, 1f, 1f, 0.72f);
                srLeft.sortingOrder = 2;
            }
        }

        private void BuildOuterBoundaryWalls(GeometryMazeLevelData levelData)
        {
            Transform parent = spawnedBoardRoot.transform;
            Vector2 inner = levelData.trayInnerSize;
            float t = 0.34f;
            Color rimColor = Color.Lerp(levelData.outerFrameColor, Color.white, 0.35f);

            CreateWallObject("Boundary_Top", parent, new Vector2(0f, inner.y * 0.5f + t * 0.5f), new Vector2(inner.x + t * 2f, t), 0f, rimColor, false);
            CreateWallObject("Boundary_Bottom", parent, new Vector2(0f, -inner.y * 0.5f - t * 0.5f), new Vector2(inner.x + t * 2f, t), 0f, rimColor, false);
            CreateWallObject("Boundary_Left", parent, new Vector2(-inner.x * 0.5f - t * 0.5f, 0f), new Vector2(t, inner.y), 0f, rimColor, false);
            CreateWallObject("Boundary_Right", parent, new Vector2(inner.x * 0.5f + t * 0.5f, 0f), new Vector2(t, inner.y), 0f, rimColor, false);
        }

        private void BuildMazeWalls(GeometryMazeLevelData levelData)
        {
            if (levelData.walls == null) return;
            for (int i = 0; i < levelData.walls.Count; i++)
            {
                MazeWallDefinition w = levelData.walls[i];
                CreateWallObject($"MazeWall_{i + 1}", spawnedBoardRoot.transform, w.position, w.size, w.rotationDeg, levelData.wallColor, true);
            }
        }

        private void CreateWallObject(string name, Transform parent, Vector2 pos, Vector2 size, float rotDeg, Color tint, bool addDropShadow)
        {
            if (addDropShadow)
            {
                GameObject shadow = CreateSlicedSprite($"{name}_Shadow", parent, pos + new Vector2(0.08f, -0.09f), size, wallSprite, new Color(0.04f, 0.08f, 0.16f, 0.30f), 4);
                shadow.transform.localRotation = Quaternion.Euler(0f, 0f, rotDeg);
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            obj.transform.localRotation = Quaternion.Euler(0f, 0f, rotDeg);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            if (wallSprite != null)
            {
                sr.sprite = wallSprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
            }
            sr.color = tint;
            sr.sortingOrder = 6;

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private void BuildHoles(GeometryMazeLevelData levelData, Action<GeometryMazeBallController, Vector3> onHoleFall)
        {
            if (levelData.holes == null) return;
            for (int i = 0; i < levelData.holes.Count; i++)
            {
                MazeHoleDefinition h = levelData.holes[i];
                GameObject holeObj = new GameObject($"Hole_{i + 1}");
                holeObj.transform.SetParent(spawnedBoardRoot.transform, false);
                holeObj.transform.localPosition = new Vector3(h.position.x, h.position.y, 0f);

                SpriteRenderer sr = holeObj.AddComponent<SpriteRenderer>();
                sr.sprite = holeSprite;
                sr.sortingOrder = 3;

                GeometryMazeHole holeComp = holeObj.AddComponent<GeometryMazeHole>();
                holeComp.Initialize(h.radius, onHoleFall);
            }
        }

        private void BuildEraserBumpers(GeometryMazeLevelData levelData)
        {
            if (levelData.eraserBumpers == null) return;
            for (int i = 0; i < levelData.eraserBumpers.Count; i++)
            {
                MazeBumperDefinition b = levelData.eraserBumpers[i];
                CreateSlicedSprite($"Eraser_{i + 1}_Shadow", spawnedBoardRoot.transform, b.position + new Vector2(0.08f, -0.09f), b.size, wallSprite, new Color(0.05f, 0.05f, 0.12f, 0.28f), 4)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, b.rotationDeg);

                GameObject obj = new GameObject($"EraserBumper_{i + 1}");
                obj.transform.SetParent(spawnedBoardRoot.transform, false);
                obj.transform.localPosition = new Vector3(b.position.x, b.position.y, 0f);
                GeometryMazeBumper bumper = obj.AddComponent<GeometryMazeBumper>();
                bumper.Initialize(b.size, b.rotationDeg, b.bounceForce, eraserSprite != null ? eraserSprite : obstacleSprite);
            }
        }

        private void BuildSwitchGates(GeometryMazeLevelData levelData)
        {
            if (levelData.switchGates == null) return;
            for (int i = 0; i < levelData.switchGates.Count; i++)
            {
                MazeSwitchGateDefinition sg = levelData.switchGates[i];

                // Create Sliding Gate Barrier
                GameObject gateObj = new GameObject($"LockedGate_{i + 1}");
                gateObj.transform.SetParent(spawnedBoardRoot.transform, false);
                gateObj.transform.localPosition = new Vector3(sg.gatePosition.x, sg.gatePosition.y, 0f);
                gateObj.transform.localRotation = Quaternion.Euler(0f, 0f, sg.gateRotationDeg);

                SpriteRenderer gSr = gateObj.AddComponent<SpriteRenderer>();
                if (lockedGateSprite != null)
                {
                    gSr.sprite = lockedGateSprite;
                    gSr.drawMode = SpriteDrawMode.Sliced;
                    gSr.size = sg.gateSize;
                }
                gSr.sortingOrder = 6;

                BoxCollider2D gCol = gateObj.AddComponent<BoxCollider2D>();
                gCol.size = sg.gateSize;

                // Create Pressure Switch Plate
                GameObject swObj = new GameObject($"SwitchPlate_{i + 1}");
                swObj.transform.SetParent(spawnedBoardRoot.transform, false);
                swObj.transform.localPosition = new Vector3(sg.switchPosition.x, sg.switchPosition.y, 0f);
                swObj.AddComponent<SpriteRenderer>();
                swObj.AddComponent<CircleCollider2D>();
                GeometryMazeSwitchGate sw = swObj.AddComponent<GeometryMazeSwitchGate>();
                sw.Initialize(gateObj, switchSprite != null ? switchSprite : circleSprite);
            }
        }

        private void BuildBonusPins(GeometryMazeLevelData levelData, Action onPinCollected)
        {
            if (levelData.bonusPins == null) return;
            for (int i = 0; i < levelData.bonusPins.Count; i++)
            {
                Vector2 pos = levelData.bonusPins[i];
                GameObject pinObj = new GameObject($"BonusPin_{i + 1}");
                pinObj.transform.SetParent(spawnedBoardRoot.transform, false);
                pinObj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

                SpriteRenderer sr = pinObj.AddComponent<SpriteRenderer>();
                sr.sprite = goldPinSprite != null ? goldPinSprite : circleSprite;
                sr.sortingOrder = 5;

                GeometryMazeCoin coin = pinObj.AddComponent<GeometryMazeCoin>();
                coin.Initialize(circleSprite, onPinCollected);
            }
        }

        private void BuildStartAndGoal(GeometryMazeLevelData levelData, Action<GeometryMazeBallController, Vector3> onGoalReached)
        {
            float pocketRadius = Mathf.Clamp(levelData.ballRadius * 1.48f, 0.34f, 0.48f);

            if (circleSprite != null)
            {
                GameObject startPad = new GameObject("StartPad");
                startPad.transform.SetParent(spawnedBoardRoot.transform, false);
                startPad.transform.localPosition = new Vector3(levelData.startPosition.x, levelData.startPosition.y, 0f);
                float padScale = pocketRadius * 1.9f;
                startPad.transform.localScale = new Vector3(padScale, padScale, 1f);
                SpriteRenderer sr = startPad.AddComponent<SpriteRenderer>();
                sr.sprite = circleSprite;
                sr.color = new Color(0.18f, 0.52f, 0.92f, 0.34f);
                sr.sortingOrder = 2;
            }

            GameObject goalObj = new GameObject("GoalPocket");
            goalObj.transform.SetParent(spawnedBoardRoot.transform, false);
            goalObj.transform.localPosition = new Vector3(levelData.goalPosition.x, levelData.goalPosition.y, 0f);
            SpriteRenderer goalSr = goalObj.AddComponent<SpriteRenderer>();
            goalSr.sprite = goalSprite;
            goalSr.sortingOrder = 4;

            GeometryMazeGoal goalComp = goalObj.AddComponent<GeometryMazeGoal>();
            goalComp.Initialize(pocketRadius, circleSprite, onGoalReached);
        }

        private void BuildObstacles(GeometryMazeLevelData levelData)
        {
            if (levelData.obstacles == null) return;
            for (int i = 0; i < levelData.obstacles.Count; i++)
            {
                MazeObstacleDefinition obsDef = levelData.obstacles[i];
                GameObject obsObj = new GameObject($"Obstacle_{i + 1}");
                obsObj.transform.SetParent(spawnedBoardRoot.transform, false);
                GeometryMazeObstacle obsComp = obsObj.AddComponent<GeometryMazeObstacle>();
                obsComp.Initialize(obsDef, obstacleSprite != null ? obstacleSprite : wallSprite);
            }
        }

        private void SpawnBall(GeometryMazeLevelData levelData)
        {
            if (ballPrefab == null)
            {
                Debug.LogError("[GeometryMazeBoardBuilder] Ball Prefab is not assigned!");
                return;
            }

            activeBall = Instantiate(ballPrefab, spawnedBoardRoot.transform);
            activeBall.name = "SteelBall";
            activeBall.Initialize(tiltInput, levelData.ballRadius, new Vector3(levelData.startPosition.x, levelData.startPosition.y, 0f));
        }

        private static GameObject CreateSlicedSprite(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
            }
            sr.color = color;
            sr.sortingOrder = order;
            return obj;
        }

        private static void CreateSimpleSprite(string name, Transform parent, Vector2 pos, Vector2 scale, Sprite sprite, Color color, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
        }
    }
}
