using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 2 rendering runtime demonstration.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Engine _engine;
    private readonly Entity _demoEntity;
    private readonly Label _engineStatusLabel;
    private readonly Renderer2D _renderer;
    private readonly Panel _viewport;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
    {
        Text = "2dGameEngine";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 480);
        ClientSize = new Size(960, 540);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            RowCount = 3,
            ColumnCount = 1,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label titleLabel = new()
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18.0f, FontStyle.Bold),
            Text = "2dGameEngine Phase 2 Rendering",
        };

        _engineStatusLabel = new Label
        {
            AutoSize = true,
            Font = new Font(FontFamily.GenericMonospace, 10.0f),
            Margin = new Padding(0, 12, 0, 0),
            Text = "Starting engine...",
        };

        _viewport = new DoubleBufferedPanel
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 16, 0, 0),
        };
        _viewport.Paint += OnViewportPaint;

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(_engineStatusLabel, 0, 1);
        layout.Controls.Add(_viewport, 0, 2);
        Controls.Add(layout);

        _renderer = new Renderer2D
        {
            ClearColor = Color.FromArgb(14, 18, 28),
        };
        _renderer.Camera.Zoom = 1.0f;

        _engine = new Engine();
        Scene scene = new("Phase 2 Rendering Demo Scene");
        _demoEntity = scene.CreateEntity("Moving Sprite");
        _demoEntity.Transform.Value.Position = new Vector2(-220.0f, 0.0f);
        _demoEntity.AddComponent(new EntityMotionComponent(new Vector2(72.0f, 0.0f)));
        _demoEntity.AddComponent(new SpriteRenderer(new Vector2(96.0f, 96.0f), Color.CornflowerBlue));

        Entity marker = scene.CreateEntity("Scene Marker");
        marker.Transform.Value.Position = new Vector2(140.0f, 48.0f);
        marker.Transform.Value.Rotation = MathF.PI / 8.0f;
        marker.AddComponent(new SpriteRenderer(new Vector2(120.0f, 72.0f), Color.Orange)
        {
            SortingOrder = -1,
        });

        _engine.SetActiveScene(scene);
        _engine.Updated += OnEngineUpdated;
        _engine.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _engine.Stop();
        _engine.Updated -= OnEngineUpdated;
        _viewport.Paint -= OnViewportPaint;
        base.OnFormClosed(e);
    }

    private void OnEngineUpdated(object? sender, EngineUpdatedEventArgs args)
    {
        Vector2 position = _demoEntity.Transform.Value.Position;
        _engineStatusLabel.Text = FormattableString.Invariant(
            $"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nEntity: {_demoEntity.Name}\nPosition: ({position.X:0.00}, {position.Y:0.00})");

        if (position.X > 260.0f)
        {
            _demoEntity.Transform.Value.Position = new Vector2(-260.0f, position.Y);
        }

        _viewport.Invalidate();
    }

    private void OnViewportPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _engine.ActiveScene, _viewport.ClientSize);
    }

    private sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
