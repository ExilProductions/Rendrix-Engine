using System.Numerics;

namespace PhysNet.Collision.Narrowphase
{
    public struct ContactPoint
    {
        public Vector3 Position;
        public Vector3 Normal;
        public float Penetration;
        public float NormalImpulse;
        public float TangentImpulse1;
        public float TangentImpulse2;
    }

    public struct ContactManifold
    {
        public ContactPoint[] Points;
        public int Count;
        public Vector3 Normal;

        public void Initialize(int maxPoints)
        {
            Points = new ContactPoint[maxPoints];
            Count = 0;
            Normal = Vector3.UnitY;
        }

        public void Add(ContactPoint cp)
        {
            if (Count < Points.Length)
            {
                Points[Count++] = cp;
            }
        }
    }
}
