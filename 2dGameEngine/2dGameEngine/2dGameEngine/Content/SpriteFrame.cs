using System.Drawing;

namespace _2dGameEngine.Content;

/// <summary>
/// Describes a named rectangular region inside a texture atlas.
/// </summary>
/// <param name="Name">The frame name within its sprite sheet.</param>
/// <param name="Texture">The texture that contains the frame pixels.</param>
/// <param name="SourceRectangle">The source rectangle within the texture.</param>
/// <param name="SpriteSheetAssetPath">The content-relative sprite sheet metadata path, when available.</param>
public sealed record SpriteFrame(string Name, TextureAsset Texture, Rectangle SourceRectangle, string? SpriteSheetAssetPath = null);
