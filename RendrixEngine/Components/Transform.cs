using System.Numerics;
using PhysNet.Math;

namespace RendrixEngine
{
    public class Transform : ITransform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Transform? Parent { get; set; }

        public Transform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        /// <summary>
        /// Local transformation matrix (Scale * Rotation * Translation)
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get
            {
                Matrix4x4 translation = Matrix4x4.CreateTranslation(Position);
                Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(Rotation);
                Matrix4x4 scale = Matrix4x4.CreateScale(Scale);
                return scale * rotation * translation;
            }
        }

        /// <summary>
        /// World transformation matrix including parent transforms
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (Parent == null)
                    return LocalMatrix;
                return LocalMatrix * Parent.WorldMatrix;
            }
        }

        /// <summary>
        /// Normalized forward direction vector
        /// </summary>
        public Vector3 Forward
        {
            get
            {
                Vector3 forward = Vector3.Transform(Vector3.UnitZ, Rotation);
                return Vector3.Normalize(forward);
            }
        }

        /// <summary>
        /// Normalized right direction vector
        /// </summary>
        public Vector3 Right
        {
            get
            {
                Vector3 right = Vector3.Transform(Vector3.UnitX, Rotation);
                return Vector3.Normalize(right);
            }
        }

        /// <summary>
        /// Normalized up direction vector
        /// </summary>
        public Vector3 Up
        {
            get
            {
                Vector3 up = Vector3.Transform(Vector3.UnitY, Rotation);
                return Vector3.Normalize(up);
            }
        }

        /// <summary>
        /// Rotate the transform around an axis by an angle (radians)
        /// </summary>
        public void Rotate(Vector3 axis, float angle)
        {
            if (axis == Vector3.Zero)
                throw new ArgumentException("Rotation axis cannot be zero.", nameof(axis));
            Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
            Rotation = Quaternion.Normalize(rot * Rotation);
        }

        /// <summary>
        /// Translate the transform by an offset vector
        /// </summary>
        public void Translate(Vector3 offset)
        {
            Position += offset;
        }

        /// <summary>
        /// Set absolute scale of the transform
        /// </summary>
        public void SetScale(Vector3 newScale)
        {
            if (newScale.X <= 0 || newScale.Y <= 0 || newScale.Z <= 0)
                throw new ArgumentException("Scale factors must be positive.", nameof(newScale));
            Scale = newScale;
        }

        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return Vector3.Transform(localPoint, Rotation) + Position;
        }

        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return Vector3.Transform(localDirection, Rotation);
        }

        public Vector3 InverseTransformPoint(Vector3 worldPoint)
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
