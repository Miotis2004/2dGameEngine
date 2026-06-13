using System;
using Microsoft.UI.Dispatching;

namespace _2dGameEngine.Core;

/// <summary>
/// Coordinates engine startup, frame timing, and scene updates.
/// </summary>
public sealed class Engine
{
    private readonly DispatcherQueueTimer _updateTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine"/> class.
    /// </summary>
    /// <param name="dispatcherQueue">The dispatcher queue used to schedule frame updates.</param>
    public Engine(DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        Time = new Time();
        _updateTimer = dispatcherQueue.CreateTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(16);
        _updateTimer.IsRepeating = true;
        _updateTimer.Tick += OnUpdateTimerTick;
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
    /// Gets a value indicating whether the engine update loop is running.
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
    /// Starts the engine update loop.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        Time.Start();
        IsRunning = true;
        _updateTimer.Start();
    }

    /// <summary>
    /// Stops the engine update loop.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _updateTimer.Stop();
        IsRunning = false;
    }

    private void OnUpdateTimerTick(DispatcherQueueTimer sender, object args)
    {
        Time.Update();
        ActiveScene?.Update(Time);
        Updated?.Invoke(this, new EngineUpdatedEventArgs(Time, ActiveScene));
    }
}
