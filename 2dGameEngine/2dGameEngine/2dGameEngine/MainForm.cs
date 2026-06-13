using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;
using _2dGameEngine.Input;
using _2dGameEngine.Physics;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 4 physics runtime demonstration.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Engine _engine;
    private readonly Entity _demoEntity;
    private readonly RigidBody2D _demoBody;
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
        KeyPreview = true;

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
            Text = "2dGameEngine Phase 4 Physics",
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
        _viewport.MouseDown += OnViewportMouseDown;
        _viewport.MouseUp += OnViewportMouseUp;
        _viewport.MouseMove += OnViewportMouseMove;
        _viewport.MouseWheel += OnViewportMouseWheel;

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
        Scene scene = new("Phase 4 Physics Demo Scene");
        _demoEntity = scene.CreateEntity("Player Rigidbody");
        _demoEntity.Transform.Value.Position = new Vector2(-220.0f, -160.0f);
        _demoBody = _demoEntity.AddComponent(new RigidBody2D());
        _demoEntity.AddComponent(new BoxCollider2D(new Vector2(64.0f, 96.0f)));
        _demoEntity.AddComponent(new PlatformerMovementComponent(260.0f, 520.0f));
        _demoEntity.AddComponent(new SpriteRenderer(new Vector2(64.0f, 96.0f), Color.CornflowerBlue));

        Entity ground = scene.CreateEntity("Ground Collider");
        ground.Transform.Value.Position = new Vector2(0.0f, 180.0f);
        ground.AddComponent(new BoxCollider2D(new Vector2(760.0f, 48.0f)));
        ground.AddComponent(new SpriteRenderer(new Vector2(760.0f, 48.0f), Color.ForestGreen)
        {
            SortingOrder = -1,
        });

        Entity platform = scene.CreateEntity("Raised Platform Collider");
        platform.Transform.Value.Position = new Vector2(180.0f, 40.0f);
        platform.AddComponent(new BoxCollider2D(new Vector2(220.0f, 36.0f)));
        platform.AddComponent(new SpriteRenderer(new Vector2(220.0f, 36.0f), Color.Orange)
        {
            SortingOrder = -1,
        });

        _engine.SetActiveScene(scene);
        _engine.Updated += OnEngineUpdated;
        KeyDown += OnFormKeyDown;
        KeyUp += OnFormKeyUp;

        _engine.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _engine.Stop();
        _engine.Updated -= OnEngineUpdated;
        KeyDown -= OnFormKeyDown;
        KeyUp -= OnFormKeyUp;
        _viewport.Paint -= OnViewportPaint;
        _viewport.MouseDown -= OnViewportMouseDown;
        _viewport.MouseUp -= OnViewportMouseUp;
        _viewport.MouseMove -= OnViewportMouseMove;
        _viewport.MouseWheel -= OnViewportMouseWheel;
        base.OnFormClosed(e);
    }

    private void OnEngineUpdated(object? sender, EngineUpdatedEventArgs args)
    {
        Vector2 position = _demoEntity.Transform.Value.Position;
        InputState input = args.Input;
        Point mouseDelta = input.MouseDelta;
        _engineStatusLabel.Text = FormattableString.Invariant(
            $"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nEntity: {_demoEntity.Name}\nPosition: ({position.X:0.00}, {position.Y:0.00})\nVelocity: ({_demoBody.Velocity.X:0.00}, {_demoBody.Velocity.Y:0.00})\nGrounded: {_demoBody.IsGrounded}\nMove: A/D or Left/Right, Jump: Space/W/Up\nMouse: ({input.MousePosition.X}, {input.MousePosition.Y}) Δ({mouseDelta.X}, {mouseDelta.Y}) Wheel: {input.MouseWheelDelta}");

        _viewport.Invalidate();
    }

    private void OnViewportPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _engine.ActiveScene, _viewport.ClientSize);
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        _engine.Input.SetKeyDown(e.KeyCode);
    }

    private void OnFormKeyUp(object? sender, KeyEventArgs e)
    {
        _engine.Input.SetKeyUp(e.KeyCode);
    }

    private void OnViewportMouseDown(object? sender, MouseEventArgs e)
    {
        _engine.Input.SetMouseButtonDown(e.Button);
        _engine.Input.SetMousePosition(e.Location);
        _viewport.Focus();
    }

    private void OnViewportMouseUp(object? sender, MouseEventArgs e)
    {
        _engine.Input.SetMouseButtonUp(e.Button);
        _engine.Input.SetMousePosition(e.Location);
    }

    private void OnViewportMouseMove(object? sender, MouseEventArgs e)
    {
        _engine.Input.SetMousePosition(e.Location);
    }

    private void OnViewportMouseWheel(object? sender, MouseEventArgs e)
    {
        _engine.Input.AddMouseWheelDelta(e.Delta);
        _engine.Input.SetMousePosition(e.Location);
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
