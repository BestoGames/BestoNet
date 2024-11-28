using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using BestoNetSamples.Singleton;
using System.Collections;
using BestoNetSamples.BestoNet.Networking;
using BestoNetSamples.Player;
using BestoNetSamples.Utils;

namespace BestoNetSamples
{
    public class GameStateManager : SingletonBehaviour<GameStateManager>
    {
        [SerializeField] private string gameSceneName = "OldSystem";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        public int FrameNumber = 0;

        private Coroutine _positionUpdateCoroutine;
        private bool _isReturningToMenu;
        private int _pendingPlayerId; // Store the player ID until scene is loaded
        
        public event Action<string> OnNotification;
        
        public bool IsGameStarted { get; private set; }
        public bool HasJoinedPlayer { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            SceneManager.LoadScene(mainMenuSceneName);
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            NetworkManager.Instance.OnPacketReceived += HandleNetworkMessage;
            NetworkManager.Instance.OnConnectionStateChanged += HandleConnectionStateChanged;
            NetworkManager.Instance.OnConnectionFailed += HandleConnectionFailed;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == mainMenuSceneName)
            {
                // Clean up any existing game state when returning to menu
                if (_positionUpdateCoroutine != null)
                {
                    StopCoroutine(_positionUpdateCoroutine);
                    _positionUpdateCoroutine = null;
                }
                PlayerManager.Instance.ClearPlayers();
            }
            else if (scene.name == gameSceneName && IsGameStarted)
            {
                StartCoroutine(InitializeGameAfterSceneLoad());
            }
        }
        
        private IEnumerator InitializeGameAfterSceneLoad()
        {
            // Wait for scene to be fully loaded
            yield return WaitInstructionCache.EndOfFrame();
            yield return WaitInstructionCache.Seconds(0.2f); // Add small delay to ensure everything is ready

            Debug.Log($"Initializing game. IsHost: {NetworkManager.Instance.IsHost}, HasJoinedPlayer: {HasJoinedPlayer}, PlayerId: {_pendingPlayerId}");
    
            // Initialize and spawn local player
            PlayerManager.Instance.Initialize(_pendingPlayerId);

            // Spawn remote player if needed
            if (NetworkManager.Instance.IsHost)
            {
                if (HasJoinedPlayer)
                {
                    PlayerManager.Instance.SpawnRemotePlayer(2);
                }
            }
            else
            {
                PlayerManager.Instance.SpawnRemotePlayer(1);
            }
            StartPositionUpdates();
        }

        public void StartHosting()
        {
            if (IsGameStarted)
            {
                ShowNotification("A game is already in progress");
                return;
            }
            NetworkManager.Instance.StartHost();
            _pendingPlayerId = 1; // Set pending ID for host
            StartGame();
        }

        public void JoinGame()
        {
            if (IsGameStarted)
            {
                ShowNotification("Already in a game");
                return;
            }
            NetworkManager.Instance.StartClient();
            _pendingPlayerId = 2; // Set pending ID for client
            ShowNotification("Attempting to join game...");
        }

        private void StartGame()
        {
            if (IsGameStarted) return; // Prevent multiple starts
    
            IsGameStarted = true;
            SceneManager.LoadScene(gameSceneName);
        }

        private void StartPositionUpdates()
        {
            if (_positionUpdateCoroutine != null)
            {
                StopCoroutine(_positionUpdateCoroutine);
            }
            _positionUpdateCoroutine = StartCoroutine(SendPositionUpdates());
        }

        private IEnumerator SendPositionUpdates()
        {
            // Add initial delay to ensure game is properly initialized
            yield return WaitInstructionCache.Seconds(0.5f);

            while (IsGameStarted)
            {
                // Only send updates if:
                // 1. We're the host with a joined player
                // 2. We're a client and fully connected
                bool shouldSendUpdates = (NetworkManager.Instance.IsHost && HasJoinedPlayer) || 
                                         (!NetworkManager.Instance.IsHost && NetworkManager.Instance.IsConnected);
                               
                if (shouldSendUpdates)
                {
                    PlayerController localPlayer = PlayerManager.Instance.GetLocalPlayer();
                    if (localPlayer != null)
                    {
                        Vector3 position = localPlayer.GetPosition();
                        Quaternion rotation = localPlayer.GetRotation();
                        string updateMsg = $"POS:{position.x},{position.y},{position.z}:{rotation.x},{rotation.y},{rotation.z},{rotation.w}";
                        NetworkManager.Instance.SendNetworkMessage(updateMsg);
                    }
                }
                yield return WaitInstructionCache.Seconds(0.05f); // 20 updates per second
            }
        }

        private void HandleNetworkMessage(byte[] data)
        {
            string message = System.Text.Encoding.UTF8.GetString(data);
            if (message.StartsWith("POS:"))
            {
                // Only handle position updates if we're fully connected and in game
                if (IsGameStarted && NetworkManager.Instance.IsConnected)
                {
                    HandlePositionUpdate(message);
                }
                return;
            }
    
            switch (message)
            {
                case "JOIN_REQUEST":
                    if (NetworkManager.Instance.IsHost)
                    {
                        if (!HasJoinedPlayer)
                        {
                            HasJoinedPlayer = true;
                            NetworkManager.Instance.SendNetworkMessage("JOIN_ACCEPTED");

                            PlayerManager.Instance.SpawnRemotePlayer(2);
                            ShowNotification("Player 2 has joined!");
                        }
                    }
                    break;
            
                case "JOIN_ACCEPTED":
                    if (!NetworkManager.Instance.IsHost && !IsGameStarted)
                    {
                        StartGame();
                        ShowNotification("Successfully joined game!");
                    }
                    break;
            
                case "GAME_FULL":
                    if (!NetworkManager.Instance.IsHost)
                    {
                        ShowNotification("Game is full!");
                        ReturnToMainMenu();
                    }
                    break;
            
                case "PLAYER_DISCONNECTED":
                    if (NetworkManager.Instance.IsHost)
                    {
                        HandlePlayerDisconnected(2);
                    }
                    break;
            
                case "HOST_DISCONNECTED":
                    if (!NetworkManager.Instance.IsHost)
                    {
                        ShowNotification("Host disconnected");
                        ReturnToMainMenu();
                    }
                    break;
            }
        }
        
        private static void HandlePositionUpdate(string message)
        {
            string[] sections = message[4..].Split(':');
            if (sections.Length != 2) return;

            string[] positionParts = sections[0].Split(',');
            string[] rotationParts = sections[1].Split(',');

            if (positionParts.Length != 3 || rotationParts.Length != 4) return;

            Vector3 position = new(
                float.Parse(positionParts[0]),
                float.Parse(positionParts[1]),
                float.Parse(positionParts[2])
            );
            Quaternion rotation = new(
                float.Parse(rotationParts[0]),
                float.Parse(rotationParts[1]),
                float.Parse(rotationParts[2]),
                float.Parse(rotationParts[3])
            );

            int remotePlayerId = NetworkManager.Instance.IsHost ? 2 : 1;
            PlayerManager.Instance.UpdateRemotePlayerPosition(remotePlayerId, position, rotation);
        }

        private void HandleConnectionStateChanged(bool connected)
        {
            if (connected && !NetworkManager.Instance.IsHost)
            {
                // Send join request when connection is established for client
                NetworkManager.Instance.SendNetworkMessage("JOIN_REQUEST");
            }
            else if (!connected && !_isReturningToMenu)
            {
                ReturnToMainMenu();
            }
        }

        private void HandleConnectionFailed()
        {
            ShowNotification("Failed to connect. Try hosting instead.");
            ReturnToMainMenu();
        }

        private void HandlePlayerDisconnected(int playerId)
        {
            if (NetworkManager.Instance.IsHost)
            {
                HasJoinedPlayer = false;
                ShowNotification("Player 2 has disconnected");
            }
            PlayerManager.Instance.RemovePlayer(playerId);
            // If we're the client, return to menu
            if (!NetworkManager.Instance.IsHost)
            {
                ReturnToMainMenu();
            }
        }

        public void ReturnToMainMenu()
        {
            if (_isReturningToMenu) return;
        
            _isReturningToMenu = true;
        
            if (_positionUpdateCoroutine != null)
            {
                StopCoroutine(_positionUpdateCoroutine);
                _positionUpdateCoroutine = null;
            }
            NetworkManager.Instance.Disconnect();
            PlayerManager.Instance.ClearPlayers();
            IsGameStarted = false;
            HasJoinedPlayer = false;
            SceneManager.LoadScene(mainMenuSceneName);
            _isReturningToMenu = false;
        }

        public void ShowNotification(string message)
        {
            OnNotification?.Invoke(message);
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived -= HandleNetworkMessage;
                NetworkManager.Instance.OnConnectionStateChanged -= HandleConnectionStateChanged;
                NetworkManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
            }
            base.OnDestroy();
        }
    }
}