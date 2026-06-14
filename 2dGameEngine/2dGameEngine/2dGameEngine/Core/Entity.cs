using System;
using System.Collections.Generic;
using System.Linq;
using _2dGameEngine.Input;

namespace _2dGameEngine.Core;

/// <summary>
/// Represents a game object that can own components and be updated by a scene.
/// </summary>
public sealed class Entity
{
    private readonly List<Component> _components = [];
    private readonly List<Entity> _children = [];

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

        child.Parent?._children.Remove(child);
        child.Parent = this;
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Removes a child entity from this entity.
    /// </summary>
    public bool RemoveChild(Entity child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!_children.Remove(child))
        {
            return false;
        }

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

        _components.Add(component);
        component.Attach(this);
        return component;
    }

    /// <summary>
    /// Gets the first component of the requested type, if present.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>The matching component, or <see langword="null"/> when not found.</returns>
    public TComponent? GetComponent<TComponent>()
        where TComponent : Component
    {
        return _components.OfType<TComponent>().FirstOrDefault();
    }

    internal void Update(Time time, InputState input)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (Component component in _components.ToArray())
        {
            if (component.IsEnabled)
            {
                component.Update(time, input);
            }
        }

        foreach (Entity child in _children.ToArray())
        {
            child.Update(time, input);
        }
    }
}
