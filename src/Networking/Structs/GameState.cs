namespace BestoNet.Networking.Structs
{
    public struct GameState
    {
        public int Frame { get; set; }
        public byte[] State { get; set; }
        public uint Checksum { get; set; }
    }
}