using System;

namespace BestoNetSamples.BestoNet.Networking.Interfaces
{
    public enum TransportState
    {
        Disconnected,
        Connecting,
        Connected,
        Failed
    }
    
    public interface INetworkTransport
    {
        event Action<byte[]> OnPacketReceived;
        event Action<TransportState> OnStateChanged;
        
        void Configure(TransportConfig config);
        void StartHost();
        void StartClient();
        void SendNetworkMessage(byte[] data);
        void SendNetworkMessage(string message);
        void Disconnect();
        TransportState GetState();
    }

    public class TransportConfig
    {
        public string RemoteAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 7777;
        public float ConnectionTimeout { get; set; } = 5f;
        public float HeartbeatInterval { get; set; } = 1f;
    }
}
