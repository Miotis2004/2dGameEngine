using System;
using System.Collections.Generic;
using System.Linq;

namespace _2dGameEngine.Animation;

/// <summary>
/// Defines an ordered sprite animation that can be played by an <see cref="AnimationPlayer"/>.
/// </summary>
public sealed class AnimationClip
{
    private readonly AnimationFrame[] _frames;

    public AnimationClip(string name, IEnumerable<AnimationFrame> frames, bool isLooping = true, string? assetPath = null, IEnumerable<AnimationEvent>? events = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(frames);

        _frames = frames.ToArray();
        if (_frames.Length == 0)
        {
            throw new ArgumentException("Animation clips must contain at least one frame.", nameof(frames));
        }

        if (_frames.Any(frame => frame.Duration <= TimeSpan.Zero))
        {
            throw new ArgumentException("Animation frame durations must be greater than zero.", nameof(frames));
        }

        Name = name;
        IsLooping = isLooping;
        Duration = TimeSpan.FromTicks(_frames.Sum(frame => frame.Duration.Ticks));
        AssetPath = assetPath;
        Events = (events ?? []).OrderBy(animationEvent => animationEvent.Time).ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<AnimationFrame> Frames => _frames;

    public bool IsLooping { get; }

    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the content-relative asset path this clip was loaded from, when available.
    /// </summary>
    public string? AssetPath { get; }

    /// <summary>
    /// Gets named animation events authored on this clip.
    /// </summary>
    public IReadOnlyList<AnimationEvent> Events { get; }

    public AnimationFrame GetFrameAt(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (IsLooping && Duration > TimeSpan.Zero)
        {
            elapsed = TimeSpan.FromTicks(elapsed.Ticks % Duration.Ticks);
        }
        else if (elapsed >= Duration)
        {
            return _frames[^1];
        }

        long accumulatedTicks = 0;
        foreach (AnimationFrame frame in _frames)
        {
            accumulatedTicks += frame.Duration.Ticks;
            if (elapsed.Ticks < accumulatedTicks)
            {
                return frame;
            }
        }

        return _frames[^1];
    }
}
