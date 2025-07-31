using System;
using System.Collections.Generic;
using System.Numerics;
using RendrixEngine.Mathematics;

namespace RendrixEngine.Models
{
    /// <summary>
    /// Represents a transformation for a 3D object, including position, rotation, and scale.
    /// Supports hierarchical transformations via parent-child relationships.
    /// </summary>
    public class Transform
    {
        public Vector3D Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3D Scale { get; set; }
        public Transform? Parent { get; set; }
        public List<Transform> Children { get; } = new();

        public Transform()
        {
            Position = new Vector3D(0, 0, 0);
            Rotation = Quaternion.Identity;
            Scale = new Vector3D(1, 1, 1);
        }

        /// <summary>
        /// Gets the local transformation matrix (scale * rotation * translation).
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
        /// Gets the world transformation matrix, incorporating parent transformations.
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
        /// Adds a child transform to this transform.
        /// </summary>
        public void AddChild(Transform child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            if (child.Parent != null)
                throw new InvalidOperationException("Child already has a parent.");
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Removes a child transform from this transform.
        /// </summary>
        public void RemoveChild(Transform child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            if (child.Parent != this)
                throw new InvalidOperationException("Child does not belong to this parent.");
            child.Parent = null;
            Children.Remove(child);
        }

        /// <summary>
        /// Rotates the transform around the Y-axis by the specified angle.
        /// </summary>
        public void RotateY(float angle)
        {
            Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
            Rotation = Quaternion.Normalize(rot * Rotation);
        }

        /// <summary>
        /// Rotates the transform around an arbitrary axis by the specified angle.
        /// </summary>
        public void Rotate(Vector3D axis, float angle)
        {
            if (axis.Normalized == new Vector3D(0, 0, 0))
                throw new ArgumentException("Rotation axis cannot be zero.", nameof(axis));
            Quaternion rot = Quaternion.CreateFromAxisAngle(axis.ToVector3(), angle);
            Rotation = Quaternion.Normalize(rot * Rotation);
        }

        /// <summary>
        /// Translates the transform by the specified offset.
        /// </summary>
        public void Translate(Vector3D offset)
        {
            Position += offset;
        }

        /// <summary>
        /// Scales the transform by the specified factors.
        /// </summary>
        public void SetScale(Vector3D newScale)
        {
            if (newScale.X <= 0 || newScale.Y <= 0 || newScale.Z <= 0)
                throw new ArgumentException("Scale factors must be positive.", nameof(newScale));
            Scale = newScale;
        }
    }
}