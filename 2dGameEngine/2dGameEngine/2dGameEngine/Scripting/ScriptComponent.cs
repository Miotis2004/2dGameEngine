using System;
using System.Collections.Generic;
using _2dGameEngine.Core;
using _2dGameEngine.Input;

namespace _2dGameEngine.Scripting;

/// <summary>
/// Base class for authored C# gameplay scripts that can be attached to entities.
/// </summary>
public abstract class ScriptComponent : Component
{
    private bool _hasStarted;

    /// <summary>
    /// Gets a dictionary of editable script properties exposed to tooling.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Called before the first script update.
    /// </summary>
    protected virtual void Start()
    {
    }

    /// <summary>
    /// Called every frame after <see cref="Start"/> has run.
    /// </summary>
    protected virtual void Tick(Time time, InputState input)
    {
    }

    /// <inheritdoc />
    public sealed override void Update(Time time, InputState input)
    {
        if (!_hasStarted)
        {
            Start();
            _hasStarted = true;
        }

        Tick(time, input);
    }
}
