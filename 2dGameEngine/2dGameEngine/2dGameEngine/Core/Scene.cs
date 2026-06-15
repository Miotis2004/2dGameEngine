using System;
using System.Collections.Generic;
using _2dGameEngine.Input;
using _2dGameEngine.ECS;
using _2dGameEngine.Physics;

namespace _2dGameEngine.Core;

/// <summary>
/// Contains and updates the entities that make up a game world.
/// </summary>
public sealed class Scene
{
    private readonly List<Entity> _entities = [];
    private readonly List<Entity> _entitiesToAdd = [];
    private readonly List<Entity> _entitiesToRemove = [];
    private bool _isUpdating;

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
    /// Gets the physics simulation for this scene.
    /// </summary>
    public PhysicsSystem Physics { get; } = new();

    public EcsWorld EcsWorld { get; } = new();

    public IList<IEcsSystem> Systems { get; } = new List<IEcsSystem> { new MovementSystem() };

    /// <summary>
    /// Adds an entity to the scene.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity.</returns>
    public Entity AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entitiesToRemove.Remove(entity);
        if (_entities.Contains(entity) || _entitiesToAdd.Contains(entity)) return entity;
        if (_isUpdating)
        {
            _entitiesToAdd.Add(entity);
        }
        else
        {
            _entities.Add(entity);
        }
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

    /// <summary>
    /// Removes an entity from the scene.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns><see langword="true"/> when the entity was present and removed.</returns>
    public bool RemoveEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        bool existed = _entities.Remove(entity);
        bool pending = _entitiesToAdd.Remove(entity);
        if (!existed && !pending)
        {
            return false;
        }
        _entitiesToRemove.Add(entity);

        if (entity.Parent is not null)
        {
            entity.Parent.RemoveChild(entity);
        }

        return true;
    }

    internal void Update(Time time, InputState input)
    {
        ApplyStructuralChanges();
        _isUpdating = true;
        EcsWorld.ApplyStructuralChanges();
        float deltaSeconds = MathF.Min((float)time.DeltaTime.TotalSeconds, 0.05f);
        for (int i = 0; i < Systems.Count; i++)
        {
            Systems[i].Update(EcsWorld, deltaSeconds);
        }

        int entityCount = _entities.Count;
        for (int i = 0; i < entityCount; i++)
        {
            Entity entity = _entities[i];
            if (entity.Parent is null)
            {
                entity.Update(time, input);
            }
        }

        Physics.Step(this, time);
        _isUpdating = false;
        ApplyStructuralChanges();
    }

    internal void ApplyStructuralChanges()
    {
        if (_entitiesToRemove.Count > 0)
        {
            foreach (Entity entity in _entitiesToRemove)
            {
                _entities.Remove(entity);
            }
            _entitiesToRemove.Clear();
        }

        if (_entitiesToAdd.Count > 0)
        {
            _entities.AddRange(_entitiesToAdd);
            _entitiesToAdd.Clear();
        }

        for (int i = 0; i < _entities.Count; i++)
        {
            _entities[i].ApplyStructuralChanges();
        }
    }
}
