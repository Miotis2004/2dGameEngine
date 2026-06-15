using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using _2dGameEngine.Content;
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
    /// Gets the render pipeline settings used for lighting, culling, effects, and diagnostics.
    /// </summary>
    public RenderPipelineAsset Pipeline { get; } = new();

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
        graphics.SmoothingMode = Pipeline.PixelPerfect ? SmoothingMode.None : SmoothingMode.AntiAlias;
        graphics.InterpolationMode = Pipeline.PixelPerfect ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;

        if (scene is null)
        {
            return;
        }

        IReadOnlyList<Light2D> lights = scene.Entities.Where(entity => entity.IsEnabled)
            .Select(entity => entity.GetComponent<Light2D>())
            .Where(light => light is { IsEnabled: true })
            .Cast<Light2D>()
            .ToArray();

        int drawCalls = 0;
        foreach (Entity entity in scene.Entities.Where(entity => entity.IsEnabled)
            .Where(IsVisibleToCamera)
            .OrderBy(GetSortingOrder))
        {
            Tilemap? tilemap = entity.GetComponent<Tilemap>();
            if (tilemap is { IsEnabled: true })
            {
                DrawTilemap(graphics, tilemap, viewportSize, lights);
                drawCalls++;
            }

            SpriteRenderer? sprite = entity.GetComponent<SpriteRenderer>();
            if (sprite is { IsEnabled: true })
            {
                DrawSprite(graphics, entity, sprite, viewportSize, lights);
                drawCalls++;
            }
        }

        DrawDebugOverlay(graphics, lights.Count, drawCalls);
    }

    private void DrawDebugOverlay(System.Drawing.Graphics graphics, int lightCount, int drawCalls)
    {
        if (Pipeline.DebugMode == RenderDebugMode.None)
        {
            return;
        }

        string text = Pipeline.DebugMode switch
        {
            RenderDebugMode.Batches => $"Batches/Draw Calls: {drawCalls}",
            RenderDebugMode.Lighting => $"2D Lights: {lightCount} | Ambient: {Pipeline.AmbientLight}",
            RenderDebugMode.Overdraw => $"Overdraw proxy: {drawCalls} submissions",
            RenderDebugMode.Colliders => "Collider debug mode enabled",
            _ => Pipeline.DebugMode.ToString(),
        };

        using SolidBrush background = new(Color.FromArgb(180, Color.Black));
        using SolidBrush foreground = new(Color.LightGreen);
        graphics.FillRectangle(background, 8, 8, 280, 28);
        graphics.DrawString(text, SystemFonts.DefaultFont, foreground, 16, 15);
    }

    private bool IsVisibleToCamera(Entity entity)
    {
        RenderLayerMask mask = Camera.CullingMask & Pipeline.CameraCullingMask;
        SpriteRenderer? sprite = entity.GetComponent<SpriteRenderer>();
        Tilemap? tilemap = entity.GetComponent<Tilemap>();
        SortingGroup2D? group = entity.GetComponent<SortingGroup2D>();
        RenderLayerMask layer = sprite?.RenderLayer ?? tilemap?.RenderLayer ?? group?.Layer ?? RenderLayerMask.Default;
        return (layer & mask) != RenderLayerMask.None;
    }

    private static int GetSortingOrder(Entity entity)
    {
        SpriteRenderer? sprite = entity.GetComponent<SpriteRenderer>();
        Tilemap? tilemap = entity.GetComponent<Tilemap>();

        int baseOrder = (sprite, tilemap) switch
        {
            ({ } spriteRenderer, { } tilemapRenderer) => Math.Min(spriteRenderer.SortingOrder, tilemapRenderer.SortingOrder),
            ({ } spriteRenderer, null) => spriteRenderer.SortingOrder,
            (null, { } tilemapRenderer) => tilemapRenderer.SortingOrder,
            _ => 0,
        };

        return baseOrder + (entity.GetComponent<SortingGroup2D>()?.SortingOrderOffset ?? 0);
    }

    private void DrawTilemap(System.Drawing.Graphics graphics, Tilemap tilemap, Size viewportSize, IReadOnlyList<Light2D> lights)
    {
        float zoom = MathF.Max(0.01f, Camera.Zoom);
        foreach ((int x, int y, _, TileDefinition definition) in tilemap.GetOccupiedTiles())
        {
            RectangleF worldBounds = tilemap.GetTileBounds(x, y);
            PointF screenPosition = Camera.WorldToScreen(new Vector2(worldBounds.X, worldBounds.Y), viewportSize);
            RectangleF screenBounds = new(screenPosition.X, screenPosition.Y, worldBounds.Width * zoom, worldBounds.Height * zoom);

            Color litColor = ApplyMaterialAndLighting(definition.Color, tilemap.Material, tilemap.RenderLayer, new Vector2(worldBounds.X + worldBounds.Width / 2.0f, worldBounds.Y + worldBounds.Height / 2.0f), lights);
            DrawFrameOrColor(graphics, definition.Frame, litColor, screenBounds);

            using Pen gridPen = new(Color.FromArgb(90, Color.Black), Math.Max(1.0f, zoom));
            graphics.DrawRectangle(gridPen, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);
        }
    }

    private static void DrawFrameOrColor(System.Drawing.Graphics graphics, SpriteFrame? frame, Color color, RectangleF bounds)
    {
        if (frame is not null)
        {
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(frame.Texture.Image, bounds, frame.SourceRectangle, GraphicsUnit.Pixel);
            return;
        }

        using SolidBrush brush = new(color);
        graphics.FillRectangle(brush, bounds);
    }

    private static void DrawSpriteFill(System.Drawing.Graphics graphics, SpriteRenderer sprite, RectangleF bounds)
    {
        if (sprite.Frame is not null)
        {
            DrawFrameOrColor(graphics, sprite.Frame, sprite.Color, bounds);
            return;
        }

        using SolidBrush brush = new(sprite.Color);
        switch (sprite.PrimitiveType)
        {
            case SpritePrimitiveType.Circle:
                graphics.FillEllipse(brush, bounds);
                break;
            case SpritePrimitiveType.Triangle:
                graphics.FillPolygon(brush, GetTrianglePoints(bounds));
                break;
            default:
                graphics.FillRectangle(brush, bounds);
                break;
        }
    }

    private static void DrawSpriteOutline(System.Drawing.Graphics graphics, SpriteRenderer sprite, RectangleF bounds, Pen pen)
    {
        if (sprite.Frame is not null || sprite.PrimitiveType == SpritePrimitiveType.Rectangle)
        {
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            return;
        }

        if (sprite.PrimitiveType == SpritePrimitiveType.Circle)
        {
            graphics.DrawEllipse(pen, bounds);
            return;
        }

        graphics.DrawPolygon(pen, GetTrianglePoints(bounds));
    }

    private Color ApplyMaterialAndLighting(Color color, Material2D material, RenderLayerMask layer, Vector2 worldPosition, IReadOnlyList<Light2D> lights)
    {
        Color tinted = Multiply(color, material.Tint);
        if (!Pipeline.EnableLighting || !material.ReceivesLighting)
        {
            return tinted;
        }

        Color lit = Multiply(tinted, Pipeline.AmbientLight);
        foreach (Light2D light in lights)
        {
            if ((light.LayerMask & layer) == RenderLayerMask.None)
            {
                continue;
            }

            Vector2 lightPosition = light.Entity?.Transform.Value.Position ?? Vector2.Zero;
            Vector2 delta = worldPosition - lightPosition;
            float angle = MathF.Abs(MathF.Atan2(delta.Y, delta.X) * 180.0f / MathF.PI - ((light.Entity?.Transform.Value.Rotation ?? 0.0f) * 180.0f / MathF.PI));
            lit = light.Evaluate(lit, delta.Length(), angle);
        }

        return lit;
    }

    private static Color Multiply(Color left, Color right) => Color.FromArgb(
        left.A * right.A / 255,
        left.R * right.R / 255,
        left.G * right.G / 255,
        left.B * right.B / 255);

    private static PointF[] GetTrianglePoints(RectangleF bounds) =>
    [
        new PointF(bounds.X + bounds.Width / 2.0f, bounds.Y),
        new PointF(bounds.Right, bounds.Bottom),
        new PointF(bounds.X, bounds.Bottom),
    ];

    private void DrawSprite(System.Drawing.Graphics graphics, Entity entity, SpriteRenderer sprite, Size viewportSize, IReadOnlyList<Light2D> lights)
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

        Color originalColor = sprite.Color;
        sprite.Color = ApplyMaterialAndLighting(sprite.Color, sprite.Material, sprite.RenderLayer, entity.Transform.Value.Position, lights);
        DrawSpriteFill(graphics, sprite, bounds);
        sprite.Color = originalColor;

        if (sprite.OutlineColor is Color outlineColor)
        {
            using Pen pen = new(outlineColor, 2.0f);
            DrawSpriteOutline(graphics, sprite, bounds, pen);
        }

        graphics.Restore(state);
    }
}
