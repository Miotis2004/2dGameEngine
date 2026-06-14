using _2dGameEngine.Core;
using _2dGameEngine.Input;

namespace _2dGameEngine.Scripting;

/// <summary>
/// Unity-style C# script base class used by Unity 2 projects.
/// </summary>
public abstract class MonoBehaviour : ScriptComponent
{
    private bool _hasAwoken;

    /// <summary>
    /// Gets the entity that owns this behaviour.
    /// </summary>
    public Entity? GameObject => Entity;

    /// <summary>
    /// Gets the transform of the owning entity, if attached.
    /// </summary>
    public Transform2D? Transform => Entity?.Transform.Value;

    /// <summary>
    /// Called once before <see cref="Start"/>. Override for early setup.
    /// </summary>
    protected virtual void Awake()
    {
    }

    /// <inheritdoc />
    public sealed override void Update(Time time, InputState input)
    {
        if (!_hasAwoken)
        {
            Awake();
            _hasAwoken = true;
        }

        base.Update(time, input);
    }
}
