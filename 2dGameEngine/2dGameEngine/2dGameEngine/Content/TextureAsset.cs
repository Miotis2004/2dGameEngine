using System;
using System.Drawing;

namespace _2dGameEngine.Content;

/// <summary>
/// Represents a loaded texture resource and its source path.
/// </summary>
public sealed class TextureAsset : IDisposable
{
    internal TextureAsset(string assetPath, Image image)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        ArgumentNullException.ThrowIfNull(image);
        AssetPath = assetPath;
        Image = image;
    }

    /// <summary>
    /// Gets the normalized content-relative asset path.
    /// </summary>
    public string AssetPath { get; }

    /// <summary>
    /// Gets the loaded image data.
    /// </summary>
    public Image Image { get; }

    /// <inheritdoc />
    public void Dispose() => Image.Dispose();
}
