using System;
using _2dGameEngine.Core;
using _2dGameEngine.Input;

namespace _2dGameEngine.Scripting;

/// <summary>
/// Represents a script attached by the editor before a compiled script type exists.
/// </summary>
public sealed class AuthoredScriptComponent : ScriptComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthoredScriptComponent"/> class.
    /// </summary>
    public AuthoredScriptComponent(string className, string scriptPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);
        ClassName = className;
        ScriptPath = scriptPath;
    }

    /// <summary>
    /// Gets the script class name.
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// Gets the project-relative script source path.
    /// </summary>
    public string ScriptPath { get; }

    /// <summary>
    /// Gets or sets a short editor-facing description of the script behavior.
    /// </summary>
    public string Description { get; set; } = "Authored gameplay script";

    /// <inheritdoc />
    protected override void Tick(Time time, InputState input)
    {
        // Authored scripts are placeholders until the game project compiles them into runtime components.
    }
}
