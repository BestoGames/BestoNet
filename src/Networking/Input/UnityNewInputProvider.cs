using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using BestoNet.Networking.Interfaces;

namespace BestoNet.Networking.Input
{
    public class UnityNewInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private InputActionAsset actionAsset;
        [SerializeField] private int historyFrames = 100;

        [Header("Input Mappings")]
        [SerializeField] private InputMapping[] inputMappings;
        
        public InputHistoryRecorder History { get; private set; }

        [System.Serializable]
        public class InputMapping
        {
            public string actionName;
            public InputFlag flag;
        }

        private readonly Dictionary<string, InputFlag> _inputFlagMapping = new();
        private ulong _currentFrameInput;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }
        
        private void Update()
        {
            History.RecordInput(_currentFrameInput, Time.frameCount);
        }

        private void OnEnable()
        {
            foreach (InputActionMap actionMap in actionAsset.actionMaps)
            {
                actionMap.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (InputActionMap actionMap in actionAsset.actionMaps)
            {
                actionMap.Disable();
            }
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            
            History = new InputHistoryRecorder(historyFrames);
            
            // Set up input mappings
            foreach (InputMapping mapping in inputMappings)
            {
                _inputFlagMapping[mapping.actionName] = mapping.flag;
            }

            // Set up input actions
            foreach (InputActionMap actionMap in actionAsset.actionMaps)
            {
                foreach (InputAction action in actionMap.actions)
                {
                    action.started += ctx => HandleInput(action.name, true);
                    action.canceled += ctx => HandleInput(action.name, false);
                }
            }
            _isInitialized = true;
        }

        private void HandleInput(string actionName, bool value)
        {
            if (_inputFlagMapping.TryGetValue(actionName, out InputFlag flag))
            {
                if (value)
                {
                    _currentFrameInput |= (ulong)flag;
                }
                else
                {
                    _currentFrameInput &= ~(ulong)flag;
                }
            }
        }

        public void ClearInputs()
        {
            _currentFrameInput = 0;
        }

        public ulong GetInput()
        {
            return _currentFrameInput;
        }
        
        public bool CheckInput(InputFlag flag)
        {
            return (_currentFrameInput & (ulong)flag) == (ulong)flag;
        }

        private void OnValidate()
        {
            if (actionAsset == null) return;
            
            // Validate input mappings against action asset
            foreach (InputMapping mapping in inputMappings)
            {
                bool found = false;
                foreach (InputActionMap actionMap in actionAsset.actionMaps)
                {
                    if (actionMap.FindAction(mapping.actionName) != null)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    UnityEngine.Debug.LogWarning($"Action '{mapping.actionName}' not found in Input Action Asset");
                }
            }
        }
    }
}