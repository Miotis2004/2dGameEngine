using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Physics;

/// <summary>
/// Applies gravity, integrates rigid body movement, and resolves axis-aligned box collisions.
/// </summary>
public sealed class PhysicsSystem
{
    private const float GroundNormalThreshold = -0.5f;
    private readonly List<PhysicsContact> _contacts = [];
    private readonly List<Collider2D> _colliders = [];
    private readonly List<Collider2D> _candidateColliders = [];
    private readonly List<RigidBody2D> _dynamicBodies = [];
    private readonly List<RectangleF> _debugBounds = [];
    private readonly List<Entity> _sleepingBodies = [];
    private readonly SpatialHashGrid _broadPhase = new();
    private int _stepIndex;

    /// <summary>
    /// Gets or sets the acceleration applied to dynamic bodies in world units per second squared.
    /// </summary>
    public Vector2 Gravity { get; set; } = new(0.0f, 980.0f);

    public float FixedDeltaSeconds { get; set; } = 1.0f / 60.0f;

    public bool UseFixedStep { get; set; } = true;

    public PhysicsLayerMatrix LayerMatrix { get; } = new();

    public PhysicsDebugSnapshot LastDebugSnapshot { get; private set; } = new([], [], [], 0, 1.0f / 60.0f);

    /// <summary>
    /// Advances physics simulation for the provided scene.
    /// </summary>
    /// <param name="scene">The scene to simulate.</param>
    /// <param name="time">Frame timing information.</param>
    public void Step(Scene scene, Time time)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(time);

        float deltaSeconds = UseFixedStep ? FixedDeltaSeconds : MathF.Min((float)time.DeltaTime.TotalSeconds, 0.05f);
        _contacts.Clear();
        if (deltaSeconds <= 0.0f)
        {
            return;
        }

        CollisionWorld.GetColliders(scene, _colliders);
        GetDynamicBodies(scene, _dynamicBodies);
        _broadPhase.Rebuild(_colliders);
        for (int i = 0; i < _dynamicBodies.Count; i++)
        {
            RigidBody2D body = _dynamicBodies[i];
            if (body.IsSleeping)
            {
                continue;
            }

            body.IsGrounded = false;
            body.Velocity += Gravity * body.GravityScale * deltaSeconds;
            Vector2 movement = body.Velocity * deltaSeconds;
            if (body.Constraints.HasFlag(RigidbodyConstraint2D.FreezePositionX)) movement.X = 0.0f;
            if (body.Constraints.HasFlag(RigidbodyConstraint2D.FreezePositionY)) movement.Y = 0.0f;
            body.Entity!.Transform.Value.Position += movement;
            ResolveCollisions(body);
        }

        _debugBounds.Clear();
        for (int i = 0; i < _colliders.Count; i++) _debugBounds.Add(_colliders[i].GetBounds());
        _sleepingBodies.Clear();
        for (int i = 0; i < _dynamicBodies.Count; i++)
        {
            if (_dynamicBodies[i].IsSleeping && _dynamicBodies[i].Entity is not null) _sleepingBodies.Add(_dynamicBodies[i].Entity!);
        }
        LastDebugSnapshot = new PhysicsDebugSnapshot([.. _contacts], [.. _debugBounds], [.. _sleepingBodies], ++_stepIndex, deltaSeconds);
    }

    private static void GetDynamicBodies(Scene scene, List<RigidBody2D> results)
    {
        results.Clear();
        for (int i = 0; i < scene.Entities.Count; i++)
        {
            Entity entity = scene.Entities[i];
            if (!entity.IsEnabled) continue;
            for (int c = 0; c < entity.Components.Count; c++)
            {
                if (entity.Components[c] is RigidBody2D { IsEnabled: true, IsKinematic: false, Entity: not null } body)
                {
                    results.Add(body);
                }
            }
        }
    }

    private void ResolveCollisions(RigidBody2D body)
    {
        Entity entity = body.Entity!;
        Collider2D? bodyCollider = null;
        for (int i = 0; i < entity.Components.Count; i++)
        {
            if (entity.Components[i] is Collider2D { IsEnabled: true, IsTrigger: false } collider)
            {
                bodyCollider = collider;
                break;
            }
        }
        if (bodyCollider is null)
        {
            return;
        }

        _broadPhase.Query(bodyCollider.GetBounds(), _candidateColliders);
        for (int i = 0; i < _candidateColliders.Count; i++)
        {
            Collider2D other = _candidateColliders[i];
            if (ReferenceEquals(other, bodyCollider) || other.IsTrigger || other.Entity is null || !LayerMatrix.CanCollide(bodyCollider.Layer, other.Layer))
            {
                continue;
            }

            RigidBody2D? otherBody = other.Entity.GetComponent<RigidBody2D>();
            if (otherBody is { IsKinematic: false })
            {
                continue;
            }

            if (other is TilemapCollider2D tilemapCollider)
            {
                ResolveTilemapCollision(body, bodyCollider, tilemapCollider);
                continue;
            }

            ResolveBoundsCollision(body, bodyCollider.GetBounds(), other.GetBounds());
        }
    }

    private void ResolveTilemapCollision(RigidBody2D body, Collider2D bodyCollider, TilemapCollider2D tilemapCollider)
    {
        foreach (RectangleF tileBounds in tilemapCollider.GetSolidTileBounds(bodyCollider.GetBounds()))
        {
            ResolveBoundsCollision(body, bodyCollider.GetBounds(), tileBounds);
        }
    }

    private void ResolveBoundsCollision(RigidBody2D body, RectangleF current, RectangleF target)
    {
        if (!CollisionWorld.Intersects(current, target))
        {
            return;
        }

        Entity entity = body.Entity!;
        Vector2 correction = GetMinimumTranslation(current, target);
        Vector2 normal = Vector2.Normalize(correction == Vector2.Zero ? new Vector2(0.0f, -1.0f) : correction);
        _contacts.Add(new PhysicsContact(entity, entity, new Vector2((MathF.Max(current.Left, target.Left) + MathF.Min(current.Right, target.Right)) / 2.0f, (MathF.Max(current.Top, target.Top) + MathF.Min(current.Bottom, target.Bottom)) / 2.0f), normal, correction.Length(), current));
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
