using System.Numerics;
using PhysNet.Collision.Narrowphase;
using PhysNet.Math;

namespace PhysNet.Dynamics
{
    internal sealed class ContactSolver
    {
        private readonly struct BodyRef
        {
            public readonly RigidBody A;
            public readonly RigidBody B;
            public BodyRef(RigidBody a, RigidBody b) { A = a; B = b; }
        }

        private struct Constraint
        {
            public BodyRef Bodies;
            public Vector3 Ra, Rb;
            public Vector3 Normal, T1, T2;
            public float MassNormal, MassT1, MassT2;
            public float Restitution;
            public float Friction;
            public float Bias;
            public float AccNormal, AccT1, AccT2;
        }

        private Constraint[] _constraints = System.Array.Empty<Constraint>();
        private int _count;
        private SolverSettings _settings = new();

        public void Configure(SolverSettings settings)
        {
            _settings = settings ?? new SolverSettings();
        }

        public void Build((RigidBody a, RigidBody b, ContactManifold manifold)[] contacts, float invDt)
        {
            _count = 0;
            int needed = 0;
            for (int i = 0; i < contacts.Length; i++) needed += contacts[i].manifold.Count;
            if (_constraints.Length < needed) _constraints = new Constraint[needed];
            for (int i = 0; i < contacts.Length; i++)
            {
                var (a, b, m) = contacts[i];
                for (int j = 0; j < m.Count; j++)
                {
                    var cp = m.Points[j];
                    var n = cp.Normal;
                    MathUtil.OrthonormalBasis(n, out var t1, out var t2);
                    var ra = cp.Position - a.Transform.Position;
                    var rb = cp.Position - b.Transform.Position;
                    var ka = Vector3.Cross(ra, n);
                    var kb = Vector3.Cross(rb, n);
                    var invMa = a.InvMass; var invMb = b.InvMass;

                    float kn = invMa + invMb + Vector3.Dot(Vector3.Transform(ka, a.InertiaWorldInv), ka) + Vector3.Dot(Vector3.Transform(kb, b.InertiaWorldInv), kb);
                    ka = Vector3.Cross(ra, t1); kb = Vector3.Cross(rb, t1);
                    float kt1 = invMa + invMb + Vector3.Dot(Vector3.Transform(ka, a.InertiaWorldInv), ka) + Vector3.Dot(Vector3.Transform(kb, b.InertiaWorldInv), kb);
                    ka = Vector3.Cross(ra, t2); kb = Vector3.Cross(rb, t2);
                    float kt2 = invMa + invMb + Vector3.Dot(Vector3.Transform(ka, a.InertiaWorldInv), ka) + Vector3.Dot(Vector3.Transform(kb, b.InertiaWorldInv), kb);

                    float restitution = Combine(a.Restitution, b.Restitution, _settings.RestitutionCombine);
                    float friction = Combine(a.Friction, b.Friction, _settings.FrictionCombine);

                    float bias = System.MathF.Max(0, cp.Penetration - _settings.PenetrationSlop) * _settings.Baumgarte * invDt;

                    _constraints[_count++] = new Constraint
                    {
                        Bodies = new BodyRef(a, b),
                        Ra = ra,
                        Rb = rb,
                        Normal = n,
                        T1 = t1,
                        T2 = t2,
                        MassNormal = kn > 0 ? 1f / kn : 0,
                        MassT1 = kt1 > 0 ? 1f / kt1 : 0,
                        MassT2 = kt2 > 0 ? 1f / kt2 : 0,
                        Restitution = restitution,
                        Friction = friction,
                        Bias = bias,
                        AccNormal = cp.NormalImpulse,
                        AccT1 = cp.TangentImpulse1,
                        AccT2 = cp.TangentImpulse2
                    };
                }
            }
        }

        public void WarmStart()
        {
            for (int i = 0; i < _count; i++)
            {
                ref var c = ref _constraints[i];
                var impulse = c.Normal * c.AccNormal + c.T1 * c.AccT1 + c.T2 * c.AccT2;
                c.Bodies.A.ApplyImpulse(-impulse, c.Bodies.A.Transform.Position + c.Ra);
                c.Bodies.B.ApplyImpulse(impulse, c.Bodies.B.Transform.Position + c.Rb);
            }
        }

        public void Solve(int iterations)
        {
            int iters = iterations > 0 ? iterations : _settings.Iterations;
            for (int it = 0; it < iters; it++)
            {
                for (int i = 0; i < _count; i++)
                {
                    ref var c = ref _constraints[i];
                    var a = c.Bodies.A; var b = c.Bodies.B;
                    var va = a.LinearVelocity + Vector3.Cross(a.AngularVelocity, c.Ra);
                    var vb = b.LinearVelocity + Vector3.Cross(b.AngularVelocity, c.Rb);
                    var rv = vb - va;

                    // restitution on first iteration only for stability
                    float vn = Vector3.Dot(rv, c.Normal);
                    float bounce = (it == 0 && vn < -1f) ? -c.Restitution * vn : 0f;
                    // Use rhs = -vn + bias + bounce so penetration bias generates positive impulse
                    float lambdaN = c.MassNormal * (-vn + c.Bias + bounce);
                    float oldN = c.AccNormal;
                    c.AccNormal = System.MathF.Max(0, oldN + lambdaN);
                    lambdaN = c.AccNormal - oldN;
                    var pN = c.Normal * lambdaN;
                    a.ApplyImpulse(-pN, a.Transform.Position + c.Ra);
                    b.ApplyImpulse(pN, b.Transform.Position + c.Rb);

                    // recompute rv for friction
                    va = a.LinearVelocity + Vector3.Cross(a.AngularVelocity, c.Ra);
                    vb = b.LinearVelocity + Vector3.Cross(b.AngularVelocity, c.Rb);
                    rv = vb - va;

                    float vt1 = Vector3.Dot(rv, c.T1);
                    float vt2 = Vector3.Dot(rv, c.T2);
                    float maxF = c.Friction * c.AccNormal;

                    float lambdaT1 = -c.MassT1 * vt1;
                    float lambdaT2 = -c.MassT2 * vt2;

                    float oldT1 = c.AccT1;
                    float oldT2 = c.AccT2;
                    c.AccT1 = Clamp(oldT1 + lambdaT1, -maxF, maxF);
                    c.AccT2 = Clamp(oldT2 + lambdaT2, -maxF, maxF);

                    var dP1 = c.T1 * (c.AccT1 - oldT1);
                    var dP2 = c.T2 * (c.AccT2 - oldT2);
                    a.ApplyImpulse(-(dP1 + dP2), a.Transform.Position + c.Ra);
                    b.ApplyImpulse(dP1 + dP2, b.Transform.Position + c.Rb);
                }
            }
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        private static float Combine(float a, float b, CombineMode mode) => mode switch
        {
            CombineMode.Max => System.MathF.Max(a, b),
            CombineMode.Min => System.MathF.Min(a, b),
            CombineMode.Multiply => System.MathF.Sqrt(a * b),
            CombineMode.Average => (a + b) * 0.5f,
            _ => (a + b) * 0.5f
        };
    }
}
