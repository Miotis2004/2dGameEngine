using System;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;

namespace _2dGameEngine.Animation;

/// <summary>
/// Advances an animation clip over time and applies its current frame to an entity sprite renderer.
/// </summary>
public sealed class AnimationPlayer : Component
{
    private TimeSpan _elapsed;

    public AnimationPlayer(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        Clip = clip;
    }

    public AnimationClip Clip { get; private set; }

    public bool IsPlaying { get; private set; } = true;

    public float PlaybackSpeed { get; set; } = 1.0f;

    public void Play(AnimationClip clip, bool restart = true)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!ReferenceEquals(Clip, clip) || restart)
        {
            Clip = clip;
            _elapsed = TimeSpan.Zero;
        }

        IsPlaying = true;
        ApplyCurrentFrame();
    }

    public void Pause() => IsPlaying = false;

    public void Resume() => IsPlaying = true;

    public void Stop()
    {
        IsPlaying = false;
        _elapsed = TimeSpan.Zero;
        ApplyCurrentFrame();
    }

    protected override void OnAttached()
    {
        ApplyCurrentFrame();
    }

    public override void Update(Time time)
    {
        if (!IsPlaying || PlaybackSpeed <= 0.0f)
        {
            return;
        }

        _elapsed += TimeSpan.FromTicks((long)(time.DeltaTime.Ticks * PlaybackSpeed));
        if (!Clip.IsLooping && _elapsed >= Clip.Duration)
        {
            _elapsed = Clip.Duration;
            IsPlaying = false;
        }

        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        SpriteRenderer? sprite = Entity?.GetComponent<SpriteRenderer>();
        if (sprite is not null)
        {
            sprite.Frame = Clip.GetFrameAt(_elapsed).SpriteFrame;
        }
    }
}
