using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Input;

namespace _2dGameEngine.Core;

/// <summary>
/// Moves an entity in response to keyboard input.
/// </summary>
/// <param name="speed">The movement speed in units per second.</param>
public sealed class EntityInputMovementComponent(float speed) : Component
{
    /// <summary>
    /// Gets or sets the movement speed in units per second.
    /// </summary>
    public float Speed { get; set; } = speed;

    /// <inheritdoc />
    public override void Update(Time time, InputState input)
    {
        if (Entity is null)
        {
            return;
        }

        Vector2 direction = Vector2.Zero;

        if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left))
        {
            direction.X -= 1.0f;
        }

        if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right))
        {
            direction.X += 1.0f;
        }

        if (input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up))
        {
            direction.Y -= 1.0f;
        }

        if (input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down))
        {
            direction.Y += 1.0f;
        }

        if (direction == Vector2.Zero)
        {
            return;
        }

        direction = Vector2.Normalize(direction);
        Entity.Transform.Value.Position += direction * Speed * (float)time.DeltaTime.TotalSeconds;
    }
}
