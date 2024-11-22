using BestoNet.Networking.Input;
using UnityEngine;

namespace BestoNet.Networking.Examples
{
    public class PlayerControllerExample : MonoBehaviour
    {
        [SerializeField] private UnityNewInputProvider inputProvider;
    
        private void Update()
        {
            ulong currentInput = inputProvider.GetInput();
        
            // Check directions
            if (currentInput.HasFlag(InputFlag.Right))
            {
                transform.position += Vector3.right * Time.deltaTime;
            }
        
            // Check combinations
            if (currentInput.HasFlag(InputFlag.DownRight))
            {
                // Handle diagonal input
            }

            if (IsSpecialMove(SpecialMoveDetector.Hadoken))
            {
                //PerformHadokenLogic();
            }
            
            // Or Check a custom sequence
            if (IsSpecialMove(new[] 
                {
                    InputFlag.Down,
                    InputFlag.DownRight,
                    InputFlag.Right,
                    InputFlag.Light | InputFlag.Medium // Two buttons together
                }))
            {
                //PerformCustomMoveLogic();
            }

            // Get raw direction
            InputFlag direction = currentInput.GetDirection();
            switch (direction)
            {
                case InputFlag.Up:
                    //Jump();
                    break;
                case InputFlag.UpRight:
                    //JumpForward();
                    break;
                // etc...
            }
        }
        
        public bool IsSpecialMove(InputFlag[] sequence, int withinFrames = 15)
        {
            return SpecialMoveDetector.DetectSpecialMove(inputProvider.History, sequence, withinFrames);
        }
    }
    
    public static class SpecialMoveDetector
    {
        public static readonly InputFlag[] Hadoken = new[]
        {
            InputFlag.Down,
            InputFlag.DownRight,
            InputFlag.Right,
            InputFlag.Special
        };

        public static readonly InputFlag[] Shoryuken = new[]
        {
            InputFlag.Right,
            InputFlag.Down,
            InputFlag.DownRight,
            InputFlag.Special
        };

        public static bool DetectSpecialMove(InputHistoryRecorder history, InputFlag[] sequence, int withinFrames = 15)
        {
            ulong[] ulongSequence = new ulong[sequence.Length];
            for (int i = 0; i < sequence.Length; i++)
            {
                ulongSequence[i] = (ulong)sequence[i];
            }
            return history.DetectSequence(ulongSequence, withinFrames);
        }
    }
}
