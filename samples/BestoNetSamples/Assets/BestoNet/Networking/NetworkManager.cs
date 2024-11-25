using System;
using System.Collections;
using BestoNetSamples.Singleton;
using BestoNetSamples.Utils;
using UnityEngine;

namespace BestoNetSamples.BestoNet.Networking
{
    public class NetworkManager : SingletonBehaviour<NetworkManager>
    {
        private UDPTransport _transport;
        private bool _isAttemptingConnection;
        private bool _isDisconnecting;
        private Coroutine _connectionAttemptCoroutine;

        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }

        public event Action<byte[]> OnPacketReceived;
        public event Action<bool> OnConnectionStateChanged; // Connected = true, Disconnected = false
        public event Action OnConnectionFailed;

        public void StartHost()
        {
            if (IsConnected || _isAttemptingConnection) return;
            
            IsHost = true;
            InitializeTransport();
        }

        public void StartClient()
        {
            if (IsConnected || _isAttemptingConnection) return;
            
            IsHost = false;
            InitializeTransport();
            _connectionAttemptCoroutine = StartCoroutine(AttemptConnection());
        }

        private void InitializeTransport()
        {
            _isAttemptingConnection = true;
            
            if (_transport == null)
            {
                _transport = gameObject.AddComponent<UDPTransport>();
                _transport.OnPacketReceived += HandlePacketReceived;
                _transport.OnConnectionStateChanged += HandleConnectionStateChanged;
                _transport.Initialize(IsHost);
            }
        }

        private IEnumerator AttemptConnection()
        {
            const int maxAttempts = 5;
            int attempts = 0;
            while (attempts < maxAttempts && !IsConnected)
            {
                _transport.SendStringMessage("JOIN_REQUEST");
                yield return WaitInstructionCache.Seconds(1f);
                attempts++;
            }
            if (!IsConnected)
            {
                OnConnectionFailed?.Invoke();
                Disconnect();
            }
            _connectionAttemptCoroutine = null;
        }

        public void SendMessage(string message)
        {
            if (!IsConnected || _transport == null) return;
            
            _transport.SendStringMessage(message);
        }

        public void SendData(byte[] data)
        {
            if (!IsConnected || _transport == null) return;
            
            _transport.SendMessage(data);
        }

        private void HandlePacketReceived(byte[] data)
        {
            OnPacketReceived?.Invoke(data);
        }

        private void HandleConnectionStateChanged(UDPTransport.ConnectionState state)
        {
            if (_isDisconnecting) return;

            switch (state)
            {
                case UDPTransport.ConnectionState.Connected:
                    IsConnected = true;
                    _isAttemptingConnection = false;
                    OnConnectionStateChanged?.Invoke(true);
                    break;
                case UDPTransport.ConnectionState.Disconnected:
                case UDPTransport.ConnectionState.Failed:
                    if (IsConnected)
                    {
                        Disconnect();
                    }
                    break;
            }
        }

        public void Disconnect()
        {
            if (_isDisconnecting) return;
            _isDisconnecting = true;
            if (_connectionAttemptCoroutine != null)
            {
                StopCoroutine(_connectionAttemptCoroutine);
                _connectionAttemptCoroutine = null;
            }
            IsConnected = false;
            _isAttemptingConnection = false;
            OnConnectionStateChanged?.Invoke(false);
            if (_transport != null)
            {
                _transport.OnPacketReceived -= HandlePacketReceived;
                _transport.OnConnectionStateChanged -= HandleConnectionStateChanged;
                Destroy(_transport);
                _transport = null;
            }
            _isDisconnecting = false;
        }

        protected override void OnDestroy()
        {
            Disconnect();
            base.OnDestroy();
        }
    }
}