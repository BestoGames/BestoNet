namespace BestoNet.Networking.Structs
{
    public struct NetworkStats
    {
        public int LocalFrame { get; set; }
        public int RemoteFrame { get; set; }
        public float FrameAdvantage { get; set; }
        public int RollbackFrames { get; set; }
    }
}