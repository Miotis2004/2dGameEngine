using System;
using _2dGameEngine.Input;

namespace _2dGameEngine.Core;

/// <summary>
/// Provides data for the engine updated event.
/// </summary>
/// <param name="time">Frame timing information.</param>
/// <param name="scene">The scene that was updated.</param>
/// <param name="input">The input state used for the update.</param>
public sealed class EngineUpdatedEventArgs(Time time, Scene? scene, InputState input) : EventArgs
{
    /// <summary>
    /// Gets frame timing information.
    /// </summary>
    public Time Time { get; } = time;

    /// <summary>
    /// Gets the scene that was updated.
    /// </summary>
    public Scene? Scene { get; } = scene;

    /// <summary>
    /// Gets the input state used for the update.
    /// </summary>
    public InputState Input { get; } = input;
}
