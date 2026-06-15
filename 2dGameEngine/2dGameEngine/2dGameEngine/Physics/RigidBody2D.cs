using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Physics;

/// <summary>
/// Adds velocity, gravity, and collision response state to an entity.
/// </summary>
public sealed class RigidBody2D : Component
{
    /// <summary>
    /// Gets or sets the current velocity in world units per second.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Gets or sets the gravity multiplier applied by the physics system.
    /// </summary>
    public float GravityScale { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets a value indicating whether physics integration and collision response should be skipped.
    /// </summary>
    public bool IsKinematic { get; set; }

    /// <summary>Gets or sets body constraints authored in the editor.</summary>
    public RigidbodyConstraint2D Constraints { get; set; }

    /// <summary>Gets or sets whether the body is sleeping and skipped by debug diagnostics.</summary>
    public bool IsSleeping { get; set; }

    /// <summary>
    /// Gets a value indicating whether the body touched walkable ground during the latest physics step.
    /// </summary>
    public bool IsGrounded { get; internal set; }
}
