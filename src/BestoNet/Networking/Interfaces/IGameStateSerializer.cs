namespace BestoNet.Networking.Interfaces
{
    public interface IGameStateSerializer
    {
        byte[] SerializeState();
        void DeserializeState(byte[] state);
        uint CalculateChecksum();
    }
}
