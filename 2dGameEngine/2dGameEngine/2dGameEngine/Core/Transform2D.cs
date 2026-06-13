using System.Numerics;

namespace _2dGameEngine.Core;

/// <summary>
/// Describes an entity's position, rotation, and scale in two-dimensional space.
/// </summary>
public sealed class Transform2D
{
    /// <summary>
    /// Gets or sets the entity position in world space.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the entity rotation in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the entity scale.
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;
}
