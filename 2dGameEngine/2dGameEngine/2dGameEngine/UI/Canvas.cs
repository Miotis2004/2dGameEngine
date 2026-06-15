using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _2dGameEngine.Core;
using _2dGameEngine.Input;

namespace _2dGameEngine.UI;

public sealed class UIEventContext
{
    public PointF PointerPosition { get; init; }
    public bool IsPrimaryDown { get; init; }
    public bool WasPrimaryReleased { get; init; }
    public bool Handled { get; set; }
}

public interface IUIEventHandler
{
    void HandleUiEvent(UIEventContext context);
}

/// <summary>
/// Root UI canvas that routes pointer events to child UI controls before gameplay input consumes them.
/// </summary>
public sealed class Canvas : Component
{
    private readonly List<Entity> _routeBuffer = [];

    public CanvasRenderMode RenderMode { get; set; } = CanvasRenderMode.ScreenSpaceOverlay;
    public SizeF ReferenceResolution { get; set; } = new(1280, 720);
    public float ScaleFactor { get; set; } = 1.0f;
    public int SortingOrder { get; set; } = 1000;

    public override void Update(Time time, InputState input)
    {
        if (Entity is null) return;
        UIEventContext context = new()
        {
            PointerPosition = input.MousePosition,
            IsPrimaryDown = input.IsMouseButtonDown(MouseButtons.Left),
            WasPrimaryReleased = input.WasMouseButtonReleased(MouseButtons.Left),
        };
        BuildRoute(Entity);
        for (int i = _routeBuffer.Count - 1; i >= 0 && !context.Handled; i--)
        {
            Entity target = _routeBuffer[i];
            for (int c = 0; c < target.Components.Count && !context.Handled; c++)
            {
                if (target.Components[c] is IUIEventHandler handler)
                {
                    handler.HandleUiEvent(context);
                }
            }
        }
    }

    private void BuildRoute(Entity root)
    {
        _routeBuffer.Clear();
        AddToRoute(root);
    }

    private void AddToRoute(Entity entity)
    {
        if (entity.GetComponent<RectTransformComponent>() is not null)
        {
            _routeBuffer.Add(entity);
        }
        for (int i = 0; i < entity.Children.Count; i++)
        {
            AddToRoute(entity.Children[i]);
        }
    }
}
