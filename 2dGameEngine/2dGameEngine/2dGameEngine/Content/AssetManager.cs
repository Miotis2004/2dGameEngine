using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using _2dGameEngine.Animation;

namespace _2dGameEngine.Content;

/// <summary>
/// Loads and caches assets from a project content root.
/// </summary>
public sealed class AssetManager : IDisposable
{
    private readonly Dictionary<string, TextureAsset> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SpriteSheetAsset> _spriteSheets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AnimationClip> _animationClips = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AnimatorController> _animatorControllers = new(StringComparer.OrdinalIgnoreCase);

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
            ?? throw new Exception($"Sprite sheet '{normalizedPath}' is empty or invalid.");

        if (string.IsNullOrWhiteSpace(document.Texture))
        {
            throw new InvalidDataException($"Sprite sheet '{normalizedPath}' does not specify a texture.");
        }

        if (document.Sprites is null)
        {
            throw new InvalidDataException($"Sprite sheet '{normalizedPath}' does not define any sprites.");
        }

        TextureAsset texture = LoadTexture(CombineAssetPath(Path.GetDirectoryName(normalizedPath), document.Texture));
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
            frames[sprite.Name] = new SpriteFrame(sprite.Name, texture, source, normalizedPath);
        }

        SpriteSheetAsset spriteSheet = new(normalizedPath, texture, frames);
        _spriteSheets[normalizedPath] = spriteSheet;
        return spriteSheet;
    }


    /// <summary>
    /// Loads animation clip metadata and returns the cached instance on subsequent calls.
    /// </summary>
    public AnimationClip LoadAnimationClip(string assetPath)
    {
        string normalizedPath = NormalizeAssetPath(assetPath);
        if (_animationClips.TryGetValue(normalizedPath, out AnimationClip? cached))
        {
            return cached;
        }

        string fullPath = ResolvePath(normalizedPath);
        AnimationClipDocument document = JsonSerializer.Deserialize<AnimationClipDocument>(File.ReadAllText(fullPath), JsonOptions)
            ?? throw new InvalidDataException($"Animation clip '{normalizedPath}' is empty or invalid.");

        if (string.IsNullOrWhiteSpace(document.Name))
        {
            throw new InvalidDataException($"Animation clip '{normalizedPath}' does not specify a name.");
        }

        if (string.IsNullOrWhiteSpace(document.SpriteSheet))
        {
            throw new InvalidDataException($"Animation clip '{normalizedPath}' does not specify a sprite sheet.");
        }

        if (document.Frames is null || document.Frames.Length == 0)
        {
            throw new InvalidDataException($"Animation clip '{normalizedPath}' does not define any frames.");
        }

        SpriteSheetAsset spriteSheet = LoadSpriteSheet(CombineAssetPath(Path.GetDirectoryName(normalizedPath), document.SpriteSheet));
        List<AnimationFrame> frames = [];
        foreach (AnimationFrameDocument frame in document.Frames)
        {
            if (string.IsNullOrWhiteSpace(frame.Sprite))
            {
                throw new InvalidDataException($"Animation clip '{normalizedPath}' contains a frame without a sprite name.");
            }

            if (frame.DurationMilliseconds <= 0)
            {
                throw new InvalidDataException($"Animation frame '{frame.Sprite}' in '{normalizedPath}' must have a positive duration.");
            }

            frames.Add(new AnimationFrame(spriteSheet.GetFrame(frame.Sprite), TimeSpan.FromMilliseconds(frame.DurationMilliseconds)));
        }

        AnimationClip clip = new(document.Name, frames, document.Loop, normalizedPath, (document.Events ?? []).Select(animationEvent =>
            new AnimationEvent(animationEvent.Name, TimeSpan.FromMilliseconds(animationEvent.TimeMilliseconds), animationEvent.Arguments)));
        _animationClips[normalizedPath] = clip;
        return clip;
    }

    /// <summary>
    /// Loads animator controller metadata and returns the cached instance on subsequent calls.
    /// </summary>
    public AnimatorController LoadAnimatorController(string assetPath)
    {
        string normalizedPath = NormalizeAssetPath(assetPath);
        if (_animatorControllers.TryGetValue(normalizedPath, out AnimatorController? cached))
        {
            return cached;
        }

        string fullPath = ResolvePath(normalizedPath);
        AnimatorControllerDocument document = JsonSerializer.Deserialize<AnimatorControllerDocument>(File.ReadAllText(fullPath), JsonOptions)
            ?? throw new InvalidDataException($"Animator controller '{normalizedPath}' is empty or invalid.");

        AnimatorController controller = new(document.Name, document.DefaultState, normalizedPath);
        foreach (AnimatorParameterDocument parameter in document.Parameters ?? [])
        {
            controller.AddParameter(new AnimatorParameter(parameter.Name, parameter.Type, parameter.FloatValue, parameter.IntValue, parameter.BoolValue));
        }

        foreach (AnimatorStateDocument state in document.States ?? [])
        {
            controller.AddState(new AnimatorState(state.Name, LoadAnimationClip(CombineAssetPath(Path.GetDirectoryName(normalizedPath), state.Clip)), state.Speed <= 0 ? 1.0f : state.Speed));
        }

        foreach (AnimatorTransitionDocument transition in document.Transitions ?? [])
        {
            controller.AddTransition(new AnimatorTransition(transition.FromState, transition.ToState, TimeSpan.FromMilliseconds(Math.Max(0, transition.ExitTimeMilliseconds)), transition.Conditions ?? []));
        }

        controller.GetState(controller.DefaultState);
        _animatorControllers[normalizedPath] = controller;
        return controller;
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
        _animationClips.Clear();
        _animatorControllers.Clear();
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

    private string ResolvePath(string normalizedPath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(ContentRoot, normalizedPath));
        string contentRoot = ContentRoot.EndsWith(Path.DirectorySeparatorChar)
            ? ContentRoot
            : ContentRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Asset paths must stay inside the content root.");
        }

        return File.Exists(fullPath) ? fullPath : throw new FileNotFoundException($"Asset '{normalizedPath}' was not found.", fullPath);
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        return assetPath.Replace('\\', '/').TrimStart('/');
    }

    private static string CombineAssetPath(string? directory, string assetPath)
    {
        string normalizedAssetPath = NormalizeAssetPath(assetPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return normalizedAssetPath;
        }

        string normalizedDirectory = NormalizeAssetPath(directory);

        // If the asset path already starts with the directory segment, don't prepend it again
        if (normalizedAssetPath.StartsWith(normalizedDirectory + '/', StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedAssetPath, normalizedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedAssetPath;
        }

        return NormalizeAssetPath(Path.Combine(directory, normalizedAssetPath));
    }

    private sealed record TextureDocument(int Width, int Height, string? Fill, TextureRegionDocument[]? Regions);

    private sealed record TextureRegionDocument(int X, int Y, int Width, int Height, string Color);

    private sealed record SpriteSheetDocument(string Texture, SpriteFrameDocument[] Sprites);

    private sealed record SpriteFrameDocument(string Name, int X, int Y, int Width, int Height);

    private sealed record AnimationClipDocument(string Name, string SpriteSheet, bool Loop, AnimationFrameDocument[] Frames, AnimationEventDocument[]? Events);

    private sealed record AnimationFrameDocument(string Sprite, double DurationMilliseconds);

    private sealed record AnimationEventDocument(string Name, double TimeMilliseconds, Dictionary<string, string>? Arguments);

    private sealed record AnimatorControllerDocument(string Name, string DefaultState, AnimatorParameterDocument[]? Parameters, AnimatorStateDocument[]? States, AnimatorTransitionDocument[]? Transitions);

    private sealed record AnimatorParameterDocument(string Name, AnimatorParameterType Type, float FloatValue, int IntValue, bool BoolValue);

    private sealed record AnimatorStateDocument(string Name, string Clip, float Speed);

    private sealed record AnimatorTransitionDocument(string FromState, string ToState, double ExitTimeMilliseconds, AnimatorCondition[]? Conditions);
}
