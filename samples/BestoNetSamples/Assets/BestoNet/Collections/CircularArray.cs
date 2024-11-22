// -----------------------------------------------------------------------------
// BestoNet - Deterministic Rollback Networking Library
// Copyright (c) 2024 BestoGames and contributors
// Licensed under the MIT License. See LICENSE in the project root.
// -----------------------------------------------------------------------------
//
// Provides a fixed-size circular array implementation for efficient circular
// buffer operations. Commonly used for managing rollback state history
// and input buffers with wraparound behavior.
//
// For more information, visit: https://github.com/BestoGames/BestoNet
// -----------------------------------------------------------------------------

using System;

namespace BestoNet.Collections
{
    /// <summary>
    /// Represents a fixed-size circular array that wraps around when accessing elements beyond its size.
    /// Elements are stored in a continuous block of memory and accessed using modulo arithmetic.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    public class CircularArray<T>
    {
        private readonly int _size;
        private readonly T[] _data;
        private int _count;

        /// <summary>
        /// Gets the number of elements currently stored in the circular array.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Initializes a new instance of the CircularArray class with the specified size.
        /// </summary>
        /// <param name="size">The fixed size of the circular array.</param>
        /// <exception cref="ArgumentException">Thrown when size is less than or equal to zero.</exception>
        public CircularArray(int size)
        {
            if (size <= 0)
                throw new ArgumentException("Size must be positive", nameof(size));

            _size = size;
            _data = new T[size];
            _count = 0;
        }

        /// <summary>
        /// Inserts a value at the specified index. If the index exceeds the array size,
        /// it wraps around to the beginning using modulo arithmetic.
        /// </summary>
        /// <param name="index">The index at which to insert the value. Can be any integer value.</param>
        /// <param name="value">The value to insert into the array.</param>
        public virtual void Insert(int index, T value)
        {
            int modIndex = index % _size;
            if (_data[modIndex] == null || _data[modIndex].Equals(default(T)))
            {
                _count = Math.Min(_count + 1, _size);
            }
            _data[modIndex] = value;
        }

        /// <summary>
        /// Retrieves the value at the specified index. If the index exceeds the array size,
        /// it wraps around to the beginning using modulo arithmetic.
        /// </summary>
        /// <param name="index">The index from which to retrieve the value. Can be any integer value.</param>
        /// <returns>The value at the specified index after applying modulo arithmetic.</returns>
        public T Get(int index)
        {
            int modIndex = index % _size;
            return _data[modIndex];
        }

        /// <summary>
        /// Returns a reference to the underlying array containing all values.
        /// </summary>
        /// <returns>The array containing all values in the circular array.</returns>
        /// <remarks>
        /// The returned array reference provides direct access to the internal storage.
        /// Modifications to the returned array will affect the CircularArray's contents.
        /// </remarks>
        public T[] GetValues()
        {
            return _data;
        }

        /// <summary>
        /// Clears all elements in the circular array, setting them to their default values.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_data, 0, _data.Length);
            _count = 0;
        }
    }
}