using System;
using System.Drawing;
using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Adds global, point, spot, or rectangular shape light contribution to the 2D renderer.
/// </summary>
public sealed class Light2D : Component
{
    /// <summary>
    /// Gets or sets the light category used by the renderer.
    /// </summary>
    public Light2DType LightType { get; set; } = Light2DType.Point;

    /// <summary>
    /// Gets or sets the light color.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the light strength. Values above one intentionally over-brighten lit sprites.
    /// </summary>
    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the point, spot, or shape light radius in world units.
    /// </summary>
    public float Radius { get; set; } = 240.0f;

    /// <summary>
    /// Gets or sets the spot cone angle in degrees.
    /// </summary>
    public float SpotAngle { get; set; } = 45.0f;

    /// <summary>
    /// Gets or sets the render layers affected by this light.
    /// </summary>
    public RenderLayerMask LayerMask { get; set; } = RenderLayerMask.Everything;

    internal Color Evaluate(Color baseColor, float distance, float angleDeltaDegrees)
    {
        float attenuation = LightType == Light2DType.Global ? 1.0f : MathF.Max(0.0f, 1.0f - distance / MathF.Max(0.01f, Radius));
        if (LightType == Light2DType.Spot)
        {
            attenuation *= angleDeltaDegrees <= SpotAngle * 0.5f ? 1.0f : 0.0f;
        }

        float amount = attenuation * MathF.Max(0.0f, Intensity);
        return Color.FromArgb(
            baseColor.A,
            Math.Clamp(baseColor.R + (int)(Color.R * amount), 0, 255),
            Math.Clamp(baseColor.G + (int)(Color.G * amount), 0, 255),
            Math.Clamp(baseColor.B + (int)(Color.B * amount), 0, 255));
    }
}

/// <summary>
/// 2D light shapes supported by the default renderer.
/// </summary>
public enum Light2DType
{
    Global,
    Point,
    Spot,
    Shape,
}
