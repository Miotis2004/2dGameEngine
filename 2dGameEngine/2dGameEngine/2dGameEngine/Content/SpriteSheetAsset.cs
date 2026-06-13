using System;
using System.Collections.Generic;

namespace _2dGameEngine.Content;

/// <summary>
/// Represents a loaded sprite sheet and the named frames it exposes.
/// </summary>
public sealed class SpriteSheetAsset
{
    private readonly Dictionary<string, SpriteFrame> _frames;

    internal SpriteSheetAsset(string assetPath, TextureAsset texture, Dictionary<string, SpriteFrame> frames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(frames);
        AssetPath = assetPath;
        Texture = texture;
        _frames = frames;
    }

    /// <summary>
    /// Gets the normalized content-relative metadata path.
    /// </summary>
    public string AssetPath { get; }

    /// <summary>
    /// Gets the texture atlas used by this sprite sheet.
    /// </summary>
    public TextureAsset Texture { get; }

    /// <summary>
    /// Gets all frames keyed by name.
    /// </summary>
    public IReadOnlyDictionary<string, SpriteFrame> Frames => _frames;

    /// <summary>
    /// Gets a named sprite frame.
    /// </summary>
    public SpriteFrame GetFrame(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _frames.TryGetValue(name, out SpriteFrame? frame)
            ? frame
            : throw new KeyNotFoundException($"Sprite frame '{name}' was not found in '{AssetPath}'.");
    }
}
