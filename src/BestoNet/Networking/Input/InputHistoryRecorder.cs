using BestoNet.Collections;

namespace BestoNet.Networking.Input
{
    /// <summary>
    /// Input history recorder for detecting complex input sequences (like fighting game moves)
    /// </summary>
    public class InputHistoryRecorder
    {
        private readonly CircularArray<InputEntry> _history;
        private readonly int _maxFrames;

        public InputHistoryRecorder(int maxFrames)
        {
            _maxFrames = maxFrames;
            _history = new CircularArray<InputEntry>(maxFrames);
        }

        public void RecordInput(ulong input, int frame)
        {
            _history.Insert(frame % _maxFrames, new InputEntry(input, frame));
        }

        public bool DetectSequence(ulong[] sequence, int withinFrames)
        {
            int currentFrame = _history.Get(_maxFrames - 1).Frame;
            int startFrame = currentFrame - withinFrames;
            int seqIndex = sequence.Length - 1;

            for (int i = _maxFrames - 1; i >= 0 && seqIndex >= 0; i--)
            {
                InputEntry entry = _history.Get(i);
                if (entry.Frame < startFrame) break;
                
                if (entry.Input == sequence[seqIndex])
                {
                    seqIndex--;
                }
            }
            return seqIndex < 0;
        }

        private struct InputEntry
        {
            public readonly ulong Input;
            public readonly int Frame;

            public InputEntry(ulong input, int frame)
            {
                Input = input;
                Frame = frame;
            }
        }
    }
}