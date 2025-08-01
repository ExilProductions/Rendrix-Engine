using System;
using System.Numerics;

namespace RendrixEngine.Mathematics
{
    public struct Vector3D
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        private const double Epsilon = 1e-6;

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D Zero => new Vector3D(0, 0, 0);
        public static Vector3D One => new Vector3D(1, 1, 1);
        public static Vector3D UnitX => new Vector3D(1, 0, 0);
        public static Vector3D UnitY => new Vector3D(0, 1, 0);
        public static Vector3D UnitZ => new Vector3D(0, 0, 1);
        public static Vector3D Right => new Vector3D(1, 0, 0);
        public static Vector3D Left => new Vector3D(-1, 0, 0);
        public static Vector3D Up => new Vector3D(0, 1, 0);
        public static Vector3D Down => new Vector3D(0, -1, 0);
        public static Vector3D Forward => new Vector3D(0, 0, 1);
        public static Vector3D Back => new Vector3D(0, 0, -1);

        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(float s, Vector3D v) => new Vector3D(s * v.X, s * v.Y, s * v.Z);
        public static Vector3D operator *(Vector3D v, float s) => s * v;

        public static bool operator ==(Vector3D left, Vector3D right)
        {
            return Math.Abs(left.X - right.X) < Epsilon &&
                   Math.Abs(left.Y - right.Y) < Epsilon &&
                   Math.Abs(left.Z - right.Z) < Epsilon;
        }

        public static bool operator !=(Vector3D left, Vector3D right)
        {
            return !(left == right);
        }
        public bool Equals(Vector3D other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3D other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hashX = Math.Round(X / Epsilon).GetHashCode();
            int hashY = Math.Round(Y / Epsilon).GetHashCode();
            int hashZ = Math.Round(Z / Epsilon).GetHashCode();
            return HashCode.Combine(hashX, hashY, hashZ);
        }

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3D Normalized
        {
            get
            {
                float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
                if (length < 1e-6f)
                    throw new InvalidOperationException("Cannot normalize zero-length vector.");
                return new Vector3D(X / length, Y / length, Z / length);
            }
        }

        public static float Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Vector3D Cross(Vector3D a, Vector3D b) => new Vector3D(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );

        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }
}