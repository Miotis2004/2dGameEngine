using System;
using System.Collections.Generic;
using System.Linq;

namespace _2dGameEngine.Core;

/// <summary>
/// Represents a game object that can own components and be updated by a scene.
/// </summary>
internal sealed class Entity
{
    private readonly List<Component> _components = [];

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
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this entity should receive updates.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the entity transform.
    /// </summary>
    public TransformComponent Transform { get; }

    /// <summary>
    /// Gets the components attached to this entity.
    /// </summary>
    public IReadOnlyList<Component> Components => _components;

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

    internal void Update(Time time)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (Component component in _components.ToArray())
        {
            if (component.IsEnabled)
            {
                component.Update(time);
            }
        }
    }
}
