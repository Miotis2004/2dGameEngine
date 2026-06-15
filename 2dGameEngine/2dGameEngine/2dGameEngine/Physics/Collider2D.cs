using System.Drawing;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Physics;

/// <summary>
/// Base type for two-dimensional collision shapes attached to entities.
/// </summary>
public abstract class Collider2D : Component
{
    /// <summary>
    /// Gets or sets an offset from the owning entity position to the collider center.
    /// </summary>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this collider reports overlap only and skips collision resolution.
    /// </summary>
    public bool IsTrigger { get; set; }

    /// <summary>Gets or sets the physics material used for friction, bounce, and density authoring.</summary>
    public PhysicsMaterial2D Material { get; set; } = new();

    /// <summary>Gets or sets the collision layer used by the physics collision matrix.</summary>
    public int Layer { get; set; }

    /// <summary>
    /// Gets the collider bounds in world space.
    /// </summary>
    /// <returns>The collider world-space bounds.</returns>
    public abstract RectangleF GetBounds();

    /// <summary>
    /// Gets the world-space center for the collider.
    /// </summary>
    protected Vector2 WorldCenter => (Entity?.Transform.Value.Position ?? Vector2.Zero) + Offset;
}
