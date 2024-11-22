// -----------------------------------------------------------------------------
// BestoNet - Deterministic Rollback Networking Library
// Copyright (c) 2024 BestoGames and contributors
// Licensed under the MIT License. See LICENSE in the project root.
// -----------------------------------------------------------------------------
//
// Defines fixed-point number types (Fixed16, Fixed32, Fixed64) that provide
// deterministic floating-point arithmetic with different precision levels.
//
// For more information, visit: https://github.com/BestoGames/BestoNet
// -----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace BestoNet.Types
{
    /// <summary>
    /// 16-bit fixed-point number with 8 bits for the integer part and 8 bits for the fraction
    /// </summary>
    public readonly struct Fixed16 : IEquatable<Fixed16>, IComparable<Fixed16>
    {
        private const int FractionBits = 8;
        private const short One = 1 << FractionBits;
        private readonly short _rawValue;

        public Fixed16(short rawValue)
        {
            _rawValue = rawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed16 FromInt(int value)
        {
            return new Fixed16((short)(value * One));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed16 FromFloat(float value)
        {
            return new Fixed16((short)(value * One));
        }

        public float ToFloat()
        {
            return (float)_rawValue / One;
        }

        public static Fixed16 operator +(Fixed16 a, Fixed16 b) => new Fixed16((short)(a._rawValue + b._rawValue));
        public static Fixed16 operator -(Fixed16 a, Fixed16 b) => new Fixed16((short)(a._rawValue - b._rawValue));
        public static Fixed16 operator *(Fixed16 a, Fixed16 b) => new Fixed16((short)((a._rawValue * b._rawValue) >> FractionBits));
        public static Fixed16 operator /(Fixed16 a, Fixed16 b) => new Fixed16((short)((a._rawValue << FractionBits) / b._rawValue));

        public bool Equals(Fixed16 other) => _rawValue == other._rawValue;
        public int CompareTo(Fixed16 other) => _rawValue.CompareTo(other._rawValue);
        public override string ToString() => ToFloat().ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 32-bit fixed-point number with 16 bits for the integer part and 16 bits for the fraction
    /// </summary>
    public readonly struct Fixed32 : IEquatable<Fixed32>, IComparable<Fixed32>
    {
        private const int FractionBits = 16;
        private const int One = 1 << FractionBits;
        private readonly int _rawValue;

        public Fixed32(int rawValue)
        {
            _rawValue = rawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 FromInt(int value)
        {
            return new Fixed32(value * One);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 FromFloat(float value)
        {
            return new Fixed32((int)(value * One));
        }

        public static Fixed32 Sqrt(Fixed32 value)
        {
            throw new NotImplementedException();
        }

        public float ToFloat()
        {
            return (float)_rawValue / One;
        }

        public static Fixed32 operator +(Fixed32 a, Fixed32 b) => new Fixed32(a._rawValue + b._rawValue);
        public static Fixed32 operator -(Fixed32 a, Fixed32 b) => new Fixed32(a._rawValue - b._rawValue);
        public static Fixed32 operator *(Fixed32 a, Fixed32 b) => new Fixed32((int)(((long)a._rawValue * b._rawValue) >> FractionBits));
        public static Fixed32 operator /(Fixed32 a, Fixed32 b) => new Fixed32((int)(((long)a._rawValue << FractionBits) / b._rawValue));

        public bool Equals(Fixed32 other) => _rawValue == other._rawValue;
        public int CompareTo(Fixed32 other) => _rawValue.CompareTo(other._rawValue);
        public override string ToString() => ToFloat().ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 64-bit fixed-point number with 32 bits for the integer part and 32 bits for the fraction
    /// </summary>
    public readonly struct Fixed64 : IEquatable<Fixed64>, IComparable<Fixed64>
    {
        private const int FractionBits = 32;
        private const long One = 1L << FractionBits;
        private readonly long _rawValue;

        public Fixed64(long rawValue)
        {
            _rawValue = rawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 FromInt(int value)
        {
            return new Fixed64((value) * One);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 FromFloat(float value)
        {
            return new Fixed64((long)(value * One));
        }

        public float ToFloat()
        {
            return (float)_rawValue / One;
        }

        public static Fixed64 operator +(Fixed64 a, Fixed64 b) => new Fixed64(a._rawValue + b._rawValue);
        public static Fixed64 operator -(Fixed64 a, Fixed64 b) => new Fixed64(a._rawValue - b._rawValue);
        public static Fixed64 operator *(Fixed64 a, Fixed64 b)
        {
            // Handle multiplication without overflow
            long ah = a._rawValue >> 32;
            long al = a._rawValue & 0xFFFFFFFF;
            long bh = b._rawValue >> 32;
            long bl = b._rawValue & 0xFFFFFFFF;
            
            long result = ((ah * bh) << (FractionBits - 32)) + (ah * bl >> 32) + (al * bh >> 32) + ((al * bl) >> FractionBits);
            return new Fixed64(result);
        }
        public static Fixed64 operator /(Fixed64 a, Fixed64 b) => new Fixed64((a._rawValue << FractionBits) / b._rawValue);

        public bool Equals(Fixed64 other) => _rawValue == other._rawValue;
        public int CompareTo(Fixed64 other) => _rawValue.CompareTo(other._rawValue);
        public override string ToString() => ToFloat().ToString(CultureInfo.InvariantCulture);
    }
}