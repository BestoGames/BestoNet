using System;
using System.IO;
using BestoNet.Collections;
using BestoNetSamples.BestoNet.Networking;
using BestoNetSamples.Singleton;
using UnityEngine;

namespace BestoNetSamples
{
    public class MatchMessageManager : SingletonBehaviour<MatchMessageManager>
    {
        [SerializeField] public int MAX_RETRY_AMOUNT = 3;
        [SerializeField] public int MATCH_MESSAGE_CHANNEL = 4;
        public int Ping { get; private set; } = 200;
        private const byte PACKET_ACK = 0;
        private const byte PACKET_INPUT = 1;
        private const int REMOTE_FRAME_UPDATE = -1;
        public CircularArray<float> sentFrameTimes = new CircularArray<float>(60);

        /* Global manager references */
        private RollbackManager rollbackManager => RollbackManager.Instance;

        private void OnAwake()
        {
            NetworkManager.Instance.OnPacketReceived += OnChatMessage;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived -= OnChatMessage;
            }
        }

        private void OnChatMessage(byte[] message)
        {
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
                    ProcessInputs(frame, input);
                    
                }             
            }
            
            reader.Close();
            memoryStream.Close();
        }

        public void ProcessInputs(int frame, ulong input)
        {
            if (frame == REMOTE_FRAME_UPDATE || rollbackManager.receivedInputs.ContainsKey(frame))
            {
                return;
            }
            SendMessageACK(frame);
            rollbackManager.SetOpponentInput(frame, input);
        }

        public void ProcessACK(int frame)
        {
            int CalculatePing = (int)((Time.time - sentFrameTimes.Get(frame)) * 1000);
            Ping = CalculatePing == 0 ? Ping : CalculatePing;
        }

        public void SendInputs(int frame, ulong input)
        {
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
                binaryWriter.Write(rollbackManager.clientInputs.Get(i).Frame);
                binaryWriter.Write(rollbackManager.clientInputs.Get(i).Input);
            }

            byte[] data = memoryStream.ToArray();
            NetworkManager.Instance.SendData(data);

            binaryWriter?.Dispose();
            binaryWriter?.Close();
            memoryStream?.Dispose();
            memoryStream?.Close();
        }

        public void SendMessageACK(int frame)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(PACKET_ACK);
            binaryWriter.Write(frame);

            byte[] data = memoryStream.ToArray();
            NetworkManager.Instance.SendData(data);

            binaryWriter?.Dispose();
            binaryWriter?.Close();
            memoryStream?.Dispose();
            memoryStream?.Close();
                
        }

    }
}