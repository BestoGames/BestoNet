using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

namespace BestoNetSamples.BestoNet.Networking
{
    public class UDPTransport : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string remoteAddress = "127.0.0.1";
        [SerializeField] private int hostPort = 7777;
        [SerializeField] private float connectionTimeout = 5f;
        [SerializeField] private float heartbeatInterval = 1f;
        
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
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private volatile bool _shutdownRequested;
        private bool _isHost;
        private int _localPort;
        
        public event Action<byte[]> OnPacketReceived;
        public event Action<ConnectionState> OnConnectionStateChanged;
        
        private IPEndPoint _lastSenderEndPoint;

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Failed
        }

        public void Initialize(bool isHost)
        {
            _isHost = isHost;
            _localPort = isHost ? hostPort : GetRandomPort();
            InitializeConnection();
        }

        private static int GetRandomPort()
        {
            // Get a random port between 7778 and 7999
            return UnityEngine.Random.Range(7778, 8000);
        }

        private void InitializeConnection()
        {
            CleanupConnection();
            try
            {
                // Resolve the remote address
                IPAddress ipAddress;
                if (remoteAddress.ToLower() == "localhost")
                {
                    ipAddress = IPAddress.Parse("127.0.0.1");
                }
                else
                {
                    // Try to parse the IP address directly
                    if (!IPAddress.TryParse(remoteAddress, out ipAddress))
                    {
                        // If parsing fails, try to resolve the hostname
                        try
                        {
                            IPHostEntry hostEntry = Dns.GetHostEntry(remoteAddress);
                            if (hostEntry.AddressList.Length > 0)
                            {
                                ipAddress = hostEntry.AddressList[0];
                            }
                            else
                            {
                                throw new Exception("Could not resolve hostname");
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Failed to resolve address: {e.Message}");
                        }
                    }
                }

                _remoteEndPoint = new IPEndPoint(ipAddress, hostPort);
                _client = new UdpClient();
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _client.Client.Bind(new IPEndPoint(IPAddress.Any, _localPort));
                _client.Client.ReceiveTimeout = 1000;
                
                UnityEngine.Debug.Log($"Initialized UDP on port {_localPort}, {(_isHost ? "hosting" : "connecting to")} {ipAddress}:{hostPort}");
                
                _isRunning = true;
                _shutdownRequested = false;
                _lastReceivedTime = Time.time;
                _lastHeartbeatTime = Time.time;
                
                _receiveThread = new Thread(ReceiveThread)
                {
                    IsBackground = true
                };
                _receiveThread.Start();
                
                SetConnectionState(ConnectionState.Connecting);
                
                if (connectionStatusText != null)
                {
                    connectionStatusText.text = _isHost 
                        ? $"Hosting on port {hostPort}" 
                        : $"Connecting to {ipAddress}:{hostPort}";
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to initialize UDP connection: {e.Message}");
                SetConnectionState(ConnectionState.Failed);
            }
        }


        private void Update()
        {
            if (_connectionState == ConnectionState.Failed) return;

            ProcessPacketQueue();
            SendHeartbeat();
            // Only check for timeouts if we're not the host
            if (!_isHost)
            {
                UpdateConnectionStatus();
            }
        }

        private void UpdateConnectionStatus()
        {
            if (_connectionState == ConnectionState.Connecting)
            {
                if (Time.time - _lastReceivedTime > connectionTimeout)
                {
                    SetConnectionState(ConnectionState.Failed);
                }
            }
            else if (_connectionState == ConnectionState.Connected)
            {
                if (Time.time - _lastReceivedTime > connectionTimeout)
                {
                    SetConnectionState(ConnectionState.Disconnected);
                }
            }
        }

        private void SendHeartbeat()
        {
            if (_connectionState == ConnectionState.Failed) return;
            if (!(Time.time - _lastHeartbeatTime >= heartbeatInterval)) return;
            
            _lastHeartbeatTime = Time.time;
            SendMessage(Encoding.UTF8.GetBytes("HEARTBEAT"));
        }

        private void ProcessPacketQueue()
        {
            while (IsP2PPacketAvailable())
            {
                byte[] packet = ReadP2PPacket();
                if (packet == null) continue;

                string message = Encoding.UTF8.GetString(packet);
                UnityEngine.Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Processing packet: {message}");
            
                if (message == "HEARTBEAT")
                {
                    _lastReceivedTime = Time.time;
                    if (_connectionState == ConnectionState.Connecting)
                    {
                        SetConnectionState(ConnectionState.Connected);
                    }
                }
                else
                {
                    OnPacketReceived?.Invoke(packet);
                }
            }
        }

        public void SendMessage(byte[] message)
        {
            if (_connectionState == ConnectionState.Failed || _client == null) return;

            try
            {
                string messageStr = Encoding.UTF8.GetString(message);
            
                // If we're the host and have a last sender, use that endpoint
                IPEndPoint targetEndPoint = _isHost && _lastSenderEndPoint != null ? 
                    _lastSenderEndPoint : _remoteEndPoint;
            
                _client.Send(message, message.Length, targetEndPoint);
                UnityEngine.Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Sent message: {messageStr} to {targetEndPoint}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[{(_isHost ? "HOST" : "CLIENT")}] Failed to send message: {e.Message}");
                SetConnectionState(ConnectionState.Failed);
            }
        }


        public void SendStringMessage(string message)
        {
            SendMessage(Encoding.UTF8.GetBytes(message));
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
                    UnityEngine.Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Received raw data: {message} from {remoteEp}");
                    
                    // Update remote endpoint if we're the host and receive a non-heartbeat message
                    if (_isHost && message != "HEARTBEAT")
                    {
                        _lastSenderEndPoint = remoteEp;
                        _remoteEndPoint = remoteEp; // Update the endpoint to reply to
                        UnityEngine.Debug.Log($"[HOST] Updated remote endpoint to {remoteEp}");
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
                            UnityEngine.Debug.LogWarning($"[{(_isHost ? "HOST" : "CLIENT")}] Socket error: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!_shutdownRequested)
                    {
                        UnityEngine.Debug.LogError($"[{(_isHost ? "HOST" : "CLIENT")}] Receive error: {e.Message}");
                    }
                    break;
                }
            }
        }

        public bool IsP2PPacketAvailable()
        {
            lock (_queueLock)
            {
                return _packetQueue.Count > 0;
            }
        }

        public byte[] ReadP2PPacket()
        {
            lock (_queueLock)
            {
                return _packetQueue.Count > 0 ? _packetQueue.Dequeue() : null;
            }
        }

        private void SetConnectionState(ConnectionState newState)
        {
            if (_connectionState == newState) return;
    
            UnityEngine.Debug.Log($"[{(_isHost ? "HOST" : "CLIENT")}] Connection state changed: {_connectionState} -> {newState}");
            _connectionState = newState;
            OnConnectionStateChanged?.Invoke(newState);
    
            if (connectionStatusText != null)
            {
                connectionStatusText.text = $"Connection: {newState}";
            }

            if (newState == ConnectionState.Failed)
            {
                CleanupConnection();
            }
        }


        private void OnDestroy()
        {
            CleanupConnection();
        }

        private void CleanupConnection()
        {
            _shutdownRequested = true;
            _isRunning = false;
            if (_receiveThread is { IsAlive: true })
            {
                // Signal the thread to stop
                _receiveThread.Join(100); // Wait up to 100ms for thread to finish
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
                    UnityEngine.Debug.LogWarning($"Error closing UDP client: {e.Message}");
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

        private void OnApplicationQuit()
        {
            CleanupConnection();
        }
    }
}