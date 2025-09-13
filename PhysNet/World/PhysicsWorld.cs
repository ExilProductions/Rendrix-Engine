using System;
using System.Collections.Generic;
using System.Numerics;
using PhysNet.Collision.Broadphase;
using PhysNet.Collision.Narrowphase;
using PhysNet.Collision.Shapes;
using PhysNet.Dynamics;
using PhysNet.Math;

namespace PhysNet.World
{
    public sealed class PhysicsWorld
    {
        private readonly List<RigidBody> _bodies = new();
        private readonly DynamicAabbTree<int> _tree = new();
        private readonly Dictionary<int, int> _bodyToNode = new();
        private readonly List<(int a, int b)> _pairs = new();

        public Vector3 Gravity = new(0, -9.81f, 0);
        public int SolverIterations = 10;
        public SolverSettings SolverSettings { get; } = new();

        public int AddBody(RigidBody body)
        {
            int id = _bodies.Count;
            _bodies.Add(body);
            var aabb = ComputeAabb(body);
            int node = _tree.Insert(aabb, id);
            _bodyToNode[id] = node;
            return id;
        }

        public void RemoveBody(int id)
        {
            if (!_bodyToNode.TryGetValue(id, out var node)) return;
            _tree.Remove(node);
            _bodyToNode.Remove(id);
            _bodies[id] = null!; // keep indices stable
        }

        private static Aabb ComputeAabb(RigidBody body)
        {
            body.Shape.GetLocalBounds(out var min, out var max);
            // conservative transform bounds by rotating extents
            var rot = Matrix4x4.CreateFromQuaternion(body.Transform.Rotation);
            var r = new Vector3(
                System.MathF.Abs(rot.M11) * max.X + System.MathF.Abs(rot.M12) * max.Y + System.MathF.Abs(rot.M13) * max.Z,
                System.MathF.Abs(rot.M21) * max.X + System.MathF.Abs(rot.M22) * max.Y + System.MathF.Abs(rot.M23) * max.Z,
                System.MathF.Abs(rot.M31) * max.X + System.MathF.Abs(rot.M32) * max.Y + System.MathF.Abs(rot.M33) * max.Z);
            var center = body.Transform.Position;
            return Aabb.FromCenterExtents(center, r);
        }

        public void Step(float dt)
        {
            // Integrate forces
            foreach (var b in _bodies)
            {
                if (b == null) continue;
                b.IntegrateVelocities(dt, Gravity);
            }

            // Broadphase update
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i]; if (b == null) continue;
                var aabb = ComputeAabb(b);
                _tree.SetLeafBox(_bodyToNode[i], aabb);
            }
            BuildPairs();

            // Collide and build constraints
            var manifolds = new List<(RigidBody, RigidBody, ContactManifold)>();
            foreach (var (ia, ib) in _pairs)
            {
                var a = _bodies[ia]; var b = _bodies[ib];
                if (a == null || b == null) continue;
                if (a.MotionType == MotionType.Static && b.MotionType == MotionType.Static) continue;
                // collision filtering
                if (((a.Group & b.Mask) == 0) || ((b.Group & a.Mask) == 0)) continue;
                if (CollidePrimitives.Collide(a.Shape, a.Transform, b.Shape, b.Transform, out var m))
                {
                    manifolds.Add((a, b, m));
                }
            }

            var solver = new ContactSolver();
            solver.Configure(SolverSettings);
            var arr = manifolds.ToArray();
            solver.Build(arr, 1f / dt);
            solver.WarmStart();
            solver.Solve(SolverIterations > 0 ? SolverIterations : SolverSettings.Iterations);

            // Integrate positions
            foreach (var b in _bodies)
            {
                if (b == null) continue;
                b.IntegrateTransform(dt);
            }
        }

        private void BuildPairs()
        {
            _pairs.Clear();
            var tmp = new List<int>(32);
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i]; if (b == null) continue;
                _tree.Query(ComputeAabb(b), tmp);
                foreach (var leaf in tmp)
                {
                    var (box, item) = _tree.GetLeaf(leaf);
                    if (item <= i) continue;
                    _pairs.Add((i, item));
                }
                tmp.Clear();
            }
        }
    }
}
