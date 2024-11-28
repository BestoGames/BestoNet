using System;
using System.IO;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace IdolShowdown.Managers
{
    public class MatchMessageManager : MonoBehaviour
    {
        [SerializeField] public int MAX_RETRY_AMOUNT = 3;
        [SerializeField] public int MATCH_MESSAGE_CHANNEL = 4;
        public int Ping { get; private set; } = 200;
        private const byte PACKET_ACK = 0;
        private const byte PACKET_INPUT = 1;
        private const int REMOTE_FRAME_UPDATE = -1;
        public CircularArray<float> sentFrameTimes = new CircularArray<float>(60);

        /* Global manager references */
        private RollbackManager rollbackManager => GlobalManager.Instance.RollbackManager;
        private LobbyManager lobbyManager => GlobalManager.Instance.LobbyManager;

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
            if (GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated)
            {
                return;
            }

            MemoryStream memoryStream = new MemoryStream(message);
            BinaryReader reader = new BinaryReader(memoryStream);

            byte PACKET_TYPE = reader.ReadByte();
            if (PACKET_TYPE == PACKET_ACK)
            {
                int frame = reader.ReadInt32();
                ProcessACK(frame);
            }
            else if(PACKET_TYPE == PACKET_INPUT)
            {
                int remoteFrameAdvantage = reader.ReadInt32();  
                int totalInputs = reader.ReadInt32();
                for(int i = 0; i <= totalInputs; i++)
                {
                    int frame = reader.ReadInt32();
                    ulong input = reader.ReadUInt64();
                    if (i == totalInputs)
                    {
                        rollbackManager.SetRemoteFrameAdvantage(frame, remoteFrameAdvantage);
                        rollbackManager.SetRemoteFrame(frame);
                    }
                    ProcessInputs(frame, input, fromUser);
                    
                }             
            }
            
            reader.Close();
            memoryStream.Close();
        }

        public void ProcessInputs(int frame, ulong input, ulong fromUser)
        {
            if (frame == REMOTE_FRAME_UPDATE || rollbackManager.receivedInputs.ContainsKey(frame))
            {
                return;
            }
            SendMessageACK(fromUser, frame);
            rollbackManager.SetOpponentInput(frame, input);
        }

        public void ProcessACK(int frame)
        {
            int CalculatePing = (int)((Time.time - sentFrameTimes.Get(frame)) * 1000);
            Ping = CalculatePing == 0 ? Ping : CalculatePing;
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
            
            sentFrameTimes.Insert(frame, Time.time);
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(PACKET_INPUT);
            binaryWriter.Write(rollbackManager.localFrameAdvantage);
            binaryWriter.Write(Math.Min(rollbackManager.MaxRollBackFrames, frame));
            for(int i = Math.Max(frame - rollbackManager.MaxRollBackFrames, 0); i <= frame; i++)
            {
                binaryWriter.Write(rollbackManager.clientInputs.Get(i).frame);
                binaryWriter.Write(rollbackManager.clientInputs.Get(i).input);
            }

            byte[] data = memoryStream.ToArray();
            SteamNetworking.SendP2PPacket(userid, data, data.Length, MATCH_MESSAGE_CHANNEL, P2PSend.UnreliableNoDelay);

            binaryWriter?.Dispose();
            binaryWriter?.Close();
            memoryStream?.Dispose();
            memoryStream?.Close();
        }

        public void SendMessageACK(ulong userid, int frame)
        {
            if (GlobalManager.Instance.OnlineComponents.rematchHelper.IsActivated)
            {
                return;
            }

            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(PACKET_ACK);
            binaryWriter.Write(frame);

            byte[] data = memoryStream.ToArray();
            SteamNetworking.SendP2PPacket(userid, data, data.Length, MATCH_MESSAGE_CHANNEL, P2PSend.UnreliableNoDelay);

            binaryWriter?.Dispose();
            binaryWriter?.Close();
            memoryStream?.Dispose();
            memoryStream?.Close();
                
        }

    }
}