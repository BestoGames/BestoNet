// -----------------------------------------------------------------------------
// BestoNet - Deterministic Rollback Networking Library
// Copyright (c) 2024 BestoGames and contributors
// Licensed under the MIT License. See LICENSE in the project root.
// -----------------------------------------------------------------------------
//
// Implements a deterministic random number generator using the xoshiro256** algorithm
// for consistent cross-platform random number generation in rollback scenarios.
// Ensures reproducible random sequences given the same seed value.
//
// For more information, visit: https://github.com/BestoGames/BestoNet
// -----------------------------------------------------------------------------

using System;
using BestoNet.Types;

namespace BestoNet.Random
{
    /// <summary>
    /// A deterministic random number generator that produces the same sequence of numbers
    /// given the same seed. Uses the xoshiro256** algorithm.
    /// https://en.wikipedia.org/wiki/Xorshift
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong _s0, _s1, _s2, _s3;

        /// <summary>
        /// Initializes a new instance of DeterministicRandom with a specific seed.
        /// </summary>
        /// <param name="seed">The seed value to initialize the random sequence.</param>
        public DeterministicRandom(long seed)
        {
            // Initialize the state using SplitMix64 for better seed distribution
            _s0 = SplitMix64(ref seed);
            _s1 = SplitMix64(ref seed);
            _s2 = SplitMix64(ref seed);
            _s3 = SplitMix64(ref seed);
        }

        /// <summary>
        /// Generates a random number between 0.0 and 1.0.
        /// </summary>
        public double NextDouble()
        {
            // Use the high 53 bits for double precision
            return (NextUInt64() >> 11) * (1.0 / (1ul << 53));
        }

        /// <summary>
        /// Generates a random integer between minValue (inclusive) and maxValue (exclusive).
        /// </summary>
        public int NextInt(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentException("minValue must be less than maxValue");

            long range = (long)maxValue - minValue;
            long scaled = (long)(NextDouble() * range);
            return (int)(scaled + minValue);
        }

        /// <summary>
        /// Generates a random Fixed32 between 0 and 1.
        /// </summary>
        public Fixed32 NextFixed32()
        {
            return Fixed32.FromFloat((float)NextDouble());
        }

        /// <summary>
        /// Generates a random Fixed64 between 0 and 1.
        /// </summary>
        public Fixed64 NextFixed64()
        {
            return Fixed64.FromFloat((float)NextDouble());
        }

        /// <summary>
        /// Fills the provided buffer with random bytes.
        /// </summary>
        public void NextBytes(byte[] buffer)
        {
            int i = 0;
            while (i < buffer.Length)
            {
                ulong value = NextUInt64();
                int remaining = buffer.Length - i;
                if (remaining >= 8)
                {
                    buffer[i++] = (byte)value;
                    buffer[i++] = (byte)(value >> 8);
                    buffer[i++] = (byte)(value >> 16);
                    buffer[i++] = (byte)(value >> 24);
                    buffer[i++] = (byte)(value >> 32);
                    buffer[i++] = (byte)(value >> 40);
                    buffer[i++] = (byte)(value >> 48);
                    buffer[i++] = (byte)(value >> 56);
                }
                else
                {
                    while (remaining-- > 0)
                    {
                        buffer[i++] = (byte)value;
                        value >>= 8;
                    }
                }
            }
        }

        private ulong NextUInt64()
        {
            ulong result = RotateLeft(_s1 * 5, 7) * 9;
            ulong t = _s1 << 17;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;

            _s2 ^= t;
            _s3 = RotateLeft(_s3, 45);

            return result;
        }

        private static ulong SplitMix64(ref long seed)
        {
            ulong newSeed = (ulong)seed + 0x9E3779B97F4A7C15UL; // Cast `seed` to `ulong`
            seed = (long)newSeed; // Convert back to `long` for storing in `seed`
            ulong z = newSeed;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        
        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}