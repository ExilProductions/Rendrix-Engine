using System.Numerics;
using PhysNet.Collision.Shapes;
using PhysNet.Math;

namespace PhysNet.Collision.Narrowphase
{
    internal static class CollidePrimitives
    {
        public static bool Collide<TA, TB>(Shape a, in TA ta, Shape b, in TB tb, out ContactManifold manifold)
            where TA : struct, ITransform
            where TB : struct, ITransform
        {
            manifold = default; manifold.Initialize(4);

            // Special cases for spheres and boxes for performance; fallback to GJK/EPA otherwise
            if (a is SphereShape sa && b is SphereShape sb)
            {
                var pa = ta.Position; var pb = tb.Position;
                var r = sa.Radius + sb.Radius;
                var d = pb - pa; var dist2 = d.LengthSquared();
                if (dist2 > r * r) return false;
                var dist = System.MathF.Sqrt(System.MathF.Max(dist2, 1e-8f));
                var n = dist > 1e-6f ? d / dist : Vector3.UnitX;
                var p = pa + n * sa.Radius;
                manifold.Normal = n;
                manifold.Add(new ContactPoint { Position = p, Normal = n, Penetration = r - dist });
                return true;
            }

            if (GjkEpa.Intersect(a, ta, b, tb, out var normal, out var depth))
            {
                // Single point manifold approximation at center along normal
                var contactPoint = (ta.Position + tb.Position) * 0.5f - normal * (depth * 0.5f);
                manifold.Normal = normal;
                manifold.Add(new ContactPoint { Position = contactPoint, Normal = normal, Penetration = depth });
                return true;
            }

            return false;
        }

        // Backward-compatible non-generic overload
        public static bool Collide(Shape a, in Transform ta, Shape b, in Transform tb, out ContactManifold manifold)
            => Collide<Transform, Transform>(a, in ta, b, in tb, out manifold);
    }
}
