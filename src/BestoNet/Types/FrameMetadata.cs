using System;

namespace BestoNet.Types
{
    /// <summary>
    /// Represents metadata for a single frame in a networked simulation.
    /// </summary>
    public readonly struct FrameMetadata : IEquatable<FrameMetadata>
    {
        /// <summary>The frame number this metadata represents.</summary>
        public readonly int Frame;
        
        /// <summary>The input state for this frame.</summary>
        public readonly ulong Input;
        
        /// <summary>Timestamp when this frame was created (in milliseconds since startup).</summary>
        public readonly long Timestamp;
        
        /// <summary>CRC32 hash of the game state at this frame, used for verification.</summary>
        public readonly uint StateHash;
        
        /// <summary>Player ID who generated this input.</summary>
        public readonly byte PlayerIndex;
        
        /// <summary>Whether this frame has been confirmed by all players.</summary>
        public readonly bool Confirmed;

        public FrameMetadata(int frame, ulong input, long timestamp, uint stateHash, byte playerIndex, bool confirmed = false)
        {
            Frame = frame;
            Input = input;
            Timestamp = timestamp;
            StateHash = stateHash;
            PlayerIndex = playerIndex;
            Confirmed = confirmed;
        }

        public bool Equals(FrameMetadata other)
        {
            return Frame == other.Frame &&
                   Input == other.Input &&
                   Timestamp == other.Timestamp &&
                   StateHash == other.StateHash &&
                   PlayerIndex == other.PlayerIndex &&
                   Confirmed == other.Confirmed;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameMetadata other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Frame);
            hash.Add(Input);
            hash.Add(Timestamp);
            hash.Add(StateHash);
            hash.Add(PlayerIndex);
            hash.Add(Confirmed);
            return hash.ToHashCode();
        }

        public static bool operator ==(FrameMetadata left, FrameMetadata right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameMetadata left, FrameMetadata right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Frame {Frame} (Player {PlayerIndex}): Input={Input:X}, Hash={StateHash:X8}, Time={Timestamp}ms, {(Confirmed ? "Confirmed" : "Pending")}";
        }
    }
}