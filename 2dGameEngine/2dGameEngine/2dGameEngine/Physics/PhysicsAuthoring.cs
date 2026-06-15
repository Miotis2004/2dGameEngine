using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Physics;

public enum PhysicsMaterialCombineMode { Average, Minimum, Maximum, Multiply }
public enum RigidbodyConstraint2D { None = 0, FreezePositionX = 1, FreezePositionY = 2, FreezeRotation = 4 }
public enum PhysicsJoint2DType { Distance, Hinge, Spring, Motor }

public sealed class PhysicsMaterial2D
{
    public string Name { get; set; } = "Default Physics Material";
    public float Friction { get; set; } = 0.4f;
    public float Bounciness { get; set; }
    public float Density { get; set; } = 1.0f;
    public PhysicsMaterialCombineMode FrictionCombine { get; set; } = PhysicsMaterialCombineMode.Average;
    public PhysicsMaterialCombineMode BounceCombine { get; set; } = PhysicsMaterialCombineMode.Maximum;
}

public sealed class PhysicsLayerMatrix
{
    private readonly bool[,] _matrix = new bool[32, 32];
    public PhysicsLayerMatrix() { for (int a = 0; a < 32; a++) for (int b = 0; b < 32; b++) _matrix[a, b] = true; }
    public bool CanCollide(int a, int b) => IsValid(a) && IsValid(b) && _matrix[a, b];
    public void SetCollision(int a, int b, bool enabled) { if (!IsValid(a) || !IsValid(b)) return; _matrix[a, b] = enabled; _matrix[b, a] = enabled; }
    private static bool IsValid(int layer) => layer is >= 0 and < 32;
}

public sealed record PhysicsContact(Entity Body, Entity Other, Vector2 Point, Vector2 Normal, float Penetration, RectangleF BroadphaseBounds);
public sealed record PhysicsDebugSnapshot(IReadOnlyList<PhysicsContact> Contacts, IReadOnlyList<RectangleF> BroadphaseBounds, IReadOnlyList<Entity> SleepingBodies, int StepIndex, float FixedDeltaSeconds);

public sealed class CircleCollider2D(float radius) : Collider2D
{
    private float _radius = MathF.Max(0.01f, radius);
    public float Radius { get => _radius; set => _radius = MathF.Max(0.01f, value); }
    public override RectangleF GetBounds() { Vector2 c = WorldCenter; return new RectangleF(c.X - Radius, c.Y - Radius, Radius * 2.0f, Radius * 2.0f); }
}

public sealed class CapsuleCollider2D(Vector2 size) : Collider2D
{
    private Vector2 _size = new(MathF.Max(1.0f, size.X), MathF.Max(1.0f, size.Y));
    public Vector2 Size { get => _size; set => _size = new Vector2(MathF.Max(1.0f, value.X), MathF.Max(1.0f, value.Y)); }
    public override RectangleF GetBounds() { Vector2 c = WorldCenter; return new RectangleF(c.X - Size.X / 2.0f, c.Y - Size.Y / 2.0f, Size.X, Size.Y); }
}

public sealed class PolygonCollider2D : Collider2D
{
    public List<Vector2> Points { get; } = [new(-24, -24), new(24, -24), new(24, 24), new(-24, 24)];
    public override RectangleF GetBounds()
    {
        Vector2 center = WorldCenter;
        if (Points.Count == 0) return new RectangleF(center.X, center.Y, 1.0f, 1.0f);
        float minX = Points.Min(p => p.X + center.X), maxX = Points.Max(p => p.X + center.X), minY = Points.Min(p => p.Y + center.Y), maxY = Points.Max(p => p.Y + center.Y);
        return new RectangleF(minX, minY, maxX - minX, maxY - minY);
    }
}

public sealed class PhysicsJoint2D : Component
{
    public PhysicsJoint2DType JointType { get; set; } = PhysicsJoint2DType.Distance;
    public Entity? ConnectedEntity { get; set; }
    public Vector2 Anchor { get; set; }
    public Vector2 ConnectedAnchor { get; set; }
    public float Distance { get; set; } = 96.0f;
    public float Frequency { get; set; } = 4.0f;
    public float DampingRatio { get; set; } = 0.5f;
    public float MotorSpeed { get; set; }
    public float MaxMotorForce { get; set; } = 1000.0f;
}
