using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BestoNet.Collections;
using BestoNet.Networking.Interfaces;
using BestoNet.Networking.Structs;
using UnityEngine;

namespace BestoNet.Networking
{
    public enum MessageType : byte
    {
        Input,
        Ping,
        Pong
    }
    
    public class NetworkTransport : MonoBehaviour, INetworkTransport
    {
        // just some default standin data
        private readonly NetworkSettings _settings = new()
        {
            Address = "127.0.0.1",
            Port = 7777,
            AutoConnect = false,
            EnablePing = true,
            PingInterval = 1.0f
        };
        
        [field: Header("Debug Settings")]
        [field: SerializeField]
         public bool SimulateLatency { get; set; }
        [field: SerializeField]
        public int SimulatedLatencyMs { get; set; }
        [field: SerializeField]
        public bool SimulatePacketLoss { get; set; }
        [field: SerializeField]
        public float PacketLossChance { get; set; }

        // some events to hook into
        public event Action<int, ulong> OnInputReceived;
        public event Action<int, int> OnAdvantageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;

        private UdpClient _client;
        private IPEndPoint _remoteEndPoint;
        private Thread _receiveThread;
        private CancellationTokenSource _cancellationToken;
        private CircularArray<long> _pingTimes;
        private long _lastPingSent;
        private readonly object _sendLock = new();
        private float _pingTimer;

        public bool IsConnected { get; private set; }
        public int AveragePing { get; private set; }
        
        // would put in array but sense its using circular array and settings its probably best to be in start
        private void Start()
        {
            _pingTimes = new CircularArray<long>(60);
            if (_settings.AutoConnect)
            {
                Connect(_settings.Address, _settings.Port);
            }
        }

        private void Update()
        {
            if (!IsConnected || !_settings.EnablePing) return;
            
            _pingTimer += Time.deltaTime;
            if (_pingTimer >= _settings.PingInterval)
            {
                _pingTimer = 0;
                SendPing();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect(string address, int port)
        {
            if (IsConnected) return;

            try
            {
                // should theses calls be cahced? I don't think it matters much since this is just for connection
                _cancellationToken = new CancellationTokenSource();
                _client = new UdpClient();
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
                _client.Connect(_remoteEndPoint);

                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.Start();

                IsConnected = true;
                OnConnected?.Invoke();

                if (_settings.EnablePing)
                {
                    SendPing();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to connect: {e.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            _cancellationToken?.Cancel();
            
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
            
            if (_receiveThread != null)
            {
                _receiveThread.Join();
                _receiveThread = null;
            }

            _cancellationToken?.Dispose();
            _cancellationToken = null;

            IsConnected = false;
            OnDisconnected?.Invoke();
        }

        public void SendInput(int frame, ulong input, int advantage)
        {
            if (!IsConnected) return;

            NetworkMessage message = new()
            {
                Type = MessageType.Input,
                Frame = frame,
                Input = input,
                Advantage = advantage
            };
            SendMessage(message);
        }

        private async void SendMessage(NetworkMessage message)
        {
            if (!IsConnected) return;

            // Simulate packet loss if debug settings is checked then just return
            if (SimulatePacketLoss && UnityEngine.Random.value < PacketLossChance / 100f) return;

            try
            {
                byte[] data = SerializeMessage(message);

                if (SimulateLatency)
                {
                    await Task.Delay(SimulatedLatencyMs);
                }

                lock (_sendLock)
                {
                    _client?.Send(data, data.Length);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to send message: {e.Message}");
                Disconnect();
            }
        }

        private void SendPing()
        {
            if (!IsConnected) return;

            _lastPingSent = DateTime.UtcNow.Ticks;
            SendMessage(new NetworkMessage 
            { 
                Type = MessageType.Ping,
                Timestamp = _lastPingSent 
            });
        }

        private void HandlePong(long timestamp)
        {
            if (timestamp != _lastPingSent) return;

            long pingTime = (DateTime.UtcNow.Ticks - timestamp) / TimeSpan.TicksPerMillisecond;
            _pingTimes.Insert(_pingTimes.Count, pingTime);

            // Calculate running average
            long sum = 0;
            int count = Math.Max(1, _pingTimes.Count);
            for (int i = 0; i < _pingTimes.Count; i++)
            {
                sum += _pingTimes.Get(i);
            }
            AveragePing = (int)(sum / count);
        }

        private void ReceiveLoop()
        {
            while (!_cancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    byte[] result = _client.Receive(ref _remoteEndPoint);
                    NetworkMessage message = DeserializeMessage(result);

                    switch (message.Type)
                    {
                        case MessageType.Input:
                            OnInputReceived?.Invoke(message.Frame, message.Input);
                            OnAdvantageReceived?.Invoke(message.Frame, message.Advantage);
                            break;
                        case MessageType.Ping:
                            // Respond to ping with pong
                            SendMessage(new NetworkMessage 
                            { 
                                Type = MessageType.Pong,
                                Timestamp = message.Timestamp 
                            });
                            break;
                        case MessageType.Pong:
                            HandlePong(message.Timestamp);
                            break;
                    }
                }
                catch (SocketException e)
                {
                    if (!_cancellationToken.Token.IsCancellationRequested)
                    {
                        UnityEngine.Debug.LogError($"Socket error: {e.Message}");
                        Disconnect();
                    }
                    break;
                }
            }
        }
        
        
        // This is where I am not sure.. I am working on setting up the serailization for objects..
        // Should the network transport also be using the same serialization system or should it be here?
        // should writer and stream calls be cached?
        private byte[] SerializeMessage(NetworkMessage message)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            
            writer.Write((byte)message.Type);
            
            switch (message.Type)
            {
                case MessageType.Input:
                    writer.Write(message.Frame);
                    writer.Write(message.Input);
                    writer.Write(message.Advantage);
                    break;
                case MessageType.Ping:
                case MessageType.Pong:
                    writer.Write(message.Timestamp);
                    break;
            }
            return stream.ToArray();
        }

        private NetworkMessage DeserializeMessage(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);
            
            MessageType type = (MessageType)reader.ReadByte();
            NetworkMessage message = new() { Type = type };

            switch (type)
            {
                case MessageType.Input:
                    message.Frame = reader.ReadInt32();
                    message.Input = reader.ReadUInt64();
                    message.Advantage = reader.ReadInt32();
                    break;
                case MessageType.Ping:
                case MessageType.Pong:
                    message.Timestamp = reader.ReadInt64();
                    break;
            }
            return message;
        }
    }
}
