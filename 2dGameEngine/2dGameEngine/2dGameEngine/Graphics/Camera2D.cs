using System;
using System.Drawing;
using System.Numerics;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Describes the viewport used to project world-space coordinates onto the screen.
/// </summary>
public sealed class Camera2D
{
    /// <summary>
    /// Gets or sets the world-space position at the center of the camera.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the zoom factor applied while rendering.
    /// </summary>
    public float Zoom { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the render layers visible to this camera.
    /// </summary>
    public RenderLayerMask CullingMask { get; set; } = RenderLayerMask.Everything;

    /// <summary>
    /// Converts a world-space point to a screen-space point.
    /// </summary>
    /// <param name="worldPosition">The point in world space.</param>
    /// <param name="viewportSize">The current viewport size.</param>
    /// <returns>The point projected into screen space.</returns>
    public PointF WorldToScreen(Vector2 worldPosition, Size viewportSize)
    {
        float zoom = MathF.Max(0.01f, Zoom);
        Vector2 viewportCenter = new(viewportSize.Width / 2.0f, viewportSize.Height / 2.0f);
        Vector2 projected = (worldPosition - Position) * zoom + viewportCenter;
        return new PointF(projected.X, projected.Y);
    }
}
