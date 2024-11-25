using System;
using System.Collections.Generic;
using System.Linq;
using BestoNetSamples.Singleton;
using UnityEngine;

namespace BestoNetSamples.Player
{
    public class PlayerManager : SingletonBehaviour<PlayerManager>
    {
        [SerializeField] private GameObject playerPrefab;
        
        [Header("Player Materials")]
        [SerializeField] private Material player1Material; // Blue material
        [SerializeField] private Material player2Material; // Red material
        
        private readonly Dictionary<int, GameObject> _players = new();
        private static readonly Vector3[] SpawnPositions = {
            new(-2f, -0.5f, 0f), // Player 1
            new(2f, -0.5f, 0f)   // Player 2
        };

        public event Action<int> OnPlayerDisconnected;

        public int LocalPlayerId { get; private set; }
        public bool HasPlayer(int playerId) => _players.ContainsKey(playerId);

        public void Initialize(int localPlayerId)
        {
            LocalPlayerId = localPlayerId;
            SpawnLocalPlayer();
        }

        public PlayerController GetLocalPlayer()
        {
            if (_players.TryGetValue(LocalPlayerId, out GameObject playerObj))
            {
                return playerObj.GetComponent<PlayerController>();
            }
            return null;
        }

        public PlayerController GetPlayer(int playerId)
        {
            if (_players.TryGetValue(playerId, out GameObject playerObj))
            {
                return playerObj.GetComponent<PlayerController>();
            }
            return null;
        }

        public IEnumerable<PlayerController> GetAllPlayers()
        {
            return _players.Values
                .Select(p => p.GetComponent<PlayerController>())
                .Where(p => p != null);
        }

        public void SpawnLocalPlayer()
        {
            SpawnPlayer(LocalPlayerId, true);
        }

        public void SpawnRemotePlayer(int playerId)
        {
            SpawnPlayer(playerId, false);
        }

        private void SpawnPlayer(int playerId, bool isLocal)
        {
            if (_players.ContainsKey(playerId))
            {
                Destroy(_players[playerId]);
                _players.Remove(playerId);
            }
            Vector3 spawnPosition = SpawnPositions[playerId - 1];
            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.name = $"Player{playerId}({(isLocal ? "Local" : "Remote")})";
            PlayerController controller = playerInstance.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetIsLocal(isLocal);
                controller.SetPlayerId(playerId);
            }
            SetPlayerMaterial(playerInstance, playerId);
            _players[playerId] = playerInstance;
            UnityEngine.Debug.Log($"Spawned {(isLocal ? "local" : "remote")} player {playerId}");
        }
        
        private void SetPlayerMaterial(GameObject playerObject, int playerId)
        {
            MeshRenderer meshRenderer = playerObject.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = playerId == 1 ? player1Material : player2Material;
                UnityEngine.Debug.Log($"Setting player {playerId} material to {(playerId == 1 ? "Player1" : "Player2")} material");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Could not find MeshRenderer on player {playerId}");
            }
        }

        public void UpdateRemotePlayerPosition(int playerId, Vector3 position, Quaternion rotation)
        {
            if (!_players.TryGetValue(playerId, out GameObject playerObj)) return;
            
            PlayerController controller = playerObj.GetComponent<PlayerController>();
            if (controller != null && !controller.IsLocal)
            {
                controller.SetPosition(position);
                controller.SetRotation(rotation);
            }
        }

        public void RemovePlayer(int playerId)
        {
            if (!_players.TryGetValue(playerId, out GameObject playerObj)) return;
            
            UnityEngine.Debug.Log($"Removing player {playerId}");
            Destroy(playerObj);
            _players.Remove(playerId);
            OnPlayerDisconnected?.Invoke(playerId);
        }

        public void ClearPlayers()
        {
            UnityEngine.Debug.Log("Clearing all players");
            foreach (GameObject player in _players.Values)
            {
                Destroy(player);
            }
            _players.Clear();
        }

        protected override void OnDestroy()
        {
            ClearPlayers();
        }
    }
}