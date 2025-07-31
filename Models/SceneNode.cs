using System;
using System.Collections.Generic;
using RendrixEngine.Mathematics;

namespace RendrixEngine.Models
{
    /// <summary>
    /// Represents a node in the scene graph, combining a transform with an optional mesh and light.
    /// </summary>
    public class SceneNode
    {
        public List<Component> Components { get; }
        public Transform Transform { get; }
        public List<SceneNode> Children { get; } = new();

        public SceneNode()
        {
            Transform = new Transform();
            Components = new List<Component>();
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

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            Components.Add(component);
            component.OnEnable(); // Call OnEnable when added
            return component;
        }

        public T? GetComponent<T>() where T : Component
        {
            foreach (var component in Components)
            {
                if (component is T typedComponent)
                    return typedComponent;
            }
            return null; // Return null if no component of type T is found
        }

        public void RemoveComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component != null)
            {
                Components.Remove(component);
                component.OnDisable(); // Call OnDisable when removed
            }
        }
    }
}