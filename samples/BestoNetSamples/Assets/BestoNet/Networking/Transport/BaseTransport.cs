using System;
using BestoNetSamples.BestoNet.Networking.Interfaces;
using UnityEngine;

namespace BestoNetSamples.BestoNet.Networking.Transport
{
    public abstract class BaseTransport : MonoBehaviour, INetworkTransport
    {
        protected TransportConfig Config;
        protected TransportState CurrentState;
        
        public event Action<byte[]> OnPacketReceived;
        public event Action<TransportState> OnStateChanged;

        protected virtual void RaiseOnPacketReceived(byte[] data)
        {
            OnPacketReceived?.Invoke(data);
        }

        protected virtual void SetState(TransportState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public virtual void Configure(TransportConfig config)
        {
            Config = config;
            CurrentState = TransportState.Disconnected;
        }

        public abstract void StartHost();
        public abstract void StartClient();
        public abstract void SendNetworkMessage(byte[] data);
        
        public virtual void SendNetworkMessage(string message)
        {
            SendNetworkMessage(System.Text.Encoding.UTF8.GetBytes(message));
        }
        
        public abstract void Disconnect();
        
        public TransportState GetState() => CurrentState;

        protected virtual void OnDestroy()
        {
            Disconnect();
        }
    }
}