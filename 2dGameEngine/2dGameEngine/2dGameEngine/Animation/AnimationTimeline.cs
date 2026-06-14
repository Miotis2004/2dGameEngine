using System;
using System.Collections.Generic;
using System.Linq;
using _2dGameEngine.Content;

namespace _2dGameEngine.Animation;

/// <summary>
/// Editor-friendly animation timeline containing sprite keyframes, events, playback controls, and onion-skin queries.
/// </summary>
public sealed class AnimationTimeline
{
    private readonly List<AnimationTimelineKeyframe> _keyframes = [];
    private readonly List<AnimationEvent> _events = [];

    public AnimationTimeline(string name, bool isLooping = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        IsLooping = isLooping;
    }

    public string Name { get; set; }

    public bool IsLooping { get; set; }

    public float PlaybackSpeed { get; set; } = 1.0f;

    public TimeSpan Duration => _keyframes.Count == 0 ? TimeSpan.Zero : _keyframes.Max(keyframe => keyframe.Time + keyframe.Duration);

    public IReadOnlyList<AnimationTimelineKeyframe> Keyframes => _keyframes;

    public IReadOnlyList<AnimationEvent> Events => _events;

    public void AddKeyframe(TimeSpan time, SpriteFrame spriteFrame, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(spriteFrame);
        if (time < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(time));
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        _keyframes.Add(new AnimationTimelineKeyframe(time, spriteFrame, duration));
        _keyframes.Sort((left, right) => left.Time.CompareTo(right.Time));
    }

    public void AddEvent(AnimationEvent animationEvent)
    {
        ArgumentNullException.ThrowIfNull(animationEvent);
        if (animationEvent.Time < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(animationEvent));
        _events.Add(animationEvent);
        _events.Sort((left, right) => left.Time.CompareTo(right.Time));
    }

    public SpriteFrame? GetFrameAt(TimeSpan time)
    {
        if (_keyframes.Count == 0) return null;
        time = NormalizeTime(time);
        return _keyframes.LastOrDefault(keyframe => keyframe.Time <= time && time < keyframe.Time + keyframe.Duration)?.SpriteFrame
            ?? _keyframes.LastOrDefault(keyframe => keyframe.Time <= time)?.SpriteFrame
            ?? _keyframes[0].SpriteFrame;
    }

    public IReadOnlyList<SpriteFrame> GetOnionSkinFrames(TimeSpan time, int previousFrames = 1, int nextFrames = 1)
    {
        if (_keyframes.Count == 0) return [];
        time = NormalizeTime(time);
        int current = Math.Max(0, _keyframes.FindLastIndex(keyframe => keyframe.Time <= time));
        int first = Math.Max(0, current - Math.Max(0, previousFrames));
        int last = Math.Min(_keyframes.Count - 1, current + Math.Max(0, nextFrames));
        return _keyframes.Skip(first).Take(last - first + 1).Select(keyframe => keyframe.SpriteFrame).ToArray();
    }

    public AnimationClip ToClip(string? assetPath = null)
    {
        if (_keyframes.Count == 0) throw new InvalidOperationException("Timeline must contain at least one keyframe before it can be converted to a clip.");
        return new AnimationClip(Name, _keyframes.Select(keyframe => new AnimationFrame(keyframe.SpriteFrame, keyframe.Duration)), IsLooping, assetPath, _events);
    }

    private TimeSpan NormalizeTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero) time = TimeSpan.Zero;
        TimeSpan duration = Duration;
        return IsLooping && duration > TimeSpan.Zero ? TimeSpan.FromTicks(time.Ticks % duration.Ticks) : time > duration ? duration : time;
    }
}

public sealed record AnimationTimelineKeyframe(TimeSpan Time, SpriteFrame SpriteFrame, TimeSpan Duration);
