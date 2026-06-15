using System;
using System.Collections.Generic;
using System.Linq;

namespace _2dGameEngine.Audio;

/// <summary>
/// Routes audio sources through named mixer groups and tracks editor-adjustable levels.
/// </summary>
public sealed class AudioMixer
{
    private readonly Dictionary<string, AudioMixerGroup> _groups = new(StringComparer.OrdinalIgnoreCase);

    public AudioMixer()
    {
        Master = GetOrCreateGroup("Master");
        GetOrCreateGroup("Music");
        GetOrCreateGroup("Sfx");
        GetOrCreateGroup("Ambience");
        GetOrCreateGroup("Ui");
    }

    public AudioMixerGroup Master { get; }

    public IReadOnlyList<AudioMixerGroup> Groups => _groups.Values.OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public AudioMixerGroup GetOrCreateGroup(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string normalized = name.Trim();
        if (_groups.TryGetValue(normalized, out AudioMixerGroup? group))
        {
            return group;
        }

        group = new AudioMixerGroup(normalized);
        _groups.Add(normalized, group);
        return group;
    }

    public float GetEffectiveVolume(string groupName)
    {
        AudioMixerGroup group = GetOrCreateGroup(groupName);
        if (Master.IsMuted || group.IsMuted)
        {
            return 0.0f;
        }

        bool anySolo = _groups.Values.Any(candidate => candidate.IsSolo);
        if (anySolo && !group.IsSolo && !ReferenceEquals(group, Master))
        {
            return 0.0f;
        }

        return Math.Clamp(Master.Volume * group.Volume, 0.0f, 1.0f);
    }
}

/// <summary>
/// Represents a named bus in the audio mixer.
/// </summary>
public sealed class AudioMixerGroup
{
    internal AudioMixerGroup(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public float Volume { get; set; } = 1.0f;

    public bool IsMuted { get; set; }

    public bool IsSolo { get; set; }
}
