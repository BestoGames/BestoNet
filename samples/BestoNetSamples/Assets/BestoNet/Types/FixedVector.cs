// -----------------------------------------------------------------------------
// BestoNet - Deterministic Rollback Networking Library
// Copyright (c) 2024 BestoGames and contributors
// Licensed under the MIT License. See LICENSE in the project root.
// -----------------------------------------------------------------------------
//
// Provides generic 2D, 3D, and 4D vector implementations using fixed-point arithmetic
// for deterministic floating-point calculations across different platforms.
// Essential for consistent physics and game state calculations.
//
// For more information, visit: https://github.com/BestoGames/BestoNet
// -----------------------------------------------------------------------------

using System;

namespace BestoNet.Types
{
    /// <summary>
    /// Represents a 2D vector with fixed-point components.
    /// </summary>
    /// <typeparam name="T">The fixed-point type (Fixed16, Fixed32, or Fixed64) used for vector components.</typeparam>
    public readonly struct Vector2<T> : IEquatable<Vector2<T>> where T : struct
    {
        /// <summary>The X component of the vector.</summary>
        public readonly T X;
        /// <summary>The Y component of the vector.</summary>
        public readonly T Y;

        /// <summary>
        /// Initializes a new instance of Vector2 with the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public Vector2(T x, T y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Gets a Vector2 with coordinates (0,0).</summary>
        public static Vector2<T> Zero => new Vector2<T>(default, default);

        
        /// <summary>
        /// Adds two vectors component-wise.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the sum.</returns>
        public static Vector2<T> operator +(Vector2<T> a, Vector2<T> b) => new Vector2<T>(
            Operations<T>.Add(a.X, b.X),
            Operations<T>.Add(a.Y, b.Y));

        /// <summary>
        /// Subtracts the second vector from the first component-wise.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the difference.</returns>
        public static Vector2<T> operator -(Vector2<T> a, Vector2<T> b) => new Vector2<T>(
            Operations<T>.Subtract(a.X, b.X),
            Operations<T>.Subtract(a.Y, b.Y));

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="a">The vector to multiply.</param>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>A new vector with scaled components.</returns>
        public static Vector2<T> operator *(Vector2<T> a, T scalar) => new Vector2<T>(
            Operations<T>.Multiply(a.X, scalar),
            Operations<T>.Multiply(a.Y, scalar));

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="a">The vector to divide.</param>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>A new vector with divided components.</returns>
        public static Vector2<T> operator /(Vector2<T> a, T scalar) => new Vector2<T>(
            Operations<T>.Divide(a.X, scalar),
            Operations<T>.Divide(a.Y, scalar));

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product value.</returns>
        public static T Dot(Vector2<T> a, Vector2<T> b) =>
            Operations<T>.Add(
                Operations<T>.Multiply(a.X, b.X),
                Operations<T>.Multiply(a.Y, b.Y));

        /// <summary>
        /// Calculates the squared magnitude of the vector.
        /// </summary>
        /// <returns>The squared magnitude value.</returns>
        public T SqrMagnitude() => Dot(this, this);

        /// <summary>
        /// Calculates the magnitude (length) of the vector.
        /// </summary>
        /// <returns>The magnitude value.</returns>
        public T Magnitude() => Operations<T>.Sqrt(SqrMagnitude());

        /// <summary>
        /// Returns a normalized version of this vector (magnitude of 1).
        /// </summary>
        /// <returns>A new normalized vector.</returns>
        public Vector2<T> Normalized()
        {
            T mag = Magnitude();
            return Operations<T>.IsZero(mag) ? Zero : this / mag;
        }

        /// <summary>
        /// Determines whether this vector is equal to another vector.
        /// </summary>
        /// <param name="other">The vector to compare with.</param>
        /// <returns>True if the vectors are equal, false otherwise.</returns>
        public bool Equals(Vector2<T> other) =>
            Operations<T>.AreEqual(X, other.X) && 
            Operations<T>.AreEqual(Y, other.Y);

        /// <summary>
        /// Returns a string representation of the vector.
        /// </summary>
        /// <returns>A string in the format "(X, Y)".</returns>
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>
    /// Represents a 3D vector with fixed-point components.
    /// </summary>
    /// <typeparam name="T">The fixed-point type (Fixed16, Fixed32, or Fixed64) used for vector components.</typeparam>
    public readonly struct Vector3<T> : IEquatable<Vector3<T>> where T : struct
    {
        /// <summary>The X component of the vector.</summary>
        public readonly T X;
        /// <summary>The Y component of the vector.</summary>
        public readonly T Y;
        /// <summary>The Z component of the vector.</summary>
        public readonly T Z;

        /// <summary>
        /// Initializes a new instance of Vector3 with the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        public Vector3(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Gets a Vector3 with coordinates (0,0,0).</summary>
        public static Vector3<T> Zero => new Vector3<T>(default, default, default);

        /// <summary>
        /// Adds two vectors component-wise.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the sum.</returns>
        public static Vector3<T> operator +(Vector3<T> a, Vector3<T> b) => new Vector3<T>(
            Operations<T>.Add(a.X, b.X),
            Operations<T>.Add(a.Y, b.Y),
            Operations<T>.Add(a.Z, b.Z));

        /// <summary>
        /// Subtracts the second vector from the first component-wise.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the difference.</returns>
        public static Vector3<T> operator -(Vector3<T> a, Vector3<T> b) => new Vector3<T>(
            Operations<T>.Subtract(a.X, b.X),
            Operations<T>.Subtract(a.Y, b.Y),
            Operations<T>.Subtract(a.Z, b.Z));

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="a">The vector to multiply.</param>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>A new vector with scaled components.</returns>
        public static Vector3<T> operator *(Vector3<T> a, T scalar) => new Vector3<T>(
            Operations<T>.Multiply(a.X, scalar),
            Operations<T>.Multiply(a.Y, scalar),
            Operations<T>.Multiply(a.Z, scalar));

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="a">The vector to divide.</param>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>A new vector with divided components.</returns>
        public static Vector3<T> operator /(Vector3<T> a, T scalar) => new Vector3<T>(
            Operations<T>.Divide(a.X, scalar),
            Operations<T>.Divide(a.Y, scalar),
            Operations<T>.Divide(a.Z, scalar));

        
        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product value.</returns>
        public static T Dot(Vector3<T> a, Vector3<T> b) =>
            Operations<T>.Add(
                Operations<T>.Add(
                    Operations<T>.Multiply(a.X, b.X),
                    Operations<T>.Multiply(a.Y, b.Y)),
                Operations<T>.Multiply(a.Z, b.Z));

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the cross product.</returns>
        public static Vector3<T> Cross(Vector3<T> a, Vector3<T> b) => new Vector3<T>(
            Operations<T>.Subtract(
                Operations<T>.Multiply(a.Y, b.Z),
                Operations<T>.Multiply(a.Z, b.Y)),
            Operations<T>.Subtract(
                Operations<T>.Multiply(a.Z, b.X),
                Operations<T>.Multiply(a.X, b.Z)),
            Operations<T>.Subtract(
                Operations<T>.Multiply(a.X, b.Y),
                Operations<T>.Multiply(a.Y, b.X)));

        /// <summary>
        /// Calculates the squared magnitude of the vector.
        /// </summary>
        /// <returns>The squared magnitude value.</returns>
        public T SqrMagnitude() => Dot(this, this);

        /// <summary>
        /// Calculates the magnitude (length) of the vector.
        /// </summary>
        /// <returns>The magnitude value.</returns>
        public T Magnitude() => Operations<T>.Sqrt(SqrMagnitude());

        /// <summary>
        /// Returns a normalized version of this vector (magnitude of 1).
        /// </summary>
        /// <returns>A new normalized vector.</returns>
        public Vector3<T> Normalized()
        {
            T mag = Magnitude();
            return Operations<T>.IsZero(mag) ? Zero : this / mag;
        }

        /// <summary>
        /// Determines whether this vector is equal to another vector.
        /// </summary>
        /// <param name="other">The vector to compare with.</param>
        /// <returns>True if the vectors are equal, false otherwise.</returns>
        public bool Equals(Vector3<T> other) =>
            Operations<T>.AreEqual(X, other.X) &&
            Operations<T>.AreEqual(Y, other.Y) &&
            Operations<T>.AreEqual(Z, other.Z);

        /// <summary>
        /// Returns a string representation of the vector.
        /// </summary>
        /// <returns>A string in the format "(X, Y, Z)".</returns>
        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    /// <summary>
    /// Represents a 4D vector with fixed-point components.
    /// </summary>
    /// <typeparam name="T">The fixed-point type (Fixed16, Fixed32, or Fixed64) used for vector components.</typeparam>
    public readonly struct Vector4<T> : IEquatable<Vector4<T>> where T : struct
    {
        /// <summary>The X component of the vector.</summary>
        public readonly T X;
        /// <summary>The Y component of the vector.</summary>
        public readonly T Y;
        /// <summary>The Z component of the vector.</summary>
        public readonly T Z;
        /// <summary>The W component of the vector.</summary>
        public readonly T W;

        /// <summary>
        /// Initializes a new instance of Vector4 with the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <param name="w">The W coordinate.</param>
        public Vector4(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>Gets a Vector4 with coordinates (0,0,0,0).</summary>
        public static Vector4<T> Zero => new Vector4<T>(default, default, default, default);

        /// <summary>
        /// Adds two vectors component-wise.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the sum.</returns>
        public static Vector4<T> operator +(Vector4<T> a, Vector4<T> b) => new Vector4<T>(
            Operations<T>.Add(a.X, b.X),
            Operations<T>.Add(a.Y, b.Y),
            Operations<T>.Add(a.Z, b.Z),
            Operations<T>.Add(a.W, b.W));
        
        /// <summary>
        /// Subtracts the second vector from the first component-wise.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A new vector representing the difference.</returns>
        public static Vector4<T> operator -(Vector4<T> a, Vector4<T> b) => new Vector4<T>(
            Operations<T>.Subtract(a.X, b.X),
            Operations<T>.Subtract(a.Y, b.Y),
            Operations<T>.Subtract(a.Z, b.Z),
            Operations<T>.Subtract(a.W, b.W));

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="a">The vector to multiply.</param>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>A new vector with scaled components.</returns>
        public static Vector4<T> operator *(Vector4<T> a, T scalar) => new Vector4<T>(
            Operations<T>.Multiply(a.X, scalar),
            Operations<T>.Multiply(a.Y, scalar),
            Operations<T>.Multiply(a.Z, scalar),
            Operations<T>.Multiply(a.W, scalar));

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="a">The vector to divide.</param>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>A new vector with divided components.</returns>
        public static Vector4<T> operator /(Vector4<T> a, T scalar) => new Vector4<T>(
            Operations<T>.Divide(a.X, scalar),
            Operations<T>.Divide(a.Y, scalar),
            Operations<T>.Divide(a.Z, scalar),
            Operations<T>.Divide(a.W, scalar));

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product value.</returns>
        public static T Dot(Vector4<T> a, Vector4<T> b) =>
            Operations<T>.Add(
                Operations<T>.Add(
                    Operations<T>.Add(
                        Operations<T>.Multiply(a.X, b.X),
                        Operations<T>.Multiply(a.Y, b.Y)),
                    Operations<T>.Multiply(a.Z, b.Z)),
                Operations<T>.Multiply(a.W, b.W));

        /// <summary>
        /// Calculates the squared magnitude of the vector.
        /// </summary>
        /// <returns>The squared magnitude value.</returns>
        public T SqrMagnitude() => Dot(this, this);

        /// <summary>
        /// Calculates the magnitude (length) of the vector.
        /// </summary>
        /// <returns>The magnitude value.</returns>
        public T Magnitude() => Operations<T>.Sqrt(SqrMagnitude());

        /// <summary>
        /// Returns a normalized version of this vector (magnitude of 1).
        /// </summary>
        /// <returns>A new normalized vector.</returns>
        public Vector4<T> Normalized()
        {
            T mag = Magnitude();
            return Operations<T>.IsZero(mag) ? Zero : this / mag;
        }

        public bool Equals(Vector4<T> other) =>
            Operations<T>.AreEqual(X, other.X) &&
            Operations<T>.AreEqual(Y, other.Y) &&
            Operations<T>.AreEqual(Z, other.Z) &&
            Operations<T>.AreEqual(W, other.W);

        public override string ToString() => $"({X}, {Y}, {Z}, {W})";
    }
}
