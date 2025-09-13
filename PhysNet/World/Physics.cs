using System.Numerics;
using PhysNet.Collision.Shapes;
using PhysNet.Dynamics;
using PhysNet.Math;

namespace PhysNet
{
    public static class Physics
    {
        public static RigidBody CreateDynamicSphere(float radius, float mass, Vector3 position)
        {
            var shape = new SphereShape(radius);
            return new RigidBody(shape, mass, new Transform(position, Quaternion.Identity));
        }

        public static RigidBody CreateStaticBox(Vector3 halfExtents, Vector3 position)
        {
            var shape = new BoxShape(halfExtents);
            var rb = new RigidBody(shape, 0, new Transform(position, Quaternion.Identity))
            {
                MotionType = MotionType.Static
            };
            return rb;
        }
    }
}
