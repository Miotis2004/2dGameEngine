using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Emits, simulates, and stores lightweight 2D particles for editor-authored visual effects.
/// </summary>
public sealed class ParticleSystem2D : Component
{
    private readonly List<Particle2D> _particles = [];
    private float _emitAccumulator;
    private readonly Random _random = new(17);

    /// <summary>Gets the currently alive particles.</summary>
    public IReadOnlyList<Particle2D> Particles => _particles;

    /// <summary>Gets or sets the number of particles spawned per second while looping.</summary>
    public float EmissionRate { get; set; } = 32.0f;

    /// <summary>Gets or sets the maximum number of live particles.</summary>
    public int MaxParticles { get; set; } = 160;

    /// <summary>Gets or sets the lifetime of each particle in seconds.</summary>
    public float Lifetime { get; set; } = 1.25f;

    /// <summary>Gets or sets the emitter cone angle in degrees.</summary>
    public float SpreadAngle { get; set; } = 55.0f;

    /// <summary>Gets or sets the initial particle speed in world units per second.</summary>
    public float StartSpeed { get; set; } = 140.0f;

    /// <summary>Gets or sets the particle start size in world units.</summary>
    public float StartSize { get; set; } = 18.0f;

    /// <summary>Gets or sets the particle end size in world units.</summary>
    public float EndSize { get; set; } = 2.0f;

    /// <summary>Gets or sets the acceleration applied to particles.</summary>
    public Vector2 Gravity { get; set; } = new(0.0f, 85.0f);

    /// <summary>Gets or sets the particle color at spawn.</summary>
    public Color StartColor { get; set; } = Color.FromArgb(240, 255, 214, 92);

    /// <summary>Gets or sets the particle color as it expires.</summary>
    public Color EndColor { get; set; } = Color.FromArgb(0, 255, 82, 32);

    /// <summary>Gets or sets the render sorting order.</summary>
    public int SortingOrder { get; set; } = 20;

    /// <summary>Gets or sets the render layer mask used by cameras.</summary>
    public RenderLayerMask RenderLayer { get; set; } = RenderLayerMask.Effects;

    /// <summary>Gets or sets a value indicating whether the emitter loops continuously.</summary>
    public bool Looping { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether particles are emitted automatically.</summary>
    public bool IsEmitting { get; set; } = true;

    /// <summary>Emits an immediate burst.</summary>
    public void EmitBurst(int count)
    {
        for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
        {
            SpawnParticle();
        }
    }

    public override void Update(Time time)
    {
        float delta = MathF.Max(0.0f, (float)time.DeltaTime.TotalSeconds);
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle2D particle = _particles[i];
            particle.Age += delta;
            if (particle.Age >= particle.Lifetime)
            {
                _particles.RemoveAt(i);
                continue;
            }

            particle.Velocity += Gravity * delta;
            particle.Position += particle.Velocity * delta;
            _particles[i] = particle;
        }

        if (!IsEmitting || (!Looping && _particles.Count > 0) || EmissionRate <= 0.0f)
        {
            return;
        }

        _emitAccumulator += EmissionRate * delta;
        int toEmit = Math.Min(MaxParticles - _particles.Count, (int)_emitAccumulator);
        if (toEmit <= 0) return;
        _emitAccumulator -= toEmit;
        EmitBurst(toEmit);
    }

    private void SpawnParticle()
    {
        Vector2 origin = Entity?.Transform.Value.Position ?? Vector2.Zero;
        float baseAngle = Entity?.Transform.Value.Rotation ?? -MathF.PI / 2.0f;
        float spreadRadians = SpreadAngle * MathF.PI / 180.0f;
        float angle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadRadians;
        float speed = StartSpeed * (0.65f + (float)_random.NextDouble() * 0.7f);
        Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
        _particles.Add(new Particle2D(origin, velocity, 0.0f, MathF.Max(0.05f, Lifetime)));
    }
}

/// <summary>Runtime state for a single simulated particle.</summary>
public struct Particle2D
{
    public Particle2D(Vector2 position, Vector2 velocity, float age, float lifetime)
    {
        Position = position;
        Velocity = velocity;
        Age = age;
        Lifetime = lifetime;
    }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public float Age { get; set; }

    public float Lifetime { get; set; }

    public readonly float NormalizedAge => Math.Clamp(Age / MathF.Max(0.0001f, Lifetime), 0.0f, 1.0f);
}
