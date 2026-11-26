using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaftRumble
{
    public class RaftGameManager : MonoBehaviour
    {
        public static RaftGameManager Instance { get; private set; }

        [Header("Game Mode Configuration")]
        [Tooltip("Select PracticeSinglePlayer for solo offline testing, or Multiplayer for online play")]
        [SerializeField] private RaftGameMode gameMode = RaftGameMode.PracticeSinglePlayer;
        [Range(1, 5)]
        [SerializeField] private int practicePlayerCount = 1;

        [Header("References")]
        [SerializeField] private RaftController raft;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private RaftGameplayUI gameplayUI;
        [SerializeField] private RaftCameraFollow cameraFollow;

        [Header("Track Points")]
        [SerializeField] private Transform trackStart;
        [SerializeField] private Transform trackFinish;

        [Header("Player Colors (1-5)")]
        [SerializeField] private Color[] playerColors = new Color[]
        {
            new Color(0.2f, 0.85f, 0.3f), // P1 Green
            new Color(0.95f, 0.25f, 0.25f), // P2 Red
            new Color(0.25f, 0.55f, 0.95f), // P3 Blue
            new Color(0.95f, 0.85f, 0.2f), // P4 Yellow
            new Color(0.7f, 0.3f, 0.9f)   // P5 Purple
        };

        private RaftGameState currentState = RaftGameState.WaitingToStart;
        private List<RaftPlayer> spawnedPlayers = new List<RaftPlayer>();

        public RaftGameMode Mode => gameMode;
        public RaftGameState State => currentState;
        public RaftController Raft => raft;
        public IReadOnlyList<RaftPlayer> Players => spawnedPlayers;

        public event Action<RaftGameState> OnGameStateChanged;

        public float TrackProgress
        {
            get
            {
                if (raft == null) return 0f;
                float startZ = trackStart != null ? trackStart.position.z : -12f;
                float finishZ = trackFinish != null ? trackFinish.position.z : 20f;
                float length = Mathf.Max(1f, finishZ - startZ);
                return Mathf.Clamp01((raft.transform.position.z - startZ) / length);
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            if (raft == null)
            {
                raft = FindAnyObjectByType<RaftController>();
                if (raft == null)
                {
                    var raftObj = GameObject.Find("Raft");
                    if (raftObj != null)
                    {
                        raft = raftObj.AddComponent<RaftController>();
                    }
                }
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<RaftCameraFollow>();
                if (cameraFollow == null && Camera.main != null)
                {
                    cameraFollow = Camera.main.gameObject.AddComponent<RaftCameraFollow>();
                }
            }

            if (gameplayUI == null)
            {
                gameplayUI = FindAnyObjectByType<RaftGameplayUI>();
                if (gameplayUI == null)
                {
                    gameplayUI = RaftGameplayUI.EnsureUIExists();
                }
            }

            if (raft != null)
            {
                raft.OnObstacleHit += HandleObstacleHit;
                raft.OnFinishReached += HandleFinishReached;

                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(raft.transform);
                }
            }

            InitializeGame();
        }

        private void OnDestroy()
        {
            if (raft != null)
            {
                raft.OnObstacleHit -= HandleObstacleHit;
                raft.OnFinishReached -= HandleFinishReached;
            }
        }

        public void InitializeGame()
        {
            ClearPlayers();

            if (gameMode == RaftGameMode.PracticeSinglePlayer)
            {
                SetupPracticeMode();
            }
            else
            {
                // In multiplayer, waiting for lobby connections (handled in Phase 2)
                SetupPracticeMode();
            }

            SetState(RaftGameState.Playing);
        }

        private void SetupPracticeMode()
        {
            int count = Mathf.Clamp(practicePlayerCount, 1, 5);

            for (int i = 0; i < count; i++)
            {
                // Angle distributed along circular rim
                // If 1 player, put at 270 degrees (back of raft)
                float angle = count == 1 ? 270f : (270f + i * (360f / count)) % 360f;
                Color col = i < playerColors.Length ? playerColors[i] : Color.white;

                RaftPlayer player = CreatePlayerInstance(i + 1, angle, col, isLocal: i == 0);
                spawnedPlayers.Add(player);

                if (i == 0 && gameplayUI != null)
                {
                    gameplayUI.BindPlayer(player, raft);
                }
            }
        }

        private RaftPlayer CreatePlayerInstance(int index, float angle, Color color, bool isLocal)
        {
            GameObject playerObj;

            if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab, raft.transform);
            }
            else
            {
                // Create a clean capsule/character placeholder procedurally
                playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObj.name = $"Player_{index}";
                playerObj.transform.SetParent(raft.transform);
                playerObj.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

                // Add a small nose/indicator pointing forward (inward toward raft center)
                GameObject faceIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                faceIndicator.name = "Nose_FaceCenter";
                faceIndicator.transform.SetParent(playerObj.transform);
                faceIndicator.transform.localPosition = new Vector3(0f, 0.3f, 0.5f);
                faceIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
                Destroy(faceIndicator.GetComponent<Collider>());

                // Butt indicator pointing backward (outward)
                GameObject buttIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                buttIndicator.name = "Butt_FartExhaust";
                buttIndicator.transform.SetParent(playerObj.transform);
                buttIndicator.transform.localPosition = new Vector3(0f, -0.2f, -0.5f);
                buttIndicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                Destroy(buttIndicator.GetComponent<Collider>());

                // Remove capsule collider so it doesn't trigger physics issues with raft
                var col = playerObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            var raftPlayer = playerObj.AddComponent<RaftPlayer>();
            raftPlayer.Initialize(raft, index, angle, color, isLocal);
            return raftPlayer;
        }

        private void ClearPlayers()
        {
            foreach (var p in spawnedPlayers)
            {
                if (p != null) Destroy(p.gameObject);
            }
            spawnedPlayers.Clear();
        }

        private void HandleObstacleHit()
        {
            if (currentState != RaftGameState.Playing) return;
            SetState(RaftGameState.GameOver);
        }

        private void HandleFinishReached()
        {
            if (currentState != RaftGameState.Playing) return;
            SetState(RaftGameState.Victory);
        }

        public void RestartGame()
        {
            if (raft != null)
            {
                raft.ResetPosition();
            }

            InitializeGame();
        }

        private void SetState(RaftGameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
    }
}
