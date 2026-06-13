using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Core;
using _2dGameEngine.Input;

namespace _2dGameEngine.Physics;

/// <summary>
/// Drives a rigid body with horizontal platformer movement and ground jumping.
/// </summary>
/// <param name="moveSpeed">The horizontal movement speed in world units per second.</param>
/// <param name="jumpSpeed">The upward jump impulse in world units per second.</param>
public sealed class PlatformerMovementComponent(float moveSpeed, float jumpSpeed) : Component
{
    private RigidBody2D? _body;

    /// <summary>
    /// Gets or sets the horizontal movement speed in world units per second.
    /// </summary>
    public float MoveSpeed { get; set; } = moveSpeed;

    /// <summary>
    /// Gets or sets the upward jump speed in world units per second.
    /// </summary>
    public float JumpSpeed { get; set; } = jumpSpeed;

    /// <inheritdoc />
    protected override void OnAttached()
    {
        _body = Entity?.GetComponent<RigidBody2D>();
    }

    /// <inheritdoc />
    public override void Update(Time time, InputState input)
    {
        if (Entity is null)
        {
            return;
        }

        _body ??= Entity.GetComponent<RigidBody2D>();
        if (_body is null)
        {
            return;
        }

        float direction = 0.0f;
        if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left))
        {
            direction -= 1.0f;
        }

        if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right))
        {
            direction += 1.0f;
        }

        Vector2 velocity = _body.Velocity;
        velocity.X = direction * MoveSpeed;

        if (_body.IsGrounded && (input.WasKeyPressed(Keys.Space) || input.WasKeyPressed(Keys.W) || input.WasKeyPressed(Keys.Up)))
        {
            velocity.Y = -JumpSpeed;
            _body.IsGrounded = false;
        }

        _body.Velocity = velocity;
    }
}
