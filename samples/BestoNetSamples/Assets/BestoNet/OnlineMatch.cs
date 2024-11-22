// /*
// *  Author: Iota
// *  Class Description: Match logic during Online
// */
//
// using IdolShowdown.Managers;
// using IdolShowdown.Platforms;
// using IdolShowdown.Networking;
//
// namespace IdolShowdown.Match
// {
//     using static ISConstants;
//
//     public class OnlineMatch : IdolMatch
//     {
//         bool started;
//         private ulong localPlayerInput = 0;
//         private ulong[] syncedInput = new ulong[2] { 0, 0 };
//         private LobbyManager lm => GlobalManager.Instance.LobbyManager;
//         private RollbackManager rollbackManager => GlobalManager.Instance.RollbackManager;
//         private bool isPlayer1Local = false;
//         private ISSpectator spectatorLogic;
//         private int timeoutFrames = 0;
//
//         public override void InitMatch()
//         {
//             frameNumber = 0;
//             StartMe();
//             localPlayerIndex = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne ? 0 : 1;
//             remotePlayerIndex = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne ? 1 : 0;
//             demoFileManager.PrepSavingDemo(GetFighterNames());
//
//             isPlayer1Local = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne;
//             spectatorLogic = FindObjectOfType<IdolShowdown.Networking.ISSpectator>();
//             // Reset spectator logic
//             spectatorLogic.ResetMe();
//
//             rollbackManager.InitDesyncDetector();
//             if (GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank != PlayerLobbyType.spectator)
//             {
//                 GlobalManager.Instance.RollbackManager.Init();
//             }
//
//         }
//
//         // Get called when match is killed/closed.
//         public override void CloseMatch()
//         {
//             // GlobalManager.Instance.OnlineComponents.ggpoMatchManager.Disconnect();
//             GlobalManager.Instance.UIManager.GameplayUIHandler.StopComponents();
//             GlobalManager.Instance.GameStateManager.StopMatchLogic();
//             
//             // Reset key vars
//             runMatchLogic = false;
//             started = false;
//
//             // Reset/Turn off online related stuff
//             if (GlobalManager.Instance.OnlineComponents.matchInfo.IsReconnecting == false && GlobalManager.Instance.LobbyManager.ChangingSettings == false)
//             {
//                 GlobalManager.Instance.LobbyManager.LeaveLobby();
//             }
//             GlobalManager.Instance.RollbackManager.SetRollbackStatus(false);
//         }
//
//         public void ResetMatchVars()
//         {
//             runMatchLogic = false;
//             started = false;
//         }
//
//         public override void UpdateMatch()
//         {
//             // Bool that prevents regular update logic from running during stage intro animation.
//             if (runMatchLogic == true && !GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated)
//             {
//                 // Turns on GUI 
//                 if (started == false)
//                 {
//                     GlobalManager.Instance.UIManager.MatchStart();
//                     GlobalManager.Instance.UIManager.GameplayUIHandler.UpdateComponents();
//                     StartDemoLogic();
//                     started = true;
//                 }
//                 
//                 if (FrameNumber == 0)
//                 {
//                     GlobalManager.Instance.RollbackManager.SaveState();
//                 }
//                                 
//                 localPlayerInput = 0;
//                 // Clear input when game is paused 
//                 if (IsPaused == false && charComponents[localPlayerIndex].StateManager.InCountdownPause == false)
//                     localPlayerInput = charComponents[localPlayerIndex].Input.ReadInput();
//
//                 syncedInput[0] = 0;
//                 syncedInput[1] = 0;
//                 
//                 bool TimeSynced = rollbackManager.CheckTimeSync(out float frameAdvantageDifference);
//                 if (TimeSynced)
//                 {
//                     timeoutFrames = 0;
//                     rollbackManager.RollbackEvent();
//
//                     // Save frame 0
//                     if (isPlayer1Local && frameNumber <= GlobalManager.Instance.RollbackManager.InputDelay)
//                         spectatorLogic.P1AddToBuffer(syncedInput, frameNumber);
//
//
//                     TimeUpdate();
//                     rollbackManager.SendLocalInput(localPlayerInput);
//                     syncedInput = rollbackManager.SynchronizeInput();
//                     
//                     /* When in delay based mode, do not update this frame and wait instead*/
//                     if(!rollbackManager.AllowUpdate())
//                     {
//                         frameNumber--;
//                         return;
//                     }
//
//                     GameUpdate(syncedInput);
//
//                     // Update match UI
//                     GlobalManager.Instance.UIManager.GameplayUIHandler.UpdateMe();
//                     rollbackManager.DesyncCheck();
//                     
//
//                     if (isPlayer1Local && frameNumber % 60 == 0)
//                     {
//                         GlobalManager.Instance.LobbyManager.SpectatorLogic.SpectatorP1Update();
//                     } 
//
//                     if (frameAdvantageDifference > rollbackManager.FrameExtensionLimit)
//                     {
//                         rollbackManager.StartFrameExtensions(frameAdvantageDifference);
//                     }
//                     
//                     rollbackManager.ExtendFrame();
//                     
//                 }
//                 else
//                 {
//                     timeoutFrames++;
//                     if (timeoutFrames > rollbackManager.TimeoutFrames)
//                     {
//                         rollbackManager.TriggerMatchTimeout();
//                     }
//
//                 }
//             }
//         }
//
//         private void GameUpdate(ulong[] inputs)
//         {
//             // Update game rules
//             gameStateManager.UpdateMe();
//             ReadInputOnline(inputs);
//             AddSpectatorFrames();
//             UpdateGeneral();
//             SyncTransform();
//             UpdatePhysics();
//             UpdatePhysicsManager();
//
//             if (!rollbackManager.isRollbackFrame && !rollbackManager.DelayBased)
//             {
//                 rollbackManager.SaveState();
//             }
//         }
//         public override void UpdateByFrame(ulong[] inputs, bool readInput = true)
//         {
//             GameUpdate(inputs);
//         }
//
//         public void ReadInputOnline(ulong[] inputs)
//         {
//             lastInputs = inputs;
//             charComponents[0].Input.ParseInput(inputs[0]);
//             charComponents[1].Input.ParseInput(inputs[1]);
//         }
//
//         public override void InitInput()
//         {
//             // Initialize Gameplay Input
//             bool isPlayerOne = GlobalManager.Instance.LobbyManager.LobbyMemberMe.userRank == PlayerLobbyType.playerOne;
//             charComponents[isPlayerOne ? 0 : 1].Input.InitializeControls(false);
//             charComponents[isPlayerOne ? 1 : 0].Input.InitializeControls(true);
//         }
//
//         public override void TimeUpdate()
//         {
//             frameNumber++;
//         }
//
//         public void AddSpectatorFrames()
//         {
//             if (isPlayer1Local && frameNumber > rollbackManager.SpectatorDelayInFrames)
//             {
//                 // Save last inputs
//                 int frameDelay = frameNumber - rollbackManager.SpectatorDelayInFrames;
//                 spectatorLogic.P1AddToBuffer(new ulong [2]{rollbackManager.clientInputs.Get(frameDelay).input, rollbackManager.receivedInputs.Get(frameDelay).input}, frameDelay);
//             }
//         }
//
//         private string[] GetFighterNames()
//         {
//             string[] fighterNames = new string[2];
//             int i = 0;
//
//             foreach (PlatformUser lobbyMember in GlobalManager.Instance.LobbyManager.LobbyMembers)
//             {
//                 if (lobbyMember.userRank != PlayerLobbyType.spectator)
//                 {
//                     fighterNames[i] = lobbyMember.userName;
//                     i++;
//                     if(i>1) { break; }
//                 }
//             }
//
//             return fighterNames;
//         }
//     }
// }