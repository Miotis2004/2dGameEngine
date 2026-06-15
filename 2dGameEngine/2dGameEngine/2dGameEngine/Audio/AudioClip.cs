using System;
using System.IO;

namespace _2dGameEngine.Audio;

/// <summary>
/// Describes an imported audio asset that can be referenced by runtime audio sources.
/// </summary>
public sealed class AudioClip
{
    public AudioClip(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Format = System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
    }

    public string Name { get; }

    public string Path { get; }

    public string Format { get; }

    public bool Exists => File.Exists(Path);
}
