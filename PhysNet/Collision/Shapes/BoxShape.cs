using System.Numerics;

namespace PhysNet.Collision.Shapes
{
    public sealed class BoxShape : Shape
    {
        public Vector3 HalfExtents { get; }
        public BoxShape(Vector3 halfExtents)
        {
            HalfExtents = new Vector3(System.MathF.Max(halfExtents.X, 1e-4f), System.MathF.Max(halfExtents.Y, 1e-4f), System.MathF.Max(halfExtents.Z, 1e-4f));
        }

        public override ShapeType Type => ShapeType.Box;

        public override Vector3 Support(Vector3 direction)
        {
            return new Vector3(
                direction.X >= 0 ? HalfExtents.X : -HalfExtents.X,
                direction.Y >= 0 ? HalfExtents.Y : -HalfExtents.Y,
                direction.Z >= 0 ? HalfExtents.Z : -HalfExtents.Z
            );
        }

        public override void ComputeInertia(float mass, out Matrix4x4 inertiaLocal, out Vector3 comLocal)
        {
            var size = HalfExtents * 2f;
            var x2 = size.X * size.X;
            var y2 = size.Y * size.Y;
            var z2 = size.Z * size.Z;
            float ix = (1f / 12f) * mass * (y2 + z2);
            float iy = (1f / 12f) * mass * (x2 + z2);
            float iz = (1f / 12f) * mass * (x2 + y2);
            inertiaLocal = new Matrix4x4(
                ix, 0, 0, 0,
                0, iy, 0, 0,
                0, 0, iz, 0,
                0, 0, 0, 1);
            comLocal = Vector3.Zero;
        }

        public override Vector3 GetLocalBounds(out Vector3 min, out Vector3 max)
        {
            min = -HalfExtents;
            max = HalfExtents;
            return max - min;
        }
    }
}
