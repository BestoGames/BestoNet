// using System;
// using BestoNet.Collections;
// using BestoNet.Networking.Input;
// using BestoNet.Networking.State;
// using BestoNet.Networking.Structs;
// using BestoNet.Types;
// using BestoNetSamples.BestoNet.Networking.Input;
// using UnityEngine;

// namespace BestoNet.Networking
// {
//     /// <summary>
//     /// Core rollback manager that coordinates rollback networking functionality.
//     /// </summary>
//     public class RollbackManager: MonoBehaviour
//     {
//         public static RollbackManager Instance { get; private set; }
    
//         [SerializeField] private RollbackConfiguration config;
//         [SerializeField] private NetworkTransport transport;
//         [SerializeField] private UnityNewInputProvider inputProvider;
    
//         public GameStateManager StateManager { get; private set; }

//         private FrameMetadataArray _localInputs;
//         private FrameMetadataArray _remoteInputs;
//         private FrameMetadataArray _predictedInputs;
//         private CircularArray<GameState> _savedStates;
//         private CircularArray<int> _localAdvantages;
//         private CircularArray<int> _remoteAdvantages;

//         private int _currentFrame;
//         private int _syncFrame;
//         private int _lastRemoteFrame;
//         private ulong _lastPredictedInput;
//         private bool _isRollingBack;
//         private int _nextStateSaveFrame;

//         public event Action<int> OnRollbackStart;
//         public event Action<int> OnRollbackComplete;
//         public event Action<int> OnFrameDropped;
//         public event Action<NetworkStats> OnNetworkStatsUpdated;

//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 StateManager = new GameStateManager();
//             }
//             else
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             InitializeBuffers();
//             SubscribeToEvents();
//         }
        
//         private void InitializeBuffers()
//         {
//             _localInputs = new FrameMetadataArray(config.InputBufferSize);
//             _remoteInputs = new FrameMetadataArray(config.InputBufferSize);
//             _predictedInputs = new FrameMetadataArray(config.InputBufferSize);
//             _savedStates = new CircularArray<GameState>(config.StateBufferSize);
//             _localAdvantages = new CircularArray<int>(config.FrameAdvantageSize);
//             _remoteAdvantages = new CircularArray<int>(config.FrameAdvantageSize);
            
//             _nextStateSaveFrame = 0;
//         }

//         private void SubscribeToEvents()
//         {
//             if (transport != null)
//             {
//                 transport.OnInputReceived += HandleRemoteInput;
//                 transport.OnAdvantageReceived += HandleRemoteAdvantage;
//             }
//             else
//             {
//                 UnityEngine.Debug.LogError("Network Transport not assigned to RollbackManager");
//             }
//         }
        
//         private void OnDestroy()
//         {
//             if (transport != null)
//             {
//                 transport.OnInputReceived -= HandleRemoteInput;
//                 transport.OnAdvantageReceived -= HandleRemoteAdvantage;
//             }
//         }
        
//         public void Update()
//         {
//             if (!ShouldAdvanceFrame()) return;

//             // Check if we need to rollback
//             if (NeedsRollback(out int rollbackFrames))
//             {
//                 PerformRollback(rollbackFrames);
//             }

//             // Process current frame
//             ulong localInput = inputProvider.GetInput();
//             SendLocalInput(localInput);
            
//             InputPair inputs = SynchronizeInputs();
//             _currentFrame++;

//             // Save state periodically or at key frames. Not sure the best way to handle this.
//             if (ShouldSaveState(_currentFrame))
//             {
//                 SaveState();
//             }
//             // Update network stats.
//             UpdateNetworkStats();
//         }

//         private bool ShouldSaveState(int frame)
//         {
//             // Save state every N frames or when it's a sync frame
//             return frame >= _nextStateSaveFrame || frame == _syncFrame;
//         }

//         private bool HasInputForFrame(int frame)
//         {
//             return _localInputs.ContainsKey(frame) && 
//                    (_remoteInputs.ContainsKey(frame) || frame > _lastRemoteFrame + config.MaxRollbackFrames);
//         }

//         private bool NeedsRollback(out int frames)
//         {
//             frames = 0;
//             if (_isRollingBack) return false;

//             for (int i = _syncFrame + 1; i <= _currentFrame; i++)
//             {
//                 if (_remoteInputs.ContainsKey(i) && 
//                     _predictedInputs.ContainsKey(i) && 
//                     _remoteInputs.GetInput(i) != _predictedInputs.GetInput(i))
//                 {
//                     frames = _currentFrame - _syncFrame;
//                     return true;
//                 }
//             }
//             return false;
//         }

//         private void PerformRollback(int frames)
//         {
//             if (frames <= 0 || frames > config.MaxRollbackFrames) return;

//             _isRollingBack = true;
//             OnRollbackStart?.Invoke(frames);
            
//             // Load state from sync frame
//             LoadState(_syncFrame);
            
//             for (int i = _syncFrame + 1; i <= _currentFrame; i++)
//             {
//                 InputPair inputs = SynchronizeInputs();
//                 // Game update logic handled by external system
//                 if (ShouldSaveState(i))
//                 {
//                     SaveState();
//                 }
//             }
//             _isRollingBack = false;
//             OnRollbackComplete?.Invoke(frames);
//         }

//         private InputPair SynchronizeInputs()
//         {
//             ulong localInput = _localInputs.GetInput(_currentFrame);
//             ulong remoteInput = PredictRemoteInput(_currentFrame);
//             return new InputPair(localInput, remoteInput);
//         }

//         private ulong PredictRemoteInput(int frame)
//         {
//             if (_remoteInputs.ContainsKey(frame))
//             {
//                 _lastPredictedInput = _remoteInputs.GetInput(frame);
//                 return _lastPredictedInput;
//             }
//             FrameMetadata prediction = new(
//                 frame,
//                 _lastPredictedInput,
//                 DateTime.UtcNow.Ticks,
//                 0,
//                 1,
//                 false
//             );
//             // Store prediction
//             _predictedInputs.Insert(frame, prediction);
//             return _lastPredictedInput;
//         }

//         private void SaveState()
//         {
//             GameState state = new()
//             {
//                 Frame = _currentFrame,
//                 State = StateManager.SerializeState(),
//                 Checksum = StateManager.CalculateChecksum()
//             };
//             _savedStates.Insert(_currentFrame, state);
//             // Save every quarter buffer. not sure why it was set like this? Coming over from ISD.
//             // Probably can just create a constant value for save timing
//             _nextStateSaveFrame = _currentFrame + config.StateBufferSize / 4;
//         }

//         private void LoadState(int frame)
//         {
//             GameState state = _savedStates.Get(frame);
//             if (state.Frame != frame)
//             {
//                 throw new InvalidOperationException($"Missing state for frame {frame}");
//             }
//             StateManager.DeserializeState(state.State);
//             _currentFrame = frame;
//         }

//         private bool ShouldAdvanceFrame()
//         {
//             if (config.IsDelayBased && !HasInputForFrame(_currentFrame + 1))
//                 return false;

//             float advantage = CalculateFrameAdvantage();
//             if (advantage > config.MaxFrameAdvantage)
//             {
//                 OnFrameDropped?.Invoke(_currentFrame);
//                 return false;
//             }
//             return true;
//         }

//         private float CalculateFrameAdvantage()
//         {
//             int localSum = 0, remoteSum = 0;
//             for (int i = 0; i < config.FrameAdvantageCheckSize; i++)
//             {
//                 localSum += _localAdvantages.Get(i);
//                 remoteSum += _remoteAdvantages.Get(i);
//             }
//             float localAvg = (float)localSum / config.FrameAdvantageCheckSize;
//             float remoteAvg = (float)remoteSum / config.FrameAdvantageCheckSize;
//             return (localAvg - remoteAvg) / 2f;
//         }

//         private void SendLocalInput(ulong input)
//         {
//             if (_isRollingBack) return;

//             FrameMetadata metadata = new(
//                 _currentFrame,
//                 input,
//                 DateTime.UtcNow.Ticks,
//                 StateManager.CalculateChecksum(),
//                 0,
//                 false
//             );

//             _localInputs.Insert(_currentFrame, metadata);
            
//             int advantage = _currentFrame - _lastRemoteFrame;
//             _localAdvantages.Insert(_currentFrame, advantage);
//             transport.SendInput(_currentFrame, input, advantage);
//         }

//         private void HandleRemoteInput(int frame, ulong input)
//         {
//             var metadata = new FrameMetadata(
//                 frame,
//                 input,
//                 DateTime.UtcNow.Ticks,
//                 0,
//                 1,
//                 false
//             );
//             _remoteInputs.Insert(frame, metadata);
//             _lastRemoteFrame = frame;
//         }

//         private void HandleRemoteAdvantage(int frame, int advantage)
//         {
//             _remoteAdvantages.Insert(frame, advantage);
//         }

//         private void UpdateNetworkStats()
//         {
//             NetworkStats stats = new()
//             {
//                 LocalFrame = _currentFrame,
//                 RemoteFrame = _lastRemoteFrame,
//                 FrameAdvantage = CalculateFrameAdvantage(),
//                 RollbackFrames = _isRollingBack ? (_currentFrame - _syncFrame) : 0
//             };
//             OnNetworkStatsUpdated?.Invoke(stats);
//         }
//     }
// }