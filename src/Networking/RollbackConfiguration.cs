using UnityEngine;

namespace BestoNet.Networking
{
    // Add any rollback or delay config as needed
    [System.Serializable]
    public class RollbackConfiguration
    {
        [field: SerializeField]
        public int InputBufferSize { get; set; } = 60;
        [field: SerializeField]
        public int StateBufferSize { get; set; } = 60;
        [field: SerializeField]
        public int FrameAdvantageSize { get; set; } = 48;
        [field: SerializeField]
        public int FrameAdvantageCheckSize { get; set; } = 32;
        [field: SerializeField]
        public int MaxRollbackFrames { get; set; } = 7;
        [field: SerializeField]
        public int MaxFrameAdvantage { get; set; } = 3;
        [field: SerializeField]
        public bool IsDelayBased { get; set; } = false;
        [field: SerializeField]
        public int InputDelay { get; set; } = 0;
    }
}