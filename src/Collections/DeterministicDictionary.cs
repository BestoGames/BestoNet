// -----------------------------------------------------------------------------
// BestoNet - Deterministic Rollback Networking Library
// Copyright (c) 2024 BestoGames and contributors
// Licensed under the MIT License. See LICENSE in the project root.
// -----------------------------------------------------------------------------
//
// Implements a deterministic dictionary that maintains consistent insertion and
// enumeration order across different platforms and runs. 
//
// For more information, visit: https://github.com/BestoGames/BestoNet
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace BestoNet.Collections
{

    /// <summary>
    /// A deterministic dictionary implementation that maintains insertion order and provides 
    /// consistent enumeration order across different platforms/runs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public class DeterministicDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents an entry in the dictionary.
        /// </summary>
        private struct Entry
        {
            /// <summary>Hash code of the key.</summary>
            public int HashCode;  
            /// <summary>Index of next entry in the same bucket.</summary>
            public int Next;
            /// <summary>The key of the entry.</summary>
            public TKey Key;
            /// <summary>The value of the entry.</summary>
            public TValue Value;
            /// <summary>Whether this entry is valid or deleted.</summary>
            public bool IsValid;
        }

        private Entry[] _entries;
        private int[] _buckets;
        private int _version;
        private const int DefaultCapacity = 4;
        private readonly IEqualityComparer<TKey> _comparer;

        /// <summary>
        /// Initializes a new instance of DeterministicDictionary with default capacity.
        /// </summary>
        public DeterministicDictionary() : this(DefaultCapacity, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of DeterministicDictionary with specified capacity and comparer.
        /// </summary>
        /// <param name="capacity">Initial capacity of the dictionary.</param>
        /// <param name="comparer">The equality comparer to use for the keys.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is negative.</exception>
        public DeterministicDictionary(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            Initialize(capacity);
        }

        private void Initialize(int capacity)
        {
            capacity = Math.Max(capacity, DefaultCapacity);
            _buckets = new int[capacity];
            _entries = new Entry[capacity];
            
            // Initialize buckets with -1 to indicate empty
            for (int i = 0; i < _buckets.Length; i++)
            {
                _buckets[i] = -1;
            }
        }

        private void Resize()
        {
            int newSize = _entries.Length * 2;
            int[] newBuckets = new int[newSize];
            Entry[] newEntries = new Entry[newSize];

            // Initialize new buckets
            for (int i = 0; i < newBuckets.Length; i++)
            {
                newBuckets[i] = -1;
            }

            // Copy existing entries
            Array.Copy(_entries, 0, newEntries, 0, Count);

            // Reindex all entries
            for (int i = 0; i < Count; i++)
            {
                if (!newEntries[i].IsValid) continue;

                int bucket = newEntries[i].HashCode % newSize;
                newEntries[i].Next = newBuckets[bucket];
                newBuckets[bucket] = i;
            }

            _buckets = newBuckets;
            _entries = newEntries;
        }

        /// <summary>
        /// Adds a key/value pair to the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the key already exists in the dictionary.</exception>
        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                value = _entries[i].Value;
                return true;
            }
            value = default;
            return false;
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (Count == _entries.Length)
            {
                Resize();
            }

            int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % _buckets.Length;

            // Check if key already exists
            int i = _buckets[targetBucket];
            while (i >= 0)
            {
                ref Entry entry = ref _entries[i];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    if (add)
                    {
                        throw new ArgumentException("Key already exists");
                    }
                    entry.Value = value;
                    _version++;
                    return;
                }
                i = entry.Next;
            }

            // Add new entry
            Entry newEntry = new Entry
            {
                HashCode = hashCode,
                Key = key,
                Value = value,
                Next = _buckets[targetBucket],
                IsValid = true
            };

            _entries[Count] = newEntry;
            _buckets[targetBucket] = Count;
            Count++;
            _version++;
        }

        private int FindEntry(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (_buckets == null) return -1;
            
            int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            int bucket = hashCode % _buckets.Length;
            int i = _buckets[bucket];

            while (i >= 0)
            {
                ref Entry entry = ref _entries[i];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    return i;
                }
                i = entry.Next;
            }
            return -1;
        }

        public bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (_buckets == null) return false;
            
            int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            int bucket = hashCode % _buckets.Length;
            int last = -1;
            int i = _buckets[bucket];

            while (i >= 0)
            {
                ref Entry entry = ref _entries[i];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = entry.Next;
                    }
                    else
                    {
                        _entries[last].Next = entry.Next;
                    }
                    entry.IsValid = false;
                    entry.Next = -1;
                    _version++;
                    return true;
                }
                last = i;
                i = entry.Next;
            }
            return false;
        }

        public void Clear()
        {
            if (Count <= 0) return;
            
            Array.Clear(_buckets, 0, _buckets.Length);
            Array.Clear(_entries, 0, _entries.Length);
            Count = 0;
            _version++;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }
        
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly DeterministicDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;

            internal Enumerator(DeterministicDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                {
                    throw new InvalidOperationException("Collection was modified during enumeration");
                }

                while (_index < _dictionary.Count)
                {
                    if (_dictionary._entries[_index].IsValid)
                    {
                        _current = new KeyValuePair<TKey, TValue>(
                            _dictionary._entries[_index].Key,
                            _dictionary._entries[_index].Value);
                        _index++;
                        return true;
                    }
                    _index++;
                }

                _index = _dictionary.Count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _dictionary.Count + 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            public void Reset()
            {
                if (_version != _dictionary._version)
                {
                    throw new InvalidOperationException("Collection was modified during enumeration");
                }
                _index = 0;
                _current = default;
            }

            public void Dispose() { }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(TKey key) => FindEntry(key) >= 0;

        /// <summary>
        /// Attempts to add a key/value pair to the dictionary. Returns true if the pair was added successfully,
        /// false if the key already exists.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>True if the pair was added successfully; false if the key already exists.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            try
            {
                Add(key, value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key.</returns>
        /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
        /// <exception cref="ArgumentNullException">Key is null.</exception>
        public TValue this[TKey key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0) return _entries[i].Value;
                throw new KeyNotFoundException();
            }
            set => Insert(key, value, false);
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// The order of the keys matches the order of insertion.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                List<TKey> keys = new(Count);
                for (int i = 0; i < Count; i++)
                {
                    if (_entries[i].IsValid)
                    {
                        keys.Add(_entries[i].Key);
                    }
                }
                return keys;
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// The order of the values matches the order of insertion of their corresponding keys.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new(Count);
                for (int i = 0; i < Count; i++)
                {
                    if (_entries[i].IsValid)
                    {
                        values.Add(_entries[i].Value);
                    }
                }
                return values;
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// Always returns false for this implementation.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds a key/value pair to the dictionary.
        /// </summary>
        /// <param name="item">The key/value pair to add.</param>
        /// <exception cref="ArgumentException">An item with the same key already exists.</exception>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific key/value pair.
        /// </summary>
        /// <param name="item">The key/value pair to locate.</param>
        /// <returns>True if the key/value pair is found in the dictionary; otherwise, false.</returns>

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            int i = FindEntry(item.Key);
            return i >= 0 && EqualityComparer<TValue>.Default.Equals(_entries[i].Value, item.Value);
        }

        /// <summary>
        /// Copies the elements of the dictionary to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException">Array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">ArrayIndex is less than 0 or greater than the array length.</exception>
        /// <exception cref="ArgumentException">The number of elements in the dictionary is greater than the available space from arrayIndex to the end of the destination array.</exception>

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Insufficient space in array");

            for (int i = 0; i < Count; i++)
            {
                if (_entries[i].IsValid)
                {
                    array[arrayIndex++] = new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
                }
            }
        }

        /// <summary>
        /// Removes a specific key/value pair from the dictionary.
        /// </summary>
        /// <param name="item">The key/value pair to remove.</param>
        /// <returns>True if the key/value pair was successfully found and removed; otherwise, false.</returns>

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            int i = FindEntry(item.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(_entries[i].Value, item.Value))
            {
                Remove(item.Key);
                return true;
            }
            return false;
        }
    }
}