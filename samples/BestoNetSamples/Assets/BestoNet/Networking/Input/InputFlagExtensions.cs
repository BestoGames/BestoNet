using System;

namespace BestoNet.Networking.Input
{
    [Flags]
    public enum InputFlag
    {
        None = 0,
        // Directions (first 8 bits)
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        UpLeft = Up | Left, // Composite direction
        UpRight = Up | Right, // Composite direction
        DownLeft = Down | Left, // Composite direction
        DownRight = Down | Right, // Composite direction

        // Buttons (next 8 bits)
        Light = 1 << 8,
        Medium = 1 << 9,
        Heavy = 1 << 10,
        Special = 1 << 11,

        // System buttons
        Start = 1 << 16,
        Select = 1 << 17
    }

    public static class InputFlagExtensions
    {
        /// <summary>
        /// Checks if all specified flags are set in the input
        /// </summary>
        public static bool HasFlag(this ulong input, InputFlag flag)
        {
            return (input & (ulong)flag) == (ulong)flag;
        }

        /// <summary>
        /// Checks if any of the specified flags are set in the input
        /// </summary>
        public static bool HasAnyFlag(this ulong input, InputFlag flags)
        {
            return (input & (ulong)flags) != 0;
        }

        /// <summary>
        /// Gets just the directional input as a flag
        /// </summary>
        public static InputFlag GetDirection(this ulong input)
        {
            return (InputFlag)(input & 0xFF); // First 8 bits are directions
        }
    }
}