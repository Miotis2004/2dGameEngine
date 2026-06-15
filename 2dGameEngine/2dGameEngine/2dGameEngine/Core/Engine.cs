using System;
using System.Windows.Forms;
using _2dGameEngine.Audio;
using _2dGameEngine.Input;
using _2dGameEngine.Services;

namespace _2dGameEngine.Core;

/// <summary>
/// Coordinates engine startup, frame timing, and scene updates.
/// </summary>
public sealed class Engine
{
    private readonly Timer _updateTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine"/> class.
    /// </summary>
    public Engine()
    {
        Time = new Time();
        Input = new InputState();
        Audio = new AudioMixer();
        Services = new GameServices();
        _updateTimer = new Timer
        {
            Interval = 16,
        };
        _updateTimer.Tick += OnUpdateTimerTick;
    }

    /// <summary>
    /// Raised after the current scene has been updated for a frame.
    /// </summary>
    public event EventHandler<EngineUpdatedEventArgs>? Updated;

    /// <summary>
    /// Raised when a runtime update throws an exception.
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Gets the active scene.
    /// </summary>
    public Scene? ActiveScene { get; private set; }

    /// <summary>
    /// Gets engine timing information.
    /// </summary>
    public Time Time { get; }

    /// <summary>
    /// Gets the current keyboard and mouse input state.
    /// </summary>
    public InputState Input { get; }

    /// <summary>
    /// Gets the runtime audio mixer shared by scene audio sources and the editor.
    /// </summary>
    public AudioMixer Audio { get; }

    /// <summary>
    /// Gets save data, localization, achievements, and leaderboard services for gameplay code.
    /// </summary>
    public GameServices Services { get; }

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

    /// <summary>
    /// Advances the active scene by exactly one frame while the update loop is stopped.
    /// </summary>
    public void Step()
    {
        if (IsRunning)
        {
            return;
        }

        UpdateFrame();
    }

    private void OnUpdateTimerTick(object? sender, EventArgs args)
    {
        UpdateFrame();
    }

    private void UpdateFrame()
    {
        try
        {
            Time.Update();
            ActiveScene?.Update(Time, Input);
            Updated?.Invoke(this, new EngineUpdatedEventArgs(Time, ActiveScene, Input));
            Input.AdvanceFrame();
        }
        catch (Exception ex)
        {
            Stop();
            ErrorOccurred?.Invoke(this, ex);
        }
    }
}
