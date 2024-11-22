using UnityEngine;
using BestoNet.Types;

namespace BestoNet.Collections
{
    /// <summary>
    /// A circular array specifically designed to store and manage frame metadata.
    /// </summary>
    public class FrameMetadataArray : CircularArray<FrameMetadata>
    {
        private int _latestInsertedFrame = -1;

        public FrameMetadataArray(int size) : base(size) { }

        public override void Insert(int frame, FrameMetadata value)
        {
            if (value.Frame != frame)
            {
                throw new System.ArgumentException("Frame number in metadata doesn't match insertion frame", nameof(value));
            }
            _latestInsertedFrame = frame;
            base.Insert(frame, value);
        }

        public bool ContainsKey(int frame)
        {
            FrameMetadata metadata = Get(frame);
            return metadata.Frame == frame;
        }

        public ulong GetInput(int frame)
        {
            if (ContainsKey(frame))
            {
                return Get(frame).Input;
            }
            Debug.LogWarning($"Missing input for frame {frame}, latest frame is {_latestInsertedFrame}");
            return 0;
        }

        public uint GetStateHash(int frame)
        {
            if (ContainsKey(frame))
            {
                return Get(frame).StateHash;
            }
            Debug.LogWarning($"Missing state hash for frame {frame}, latest frame is {_latestInsertedFrame}");
            return 0;
        }

        public int GetLatestFrame()
        {
            return _latestInsertedFrame;
        }

        public bool IsFrameConfirmed(int frame)
        {
            return ContainsKey(frame) && Get(frame).Confirmed;
        }
    }
}