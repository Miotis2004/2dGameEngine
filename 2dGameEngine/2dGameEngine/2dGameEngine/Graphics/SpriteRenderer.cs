using System.Drawing;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Renders a simple colored rectangle for an entity until texture-backed sprites are introduced.
/// </summary>
/// <param name="size">The sprite size in world units.</param>
/// <param name="color">The sprite fill color.</param>
public sealed class SpriteRenderer(Vector2 size, Color color) : Component
{
    /// <summary>
    /// Gets or sets the sprite size in world units.
    /// </summary>
    public Vector2 Size { get; set; } = size;

    /// <summary>
    /// Gets or sets the sprite fill color.
    /// </summary>
    public Color Color { get; set; } = color;

    /// <summary>
    /// Gets or sets the optional sprite outline color.
    /// </summary>
    public Color? OutlineColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the draw order. Higher values are rendered later.
    /// </summary>
    public int SortingOrder { get; set; }
}
