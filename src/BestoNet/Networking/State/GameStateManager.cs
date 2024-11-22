using System;
using System.Collections.Generic;
using System.IO;
using BestoNet.Networking.Interfaces;

namespace BestoNet.Networking.State
{
    /// <summary>
    /// Central component that tracks and manages game state serialization
    /// </summary>
    public class GameStateManager : IGameStateSerializer
    {
        // maybe could make an array
        private readonly List<INetworkSerializable> _trackedObjects = new();

        public void RegisterObject(INetworkSerializable obj)
        {
            if (!_trackedObjects.Contains(obj))
            {
                _trackedObjects.Add(obj);
            }
        }

        public void UnregisterObject(INetworkSerializable obj)
        {
            _trackedObjects.Remove(obj);
        }

        // TODO: should writer and stream be cached or pooled?
        public byte[] SerializeState()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            // Write number of objects
            writer.Write(_trackedObjects.Count);

            // Write each object's state
            foreach (INetworkSerializable obj in _trackedObjects)
            {
                // Write object identifier
                writer.Write(obj.NetworkId);
                // Let object write its state
                obj.Serialize(writer);
            }
            return stream.ToArray();
        }

        public void DeserializeState(byte[] state)
        {
            using MemoryStream stream = new(state);
            using BinaryReader reader = new(stream);
            try
            {
                int objectCount = reader.ReadInt32();
                for (int i = 0; i < objectCount; i++)
                {
                    // Read object identifier
                    string networkId = reader.ReadString();
                    // Find corresponding object
                    INetworkSerializable obj = _trackedObjects.Find(o => o.NetworkId == networkId);
                    if (obj != null)
                    {
                        obj.Deserialize(reader);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"No registered object found with network ID: {networkId}");
                        // Skip this object's data
                        obj?.SkipDeserialize(reader);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error deserializing game state: {e.Message}");
            }
        }

        public uint CalculateChecksum()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            foreach (INetworkSerializable obj in _trackedObjects)
            {
                writer.Write(obj.NetworkId);
                obj.Serialize(writer);
            }
            byte[] data = stream.ToArray();
            uint checksum = 0;

            // FNV-1a hash
            // https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
            // can use a different algorithm....
            const uint fnvPrime = 0x01000193;
            const uint fnvOffset = 0x811C9DC5;
            checksum = fnvOffset;
            
            for (int i = 0; i < data.Length; i++)
            {
                checksum ^= data[i];
                checksum *= fnvPrime;
            }
            return checksum;
        }
    }
}