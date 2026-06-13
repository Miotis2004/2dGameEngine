using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Draws the active scene using the current camera and sprite renderer components.
/// </summary>
public sealed class Renderer2D
{
    /// <summary>
    /// Gets the camera used to project world-space positions.
    /// </summary>
    public Camera2D Camera { get; } = new();

    /// <summary>
    /// Gets or sets the color used to clear the viewport before rendering.
    /// </summary>
    public Color ClearColor { get; set; } = Color.FromArgb(20, 24, 32);

    /// <summary>
    /// Renders every enabled sprite renderer in the provided scene.
    /// </summary>
    /// <param name="graphics">The drawing surface.</param>
    /// <param name="scene">The scene to render.</param>
    /// <param name="viewportSize">The viewport size in pixels.</param>
    public void Render(System.Drawing.Graphics graphics, Scene? scene, Size viewportSize)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        graphics.Clear(ClearColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (scene is null)
        {
            return;
        }

        foreach (Entity entity in scene.Entities.Where(entity => entity.IsEnabled)
            .OrderBy(entity => entity.GetComponent<SpriteRenderer>()?.SortingOrder ?? 0))
        {
            SpriteRenderer? sprite = entity.GetComponent<SpriteRenderer>();
            if (sprite is null || !sprite.IsEnabled)
            {
                continue;
            }

            DrawSprite(graphics, entity, sprite, viewportSize);
        }
    }

    private void DrawSprite(System.Drawing.Graphics graphics, Entity entity, SpriteRenderer sprite, Size viewportSize)
    {
        PointF screenPosition = Camera.WorldToScreen(entity.Transform.Value.Position, viewportSize);
        float zoom = MathF.Max(0.01f, Camera.Zoom);
        float width = MathF.Max(1.0f, sprite.Size.X * entity.Transform.Value.Scale.X * zoom);
        float height = MathF.Max(1.0f, sprite.Size.Y * entity.Transform.Value.Scale.Y * zoom);
        RectangleF bounds = new(screenPosition.X - width / 2.0f, screenPosition.Y - height / 2.0f, width, height);

        GraphicsState state = graphics.Save();
        graphics.TranslateTransform(screenPosition.X, screenPosition.Y);
        graphics.RotateTransform(entity.Transform.Value.Rotation * 180.0f / MathF.PI);
        graphics.TranslateTransform(-screenPosition.X, -screenPosition.Y);

        using SolidBrush brush = new(sprite.Color);
        graphics.FillRectangle(brush, bounds);

        if (sprite.OutlineColor is Color outlineColor)
        {
            using Pen pen = new(outlineColor, 2.0f);
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        graphics.Restore(state);
    }
}
