using BestoNet.Networking.Input;
using BestoNetSamples.BestoNet.Networking;
using BestoNetSamples.BestoNet.Networking.Input;
using UnityEngine;

namespace BestoNetSamples.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")] 
        [SerializeField] private bool isLocal = true;
        [SerializeField] private int playerId;
        [SerializeField] private float moveSpeed = 20f;
        
        [Header("Interpolation Settings")]
        [SerializeField] private float interpolationSpeed = 15f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float maxInterpolationDistance = 3f;
        
        private ulong _lastProcessedInput;
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private Quaternion _targetRotation;
        
        public int PlayerId => playerId;
        public bool IsLocal => isLocal;
        
        private void Awake()
        {
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
        }

        private void Update()
        {
            if (isLocal)
            {
                HandleInput();
                CheckEscapeKey();
            }
            UpdateMovement();
            UpdateRotation();
        }

        private void CheckEscapeKey()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            
            if (NetworkManager.Instance.IsHost)
            {
                // Host is disconnecting - notify clients and return to menu
                NetworkManager.Instance.SendMessage("HOST_DISCONNECTED");
                GameStateManager.Instance.ReturnToMainMenu();
            }
            else
            {
                // Client is disconnecting - notify host and return to menu
                NetworkManager.Instance.SendMessage("PLAYER_DISCONNECTED");
                GameStateManager.Instance.ReturnToMainMenu();
            }
        }
        
        public void SetIsLocal(bool local)
        {
            isLocal = local;
        }

        public void SetPlayerId(int newId)
        {
            playerId = newId;
        }

        private void HandleInput()
        {
            if (!isLocal) return;
            
            _lastProcessedInput = UnityNewInputProvider.Instance.GetInput();
            Vector3 movement = Vector3.zero;
            
            if ((_lastProcessedInput & (ulong)InputFlag.Up) != 0) movement.z += 1;
            if ((_lastProcessedInput & (ulong)InputFlag.Down) != 0) movement.z -= 1;
            if ((_lastProcessedInput & (ulong)InputFlag.Left) != 0) movement.x -= 1;
            if ((_lastProcessedInput & (ulong)InputFlag.Right) != 0) movement.x += 1;
            
            if (movement != Vector3.zero)
            {
                movement.Normalize();
                _moveDirection = movement;
                _targetPosition += movement * (moveSpeed * Time.deltaTime);
                // Clamp maximum distance to prevent teleporting
                Vector3 offset = _targetPosition - transform.position;
                if (offset.magnitude > maxInterpolationDistance)
                {
                    _targetPosition = transform.position + offset.normalized * maxInterpolationDistance;
                }
                // Update rotation target when moving
                _targetRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);
            }
        }

        private void UpdateMovement()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _targetPosition,
                ref _velocity,
                1f / interpolationSpeed
            );
        }

        private void UpdateRotation()
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                _targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }

        public void SetPosition(Vector3 position)
        {
            if (!isLocal)
            {
                _targetPosition = position;
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            if (!isLocal)
            {
                _targetRotation = rotation;
            }
        }

        public Vector3 GetPosition() => _targetPosition;
        public Quaternion GetRotation() => _targetRotation;
        public ulong GetLastProcessedInput() => _lastProcessedInput;
    }
}