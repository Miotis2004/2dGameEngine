using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace _2dGameEngine.Content;

/// <summary>
/// Loads and caches assets from a project content root.
/// </summary>
public sealed class AssetManager : IDisposable
{
    private readonly Dictionary<string, TextureAsset> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SpriteSheetAsset> _spriteSheets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetManager"/> class.
    /// </summary>
    public AssetManager(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        ContentRoot = Path.GetFullPath(contentRoot);
    }

    /// <summary>
    /// Gets the absolute directory assets are loaded from.
    /// </summary>
    public string ContentRoot { get; }

    /// <summary>
    /// Loads an image texture and returns the cached instance on subsequent calls.
    /// </summary>
    public TextureAsset LoadTexture(string assetPath)
    {
        string normalizedPath = NormalizeAssetPath(assetPath);
        if (_textures.TryGetValue(normalizedPath, out TextureAsset? cached))
        {
            return cached;
        }

        string fullPath = ResolvePath(normalizedPath);
        Image image = Path.GetExtension(fullPath).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? LoadJsonTexture(fullPath, normalizedPath)
            : LoadImageTexture(fullPath);
        TextureAsset texture = new(normalizedPath, image);
        _textures[normalizedPath] = texture;
        return texture;
    }

    /// <summary>
    /// Loads sprite sheet metadata and returns the cached instance on subsequent calls.
    /// </summary>
    public SpriteSheetAsset LoadSpriteSheet(string assetPath)
    {
        string normalizedPath = NormalizeAssetPath(assetPath);
        if (_spriteSheets.TryGetValue(normalizedPath, out SpriteSheetAsset? cached))
        {
            return cached;
        }

        string fullPath = ResolvePath(normalizedPath);
        SpriteSheetDocument document = JsonSerializer.Deserialize<SpriteSheetDocument>(File.ReadAllText(fullPath), JsonOptions)
            ?? throw new InvalidDataException($"Sprite sheet '{normalizedPath}' is empty or invalid.");

        if (string.IsNullOrWhiteSpace(document.Texture))
        {
            throw new InvalidDataException($"Sprite sheet '{normalizedPath}' does not specify a texture.");
        }

        if (document.Sprites is null)
        {
            throw new InvalidDataException($"Sprite sheet '{normalizedPath}' does not define any sprites.");
        }

        TextureAsset texture = LoadTexture(ResolveReferencedAssetPath(Path.GetDirectoryName(normalizedPath), document.Texture));
        Dictionary<string, SpriteFrame> frames = new(StringComparer.OrdinalIgnoreCase);
        foreach (SpriteFrameDocument sprite in document.Sprites)
        {
            if (string.IsNullOrWhiteSpace(sprite.Name))
            {
                throw new InvalidDataException($"Sprite sheet '{normalizedPath}' contains a frame without a name.");
            }

            if (sprite.Width <= 0 || sprite.Height <= 0)
            {
                throw new InvalidDataException($"Sprite frame '{sprite.Name}' in '{normalizedPath}' must have positive dimensions.");
            }

            Rectangle source = new(sprite.X, sprite.Y, sprite.Width, sprite.Height);
            frames[sprite.Name] = new SpriteFrame(sprite.Name, texture, source);
        }

        SpriteSheetAsset spriteSheet = new(normalizedPath, texture, frames);
        _spriteSheets[normalizedPath] = spriteSheet;
        return spriteSheet;
    }

    /// <summary>
    /// Clears all cached assets and releases loaded textures.
    /// </summary>
    public void Clear()
    {
        foreach (TextureAsset texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
        _spriteSheets.Clear();
    }

    /// <inheritdoc />
    public void Dispose() => Clear();

    private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    private static Image LoadImageTexture(string fullPath)
    {
        using FileStream stream = File.OpenRead(fullPath);
        using Image loadedImage = Image.FromStream(stream);
        return new Bitmap(loadedImage);
    }

    private static Image LoadJsonTexture(string fullPath, string normalizedPath)
    {
        TextureDocument document = JsonSerializer.Deserialize<TextureDocument>(File.ReadAllText(fullPath), JsonOptions)
            ?? throw new InvalidDataException($"Texture '{normalizedPath}' is empty or invalid.");

        if (document.Width <= 0 || document.Height <= 0)
        {
            throw new InvalidDataException($"Texture '{normalizedPath}' must have positive dimensions.");
        }

        Bitmap bitmap = new(document.Width, document.Height);
        using System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.Clear(ParseColor(document.Fill ?? "#00000000", normalizedPath));

        foreach (TextureRegionDocument region in document.Regions ?? [])
        {
            if (region.Width <= 0 || region.Height <= 0)
            {
                throw new InvalidDataException($"Texture region in '{normalizedPath}' must have positive dimensions.");
            }

            using SolidBrush brush = new(ParseColor(region.Color, normalizedPath));
            graphics.FillRectangle(brush, region.X, region.Y, region.Width, region.Height);
        }

        return bitmap;
    }

    private static Color ParseColor(string? value, string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Texture '{normalizedPath}' contains an empty color value.");
        }

        try
        {
            if (value.StartsWith('#') && value.Length == 9)
            {
                int alpha = int.Parse(value[1..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int red = int.Parse(value[3..5], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int green = int.Parse(value[5..7], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int blue = int.Parse(value[7..9], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return Color.FromArgb(alpha, red, green, blue);
            }

            return ColorTranslator.FromHtml(value);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        {
            throw new InvalidDataException($"Texture '{normalizedPath}' contains invalid color value '{value}'.", ex);
        }
    }

    private string ResolveReferencedAssetPath(string? referringDirectory, string assetPath)
    {
        string directPath = NormalizeAssetPath(assetPath);
        if (AssetExists(directPath))
        {
            return directPath;
        }

        return CombineAssetPath(referringDirectory, directPath);
    }

    private bool AssetExists(string normalizedPath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(ContentRoot, normalizedPath));
        return IsInsideContentRoot(fullPath) && File.Exists(fullPath);
    }

    private string ResolvePath(string normalizedPath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(ContentRoot, normalizedPath));
        if (!IsInsideContentRoot(fullPath))
        {
            throw new InvalidOperationException("Asset paths must stay inside the content root.");
        }

        return File.Exists(fullPath) ? fullPath : throw new FileNotFoundException($"Asset '{normalizedPath}' was not found.", fullPath);
    }

    private bool IsInsideContentRoot(string fullPath)
    {
        string contentRoot = ContentRoot.EndsWith(Path.DirectorySeparatorChar)
            ? ContentRoot
            : ContentRoot + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        return assetPath.Replace('\\', '/').TrimStart('/');
    }

    private static string CombineAssetPath(string? directory, string assetPath)
    {
        string normalizedAssetPath = NormalizeAssetPath(assetPath);
        return string.IsNullOrWhiteSpace(directory)
            ? normalizedAssetPath
            : NormalizeAssetPath(Path.Combine(directory, normalizedAssetPath));
    }

    private sealed record TextureDocument(int Width, int Height, string? Fill, TextureRegionDocument[]? Regions);

    private sealed record TextureRegionDocument(int X, int Y, int Width, int Height, string Color);

    private sealed record SpriteSheetDocument(string Texture, SpriteFrameDocument[] Sprites);

    private sealed record SpriteFrameDocument(string Name, int X, int Y, int Width, int Height);
}
