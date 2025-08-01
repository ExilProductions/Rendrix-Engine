using System;
using System.Collections.Generic;
using System.Numerics;
using RendrixEngine.Mathematics;

namespace RendrixEngine.Components
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

        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (Parent == null)
                    return LocalMatrix;
                return LocalMatrix * Parent.WorldMatrix;
            }
        }

        public void Rotate(Vector3D axis, float angle)
        {
            if (axis.Normalized == new Vector3D(0, 0, 0))
                throw new ArgumentException("Rotation axis cannot be zero.", nameof(axis));
            Quaternion rot = Quaternion.CreateFromAxisAngle(axis.ToVector3(), angle);
            Rotation = Quaternion.Normalize(rot * Rotation);
        }

        public void Translate(Vector3D offset)
        {
            Position += offset;
        }

        public void SetScale(Vector3D newScale)
        {
            if (newScale.X <= 0 || newScale.Y <= 0 || newScale.Z <= 0)
                throw new ArgumentException("Scale factors must be positive.", nameof(newScale));
            Scale = newScale;
        }
    }
}