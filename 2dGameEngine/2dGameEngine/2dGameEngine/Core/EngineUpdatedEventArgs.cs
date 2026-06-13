using System;

namespace _2dGameEngine.Core;

/// <summary>
/// Provides data for the engine updated event.
/// </summary>
/// <param name="time">Frame timing information.</param>
/// <param name="scene">The scene that was updated.</param>
internal sealed class EngineUpdatedEventArgs(Time time, Scene? scene) : EventArgs
{
    /// <summary>
    /// Gets frame timing information.
    /// </summary>
    public Time Time { get; } = time;

    /// <summary>
    /// Gets the scene that was updated.
    /// </summary>
    public Scene? Scene { get; } = scene;
}
