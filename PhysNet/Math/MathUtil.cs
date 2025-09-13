using System;
using System.Numerics;

namespace PhysNet.Math
{
    internal static class MathUtil
    {
        public const float Epsilon = 1e-6f;
        public const float LargeEpsilon = 1e-4f;
        public const float BiasRelative = 0.95f;
        public const float BiasAbsolute = 0.01f;

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        public static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

        public static Vector3 SafeNormalize(Vector3 v)
        {
            var len = v.Length();
            if (len > Epsilon) return v / len;
            return Vector3.Zero;
        }

        public static float Max3(float a, float b, float c) => System.Math.Max(a, System.Math.Max(b, c));
        public static float Min3(float a, float b, float c) => System.Math.Min(a, System.Math.Min(b, c));

        public static void OrthonormalBasis(Vector3 n, out Vector3 t, out Vector3 b)
        {
            if (System.Math.Abs(n.Z) > 0.70710678f)
            {
                float a = n.Y * n.Y + n.Z * n.Z;
                float k = 1.0f / System.MathF.Sqrt(a);
                t = new Vector3(0, -n.Z * k, n.Y * k);
                b = Vector3.Cross(n, t);
            }
            else
            {
                float a = n.X * n.X + n.Y * n.Y;
                float k = 1.0f / System.MathF.Sqrt(a);
                t = new Vector3(-n.Y * k, n.X * k, 0);
                b = Vector3.Cross(n, t);
            }
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            var f = Vector3.Normalize(from);
            var t = Vector3.Normalize(to);
            var cos = Vector3.Dot(f, t);
            if (cos >= 1 - Epsilon) return Quaternion.Identity;
            if (cos <= -1 + Epsilon)
            {
                OrthonormalBasis(f, out var ortho, out _);
                return Quaternion.CreateFromAxisAngle(ortho, System.MathF.PI);
            }
            var axis = Vector3.Cross(f, t);
            var s = System.MathF.Sqrt((1 + cos) * 2);
            var invs = 1 / s;
            return new Quaternion(axis * invs, s * 0.5f);
        }
    }
}
