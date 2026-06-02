using System;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using MessagePack;
using static IdolShowdown.Managers.RollbackManager;
namespace IdolShowdown.Managers
{
    [MessagePackObject]
    public struct PacketHeader
    {
        [Key(0)] public bool packetType;
    }
    [MessagePackObject]
    public struct InputPacket
    {
        [Key(0)] public bool packetType;
        [Key(1)] public int localFrameAdvantage;
        [Key(2)] public int bufferSize;
        [Key(3)] public InputFrame[] inputs;
    }
    [MessagePackObject]
    public struct PingPacket
    {
        [Key(0)] public bool packetType;
        [Key(1)] public int frame;
    }

    [MessagePackObject]
    public struct InputFrame
    {
        [Key(0)] public int frame;
        [Key(1)] public ulong input;
    }
    public class MatchMessageManager : MonoBehaviour
    {
        [SerializeField] public int MAX_RETRY_AMOUNT = 3;
        [SerializeField] public int MATCH_MESSAGE_CHANNEL = 4;
        [SerializeField] public int INPUT_BUFFER_SIZE = 8;
        [SerializeField] public float PACKET_LOSS_THRESHOLD = 0.05f;
        [SerializeField] public int CONSECUTIVE_DROP_THRESHOLD = 4;
        public int Ping { get; private set; } = 200;
        private const bool PACKET_ACK = true;
        private const int RECEIVED_FRAME_TIMES_SIZE = 32;
        private int currentBufferSize = 0;
        public float LastPacketLoss { get; private set; } = 0;
        public CircularArray<float> sentFrameTimes = new CircularArray<float>(60);
        public CircularArray<int> receivedFrameTimes = new CircularArray<int>(RECEIVED_FRAME_TIMES_SIZE);

        /* Global manager references */
        private RollbackManager rollbackManager => GlobalManager.Instance.RollbackManager;
        private LobbyManager lobbyManager => GlobalManager.Instance.LobbyManager;
        private LogManager logManager => GlobalManager.Instance.LogManager;

        /* Debugging tools */
        private bool dropFrames = false;
        private int dropFrameCounter = 0;
        [SerializeField] private int DebugDropFrameCount = 12;

        public void Start()
        {
            ResetReceivedFrameACKsBuffer();
        }

        void Update()
        {
            if (lobbyManager != null && lobbyManager.CurrentLobby != null && lobbyManager.LobbyMemberMe.userRank != PlayerLobbyType.spectator)
            {
                while (SteamNetworking.IsP2PPacketAvailable(MATCH_MESSAGE_CHANNEL))
                {
                    P2Packet? lastPacket = SteamNetworking.ReadP2PPacket(MATCH_MESSAGE_CHANNEL);
                    if (lastPacket.HasValue && lastPacket.Value.Data != null)
                    {
                        try
                        {
                            OnChatMessage(lastPacket.Value.SteamId.Value, lastPacket.Value.Data);
                        }
                        catch (System.Exception e)
                        {
                            UnityEngine.Debug.LogError(e);
                        }
                    }
                }
            }
        }

        private void OnChatMessage(ulong fromUser, byte[] message)
        {
            if (GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated || !GlobalManager.Instance.MatchRunner.IsRunning)
            {
                return;
            }

            var packet = MessagePackSerializer.Deserialize<PacketHeader>(message);

            bool PACKET_ACK = packet.packetType;
            if (PACKET_ACK)
            {
                var pingPacket = MessagePackSerializer.Deserialize<PingPacket>(message);
                int frame = pingPacket.frame;
                ProcessACK(frame);
            }
            else
            {
                var inputPacket = MessagePackSerializer.Deserialize<InputPacket>(message);
                int remoteFrameAdvantage = inputPacket.localFrameAdvantage;
                int bufferSize = inputPacket.bufferSize;

                if (bufferSize < 2)
                {
                    Debug.LogError("Buffer has less than 2 inputs!");
                    return;
                }

                InputFrame firstInput = inputPacket.inputs[0];
                InputFrame lastInput = inputPacket.inputs[bufferSize - 1];

                int currentInputPointer = 0;
                for (int currentFrame = firstInput.frame; currentFrame <= lastInput.frame; currentFrame++)
                {
                    InputFrame currentInput = inputPacket.inputs[currentInputPointer];

                    ProcessInputs(currentFrame, currentInput.input, fromUser);
                    if (currentFrame == lastInput.frame)
                    {
                        rollbackManager.SetRemoteFrameAdvantage(currentFrame, remoteFrameAdvantage);
                        rollbackManager.SetRemoteFrame(currentFrame);
                        SendMessageACK(fromUser, currentFrame);
                        break;
                    }
                    
                    InputFrame nextInput = inputPacket.inputs[currentInputPointer + 1];
                    if (nextInput.frame - 1 == currentFrame)
                    {
                        currentInputPointer++;
                    }
                }
            }
        }

        public void ProcessInputs(int frame, ulong input, ulong fromUser)
        {
            if (rollbackManager.receivedInputs.ContainsKey(frame))
            {
                return;
            }
            rollbackManager.SetOpponentInput(frame, input);
        }

        public void ProcessACK(int frame)
        {
            receivedFrameTimes.Insert(frame, frame);
            int CalculatePing = (int)((Time.time - sentFrameTimes.Get(frame)) * 1000);
            Ping = CalculatePing == 0 ? Ping : CalculatePing;
        }

        public float PacketLoss()
        {
            int currentFrame = GlobalManager.Instance.RollbackManager.localFrame;
            int droppedFrame = 0;

            for (int frameToCheck = currentFrame - (RECEIVED_FRAME_TIMES_SIZE / 2); frameToCheck >= Math.Max(currentFrame - RECEIVED_FRAME_TIMES_SIZE, 0); frameToCheck--)
            {
                int receivedFrame = receivedFrameTimes.Get(frameToCheck);
                /* -1 means that the frame has just been initialized at the start of the match*/
                if (receivedFrame != -1 && receivedFrame != frameToCheck)
                {
                    droppedFrame++;
                }
            }
            LastPacketLoss = (float)droppedFrame / (RECEIVED_FRAME_TIMES_SIZE / 2);
            return LastPacketLoss;
        }

        public void SetInputBufferSize()
        {
            if (rollbackManager.consecutiveDrop > CONSECUTIVE_DROP_THRESHOLD)
            {
                if (currentBufferSize != INPUT_BUFFER_SIZE * 4)
                {
                    logManager.Info("Consecutive drops are high! increasing buffer to " + INPUT_BUFFER_SIZE * 4);
                }
                currentBufferSize = INPUT_BUFFER_SIZE * 4;
            }
            else if (PacketLoss() > PACKET_LOSS_THRESHOLD)
            {
                if (currentBufferSize != INPUT_BUFFER_SIZE * 2)
                {
                    logManager.Info("Packet loss is high! increasing buffer to " + INPUT_BUFFER_SIZE * 2);
                }
                currentBufferSize = INPUT_BUFFER_SIZE * 2;
            }
            else
            {
                currentBufferSize = INPUT_BUFFER_SIZE;
            }
        }

        public void ResetReceivedFrameACKsBuffer()
        {
            for (int i = 0; i < RECEIVED_FRAME_TIMES_SIZE; i++)
            {
                sentFrameTimes.Insert(i, -1);
            }
        }

        public void SendInputs(ulong userid, int frame, ulong input)
        {
            if (GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated)
            {
                return;
            }

            if (!rollbackManager.clientInputs.ContainsKey(frame))
            {
                rollbackManager.SetClientInput(frame, input);
            }

            var inputs = new List<InputFrame>();
            bool isFirstFrame = true;
            ulong lastInput = 0;

            int startFrame = Math.Max(frame - currentBufferSize + 1, 0);
            for (int matchFrame = startFrame; matchFrame <= frame; matchFrame++)
            {
                FrameMetadata frameMetadata = rollbackManager.clientInputs.Get(matchFrame);
                // This will ensure that we have at least the first and last frame
                if (frameMetadata.input == lastInput && !isFirstFrame && matchFrame != frame)
                    continue;

                inputs.Add(new InputFrame
                {
                    frame = frameMetadata.frame,
                    input = frameMetadata.input
                });

                isFirstFrame = false;
                lastInput = frameMetadata.input;
            }

            var packet = new InputPacket
            {
                packetType = !PACKET_ACK,
                localFrameAdvantage = rollbackManager.localFrameAdvantage,
                bufferSize = inputs.Count,
                inputs = inputs.ToArray()
            };
            
            byte[] data = MessagePackSerializer.Serialize(packet);

#if UNITY_EDITOR
            if (dropFrames == true)
            {
                dropFrameCounter++;
                if (dropFrameCounter > DebugDropFrameCount)
                {
                    dropFrameCounter = 0;
                    dropFrames = false;
                }
            }
            else
            {
                SteamNetworking.SendP2PPacket(userid, data, data.Length, MATCH_MESSAGE_CHANNEL, P2PSend.UnreliableNoDelay);
            }
#else
            SteamNetworking.SendP2PPacket(userid, data, data.Length, MATCH_MESSAGE_CHANNEL, P2PSend.UnreliableNoDelay);
#endif
            sentFrameTimes.Insert(frame, Time.time);
        }

        public void SendMessageACK(ulong userid, int frame)
        {
            if (GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated)
            {
                return;
            }

            var packet = new PingPacket
            {
                packetType = PACKET_ACK,
                frame = frame
            };

            byte[] data = MessagePackSerializer.Serialize(packet);
            SteamNetworking.SendP2PPacket(userid, data, data.Length, MATCH_MESSAGE_CHANNEL, P2PSend.UnreliableNoDelay);

        }

        private void DropFramesDebug()
        {
#if UNITY_EDITOR
            dropFrames = true;
#endif
        }

    }
}