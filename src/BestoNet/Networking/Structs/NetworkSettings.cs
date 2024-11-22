using System;

namespace BestoNet.Networking.Structs
{
    [Serializable]
    public struct NetworkSettings
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public bool AutoConnect { get; set; }
        public bool EnablePing { get; set; }
        public float PingInterval { get; set; }
    }
}
