using System.Drawing;

namespace _2dGameEngine.Content;

/// <summary>
/// Describes a named rectangular region inside a texture atlas.
/// </summary>
public sealed record SpriteFrame(string Name, TextureAsset Texture, Rectangle SourceRectangle);
