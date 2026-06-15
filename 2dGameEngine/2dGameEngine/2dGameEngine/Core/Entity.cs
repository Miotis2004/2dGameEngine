using System;
using System.Collections.Generic;
using _2dGameEngine.Input;

namespace _2dGameEngine.Core;

/// <summary>
/// Represents a game object that can own components and be updated by a scene.
/// </summary>
public sealed class Entity
{
    private readonly List<Component> _components = [];
    private readonly List<Component> _componentsToAdd = [];
    private readonly List<Component> _componentsToRemove = [];
    private readonly List<Entity> _children = [];
    private readonly List<Entity> _childrenToAdd = [];
    private readonly List<Entity> _childrenToRemove = [];
    private bool _isUpdating;

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    /// <param name="name">The human-readable entity name.</param>
    public Entity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Transform = AddComponent(new TransformComponent());
    }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets a value indicating whether this entity should receive updates.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the parent entity when this entity is part of a hierarchy.
    /// </summary>
    public Entity? Parent { get; private set; }

    /// <summary>
    /// Gets the entity transform.
    /// </summary>
    public TransformComponent Transform { get; }

    /// <summary>
    /// Gets the components attached to this entity.
    /// </summary>
    public IReadOnlyList<Component> Components => _components;

    /// <summary>
    /// Gets the direct child entities owned by this entity.
    /// </summary>
    public IReadOnlyList<Entity> Children => _children;


    /// <summary>
    /// Adds a child entity to this entity.
    /// </summary>
    public Entity AddChild(Entity child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (ReferenceEquals(child, this) || IsDescendantOf(child))
        {
            throw new InvalidOperationException("An entity cannot be parented to itself or one of its descendants.");
        }

        child.Parent?.RemoveChild(child);
        child.Parent = this;
        _childrenToRemove.Remove(child);
        if (_children.Contains(child) || _childrenToAdd.Contains(child)) return child;
        if (_isUpdating)
        {
            _childrenToAdd.Add(child);
        }
        else
        {
            _children.Add(child);
        }
        return child;
    }

    /// <summary>
    /// Removes a child entity from this entity.
    /// </summary>
    public bool RemoveChild(Entity child)
    {
        ArgumentNullException.ThrowIfNull(child);
        bool existed = _children.Remove(child);
        bool pending = _childrenToAdd.Remove(child);
        if (!existed && !pending)
        {
            return false;
        }

        _childrenToRemove.Add(child);
        child.Parent = null;
        return true;
    }

    private bool IsDescendantOf(Entity possibleParent)
    {
        for (Entity? current = Parent; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, possibleParent))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds a component to this entity.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="component">The component instance to add.</param>
    /// <returns>The added component.</returns>
    public TComponent AddComponent<TComponent>(TComponent component)
        where TComponent : Component
    {
        ArgumentNullException.ThrowIfNull(component);

        if (component.Entity is not null)
        {
            throw new InvalidOperationException("A component cannot be attached to more than one entity.");
        }

        if (_isUpdating)
        {
            _componentsToAdd.Add(component);
        }
        else
        {
            _components.Add(component);
        }
        component.Attach(this);
        return component;
    }

    /// <summary>
    /// Gets the first component of the requested type, if present.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>The matching component, or <see langword="null"/> when not found.</returns>
        /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    /// <param name="component">The component to remove.</param>
    /// <returns><see langword="true"/> when the component was present and removed.</returns>
    public bool RemoveComponent(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        bool existed = _components.Contains(component) && !_componentsToRemove.Contains(component);
        bool pending = _componentsToAdd.Remove(component);
        if (!existed && !pending)
        {
            return false;
        }

        if (existed)
        {
            _componentsToRemove.Add(component);
        }

        component.Detach();
        return true;
    }

    public TComponent? GetComponent<TComponent>()
        where TComponent : Component
    {
        foreach (Component component in _components)
        {
            if (component is TComponent match) return match;
        }

        foreach (Component component in _componentsToAdd)
        {
            if (component is TComponent match) return match;
        }

        return null;
    }

    internal void Update(Time time, InputState input)
    {
        if (!IsEnabled)
        {
            return;
        }

        ApplyStructuralChanges();
        _isUpdating = true;
        int componentCount = _components.Count;
        for (int i = 0; i < componentCount; i++)
        {
            Component component = _components[i];
            if (component.IsEnabled)
            {
                component.Update(time, input);
            }
        }

        int childCount = _children.Count;
        for (int i = 0; i < childCount; i++)
        {
            _children[i].Update(time, input);
        }

        _isUpdating = false;
        ApplyStructuralChanges();
    }

    internal void ApplyStructuralChanges()
    {
                if (_componentsToRemove.Count > 0)
        {
            foreach (Component comp in _componentsToRemove)
            {
                _components.Remove(comp);
            }
            _componentsToRemove.Clear();
        }

        if (_componentsToAdd.Count > 0)
        {
            _components.AddRange(_componentsToAdd);
            _componentsToAdd.Clear();
        }

        if (_childrenToRemove.Count > 0)
        {
            foreach (Entity child in _childrenToRemove)
            {
                _children.Remove(child);
            }
            _childrenToRemove.Clear();
        }

        if (_childrenToAdd.Count > 0)
        {
            _children.AddRange(_childrenToAdd);
            _childrenToAdd.Clear();
        }
    }
}
