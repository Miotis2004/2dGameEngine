using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Physics;

/// <summary>
/// Applies gravity, integrates rigid body movement, and resolves axis-aligned box collisions.
/// </summary>
public sealed class PhysicsSystem
{
    private const float GroundNormalThreshold = -0.5f;

    /// <summary>
    /// Gets or sets the acceleration applied to dynamic bodies in world units per second squared.
    /// </summary>
    public Vector2 Gravity { get; set; } = new(0.0f, 980.0f);

    /// <summary>
    /// Advances physics simulation for the provided scene.
    /// </summary>
    /// <param name="scene">The scene to simulate.</param>
    /// <param name="time">Frame timing information.</param>
    public void Step(Scene scene, Time time)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(time);

        float deltaSeconds = MathF.Min((float)time.DeltaTime.TotalSeconds, 0.05f);
        if (deltaSeconds <= 0.0f)
        {
            return;
        }

        List<Collider2D> colliders = [.. CollisionWorld.GetColliders(scene)];
        foreach (RigidBody2D body in GetDynamicBodies(scene))
        {
            body.IsGrounded = false;
            body.Velocity += Gravity * body.GravityScale * deltaSeconds;
            body.Entity!.Transform.Value.Position += body.Velocity * deltaSeconds;
            ResolveCollisions(body, colliders);
        }
    }

    private static IEnumerable<RigidBody2D> GetDynamicBodies(Scene scene)
    {
        return scene.Entities.Where(entity => entity.IsEnabled)
            .SelectMany(entity => entity.Components.OfType<RigidBody2D>())
            .Where(body => body.IsEnabled && !body.IsKinematic && body.Entity is not null);
    }

    private static void ResolveCollisions(RigidBody2D body, IReadOnlyList<Collider2D> colliders)
    {
        Entity entity = body.Entity!;
        Collider2D? bodyCollider = entity.Components.OfType<Collider2D>().FirstOrDefault(collider => collider.IsEnabled && !collider.IsTrigger);
        if (bodyCollider is null)
        {
            return;
        }

        foreach (Collider2D other in colliders)
        {
            if (ReferenceEquals(other, bodyCollider) || other.IsTrigger || other.Entity is null)
            {
                continue;
            }

            RigidBody2D? otherBody = other.Entity.GetComponent<RigidBody2D>();
            if (otherBody is { IsKinematic: false })
            {
                continue;
            }

            RectangleF current = bodyCollider.GetBounds();
            RectangleF target = other.GetBounds();
            if (!CollisionWorld.Intersects(current, target))
            {
                continue;
            }

            Vector2 correction = GetMinimumTranslation(current, target);
            entity.Transform.Value.Position += correction;

            if (MathF.Abs(correction.X) > 0.0f)
            {
                body.Velocity = new Vector2(0.0f, body.Velocity.Y);
            }

            if (MathF.Abs(correction.Y) > 0.0f)
            {
                if (correction.Y < GroundNormalThreshold)
                {
                    body.IsGrounded = true;
                }

                body.Velocity = new Vector2(body.Velocity.X, 0.0f);
            }
        }
    }

    private static Vector2 GetMinimumTranslation(RectangleF moving, RectangleF target)
    {
        float moveLeft = target.Left - moving.Right;
        float moveRight = target.Right - moving.Left;
        float moveUp = target.Top - moving.Bottom;
        float moveDown = target.Bottom - moving.Top;

        float resolveX = MathF.Abs(moveLeft) < MathF.Abs(moveRight) ? moveLeft : moveRight;
        float resolveY = MathF.Abs(moveUp) < MathF.Abs(moveDown) ? moveUp : moveDown;

        return MathF.Abs(resolveX) < MathF.Abs(resolveY) ? new Vector2(resolveX, 0.0f) : new Vector2(0.0f, resolveY);
    }
}
