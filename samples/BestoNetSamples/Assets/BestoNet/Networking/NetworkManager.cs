using System;
using BestoNetSamples.BestoNet.Networking.Interfaces;
using BestoNetSamples.BestoNet.Networking.Transport;
using BestoNetSamples.Singleton;
using UnityEngine;

namespace BestoNetSamples.BestoNet.Networking
{
    public class NetworkManager : SingletonBehaviour<NetworkManager>
    {
        [SerializeField] private TransportType transportType = TransportType.UDP;
        [SerializeField] private string remoteAddress = "127.0.0.1";
        [SerializeField] private int port = 7777;
        [SerializeField] private float connectionTimeout = 5f;
        [SerializeField] private float heartbeatInterval = 1f;
        
        private INetworkTransport _transport;
        
        public bool IsHost { get; private set; }
        public bool IsConnected => _transport?.GetState() == TransportState.Connected;

        public event Action<byte[]> OnPacketReceived;
        public event Action<bool> OnConnectionStateChanged;
        public event Action OnConnectionFailed;

        public enum TransportType
        {
            UDP,
            Steam,
            Epic,
            PlayFab
        }

        protected override void OnAwake()
        {
            InitializeTransport();
        }

        private void InitializeTransport()
        {
            // Create transport instance based on selected type
            _transport = transportType switch
            {
                TransportType.UDP => gameObject.AddComponent<UDPTransport>(),
                _ => throw new ArgumentException($"Unsupported transport type: {transportType}")
            };

            print("TransportType: " + transportType);
            
            TransportConfig config = new()
            {
                RemoteAddress = remoteAddress,
                Port = port,
                ConnectionTimeout = connectionTimeout,
                HeartbeatInterval = heartbeatInterval
            };

            // Just configure the transport, don't initialize connection yet
            _transport.Configure(config);
            
            // Connect transport events
            _transport.OnPacketReceived += HandlePacketReceived;
            _transport.OnStateChanged += HandleTransportStateChanged;
        }

        public void StartHost()
        {
            if (IsConnected) return;
            
            print("start host");
            IsHost = true;
            _transport.StartHost();
        }

        public void StartClient()
        {
            if (IsConnected) return;
            
            IsHost = false;
            _transport.StartClient();
        }

        public void SendNetworkMessage(string message)
        {
            _transport?.SendNetworkMessage(message);
        }

        public void SendData(byte[] data)
        {
            _transport?.SendNetworkMessage(data);
        }

        private void HandlePacketReceived(byte[] data)
        {
            OnPacketReceived?.Invoke(data);
        }

        private void HandleTransportStateChanged(TransportState state)
        {
            switch (state)
            {
                case TransportState.Connected:
                    OnConnectionStateChanged?.Invoke(true);
                    break;
                case TransportState.Failed:
                    OnConnectionFailed?.Invoke();
                    break;
                case TransportState.Disconnected:
                    OnConnectionStateChanged?.Invoke(false);
                    break;
            }
        }

        public void Disconnect()
        {
            if (_transport != null)
            {
                _transport.OnPacketReceived -= HandlePacketReceived;
                _transport.OnStateChanged -= HandleTransportStateChanged;
                _transport.Disconnect();
            }
            IsHost = false;
        }

        protected override void OnDestroy()
        {
            Disconnect();
            base.OnDestroy();
        }
    }
}