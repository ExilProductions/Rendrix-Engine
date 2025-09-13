using System.Numerics;

namespace PhysNet.Math
{
    /// <summary>
    /// Abstraction over a spatial transform (position + rotation) used by the physics pipeline.
    /// Implement this interface to provide a custom transform type.
    /// </summary>
    public interface ITransform
    {
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }

        Vector3 TransformPoint(Vector3 localPoint);
        Vector3 TransformDirection(Vector3 localDirection);
        Vector3 InverseTransformPoint(Vector3 worldPoint);
        Vector3 InverseTransformDirection(Vector3 worldDirection);
        Matrix4x4 ToMatrix();
    }

    /// <summary>
    /// Default transform implementation. (Previously a standalone struct; now implements ITransform.)
    /// </summary>
    public struct Transform : ITransform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public Transform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = Quaternion.Normalize(rotation);
        }

        public static Transform Identity => new(Vector3.Zero, Quaternion.Identity);

        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return Vector3.Transform(localPoint, Rotation) + Position;
        }

        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return Vector3.Transform(localDirection, Rotation);
        }

        public Vector3 InverseTransformPoint(Vector3 worldPoint
        )
        {
            var invRot = Quaternion.Conjugate(Rotation);
            return Vector3.Transform(worldPoint - Position, invRot);
        }

        public Vector3 InverseTransformDirection(Vector3 worldDirection)
        {
            var invRot = Quaternion.Conjugate(Rotation);
            return Vector3.Transform(worldDirection, invRot);
        }

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
        }
    }
}
