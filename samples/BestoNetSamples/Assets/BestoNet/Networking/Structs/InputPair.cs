namespace BestoNet.Networking.Structs
{
    // just for storing remote and local inputs together for more readability and easier debugging
    public struct InputPair
    {
        public readonly ulong LocalInput;
        public readonly ulong RemoteInput;

        public InputPair(ulong localInput, ulong remoteInput)
        {
            LocalInput = localInput;
            RemoteInput = remoteInput;
        }
    }
}
