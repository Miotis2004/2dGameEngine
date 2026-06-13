using System;
using System.Drawing;
using System.Numerics;

namespace _2dGameEngine.Physics;

/// <summary>
/// Represents an axis-aligned rectangular collider in world space.
/// </summary>
/// <param name="size">The collider size in world units.</param>
public sealed class BoxCollider2D(Vector2 size) : Collider2D
{
    private Vector2 _size = ValidateSize(size);

    /// <summary>
    /// Gets or sets the collider size in world units.
    /// </summary>
    public Vector2 Size
    {
        get => _size;
        set => _size = ValidateSize(value);
    }

    /// <inheritdoc />
    public override RectangleF GetBounds()
    {
        Vector2 center = WorldCenter;
        return new RectangleF(center.X - Size.X / 2.0f, center.Y - Size.Y / 2.0f, Size.X, Size.Y);
    }

    private static Vector2 ValidateSize(Vector2 size)
    {
        if (size.X <= 0.0f || size.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Collider size must be positive on both axes.");
        }

        return size;
    }
}
