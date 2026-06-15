using System;
using System.Collections.Generic;
using System.Numerics;

namespace _2dGameEngine.ECS;

/// <summary>
/// Marker interface for unmanaged data-oriented ECS components.
/// </summary>
public interface IDataComponent { }

public readonly record struct EntityId(int Value)
{
    public bool IsValid => Value > 0;
    public override string ToString() => Value.ToString();
}

public interface IEcsSystem
{
    void Update(EcsWorld world, float deltaSeconds);
}

public struct PositionComponent : IDataComponent
{
    public Vector2 Value;
}

public struct VelocityComponent : IDataComponent
{
    public Vector2 Value;
}

/// <summary>
/// Lightweight sparse-set ECS world that stores unmanaged component arrays contiguously per component type.
/// Structural changes are deferred until the frame boundary to keep hot-path iteration allocation-free.
/// </summary>
public sealed class EcsWorld
{
    private readonly Dictionary<Type, IComponentStore> _stores = [];
    private readonly Queue<Action> _commands = new();
    private int _nextEntityId;

    public EntityId CreateEntity()
    {
        EntityId id = new(++_nextEntityId);
        return id;
    }

    public void DestroyEntity(EntityId entity) => _commands.Enqueue(() =>
    {
        foreach (IComponentStore store in _stores.Values)
        {
            store.Remove(entity.Value);
        }
    });

    public void SetComponent<TComponent>(EntityId entity, TComponent component)
        where TComponent : unmanaged, IDataComponent => _commands.Enqueue(() => GetStore<TComponent>().Set(entity.Value, component));

    public bool TryGetComponent<TComponent>(EntityId entity, out TComponent component)
        where TComponent : unmanaged, IDataComponent => GetStore<TComponent>().TryGet(entity.Value, out component);

    public void ApplyStructuralChanges()
    {
        while (_commands.Count > 0)
        {
            _commands.Dequeue().Invoke();
        }
    }

    public ComponentStore<TComponent> GetStore<TComponent>() where TComponent : unmanaged, IDataComponent
    {
        Type type = typeof(TComponent);
        if (!_stores.TryGetValue(type, out IComponentStore? store))
        {
            store = new ComponentStore<TComponent>();
            _stores.Add(type, store);
        }

        return (ComponentStore<TComponent>)store;
    }

    public void ForEach<TFirst, TSecond>(Action<EntityId, ComponentStore<TFirst>, int, ComponentStore<TSecond>, int> action)
        where TFirst : unmanaged, IDataComponent
        where TSecond : unmanaged, IDataComponent
    {
        ComponentStore<TFirst> first = GetStore<TFirst>();
        ComponentStore<TSecond> second = GetStore<TSecond>();
        for (int i = 0; i < first.Count; i++)
        {
            int entity = first.Entities[i];
            if (second.TryGetIndex(entity, out int secondIndex))
            {
                action(new EntityId(entity), first, i, second, secondIndex);
            }
        }
    }

    private interface IComponentStore
    {
        void Remove(int entity);
    }

    public sealed class ComponentStore<TComponent> : IComponentStore where TComponent : unmanaged, IDataComponent
    {
        private readonly Dictionary<int, int> _indices = [];
        public List<int> Entities { get; } = [];
        public List<TComponent> Components { get; } = [];
        public int Count => Components.Count;

        public void Set(int entity, TComponent component)
        {
            if (_indices.TryGetValue(entity, out int index))
            {
                Components[index] = component;
                return;
            }

            _indices.Add(entity, Components.Count);
            Entities.Add(entity);
            Components.Add(component);
        }

        public bool TryGet(int entity, out TComponent component)
        {
            if (_indices.TryGetValue(entity, out int index))
            {
                component = Components[index];
                return true;
            }

            component = default;
            return false;
        }

        public bool TryGetIndex(int entity, out int index) => _indices.TryGetValue(entity, out index);

        public void Remove(int entity)
        {
            if (!_indices.TryGetValue(entity, out int index)) return;
            int last = Components.Count - 1;
            if (index != last)
            {
                Components[index] = Components[last];
                Entities[index] = Entities[last];
                _indices[Entities[index]] = index;
            }
            Components.RemoveAt(last);
            Entities.RemoveAt(last);
            _indices.Remove(entity);
        }
    }
}

public sealed class MovementSystem : IEcsSystem
{
    public void Update(EcsWorld world, float deltaSeconds)
    {
        world.ForEach<PositionComponent, VelocityComponent>((_, positions, positionIndex, velocities, velocityIndex) =>
        {
            PositionComponent position = positions.Components[positionIndex];
            position.Value += velocities.Components[velocityIndex].Value * deltaSeconds;
            positions.Components[positionIndex] = position;
        });
    }
}
