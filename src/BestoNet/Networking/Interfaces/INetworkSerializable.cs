using System.IO;

namespace BestoNet.Networking.Interfaces
{
    public interface INetworkSerializable
    {
        string NetworkId { get; }
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
        void SkipDeserialize(BinaryReader reader);
    }
}