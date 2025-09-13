using System.Numerics;
using PhysNet.Collision.Shapes;
using PhysNet.Math;

namespace PhysNet.Dynamics
{
    public enum MotionType { Static, Kinematic, Dynamic }

    [System.Flags]
    public enum CollisionMask : uint
    {
        None = 0,
        Default = 1u << 0,
        Static = 1u << 1,
        Dynamic = 1u << 2,
        All = 0xFFFFFFFF
    }

    public sealed class RigidBody
    {
        public Shape Shape { get; }
        public Transform Transform;
        public float Mass { get; private set; }
        public Matrix4x4 InertiaLocal { get; private set; }
        public Matrix4x4 InertiaWorldInv { get; private set; }
        public Vector3 CenterOfMassLocal { get; private set; }

        public MotionType MotionType { get; set; } = MotionType.Dynamic;

        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public float LinearDamping = 0.01f;
        public float AngularDamping = 0.05f;

        public float Restitution => Shape.Restitution;
        public float Friction => Shape.Friction;

        public bool IsAwake { get; set; } = true;

        // Collision filtering
        public CollisionMask Group = CollisionMask.Default;
        public CollisionMask Mask = CollisionMask.All;

        public RigidBody(Shape shape, float mass, Transform transform)
        {
            Shape = shape;
            SetMass(mass);
            Transform = transform;
            UpdateInertiaWorldInv();
            if (MotionType == MotionType.Static)
            {
                Group = CollisionMask.Static;
            }
            else
            {
                Group = CollisionMask.Dynamic;
            }
        }

        public void SetMass(float mass)
        {
            if (MotionType != MotionType.Dynamic || mass <= 0)
            {
                Mass = 0;
                InertiaLocal = Matrix4x4.Identity;
                CenterOfMassLocal = Vector3.Zero;
            }
            else
            {
                Mass = mass;
                Shape.ComputeInertia(mass, out var inertia, out var com);
                InertiaLocal = inertia;
                CenterOfMassLocal = com;
            }
        }

        public void UpdateInertiaWorldInv()
        {
            // For diagonal inertia matrix we can invert element-wise; here assume diagonal for primitive shapes
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(Transform.Rotation);
            Matrix4x4 rotT = Matrix4x4.Transpose(rot);
            // Build world inertia: R * I_local * R^T; then invert assuming symmetric positive-definite
            var iw = rot * InertiaLocal * rotT;
            // Invert 3x3 block numerically
            float a = iw.M11, b = iw.M12, c = iw.M13;
            float d = iw.M21, e = iw.M22, f = iw.M23;
            float g = iw.M31, h = iw.M32, k = iw.M33;
            float det = a * (e * k - f * h) - b * (d * k - f * g) + c * (d * h - e * g);
            if (System.MathF.Abs(det) < 1e-8f) det = 1e-8f;
            float invDet = 1f / det;
            var m11 = (e * k - f * h) * invDet;
            var m12 = (c * h - b * k) * invDet;
            var m13 = (b * f - c * e) * invDet;
            var m21 = (f * g - d * k) * invDet;
            var m22 = (a * k - c * g) * invDet;
            var m23 = (c * d - a * f) * invDet;
            var m31 = (d * h - e * g) * invDet;
            var m32 = (b * g - a * h) * invDet;
            var m33 = (a * e - b * d) * invDet;
            InertiaWorldInv = new Matrix4x4(
                m11, m12, m13, 0,
                m21, m22, m23, 0,
                m31, m32, m33, 0,
                0, 0, 0, 1);
        }

        public bool HasFiniteMass => Mass > 0;
        public float InvMass => HasFiniteMass ? 1f / Mass : 0f;

        public void ApplyImpulse(Vector3 impulse, Vector3 contactPoint)
        {
            if (MotionType != MotionType.Dynamic) return;
            LinearVelocity += impulse * InvMass;
            var r = contactPoint - (Transform.Position + Vector3.Transform(CenterOfMassLocal, Transform.Rotation));
            AngularVelocity += Vector3.Transform(Vector3.Cross(r, impulse), InertiaWorldInv);
            IsAwake = true;
        }

        public void IntegrateVelocities(float dt, Vector3 gravity)
        {
            if (MotionType != MotionType.Dynamic || !IsAwake) return;
            LinearVelocity += gravity * dt;
            LinearVelocity *= System.MathF.Max(0f, 1f - LinearDamping * dt);
            AngularVelocity *= System.MathF.Max(0f, 1f - AngularDamping * dt);
        }

        public void IntegrateTransform(float dt)
        {
            if (MotionType == MotionType.Static) return;
            Transform.Position += LinearVelocity * dt;
            var ang = AngularVelocity;
            float angle = ang.Length();
            if (angle > 1e-6f)
            {
                var axis = ang / angle;
                var dq = Quaternion.CreateFromAxisAngle(axis, angle * dt);
                Transform.Rotation = Quaternion.Normalize(dq * Transform.Rotation);
            }
            UpdateInertiaWorldInv();
        }
    }
}
