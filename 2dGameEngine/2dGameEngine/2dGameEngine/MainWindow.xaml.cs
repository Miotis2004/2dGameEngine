using System;
using System.Numerics;
using _2dGameEngine.Core;
using Microsoft.UI.Xaml;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 1 engine runtime demonstration.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly Engine _engine;
    private readonly Entity _demoEntity;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _engine = new Engine(DispatcherQueue);
        Scene scene = new("Phase 1 Demo Scene");
        _demoEntity = scene.CreateEntity("Updating Entity");
        _demoEntity.AddComponent(new EntityMotionComponent(new Vector2(32.0f, 0.0f)));

        _engine.SetActiveScene(scene);
        _engine.Updated += OnEngineUpdated;
        _engine.Start();
    }

    private void OnEngineUpdated(object? sender, EngineUpdatedEventArgs args)
    {
        Vector2 position = _demoEntity.Transform.Value.Position;
        EngineStatusText.Text = FormattableString.Invariant(
            $"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nEntity: {_demoEntity.Name}\nPosition: ({position.X:0.00}, {position.Y:0.00})");
    }
}
