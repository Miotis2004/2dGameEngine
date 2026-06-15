using _2dGameEngine.Input;

namespace _2dGameEngine.Core;

/// <summary>
/// Base type for reusable behavior attached to an entity.
/// </summary>
public abstract class Component
{
    /// <summary>
    /// Gets the entity that owns this component.
    /// </summary>
    public Entity? Entity { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this component should receive updates.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    internal void Attach(Entity entity)
    {
        Entity = entity;
        OnAttached();
    }

    /// <summary>
    /// Called after the component is attached to an entity.
    /// </summary>
    protected virtual void OnAttached()
    {
    }

    internal void Detach()
    {
        Entity = null;
        OnDetached();
    }

    /// <summary>
    /// Called after the component is detached from an entity.
    /// </summary>
    protected virtual void OnDetached()
    {
    }


    /// <summary>
    /// Called once per frame while the component and owning entity are enabled.
    /// </summary>
    /// <param name="time">Frame timing information.</param>
    public virtual void Update(Time time)
    {
    }

    /// <summary>
    /// Called once per frame while the component and owning entity are enabled with access to input state.
    /// </summary>
    /// <param name="time">Frame timing information.</param>
    /// <param name="input">Current input state.</param>
    public virtual void Update(Time time, InputState input)
    {
        Update(time);
    }
}
