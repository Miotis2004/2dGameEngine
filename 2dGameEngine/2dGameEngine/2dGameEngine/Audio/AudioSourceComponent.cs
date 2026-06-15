using System;
using System.Media;
using _2dGameEngine.Core;

namespace _2dGameEngine.Audio;

/// <summary>
/// Component that plays a single audio clip through a named mixer group.
/// </summary>
public sealed class AudioSourceComponent : Component, IDisposable
{
    private SoundPlayer? _soundPlayer;
    private bool _started;

    public AudioClip? Clip { get; set; }

    public string MixerGroup { get; set; } = "Sfx";

    public float Volume { get; set; } = 1.0f;

    public bool PlayOnStart { get; set; }

    public bool Loop { get; set; }

    public bool IsPlaying { get; private set; }

    public void Play()
    {
        if (Clip is null || !Clip.Exists)
        {
            return;
        }

        if (!string.Equals(Clip.Format, "WAV", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _soundPlayer?.Dispose();
        _soundPlayer = new SoundPlayer(Clip.Path);
        if (Loop)
        {
            _soundPlayer.PlayLooping();
        }
        else
        {
            _soundPlayer.Play();
        }

        IsPlaying = true;
    }

    public void Stop()
    {
        _soundPlayer?.Stop();
        IsPlaying = false;
    }

    public override void Update(Time time)
    {
        if (!_started && PlayOnStart)
        {
            _started = true;
            Play();
        }
    }

    public void Dispose()
    {
        _soundPlayer?.Dispose();
    }
}
