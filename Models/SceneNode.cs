using System;
using System.Collections.Generic;
using Ascii3DRenderer.Mathematics;

namespace Ascii3DRenderer.Models
{
    /// <summary>
    /// Represents a node in the scene graph, combining a transform with an optional mesh and light.
    /// </summary>
    public class SceneNode
    {
        public Transform Transform { get; }
        public Mesh? Mesh { get; }
        public Light? Light { get; }
        public List<SceneNode> Children { get; } = new();

        public SceneNode(Transform transform, Mesh? mesh = null, Light? light = null)
        {
            Transform = transform ?? throw new ArgumentNullException(nameof(transform));
            Mesh = mesh;
            Light = light;
        }

        /// <summary>
        /// Adds a child node to this node.
        /// </summary>
        public void AddChild(SceneNode child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            if (child.Transform.Parent != null)
                throw new InvalidOperationException("Child node already has a parent.");
            child.Transform.Parent = Transform;
            Children.Add(child);
        }

        /// <summary>
        /// Removes a child node from this node.
        /// </summary>
        public void RemoveChild(SceneNode child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            if (child.Transform.Parent != Transform)
                throw new InvalidOperationException("Child does not belong to this parent.");
            child.Transform.Parent = null;
            Children.Remove(child);
        }
    }
}