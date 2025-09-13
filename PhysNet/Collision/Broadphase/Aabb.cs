using System.Numerics;

namespace PhysNet.Collision.Broadphase
{
    public struct Aabb
    {
        public Vector3 Min;
        public Vector3 Max;

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public static Aabb FromCenterExtents(Vector3 center, Vector3 extents)
        {
            return new Aabb(center - extents, center + extents);
        }

        public Vector3 Extents => (Max - Min) * 0.5f;
        public Vector3 Center => (Max + Min) * 0.5f;

        public void Expand(float amount)
        {
            var v = new Vector3(amount);
            Min -= v;
            Max += v;
        }

        public void Encapsulate(Aabb other)
        {
            Min = Vector3.Min(Min, other.Min);
            Max = Vector3.Max(Max, other.Max);
        }

        public bool Overlaps(in Aabb other)
        {
            return !(Max.X < other.Min.X || Min.X > other.Max.X ||
                     Max.Y < other.Min.Y || Min.Y > other.Max.Y ||
                     Max.Z < other.Min.Z || Min.Z > other.Max.Z);
        }

        public float SurfaceArea()
        {
            var d = Max - Min;
            return 2f * (d.X * d.Y + d.Y * d.Z + d.Z * d.X);
        }

        public float EncapsulatedSurfaceArea(in Aabb other)
        {
            var min = Vector3.Min(Min, other.Min);
            var max = Vector3.Max(Max, other.Max);
            var d = max - min;
            return 2f * (d.X * d.Y + d.Y * d.Z + d.Z * d.X);
        }
    }
}
