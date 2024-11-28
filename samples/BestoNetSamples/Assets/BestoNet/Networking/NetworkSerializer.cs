using System;
using System.Runtime.InteropServices;

namespace BestoNetSamples.BestoNet.Networking
{
    public static class NetworkSerializer
    {
        /// <summary>
        /// Serializes a struct to a byte array
        /// </summary>
        /// <typeparam name="T">Struct type to serialize</typeparam>
        /// <param name="data">Struct instance to serialize</param>
        /// <returns>Byte array containing the serialized data</returns>
        public static byte[] Serialize<T>(T data) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(data, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                return arr;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Deserializes a byte array back into a struct
        /// </summary>
        /// <typeparam name="T">Struct type to deserialize into</typeparam>
        /// <param name="arr">Byte array containing the serialized data</param>
        /// <returns>Deserialized struct instance</returns>
        public static T Deserialize<T>(byte[] arr) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (arr.Length != size)
            {
                throw new ArgumentException($"Array size ({arr.Length}) does not match struct size ({size})");
            }
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(arr, 0, ptr, size);
                return Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Gets the size in bytes of a struct type
        /// </summary>
        /// <typeparam name="T">Struct type to measure</typeparam>
        /// <returns>Size in bytes</returns>
        public static int GetStructSize<T>() where T : struct
        {
            return Marshal.SizeOf<T>();
        }
    }
}