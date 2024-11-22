using System;
using System.IO;
using BestoNet.Networking.Interfaces;
using UnityEngine;

namespace BestoNet.Networking.Examples
{
    // Example implementation for transform state
    public class NetworkTransform : MonoBehaviour, INetworkSerializable
    {
        [SerializeField] private string networkId;
        public string NetworkId => networkId;

        private void Start()
        {
            // Register with GameStateManager
            // I don't like using NewGuid for id's...
            // It's not TERRRIBLE. But can probably come up with an id generation algorithm or just by index.
            if (string.IsNullOrEmpty(networkId))
            {
                networkId = Guid.NewGuid().ToString();
            }
            RollbackManager.Instance.StateManager.RegisterObject(this);
        }

        private void OnDestroy()
        {
            // Unregister from GameStateManager
            RollbackManager.Instance.StateManager.UnregisterObject(this);
        }

        public void Serialize(BinaryWriter writer)
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            Vector3 scale = transform.localScale;

            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);

            writer.Write(rotation.x);
            writer.Write(rotation.y);
            writer.Write(rotation.z);
            writer.Write(rotation.w);

            writer.Write(scale.x);
            writer.Write(scale.y);
            writer.Write(scale.z);
        }

        public void Deserialize(BinaryReader reader)
        {
            Vector3 position = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            Quaternion rotation = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            Vector3 scale = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }

        public void SkipDeserialize(BinaryReader reader)
        {
            // Skip position (3 floats)
            reader.BaseStream.Position += sizeof(float) * 3;
            // Skip rotation (4 floats)
            reader.BaseStream.Position += sizeof(float) * 4;
            // Skip scale (3 floats)
            reader.BaseStream.Position += sizeof(float) * 3;
        }
    }
}