using System;

namespace _2dGameEngine.Core;

/// <summary>
/// Tracks frame timing information for engine updates.
/// </summary>
internal sealed class Time
{
    private DateTimeOffset _lastUpdateTime;

    /// <summary>
    /// Gets the amount of time that elapsed during the previous frame.
    /// </summary>
    public TimeSpan DeltaTime { get; private set; }

    /// <summary>
    /// Gets the total amount of time that has elapsed since the engine started.
    /// </summary>
    public TimeSpan TotalTime { get; private set; }

    /// <summary>
    /// Gets the current frame number, starting at zero before the first update.
    /// </summary>
    public ulong FrameCount { get; private set; }

    /// <summary>
    /// Starts timing from the current instant.
    /// </summary>
    public void Start()
    {
        _lastUpdateTime = DateTimeOffset.UtcNow;
        DeltaTime = TimeSpan.Zero;
        TotalTime = TimeSpan.Zero;
        FrameCount = 0;
    }

    /// <summary>
    /// Advances the timing state to the current instant.
    /// </summary>
    public void Update()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DeltaTime = now - _lastUpdateTime;
        TotalTime += DeltaTime;
        _lastUpdateTime = now;
        FrameCount++;
    }
}
