using System.Numerics;
using PhysNet.Math;

namespace PhysNet.Collision.Shapes
{
    public enum ShapeType
    {
        Sphere,
        Box,
        Capsule,
        Cylinder,
        ConvexHull
    }

    public abstract class Shape
    {
        public abstract ShapeType Type { get; }
        public float Density { get; set; } = 1000f; // kg/m^3
        public float Restitution { get; set; } = 0.2f;
        public float Friction { get; set; } = 0.5f;

        public abstract Vector3 Support(Vector3 direction);
        public abstract void ComputeInertia(float mass, out Matrix4x4 inertiaLocal, out Vector3 comLocal);
        public abstract Vector3 GetLocalBounds(out Vector3 min, out Vector3 max);
    }
}
