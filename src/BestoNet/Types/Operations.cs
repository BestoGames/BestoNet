// -----------------------------------------------------------------------------
// BestoNet - Deterministic Rollback Networking Library
// Copyright (c) 2024 BestoGames and contributors
// Licensed under the MIT License. See LICENSE in the project root.
// -----------------------------------------------------------------------------
//
// Implements generic fixed-point arithmetic operations supporting multiple precision levels
// (Fixed16, Fixed32, Fixed64) with consistent behavior across platforms.
//
// For more information, visit: https://github.com/BestoGames/BestoNet
// -----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace BestoNet.Types
{
    /// <summary>
    /// Provides generic operations for fixed-point number types.
    /// </summary>
    /// <typeparam name="T">The fixed-point type to perform operations on.</typeparam>
    internal static class Operations<T> where T : struct
    {
        /// <summary>
        /// Adds two fixed-point numbers.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        /// <returns>The sum of the two numbers.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Add(T a, T b)
        {
            if (typeof(T) == typeof(Fixed16)) return (T)(object)((Fixed16)(object)a + (Fixed16)(object)b);
            if (typeof(T) == typeof(Fixed32)) return (T)(object)((Fixed32)(object)a + (Fixed32)(object)b);
            if (typeof(T) == typeof(Fixed64)) return (T)(object)((Fixed64)(object)a + (Fixed64)(object)b);
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        /// <summary>
        /// Subtracts two fixed-point numbers.
        /// </summary>
        /// <param name="a">The number to subtract from.</param>
        /// <param name="b">The number to subtract.</param>
        /// <returns>The difference between the two numbers.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Subtract(T a, T b)
        {
            if (typeof(T) == typeof(Fixed16)) return (T)(object)((Fixed16)(object)a - (Fixed16)(object)b);
            if (typeof(T) == typeof(Fixed32)) return (T)(object)((Fixed32)(object)a - (Fixed32)(object)b);
            if (typeof(T) == typeof(Fixed64)) return (T)(object)((Fixed64)(object)a - (Fixed64)(object)b);
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        /// <summary>
        /// Multiplies two fixed-point numbers.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        /// <returns>The product of the two numbers.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Multiply(T a, T b)
        {
            if (typeof(T) == typeof(Fixed16)) return (T)(object)((Fixed16)(object)a * (Fixed16)(object)b);
            if (typeof(T) == typeof(Fixed32)) return (T)(object)((Fixed32)(object)a * (Fixed32)(object)b);
            if (typeof(T) == typeof(Fixed64)) return (T)(object)((Fixed64)(object)a * (Fixed64)(object)b);
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        /// <summary>
        /// Divides two fixed-point numbers.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The quotient of the division.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Divide(T a, T b)
        {
            if (typeof(T) == typeof(Fixed16)) return (T)(object)((Fixed16)(object)a / (Fixed16)(object)b);
            if (typeof(T) == typeof(Fixed32)) return (T)(object)((Fixed32)(object)a / (Fixed32)(object)b);
            if (typeof(T) == typeof(Fixed64)) return (T)(object)((Fixed64)(object)a / (Fixed64)(object)b);
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        /// <summary>
        /// Calculates the square root of a fixed-point number.
        /// </summary>
        /// <param name="a">The number to calculate the square root of.</param>
        /// <returns>The square root of the input number.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sqrt(T a)
        {
            if (typeof(T) == typeof(Fixed16)) return (T)(object)Fixed16.FromFloat(MathF.Sqrt(((Fixed16)(object)a).ToFloat()));
            if (typeof(T) == typeof(Fixed32)) return (T)(object)Fixed32.Sqrt((Fixed32)(object)a);
            if (typeof(T) == typeof(Fixed64)) return (T)(object)Fixed64.FromFloat(MathF.Sqrt(((Fixed64)(object)a).ToFloat()));
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        /// <summary>
        /// Determines if a fixed-point number is zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is zero, false otherwise.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(T value)
        {
            if (typeof(T) == typeof(Fixed16)) return ((Fixed16)(object)value).Equals(Fixed16.FromInt(0));
            if (typeof(T) == typeof(Fixed32)) return ((Fixed32)(object)value).Equals(Fixed32.FromInt(0));
            if (typeof(T) == typeof(Fixed64)) return ((Fixed64)(object)value).Equals(Fixed64.FromInt(0));
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }

        /// <summary>
        /// Compares two fixed-point numbers for equality.
        /// </summary>
        /// <param name="a">The first number to compare.</param>
        /// <param name="b">The second number to compare.</param>
        /// <returns>True if the numbers are equal, false otherwise.</returns>
        /// <exception cref="NotSupportedException">Thrown when T is not a supported fixed-point type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqual(T a, T b)
        {
            if (typeof(T) == typeof(Fixed16)) return ((Fixed16)(object)a).Equals((Fixed16)(object)b);
            if (typeof(T) == typeof(Fixed32)) return ((Fixed32)(object)a).Equals((Fixed32)(object)b);
            if (typeof(T) == typeof(Fixed64)) return ((Fixed64)(object)a).Equals((Fixed64)(object)b);
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }
    }
}
