using System;
using System.Collections.Generic;

namespace _2dGameEngine.Core;

/// <summary>
/// Contains and updates the entities that make up a game world.
/// </summary>
public sealed class Scene
{
    private readonly List<Entity> _entities = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Scene"/> class.
    /// </summary>
    /// <param name="name">The scene name.</param>
    public Scene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the scene name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the entities in this scene.
    /// </summary>
    public IReadOnlyList<Entity> Entities => _entities;

    /// <summary>
    /// Adds an entity to the scene.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity.</returns>
    public Entity AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Add(entity);
        return entity;
    }

    /// <summary>
    /// Creates and adds an entity to the scene.
    /// </summary>
    /// <param name="name">The entity name.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity(string name)
    {
        Entity entity = new(name);
        return AddEntity(entity);
    }

    internal void Update(Time time)
    {
        foreach (Entity entity in _entities.ToArray())
        {
            entity.Update(time);
        }
    }
}
