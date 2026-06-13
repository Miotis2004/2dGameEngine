using System;

namespace _2dGameEngine.Core;

/// <summary>
/// Coordinates engine startup, frame timing, and scene updates.
/// </summary>
internal sealed class Engine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Engine"/> class.
    /// </summary>
    public Engine()
    {
        Time = new Time();
    }

    /// <summary>
    /// Raised after the current scene has been updated for a frame.
    /// </summary>
    public event EventHandler<EngineUpdatedEventArgs>? Updated;

    /// <summary>
    /// Gets the active scene.
    /// </summary>
    public Scene? ActiveScene { get; private set; }

    /// <summary>
    /// Gets engine timing information.
    /// </summary>
    public Time Time { get; }

    /// <summary>
    /// Gets a value indicating whether the engine is accepting update ticks.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Sets the active scene to update each frame.
    /// </summary>
    /// <param name="scene">The scene to make active.</param>
    public void SetActiveScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ActiveScene = scene;
    }

    /// <summary>
    /// Starts the engine runtime state.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        Time.Start();
        IsRunning = true;
    }

    /// <summary>
    /// Stops the engine runtime state.
    /// </summary>
    public void Stop()
    {
        IsRunning = false;
    }

    /// <summary>
    /// Advances the engine by one frame. The host application owns how often this method is called.
    /// </summary>
    public void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        Time.Update();
        ActiveScene?.Update(Time);
        Updated?.Invoke(this, new EngineUpdatedEventArgs(Time, ActiveScene));
    }
}
