using RendrixEngine.Components;

namespace RendrixEngine.Systems
{
    public class SceneNode
    {
        public List<Component> Components { get; }
        public Transform Transform { get; }
        public List<SceneNode> Children { get; } = new();

        public string Name { get; set; }

        public SceneNode(string name)
        {
            Transform = new Transform();
            Components = new List<Component>();
            Name = name;
        }
        public void AddChild(SceneNode child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            if (child.Transform.Parent != null)
                throw new InvalidOperationException("Child node already has a parent.");
            child.Transform.Parent = Transform;
            Children.Add(child);
        }
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
            component.OnAwake();
            component.Transform = Transform;
            return component;
        }

        public T? GetComponent<T>() where T : Component
        {
            foreach (var component in Components)
            {
                if (component is T typedComponent)
                    return typedComponent;
            }
            return null;
        }

        public void RemoveComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component != null)
            {
                Components.Remove(component);
                component.OnDisable();
            }
        }
    }
}