/*
*  Author: IotaBoyBad (Rinat Satybaldinov)
*  Class Description: Match logic during Online
*/

using IdolShowdown.Managers;
using IdolShowdown.Platforms;
using IdolShowdown.Networking;
using IdolShowdown.Demo;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace IdolShowdown.Match
{
    using static ISConstants;

    public class OnlineMatch : IdolMatch
    {
        bool started;
        private ulong localPlayerInput = 0;
        private ulong[] syncedInput = new ulong[2] { 0, 0 };
        private LobbyManager lm => GlobalManager.Instance.LobbyManager;
        private RollbackManager rollbackManager => GlobalManager.Instance.RollbackManager;
        private bool isPlayer1Local = false;
        private ISSpectator spectatorLogic;
        private PVPDemoRecorder recorder;

        public override void InitMatch()
        {
            frameNumber = 0;
            StartMe();
            localPlayerIndex = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne ? 0 : 1;
            remotePlayerIndex = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne ? 1 : 0;
            //demoFileManager.PrepSavingDemo(GetFighterNames());

            isPlayer1Local = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne;
            spectatorLogic = FindObjectOfType<IdolShowdown.Networking.ISSpectator>();
            // Reset spectator logic
            spectatorLogic.ResetMe();

            if (TryGetComponent(out recorder) == false)
            {
                recorder = gameObject.AddComponent<PVPDemoRecorder>();
            }


            if (GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank != PlayerLobbyType.spectator)
            {
                GlobalManager.Instance.RollbackManager.Init();
            }

            recorder.Init();
        }

        // Get called when match is killed/closed.
        public override void CloseMatch()
        {
            // GlobalManager.Instance.OnlineComponents.ggpoMatchManager.Disconnect();
            GlobalManager.Instance.UIManager.GameplayUIHandler.StopComponents();
            GlobalManager.Instance.GameStateManager.StopMatchLogic();

            // Reset key vars
            runMatchLogic = false;
            started = false;

            // Reset/Turn off online related stuff
            if (GlobalManager.Instance.OnlineComponents.matchInfo.IsReconnecting == false && GlobalManager.Instance.LobbyManager.ChangingSettings == false)
            {
                GlobalManager.Instance.LobbyManager.LeaveLobby();
            }
            GlobalManager.Instance.RollbackManager.SetRollbackStatus(false);
        }

        public void ResetMatchVars()
        {
            runMatchLogic = false;
            started = false;
        }

        public override void UpdateMatch()
        {
            // Bool that prevents regular update logic from running during stage intro animation.
            if (runMatchLogic == true && !GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated)
            {
                // Turns on GUI 
                if (started == false)
                {
                    GlobalManager.Instance.UIManager.MatchStart();
                    GlobalManager.Instance.UIManager.GameplayUIHandler.UpdateComponents();

                    rollbackManager.desyncDetector.SetDemoRecorder(recorder);

                    started = true;
                }

                if (FrameNumber <= rollbackManager.InputDelay)
                {
                    recorder.RecordDebugFrame(frameNumber, syncedInput);
                    recorder.RecordFrame(frameNumber);
                    GlobalManager.Instance.RollbackManager.SaveState();
                }

                localPlayerInput = 0;
                // Clear input when game is paused 
                if (IsPaused == false && charComponents[localPlayerIndex].StateManager.InCountdownPause == false)
                    localPlayerInput = charComponents[localPlayerIndex].Input.ReadInput();

                syncedInput[0] = 0;
                syncedInput[1] = 0;

                bool TimeSynced = rollbackManager.CheckTimeSync(out float frameAdvantageDifference);
                if (TimeSynced)
                {
                    GlobalManager.Instance.MatchMessageManager.SetInputBufferSize();
                    rollbackManager.RollbackEvent();

                    // Save frame 0
                    if (isPlayer1Local && frameNumber <= GlobalManager.Instance.RollbackManager.InputDelay)
                        spectatorLogic.P1AddToBuffer(syncedInput, frameNumber);

                    TimeUpdate();
                    rollbackManager.SendLocalInput(localPlayerInput);
                    syncedInput = rollbackManager.SynchronizeInput();

                    /* When in delay based mode, do not update this frame and wait instead*/
                    if (!rollbackManager.AllowUpdate())
                    {
                        frameNumber--;
                        return;
                    }

                    GameUpdate(syncedInput);

                    // Update match UI
                    GlobalManager.Instance.UIManager.GameplayUIHandler.UpdateMe();
                    rollbackManager.DesyncCheck();


                    if (isPlayer1Local && frameNumber % 60 == 0)
                    {
                        GlobalManager.Instance.LobbyManager.SpectatorLogic.SpectatorP1Update();
                    }

                    if (frameNumber % 600 == 0)
                    {
                        GlobalManager.Instance.RollbackManager.UpdateInputDelay();
                    }

                    if (frameAdvantageDifference > rollbackManager.FrameExtensionLimit)
                    {
                        rollbackManager.StartFrameExtensions(frameAdvantageDifference);
                    }

                    rollbackManager.ExtendFrame();

                }
            }
        }

        private void GameUpdate(ulong[] inputs)
        {
            // Update game rules
            gameStateManager.UpdateMe();
            ReadInputOnline(inputs);
            AddSpectatorFrames();
            RecordInputs();
            UpdateGeneral();
            SyncTransform();
            UpdatePhysics();
            UpdatePhysicsManager();
            recorder.RecordDebugFrame(frameNumber, inputs);

            if (!rollbackManager.DelayBased)
            {
                if (!rollbackManager.isRollbackFrame)
                {
                    rollbackManager.SaveState();
                }
            }
        }

        public override void UpdateByFrame(ulong[] inputs, bool readInput = true)
        {
            GameUpdate(inputs);
        }

        public void ReadInputOnline(ulong[] inputs)
        {
            lastInputs = inputs;
            charComponents[0].Input.ParseInput(inputs[0]);
            charComponents[1].Input.ParseInput(inputs[1]);
        }

        public override void InitInput()
        {
            // Initialize Gameplay Input
            bool isPlayerOne = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne;
            charComponents[isPlayerOne ? 0 : 1].Input.InitializeControls(false);
            charComponents[isPlayerOne ? 1 : 0].Input.InitializeControls(true);
        }

        public override void TimeUpdate()
        {
            frameNumber++;
        }

        public void AddSpectatorFrames()
        {
            if (rollbackManager.isRollbackFrame)
            {
                return;
            }

            if (isPlayer1Local)
            {
                // Save last inputs
                int frameDelay = frameNumber - rollbackManager.SpectatorDelayInFrames;
                if (frameDelay > 0 && rollbackManager.receivedInputs.ContainsKey(frameDelay) && rollbackManager.clientInputs.ContainsKey(frameDelay))
                {
                    spectatorLogic.P1AddToBuffer(new ulong[] { rollbackManager.clientInputs.Get(frameDelay).input, rollbackManager.receivedInputs.Get(frameDelay).input }, frameDelay);
                }
            }
        }

        public void RecordInputs()
        {
            if (rollbackManager.isRollbackFrame)
            {
                return;
            }

            int frameDelay = frameNumber - rollbackManager.SpectatorDelayInFrames;
            if (frameDelay >= 0 && rollbackManager.receivedInputs.ContainsKey(frameDelay) && rollbackManager.clientInputs.ContainsKey(frameDelay))
            {
                if (GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne)
                {
                    recorder.RecordFrame(frameDelay, new ulong[2] { rollbackManager.clientInputs.Get(frameDelay).input, rollbackManager.receivedInputs.Get(frameDelay).input });
                }
                else
                {
                    recorder.RecordFrame(frameDelay, new ulong[2] { rollbackManager.receivedInputs.Get(frameDelay).input, rollbackManager.clientInputs.Get(frameDelay).input });
                }
            }
        }

        public override void SaveDemo()
        {
            recorder.MarkDemoEnd();
            GlobalManager.Instance.DemoManager.SaveDemoToFile(recorder.GetDemo());
        }

        public void SaveDemoDesync()
        {
            SaveDebugInputs();
            recorder.MarkDemoEnd();
            GlobalManager.Instance.DemoManager.SaveDemoToFileDesync(recorder.GetDemo());
        }

        public void SaveDebugInputs()
        {
            string[] p1Logs = recorder.GetP1DesyncLogs();
            string[] p2Logs = recorder.GetP2DesyncLogs();
            GlobalManager.Instance.FilesManager.SaveToLog("P2Desync.log", p2Logs);
            GlobalManager.Instance.FilesManager.SaveToLog("P1Desync.log", p1Logs);
        }
    }
}