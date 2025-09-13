using System.Numerics;

namespace PhysNet.Collision.Shapes
{
    public sealed class CylinderShape : Shape
    {
        public float Radius { get; }
        public float HalfHeight { get; }

        public CylinderShape(float radius, float halfHeight)
        {
            Radius = System.MathF.Max(radius, 1e-4f);
            HalfHeight = System.MathF.Max(halfHeight, 1e-4f);
        }

        public override ShapeType Type => ShapeType.ConvexHull; // treat as convex primitive in generic code

        public override Vector3 Support(Vector3 direction)
        {
            var d = direction;
            var radial = new Vector2(d.X, d.Z);
            float len = radial.Length();
            Vector2 cap = len > 1e-6f ? radial / len * Radius : new Vector2(Radius, 0);
            float y = d.Y >= 0 ? HalfHeight : -HalfHeight;
            return new Vector3(cap.X, y, cap.Y);
        }

        public override void ComputeInertia(float mass, out Matrix4x4 inertiaLocal, out Vector3 comLocal)
        {
            float r2 = Radius * Radius;
            float h = HalfHeight * 2f;
            float ix = 0.25f * mass * r2 + (1f / 12f) * mass * h * h; // about x and z
            float iy = 0.5f * mass * r2; // about y
            inertiaLocal = new Matrix4x4(
                ix, 0, 0, 0,
                0, iy, 0, 0,
                0, 0, ix, 0,
                0, 0, 0, 1);
            comLocal = Vector3.Zero;
        }

        public override Vector3 GetLocalBounds(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(-Radius, -HalfHeight, -Radius);
            max = new Vector3(Radius, HalfHeight, Radius);
            return max - min;
        }
    }
}
