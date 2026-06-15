using System.Drawing;
using System.Numerics;
using _2dGameEngine.Content;
using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Primitive shapes available for color-backed sprites.
/// </summary>
public enum SpritePrimitiveType
{
    Rectangle,
    Circle,
    Triangle,
}

/// <summary>
/// Renders a texture-backed sprite or a simple colored primitive for an entity.
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
    /// Gets or sets the primitive shape used when no texture frame is assigned.
    /// </summary>
    public SpritePrimitiveType PrimitiveType { get; set; } = SpritePrimitiveType.Rectangle;

    /// <summary>
    /// Gets or sets the optional texture frame drawn for this sprite.
    /// </summary>
    public SpriteFrame? Frame { get; set; }

    /// <summary>
    /// Gets or sets the optional sprite outline color.
    /// </summary>
    public Color? OutlineColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the draw order. Higher values are rendered later.
    /// </summary>
    public int SortingOrder { get; set; }

    /// <summary>
    /// Gets or sets the render layer used by camera culling and 2D lights.
    /// </summary>
    public RenderLayerMask RenderLayer { get; set; } = RenderLayerMask.Default;

    /// <summary>
    /// Gets or sets the material controlling tint, blend, and lighting behavior.
    /// </summary>
    public Material2D Material { get; set; } = new();
}
