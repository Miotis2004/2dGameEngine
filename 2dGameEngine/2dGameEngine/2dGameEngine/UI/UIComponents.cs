using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Input;

namespace _2dGameEngine.UI;

public enum CanvasRenderMode { ScreenSpaceOverlay, WorldSpace }
public enum UIAnchorPreset { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight, Stretch }
public enum UILayoutDirection { Horizontal, Vertical }

public sealed class CanvasComponent : Component
{
    public CanvasRenderMode RenderMode { get; set; } = CanvasRenderMode.ScreenSpaceOverlay;
    public SizeF ReferenceResolution { get; set; } = new(1280, 720);
    public float ScaleFactor { get; set; } = 1.0f;
    public int SortingOrder { get; set; } = 1000;
}

public sealed class RectTransformComponent : Component
{
    public Vector2 AnchoredPosition { get; set; }
    public Vector2 Size { get; set; } = new(160, 48);
    public UIAnchorPreset Anchor { get; set; } = UIAnchorPreset.Center;
    public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);
}

public sealed class UIPanelComponent : Component
{
    public Color BackgroundColor { get; set; } = Color.FromArgb(190, 24, 28, 36);
    public Color BorderColor { get; set; } = Color.FromArgb(220, 90, 105, 130);
    public float BorderThickness { get; set; } = 1.0f;
}

public sealed class UIImageComponent : Component
{
    public SpriteFrame? Frame { get; set; }
    public Color Tint { get; set; } = Color.White;
    public string? SourcePath { get; set; }
}

public sealed class UITextComponent : Component
{
    public string Text { get; set; } = "Text";
    public string FontFamily { get; set; } = "Segoe UI";
    public float FontSize { get; set; } = 16.0f;
    public Color Color { get; set; } = Color.White;
    public ContentAlignment Alignment { get; set; } = ContentAlignment.MiddleCenter;
}

public sealed class UIButtonComponent : Component
{
    public string Label { get; set; } = "Button";
    public Color NormalColor { get; set; } = Color.FromArgb(230, 56, 72, 104);
    public Color HighlightedColor { get; set; } = Color.FromArgb(245, 84, 112, 164);
    public Color PressedColor { get; set; } = Color.FromArgb(245, 40, 56, 88);
    public string? ActionName { get; set; }
    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }
    public event EventHandler? Clicked;
    public void Click() => Clicked?.Invoke(this, EventArgs.Empty);
    public override void Update(Time time, InputState input)
    {
        if (Entity?.GetComponent<RectTransformComponent>() is not { } rect) return;
        RectangleF bounds = UICanvasUtility.GetScreenRect(Entity, rect, UICanvasUtility.CurrentViewportSize);
        IsHovered = bounds.Contains(input.MousePosition);
        IsPressed = IsHovered && input.IsMouseButtonDown(MouseButtons.Left);
        if ((IsHovered && input.WasMouseButtonReleased(MouseButtons.Left)) || input.WasKeyPressed(Keys.Enter) || input.WasKeyPressed(Keys.Space)) Click();
    }
}

public sealed class UISliderComponent : Component
{
    private float _value = 0.5f;
    public float MinValue { get; set; }
    public float MaxValue { get; set; } = 1.0f;
    public float Value { get => _value; set => _value = Math.Clamp(value, MinValue, MaxValue); }
    public Color FillColor { get; set; } = Color.DeepSkyBlue;
    public override void Update(Time time, InputState input)
    {
        if (Entity?.GetComponent<RectTransformComponent>() is not { } rect) return;
        RectangleF bounds = UICanvasUtility.GetScreenRect(Entity, rect, UICanvasUtility.CurrentViewportSize);
        if (!input.IsMouseButtonDown(MouseButtons.Left) || !bounds.Contains(input.MousePosition)) return;
        float t = Math.Clamp((input.MousePosition.X - bounds.Left) / Math.Max(1, bounds.Width), 0, 1);
        Value = MinValue + (MaxValue - MinValue) * t;
    }
}

public sealed class UILayoutGroupComponent : Component
{
    public UILayoutDirection Direction { get; set; } = UILayoutDirection.Vertical;
    public float Spacing { get; set; } = 8.0f;
    public Padding Padding { get; set; } = new(8);
    public override void Update(Time time)
    {
        if (Entity is null) return;
        float cursor = Direction == UILayoutDirection.Vertical ? Padding.Top : Padding.Left;
        foreach (Entity child in Entity.Children)
        {
            if (child.GetComponent<RectTransformComponent>() is not { } rect) continue;
            rect.Anchor = UIAnchorPreset.TopLeft;
            rect.Pivot = Vector2.Zero;
            rect.AnchoredPosition = Direction == UILayoutDirection.Vertical ? new(Padding.Left, cursor) : new(cursor, Padding.Top);
            cursor += (Direction == UILayoutDirection.Vertical ? rect.Size.Y : rect.Size.X) + Spacing;
        }
    }
}

public static class UICanvasUtility
{
    public static Size CurrentViewportSize { get; private set; } = new(1280, 720);

    public static void SetCurrentViewportSize(Size viewport) => CurrentViewportSize = viewport;

    public static RectangleF GetScreenRect(Entity entity, RectTransformComponent rect, Size viewport)
    {
        Vector2 origin = rect.Anchor switch
        {
            UIAnchorPreset.TopLeft => Vector2.Zero,
            UIAnchorPreset.Top => new(viewport.Width / 2f, 0),
            UIAnchorPreset.TopRight => new(viewport.Width, 0),
            UIAnchorPreset.Left => new(0, viewport.Height / 2f),
            UIAnchorPreset.Right => new(viewport.Width, viewport.Height / 2f),
            UIAnchorPreset.BottomLeft => new(0, viewport.Height),
            UIAnchorPreset.Bottom => new(viewport.Width / 2f, viewport.Height),
            UIAnchorPreset.BottomRight => new(viewport.Width, viewport.Height),
            _ => new(viewport.Width / 2f, viewport.Height / 2f),
        };
        Vector2 position = origin + rect.AnchoredPosition + entity.Transform.Value.Position;
        return new RectangleF(position.X - rect.Size.X * rect.Pivot.X, position.Y - rect.Size.Y * rect.Pivot.Y, rect.Size.X, rect.Size.Y);
    }
}
