using System;

namespace BestoNet.Networking.Interfaces
{
    public interface INetworkTransport
    {
        event Action<int, ulong> OnInputReceived;
        event Action<int, int> OnAdvantageReceived;
        void SendInput(int frame, ulong input, int advantage);
        void Connect(string address, int port);
        void Disconnect();
    }
}
