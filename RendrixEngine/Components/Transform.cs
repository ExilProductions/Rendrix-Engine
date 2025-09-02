using System.Numerics;
using System;

namespace RendrixEngine
{
    public class Transform
    {
        public Vector3D Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3D Scale { get; set; }
        public Transform? Parent { get; set; }

        public Transform()
        {
            Position = new Vector3D(0, 0, 0);
            Rotation = Quaternion.Identity;
            Scale = new Vector3D(1, 1, 1);
        }

        /// <summary>
        /// Local transformation matrix (Scale * Rotation * Translation)
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get
            {
                Matrix4x4 translation = Matrix4x4.CreateTranslation(Position.ToVector3());
                Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(Rotation);
                Matrix4x4 scale = Matrix4x4.CreateScale(Scale.ToVector3());
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
        public Vector3D Forward
        {
            get
            {
                Vector3 forward = Vector3.Transform(Vector3.UnitZ, Rotation);
                return new Vector3D(forward.X, forward.Y, forward.Z).Normalized;
            }
        }

        /// <summary>
        /// Normalized right direction vector
        /// </summary>
        public Vector3D Right
        {
            get
            {
                Vector3 right = Vector3.Transform(Vector3.UnitX, Rotation);
                return new Vector3D(right.X, right.Y, right.Z).Normalized;
            }
        }

        /// <summary>
        /// Normalized up direction vector
        /// </summary>
        public Vector3D Up
        {
            get
            {
                Vector3 up = Vector3.Transform(Vector3.UnitY, Rotation);
                return new Vector3D(up.X, up.Y, up.Z).Normalized;
            }
        }

        /// <summary>
        /// Rotate the transform around an axis by an angle (radians)
        /// </summary>
        public void Rotate(Vector3D axis, float angle)
        {
            if (axis.Normalized == new Vector3D(0, 0, 0))
                throw new ArgumentException("Rotation axis cannot be zero.", nameof(axis));
            Quaternion rot = Quaternion.CreateFromAxisAngle(axis.ToVector3(), angle);
            Rotation = Quaternion.Normalize(rot * Rotation);
        }

        /// <summary>
        /// Translate the transform by an offset vector
        /// </summary>
        public void Translate(Vector3D offset)
        {
            Position += offset;
        }

        /// <summary>
        /// Set absolute scale of the transform
        /// </summary>
        public void SetScale(Vector3D newScale)
        {
            if (newScale.X <= 0 || newScale.Y <= 0 || newScale.Z <= 0)
                throw new ArgumentException("Scale factors must be positive.", nameof(newScale));
            Scale = newScale;
        }
    }
}
