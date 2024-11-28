using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BestoNetSamples.BestoNet.Networking.Interfaces;
using TMPro;
using UnityEngine;

namespace BestoNetSamples.BestoNet.Networking.Transport
{
    public class UDPTransport : BaseTransport
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        
        private UdpClient _client;
        private IPEndPoint _remoteEndPoint;
        private Thread _receiveThread;
        private readonly Queue<byte[]> _packetQueue = new();
        private readonly object _queueLock = new();
        private volatile bool _isRunning;
        private float _lastHeartbeatTime;
        private float _lastReceivedTime = float.MaxValue;
        private volatile bool _shutdownRequested;
        private bool _isHost;
        private int _localPort;
        private IPEndPoint _lastSenderEndPoint;

        public override void StartHost()
        {
            _isHost = true;
            _localPort = Config.Port;
            InitializeConnection();
        }

        public override void StartClient()
        {
            _isHost = false;
            _localPort = GetRandomPort();
            InitializeConnection();
        }

        private static int GetRandomPort()
        {
            return UnityEngine.Random.Range(7778, 8000);
        }

        private void InitializeConnection()
        {
            CleanupConnection();
            try
            {
                // Resolve the remote address
                IPAddress ipAddress;
                if (Config.RemoteAddress.ToLower() == "localhost")
                {
                    ipAddress = IPAddress.Parse("127.0.0.1");
                }
                else
                {
                    if (!IPAddress.TryParse(Config.RemoteAddress, out ipAddress))
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(Config.RemoteAddress);
                        if (hostEntry.AddressList.Length > 0)
                        {
                            ipAddress = hostEntry.AddressList[0];
                        }
                        else
                        {
                            throw new Exception("Could not resolve hostname");
                        }
                    }
                }

                _client = new UdpClient();
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                // Bind to the appropriate port
                _client.Client.Bind(new IPEndPoint(IPAddress.Any, _localPort));
                
                // For client, set remote endpoint to host's port (7777)
                // For host, this will be updated when we receive the first message from client
                _remoteEndPoint = new IPEndPoint(ipAddress, _isHost ? _localPort : 7777);
                _client.Client.ReceiveTimeout = 1000;
                
                Debug.Log($"Initialized UDP on port {_localPort}, {(_isHost ? "hosting" : "connecting to")} {ipAddress}:{(_isHost ? _localPort : 7777)}");
                _isRunning = true;
                _shutdownRequested = false;
                _lastReceivedTime = Time.time;
                _lastHeartbeatTime = Time.time;
                _receiveThread = new Thread(ReceiveThread)
                {
                    IsBackground = true
                };
                _receiveThread.Start();
                SetState(TransportState.Connecting);
                if (connectionStatusText != null)
                {
                    connectionStatusText.text = _isHost 
                        ? $"Hosting on port {_localPort}" 
                        : $"Connecting to {ipAddress}:7777";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize UDP connection: {e.Message}");
                SetState(TransportState.Failed);
            }
        }

        private void Update()
        {
            if (CurrentState == TransportState.Failed) return;

            ProcessPacketQueue();
            SendHeartbeat();
            if (!_isHost)
            {
                UpdateConnectionStatus();
            }
        }

        private void UpdateConnectionStatus()
        {
            if (CurrentState == TransportState.Connecting)
            {
                if (Time.time - _lastReceivedTime > Config.ConnectionTimeout)
                {
                    SetState(TransportState.Failed);
                    print("connection failed");
                }
            }
            else if (CurrentState == TransportState.Connected)
            {
                if (Time.time - _lastReceivedTime > Config.ConnectionTimeout)
                {
                    SetState(TransportState.Disconnected);
                    print("disconnect");
                }
            }
        }

        private void SendHeartbeat()
        {
            if (CurrentState == TransportState.Failed) return;
            if (!(Time.time - _lastHeartbeatTime >= Config.HeartbeatInterval)) return;
            
            _lastHeartbeatTime = Time.time;
            SendNetworkMessage("HEARTBEAT");
        }

        private void ProcessPacketQueue()
        {
            while (IsP2PPacketAvailable())
            {
                byte[] packet = ReadP2PPacket();
                if (packet == null) continue;

                string message = Encoding.UTF8.GetString(packet);
                Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Processing packet: {message}");
                if (message == "HEARTBEAT")
                {
                    _lastReceivedTime = Time.time;
                    if (CurrentState == TransportState.Connecting)
                    {
                        SetState(TransportState.Connected);
                        // If we're the client, immediately send a JOIN_REQUEST after connection
                        if (!_isHost)
                        {
                            SendNetworkMessage("JOIN_REQUEST");
                        }
                    }
                }
                else
                {
                    RaiseOnPacketReceived(packet);
                }
            }
        }

        public override void SendNetworkMessage(byte[] message)
        {
            if (CurrentState == TransportState.Failed || _client == null) return;

            try
            {
                string messageStr = Encoding.UTF8.GetString(message);
                IPEndPoint targetEndPoint = _isHost && _lastSenderEndPoint != null ? _lastSenderEndPoint : _remoteEndPoint;
                _client.Send(message, message.Length, targetEndPoint);
                Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Sent message: {messageStr} to {targetEndPoint}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{(_isHost ? "HOST" : "CLIENT")}] Failed to send message: {e.Message}");
                SetState(TransportState.Failed);
            }
        }

        private void ReceiveThread()
        {
            while (_isRunning && !_shutdownRequested)
            {
                try
                {
                    IPEndPoint remoteEp = new(IPAddress.Any, 0);
                    byte[] data = _client?.Receive(ref remoteEp);

                    if (data == null) continue;
                
                    string message = Encoding.UTF8.GetString(data);
                    Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Received raw data: {message} from {remoteEp}");
                    // For host: Update remote endpoint when receiving any message from client
                    if (_isHost)
                    {
                        _lastSenderEndPoint = remoteEp;
                        _remoteEndPoint = remoteEp;
                        Debug.Log($"[HOST] Updated remote endpoint to {remoteEp}");
                    }
                    lock (_queueLock)
                    {
                        _packetQueue.Enqueue(data);
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        if (!_shutdownRequested)
                        {
                            Debug.LogWarning($"[{(_isHost ? "HOST" : "CLIENT")}] Socket error: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!_shutdownRequested)
                    {
                        Debug.LogError($"[{(_isHost ? "HOST" : "CLIENT")}] Receive error: {e.Message}");
                    }
                    break;
                }
            }
        }
        
        private bool IsP2PPacketAvailable()
        {
            lock (_queueLock)
            {
                return _packetQueue.Count > 0;
            }
        }

        private byte[] ReadP2PPacket()
        {
            lock (_queueLock)
            {
                return _packetQueue.Count > 0 ? _packetQueue.Dequeue() : null;
            }
        }

        public override void Disconnect()
        {
            CleanupConnection();
            SetState(TransportState.Disconnected);
        }

        private void CleanupConnection()
        {
            _shutdownRequested = true;
            _isRunning = false;
            if (_receiveThread is { IsAlive: true })
            {
                _receiveThread.Join(100);
                _receiveThread = null;
            }
            if (_client != null)
            {
                try
                {
                    _client.Close();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error closing UDP client: {e.Message}");
                }
                finally
                {
                    _client = null;
                }
            }
            lock (_queueLock)
            {
                _packetQueue.Clear();
            }
        }

        protected override void OnDestroy()
        {
            CleanupConnection();
            base.OnDestroy();
        }

        private void OnApplicationQuit()
        {
            CleanupConnection();
        }
    }
}