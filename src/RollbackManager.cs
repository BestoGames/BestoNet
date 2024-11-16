using System;
using IdolShowdown.Match;
using IdolShowdown.Networking;
using IdolShowdown.Platforms;
using UnityEngine;
using UnityGGPO;

namespace IdolShowdown.Managers
{
    public class RollbackManager : MonoBehaviour
    {
        public struct GameState {
            public int frame;
            public byte[] state;
        }
        public struct FrameMetadata {
            public int frame;
            public ulong input;
        }

        private LobbyManager lobbyManager => GlobalManager.Instance.LobbyManager;
        private MatchMessageManager matchManager => GlobalManager.Instance.MatchMessageManager;
        private MatchRunner matchRunner => GlobalManager.Instance.MatchRunner;
        public DesyncDetector desyncDetector { get; private set; } = null; 
        [SerializeField] public int LoadStateFrameDebug = 0;
        [Header("Mode Management")]
        [SerializeField] public bool AutosetDelay = false;
        [SerializeField] public int InputDelay = 0;
        [SerializeField] public bool DelayBased = false;
        [Header("Frame Dropping Management")]
        [SerializeField] public int MaxRollBackFrames = 4;
        [SerializeField] public int FrameAdvantageLimit = 3;
        [Header("Frame Extensions Management")]
        [SerializeField] public int SleepTimeMicro = 1500;
        [SerializeField] public float FrameExtensionLimit = 1.5f;
        [SerializeField] public int FrameExtensionWindow = 7;
        [Header("Match timeout")]
        [SerializeField] public int TimeoutFrames = 1000;
        [Header("Spectator")]
        [SerializeField] public int SpectatorDelayInFrames = 20;
        public const int StateArraySize = 60;
        public const int InputArraySize = 60;
        public const int FrameAdvantageArraySize = 48;
        public const int FrameAdvantageCheckSize = 32;
        public int RollbackFrames { get; private set; } = 0;
        public int RollbackFramesUI { get; private set; } = 0;
        public bool isRollbackFrame { get; private set; } = false;
        public bool physicsRollbackFrame { get; private set; } = false;
        private int lastDroppedFrame = -1;
        private PlatformUser client;
        private PlatformUser opponent;
        public FrameMetadataArray receivedInputs { get; private set; } = new FrameMetadataArray(InputArraySize);
        public FrameMetadataArray opponentInputs { get; private set; } = new FrameMetadataArray(InputArraySize);
        public FrameMetadataArray clientInputs { get; private set; } = new FrameMetadataArray(InputArraySize);
        public CircularArray<int> remoteFrameAdvantages {get; private set; } = new CircularArray<int>(FrameAdvantageArraySize);
        public CircularArray<int> localFrameAdvantages {get; private set; } = new CircularArray<int>(FrameAdvantageArraySize);
        public GameState[] states = new GameState[StateArraySize];
        private ulong opponentLastAppliedInput = 0; // defaults to standing still
        private int totalConsecutiveFrameExtensions = 0;
        public int remoteFrame { get; private set; } = 0;
        public int syncFrame { get; private set; } = 0;
        public int localFrameAdvantage {get; private set;} = 0;
        public int localFrame => matchRunner.FrameNumber;
        public void Init()
        {
            UnityEngine.Debug.Log("Initializing OnlineMatch connection");
            client = lobbyManager.LobbyMemberMe.userID == lobbyManager.getP1().userID ? lobbyManager.getP1() : lobbyManager.getP2();
            opponent = lobbyManager.LobbyMemberMe.userID == lobbyManager.getP1().userID ? lobbyManager.getP2() : lobbyManager.getP1();

            if (AutosetDelay)
            {
                InputDelay = GlobalManager.Instance.OnlineComponents.matchInfo.LobbyHelper.GetInputDelay();
            }

            ClearVars();
        }

        public void ClearVars()
        {
            receivedInputs.Clear();
            opponentInputs.Clear();
            clientInputs.Clear();
            remoteFrameAdvantages.Clear();
            localFrameAdvantages.Clear();

            remoteFrame = 0;
            syncFrame = 0;
            opponentLastAppliedInput = 0;
            totalConsecutiveFrameExtensions = FrameExtensionWindow;
            matchManager.sentFrameTimes.Clear();

            for (int i = 0; i < StateArraySize; i++)
            {
                states[i] = new GameState(){
                    frame = -1,
                    state = new byte[0]
                };
            }

            for (int i = 0; i <= InputDelay; i++)
            {
                clientInputs.Insert(i, new FrameMetadata(){
                    frame = i,
                    input = 0
                });
                opponentInputs.Insert(i, new FrameMetadata(){
                    frame = i,
                    input = 0
                });
                receivedInputs.Insert(i, new FrameMetadata(){
                    frame = i,
                    input = 0
                });
            }
        }

        public void RollbackEvent()
        {   
            if(DelayBased || GlobalManager.Instance.GameStateManager.MatchEnded)
            {
                return;
            }
            
            SetRollbackStatus(true);
            RollbackFrames = 0;
            int framesBeforeRollback = localFrame;

            bool foundDesyncedFrame = false;
            for (int i = syncFrame + 1; i <= framesBeforeRollback; i++)
            {
                /* Do not rollback for frames that we have predicted correctly */
                if (receivedInputs.ContainsKey(i) && opponentInputs.ContainsKey(i) && opponentInputs.GetInput(i) == receivedInputs.GetInput(i) && states[i % StateArraySize].frame == i)
                {
                    syncFrame = i;   
                }
                /* Only perform rollbacks if we find a desynced frame, since we don't know if the predicted input is right or wrong yet */
                else if (receivedInputs.ContainsKey(i) && opponentInputs.ContainsKey(i) && opponentInputs.GetInput(i) != receivedInputs.GetInput(i))
                {
                    foundDesyncedFrame = true;
                    break;
                }
            }

            if (!foundDesyncedFrame)
            {
                SetRollbackStatus(false);
                return;
            }

            // Debug.Log(string.Format("Sync frame {0}, Local Frame {1}, Remote Frame {2}", syncFrame, framesBeforeRollback, remoteFrame));
            if (syncFrame < remoteFrame && syncFrame < localFrame)
            {
                LoadState(syncFrame);           
                RollbackFrames = framesBeforeRollback - syncFrame;
                RollbackFramesUI = Math.Max(RollbackFramesUI, RollbackFrames);

                // Debug.Log(string.Format("Resimulating from {0} to {1}", syncFrame, framesBeforeRollback));
                for (int i = syncFrame + 1; i <= framesBeforeRollback; i++)
                {
                    ((OnlineMatch)GlobalManager.Instance.MatchRunner.CurrentMatch).TimeUpdate();
                    ulong [] inputs = SynchronizeInput();
                    GlobalManager.Instance.MatchRunner.CurrentMatch.UpdateByFrame(inputs);
                    /* Speculative saving */ 
                    if (i == remoteFrame || syncFrame + Mathf.Floor(RollbackFrames / 2) == i)
                    {
                        SaveState();
                    }
                    else
                    {
                        ClearState(i);
                    }
                }
            } 

            SetRollbackStatus(false);
        }

        public bool SendLocalInput(ulong input) 
        {
            if (opponent == null || isRollbackFrame)
            {
                return false;
            }
            matchManager.SendInputs(opponent.userID, matchRunner.FrameNumber + InputDelay, input);
            return true;
        }

        public bool AllowUpdate()
        {
            /* Check if we have input for the next frame */
            int frame = matchRunner.FrameNumber;
            if (!receivedInputs.ContainsKey(frame))
            {
                if (DelayBased || frame < 10)
                {
                    return false;
                }
            } 

            return true;
        }
        public ulong[] SynchronizeInput()
        {
            int frame = matchRunner.FrameNumber;
            
            ulong opponentInput = PredictOpponentInput(frame, out bool found);
            if (client.userRank == PlayerLobbyType.playerOne)
            {
                return new ulong[2] {clientInputs.GetInput(frame), opponentInput};
            }
            else
            {
                return new ulong[2] {opponentInput, clientInputs.GetInput(frame)};
            }
        }
        
        private ulong PredictOpponentInput(int frame, out bool found)
        {
            if (receivedInputs.ContainsKey(frame))
            {
                found = true;
                opponentLastAppliedInput = receivedInputs.GetInput(frame);
                opponentInputs.Insert(frame, receivedInputs.Get(frame));
                return receivedInputs.GetInput(frame);
            }
            else
            {
                found = false;
                opponentInputs.Insert(frame, new FrameMetadata(){
                    frame = frame,
                    input = opponentLastAppliedInput
                });
                return opponentLastAppliedInput;
            }
        }

        public void SaveState()
        {
            byte[] gameState = GlobalManager.Instance.OnStageObjects.ToBytes();
            int checksum = Utils.CalcFletcher32(gameState);
            states[localFrame % StateArraySize] = new GameState(){
                frame = localFrame,
                state = gameState
            };
            GlobalManager.Instance.MatchRunner.CurrentMatch.demoRecorder.RecordLogic(gameState, localFrame, checksum);
        }

        public void ClearState(int frame)
        {
            states[frame % StateArraySize].frame = -1;
        }

        public void LoadState(int frame)
        {   
            if(states[frame % StateArraySize].frame != frame)
            {
                UnityEngine.Debug.Log("Missing state when loading from frame " + frame);
                return;
            }
            GlobalManager.Instance.OnStageObjects.FromBytes(states[frame % StateArraySize].state);
            GlobalManager.Instance.MatchRunner.CurrentMatch.ForceSetFrame(frame);
            GlobalManager.Instance.MatchRunner.CurrentMatch.UpdatePhysics(true);
        }

        public void ExtendFrame()
        {
            if (FPSLock.Instance.EnableRateLock == false)
            {
                return;
            }

            if (totalConsecutiveFrameExtensions < FrameExtensionWindow)
            {
                totalConsecutiveFrameExtensions++;
            }
            else
            {
                FPSLock.Instance.SetFrameExtension(0);
            }
        }

        public void StartFrameExtensions(float frameAdvantageDiffereence)
        {
            if (FPSLock.Instance.EnableRateLock == false)
            {
                return;
            }

            if (totalConsecutiveFrameExtensions == FrameExtensionWindow)
            {
                Debug.Log(string.Format("Local frame {1}, Frame Advantage {0}", frameAdvantageDiffereence, localFrame));
                FPSLock.Instance.SetFrameExtension(SleepTimeMicro);
                totalConsecutiveFrameExtensions = 0;
            }
        }

        public bool CheckTimeSync(out float frameAdvantageDifference)
        {
            localFrameAdvantage = localFrame - remoteFrame;
            SetLocalFrameAdvantage(localFrameAdvantage);
            frameAdvantageDifference = GetAverageFrameAdvantage();
            if (localFrameAdvantage > MaxRollBackFrames && !isRollbackFrame)
            {
                Debug.Log(string.Format("Local frame {4}, localFrameAdvantage {2}:{3}, Dropping frame", frameAdvantageDifference, FrameAdvantageLimit, localFrameAdvantage, MaxRollBackFrames, localFrame));
                lastDroppedFrame = localFrame;
                return false;
            }

            if (localFrame == lastDroppedFrame)
            {
                return true;
            }

            if (frameAdvantageDifference > FrameAdvantageLimit && !isRollbackFrame)
            {
                Debug.Log(string.Format("Local frame {4}, Frame Difference {0}:{1}, Dropping frame", frameAdvantageDifference, FrameAdvantageLimit, localFrameAdvantage, MaxRollBackFrames, localFrame));
                lastDroppedFrame = localFrame;
                return false;
            }
            return true;
        }

        public void SetRollbackStatus(bool status)
        {
            isRollbackFrame = status; 
        }

        public void SetPhysicsRollbackStatus(bool status)
        {
            physicsRollbackFrame = status; 
        }

        public void ResetRollbackFrames()
        {
            RollbackFrames = 0;
        }

        public void SetClientInput(int frame, ulong input)
        {
            clientInputs.Insert(frame, new FrameMetadata(){
                frame = frame,
                input = input
            });
        }

        public void SetOpponentInput(int frame, ulong input)
        {
            receivedInputs.Insert(frame, new FrameMetadata(){
                frame = frame,
                input = input
            });
            remoteFrame = frame;
        }

        public void SetRemoteFrameAdvantage(int recFrame, int recAdvantage)
        {
            remoteFrameAdvantages.Insert(recFrame, recAdvantage);
        }

        public void SetLocalFrameAdvantage(int advantage)
        {
            localFrameAdvantages.Insert(localFrame, advantage);
        }

        public float GetAverageFrameAdvantage()
        {
            int remoteAverage = 0;
            int localAverage = 0;
            for (int i = 0; i < FrameAdvantageCheckSize; i++)
            {
                localAverage += localFrameAdvantages.Get(i);
                remoteAverage += remoteFrameAdvantages.Get(i);
            }
            float remoteAverageFloat = (float)remoteAverage / FrameAdvantageArraySize;
            float localAverageFloat = (float)localAverage / FrameAdvantageArraySize;
            return (localAverageFloat - remoteAverageFloat) / 2f;
        }

        public void ResetUIRollbackFrames()
        {
            RollbackFramesUI = 0;
        }
        public void DesyncCheck()
        {
            if (GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank != PlayerLobbyType.spectator)
                desyncDetector.GetFrameSendToOpponent();
        }

        public void InitDesyncDetector()
        {
            if (desyncDetector == null)
            {
                desyncDetector = gameObject.AddComponent<DesyncDetector>();
                desyncDetector.Initialize();
            }
        }

        public void TriggerDesyncedStatus()
        {
            Disconnect();
            
            GlobalManager.Instance.MatchRunner.CurrentMatch.SaveDemoLogic(string.Format("Desync: {0} {1} V {2} {3}", GlobalManager.Instance.GameManager.GetPlayer(PlayerNumber.playerOne)?.charName, GlobalManager.Instance.GameManager.Collabs[0].charName, GlobalManager.Instance.GameManager.GetPlayer(PlayerNumber.playerTwo)?.charName, GlobalManager.Instance.GameManager.Collabs[1].charName, Application.version)); // Save the demo
            GlobalManager.Instance.LobbyManager.UpdateLastPlayerInfo();
            TerminateMatch(Localization.Localization.GetLocalized("DISCONNECT_REASON_TIMEOUT"));
        }

        public void TriggerMatchTimeout()
        {
            Disconnect();
            TerminateMatch(Localization.Localization.GetLocalized("DISCONNECT_REASON_TIMEOUT"));
        }

        public void Disconnect()
        {
            UnityEngine.Debug.Log("Disconnecting OnlineMatch connection");
            ClearVars();
            ((OnlineMatch)GlobalManager.Instance.MatchRunner.CurrentMatch).ResetMatchVars();  
            GlobalManager.Instance.RollbackManager.SetRollbackStatus(false);
        }

        private void DebugLoadState()
        {
#if UNITY_EDITOR
            LoadState(LoadStateFrameDebug);
#endif
        }
    }
}
