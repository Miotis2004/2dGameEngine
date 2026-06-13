using System.Numerics;

namespace _2dGameEngine.Core;

/// <summary>
/// Simple Phase 1 component that moves its entity each frame to prove updates are flowing.
/// </summary>
/// <param name="velocity">The movement velocity in units per second.</param>
public sealed class EntityMotionComponent(Vector2 velocity) : Component
{
    /// <summary>
    /// Gets or sets the movement velocity in units per second.
    /// </summary>
    public Vector2 Velocity { get; set; } = velocity;

    /// <inheritdoc />
    public override void Update(Time time)
    {
        if (Entity is null)
        {
            return;
        }

        Entity.Transform.Value.Position += Velocity * (float)time.DeltaTime.TotalSeconds;
    }
}
