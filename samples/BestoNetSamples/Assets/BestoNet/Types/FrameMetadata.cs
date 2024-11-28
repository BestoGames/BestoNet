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

        public FrameMetadata(int frame, ulong input)
        {
            Frame = frame;
            Input = input;
        }

        public bool Equals(FrameMetadata other)
        {
            return Frame == other.Frame &&
                   Input == other.Input;
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
            return $"Frame {Frame} : Input {Input}";
        }
    }
}