using System;
using System.Numerics;
using PhysNet.Collision.Shapes;
using PhysNet.Math;

namespace PhysNet.Collision.Narrowphase
{
    internal static class GjkEpa
    {
        private struct Simplex
        {
            public Vector3 A, B, C, D;
            public int Count;
        }

        // Generic support function operating on any ITransform implementor (struct to avoid boxing)
        private static Vector3 Support<TA, TB>(Shape a, in TA ta, Shape b, in TB tb, Vector3 dir)
            where TA : struct, ITransform
            where TB : struct, ITransform
        {
            var sa = ta.TransformPoint(a.Support(ta.InverseTransformDirection(dir)));
            var sb = tb.TransformPoint(b.Support(tb.InverseTransformDirection(-dir)));
            return sa - sb;
        }

        public static bool Intersect<TA, TB>(Shape a, in TA ta, Shape b, in TB tb, out Vector3 normal, out float depth)
            where TA : struct, ITransform
            where TB : struct, ITransform
        {
            normal = Vector3.UnitY; depth = 0;
            Vector3 dir = ta.Position - tb.Position;
            if (dir.LengthSquared() < 1e-8f) dir = new Vector3(1, 0, 0);

            Simplex s = default; s.Count = 0;
            Vector3 p = Support(a, ta, b, tb, dir);
            s.A = p; s.Count = 1;
            dir = -p;

            for (int iter = 0; iter < 32; iter++)
            {
                Vector3 newPoint = Support(a, ta, b, tb, dir);
                if (Vector3.Dot(newPoint, dir) < 0)
                {
                    return false; // no intersection
                }

                if (s.Count == 1)
                {
                    s.B = newPoint; s.Count = 2;
                    dir = TripleCross(s.B - s.A, -s.A);
                }
                else if (s.Count == 2)
                {
                    s.C = newPoint; s.Count = 3;
                    if (!HandleTriangle(ref s, ref dir)) return false;
                }
                else
                {
                    s.D = newPoint; s.Count = 4;
                    if (HandleTetrahedron(ref s, ref dir))
                    {
                        // Overlapping, proceed to EPA for normal and depth
                        return EPA(a, ta, b, tb, ref s, out normal, out depth);
                    }
                }
            }
            return false;
        }

        // Backward-compatible overload for existing Transform usages
        public static bool Intersect(Shape a, in Transform ta, Shape b, in Transform tb, out Vector3 normal, out float depth)
            => Intersect<Transform, Transform>(a, in ta, b, in tb, out normal, out depth);

        private static Vector3 TripleCross(Vector3 a, Vector3 b) => Vector3.Cross(Vector3.Cross(a, b), a);

        private static bool HandleTriangle(ref Simplex s, ref Vector3 dir)
        {
            var a = s.C; var b = s.B; var c = s.A;
            var ab = b - a; var ac = c - a; var ao = -a;
            var abc = Vector3.Cross(ab, ac);

            if (Vector3.Dot(Vector3.Cross(abc, ac), ao) > 0)
            {
                s.B = a; s.A = c; s.Count = 2;
                dir = TripleCross(ac, ao);
                return true;
            }
            if (Vector3.Dot(Vector3.Cross(ab, abc), ao) > 0)
            {
                s.C = a; s.A = b; s.Count = 2;
                dir = TripleCross(ab, ao);
                return true;
            }

            if (Vector3.Dot(abc, ao) > 0)
            {
                dir = abc;
            }
            else
            {
                // swap winding
                var t = s.A; s.A = s.B; s.B = t;
                dir = -abc;
            }
            return true;
        }

        private static bool HandleTetrahedron(ref Simplex s, ref Vector3 dir)
        {
            var a = s.D; var b = s.C; var c = s.B; var d = s.A;
            var ao = -a;
            var ab = b - a; var ac = c - a; var ad = d - a;
            var abc = Vector3.Cross(ab, ac);
            var acd = Vector3.Cross(ac, ad);
            var adb = Vector3.Cross(ad, ab);

            if (Vector3.Dot(abc, ao) > 0)
            {
                s.B = c; s.C = b; s.D = default; s.Count = 3;
                dir = abc; return true;
            }
            if (Vector3.Dot(acd, ao) > 0)
            {
                s.B = d; s.C = c; s.D = default; s.Count = 3;
                dir = acd; return true;
            }
            if (Vector3.Dot(adb, ao) > 0)
            {
                s.B = b; s.C = d; s.D = default; s.Count = 3;
                dir = adb; return true;
            }
            return true; // origin inside tetrahedron
        }

        private static bool EPA<TA, TB>(Shape a, in TA ta, Shape b, in TB tb, ref Simplex s, out Vector3 normal, out float depth)
            where TA : struct, ITransform
            where TB : struct, ITransform
        {
            normal = Vector3.UnitY; depth = 0;
            var faces = new System.Collections.Generic.List<(Vector3 a, Vector3 b, Vector3 c, Vector3 n, float d)>();
            void AddFace(Vector3 va, Vector3 vb, Vector3 vc)
            {
                var n = Vector3.Normalize(Vector3.Cross(vb - va, vc - va));
                float d = Vector3.Dot(n, va);
                if (d < 0) { n = -n; d = -d; var t = vb; vb = vc; vc = t; }
                faces.Add((va, vb, vc, n, d));
            }

            AddFace(s.A, s.B, s.C);
            AddFace(s.A, s.C, s.D);
            AddFace(s.A, s.D, s.B);
            AddFace(s.B, s.D, s.C);

            for (int iter = 0; iter < 64; iter++)
            {
                // find closest face to origin
                int closest = 0; float minD = faces[0].d;
                for (int i = 1; i < faces.Count; i++) if (faces[i].d < minD) { minD = faces[i].d; closest = i; }
                var face = faces[closest];

                var p = Support(a, ta, b, tb, face.n);
                float dist = Vector3.Dot(p, face.n);
                float sep = dist - face.d;
                if (sep <= 1e-4f)
                {
                    normal = face.n; depth = dist;
                    return true;
                }

                // Add new point and rebuild horizon
                var toRemove = new System.Collections.Generic.HashSet<int>();
                var edges = new System.Collections.Generic.List<(Vector3, Vector3)>();
                for (int i = 0; i < faces.Count; i++)
                {
                    var f = faces[i];
                    if (Vector3.Dot(f.n, p - f.a) > 0)
                    {
                        toRemove.Add(i);
                        AddEdge(f.a, f.b);
                        AddEdge(f.b, f.c);
                        AddEdge(f.c, f.a);
                    }
                }
                void AddEdge(Vector3 u, Vector3 v)
                {
                    for (int e = 0; e < edges.Count; e++)
                    {
                        var (eu, ev) = edges[e];
                        if ((eu == v && ev == u)) { edges.RemoveAt(e); return; }
                    }
                    edges.Add((u, v));
                }
                var newFaces = new System.Collections.Generic.List<(Vector3, Vector3, Vector3)>();
                for (int i = faces.Count - 1; i >= 0; i--) if (toRemove.Contains(i)) faces.RemoveAt(i);
                foreach (var (u, v) in edges) newFaces.Add((u, v, p));
                foreach (var (u, v, w) in newFaces) AddFace(u, v, w);
            }
            return false;
        }
    }
}
