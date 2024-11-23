using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

namespace BestoNetSamples.BestoNet
{
    public class UDPTransport : MonoBehaviour
    {
        private UdpClient _client;
        private IPEndPoint _remoteEndPoint;
        private CancellationTokenSource _cancellationToken;

        [SerializeField] private string remoteAddress = "127.0.0.1";
        [SerializeField] private int localPort = 7777;
        [SerializeField] private int remotePort = 7777;

        [SerializeField] private TextMeshProUGUI portip;

        private void Awake()
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
            _client = new UdpClient(localPort);
            
            portip.text = remoteAddress + " : " + localPort;
        }
        
        public void SendMessage()
        {
            byte[] message = Encoding.UTF8.GetBytes("Hello World");
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

        public void ReceiveMessage()
        {
            try
            {
                string message = Encoding.UTF8.GetString(_client.Receive(ref _remoteEndPoint));
                UnityEngine.Debug.Log($"Received message: {message}");
            }
            catch (SocketException e)
            {
                UnityEngine.Debug.LogError($"Socket error: {e.Message}");
            }
        }
    }
}
