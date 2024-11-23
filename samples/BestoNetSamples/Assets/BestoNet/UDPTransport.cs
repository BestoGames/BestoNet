using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace BestoNetSamples.BestoNet
{
    public class UDPTransport : MonoBehaviour
    {
        private UdpClient _client;
        private IPEndPoint _remoteEndPoint;
        private Thread _receiveThread;
        private Queue<byte[]> _packetQueue = new Queue<byte[]>();
        private bool _connected;
        public event Action<byte[]> OnPacketReceived;

        [SerializeField] private string remoteAddress = "127.0.0.1";
        [SerializeField] private int localPort = 7777;
        [SerializeField] private int remotePort = 7777;

        [SerializeField] private TextMeshProUGUI portip;

        private void Awake()
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
            _client = new UdpClient(localPort);
            
            _receiveThread = new Thread(ReceiveMessage);
            _receiveThread.Start();
            _receiveThread.IsBackground = true; 
            
            _connected = true;
            
            portip.text = remoteAddress + " : " + localPort;
        }

        private void Update()
        {
            if(IsP2PPacketAvailable())
            {
                byte[] message = ReadP2PPacket();
                UnityEngine.Debug.Log(Encoding.UTF8.GetString(message));
            }
        }

        private void OnDestroy()
        {
            _receiveThread.Abort();
        }

        public void SendStringMessage(string message)
        {
            SendMessage(Encoding.UTF8.GetBytes(message));
        }
        
        public void SendMessage(byte[] message)
        {
            try
            {
                _client.Send(message, message.Length, _remoteEndPoint);
                UnityEngine.Debug.Log($"Sending message: {message}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to send message: {e.Message}");
            }
        }

        public bool IsP2PPacketAvailable()
        {
            return _packetQueue.Count > 0;
        }

        public byte[] ReadP2PPacket()
        {
            return _packetQueue.Dequeue();
        }

        public void ReceiveMessage()
        {
            while (_connected)
            {
                try
                {
                    byte[] message = _client.Receive(ref _remoteEndPoint);
                    _packetQueue.Enqueue(message);
                }
                catch (SocketException e)
                {
                    UnityEngine.Debug.LogError($"Socket error: {e.Message}");
                }
            }
        }
    }
}
