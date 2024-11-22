namespace BestoNet.Networking.Structs
{
    public struct NetworkMessage
    {
        public MessageType Type { get; set; }
        public int Frame { get; set; }
        public ulong Input { get; set; }
        public int Advantage { get; set; }
        public long Timestamp { get; set; }
    }
}
