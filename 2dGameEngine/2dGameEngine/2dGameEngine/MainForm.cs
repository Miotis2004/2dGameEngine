using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;
using _2dGameEngine.Input;
using _2dGameEngine.Physics;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 6 content-system runtime demonstration.
/// </summary>
public sealed class MainForm : Form
{
    private readonly AssetManager _assets;
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
            Text = "2dGameEngine Phase 6 Content System",
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

        _assets = new AssetManager(Path.Combine(AppContext.BaseDirectory, "Content"));
        SpriteSheetAsset tileSprites = _assets.LoadSpriteSheet("Assets/demo-tiles.spritesheet.json");

        _engine = new Engine();
        Scene scene = new("Phase 6 Content Demo Scene");
        _demoEntity = scene.CreateEntity("Player Rigidbody");
        _demoEntity.Transform.Value.Position = new Vector2(-220.0f, -160.0f);
        _demoBody = _demoEntity.AddComponent(new RigidBody2D());
        _demoEntity.AddComponent(new BoxCollider2D(new Vector2(64.0f, 96.0f)));
        _demoEntity.AddComponent(new PlatformerMovementComponent(260.0f, 520.0f));
        _demoEntity.AddComponent(new SpriteRenderer(new Vector2(64.0f, 96.0f), Color.CornflowerBlue));

        Entity level = scene.CreateEntity("Tilemap Level");
        level.Transform.Value.Position = new Vector2(-384.0f, -96.0f);
        Tilemap tilemap = level.AddComponent(new Tilemap(24, 10, new Vector2(32.0f, 32.0f))
        {
            SortingOrder = -10,
        });
        tilemap.SetDefinition(new TileDefinition(1, Color.ForestGreen) { Frame = tileSprites.GetFrame("grass") });
        tilemap.SetDefinition(new TileDefinition(2, Color.SaddleBrown) { Frame = tileSprites.GetFrame("dirt") });
        tilemap.SetDefinition(new TileDefinition(3, Color.Orange) { Frame = tileSprites.GetFrame("platform") });

        for (int x = 0; x < tilemap.Width; x++)
        {
            tilemap.SetTile(x, 8, 1);
            tilemap.SetTile(x, 9, 2);
        }

        for (int x = 14; x < 20; x++)
        {
            tilemap.SetTile(x, 5, 3);
        }

        for (int x = 4; x < 9; x++)
        {
            tilemap.SetTile(x, 6, 3);
        }

        level.AddComponent(new TilemapCollider2D());

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
        _assets.Dispose();
        base.OnFormClosed(e);
    }

    private void OnEngineUpdated(object? sender, EngineUpdatedEventArgs args)
    {
        Vector2 position = _demoEntity.Transform.Value.Position;
        InputState input = args.Input;
        Point mouseDelta = input.MouseDelta;
        _engineStatusLabel.Text = FormattableString.Invariant(
            $"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nEntity: {_demoEntity.Name}\nPosition: ({position.X:0.00}, {position.Y:0.00})\nVelocity: ({_demoBody.Velocity.X:0.00}, {_demoBody.Velocity.Y:0.00})\nGrounded: {_demoBody.IsGrounded}\nMove: A/D or Left/Right, Jump: Space/W/Up, Land on externally loaded sprite-sheet tiles\nMouse: ({input.MousePosition.X}, {input.MousePosition.Y}) Δ({mouseDelta.X}, {mouseDelta.Y}) Wheel: {input.MouseWheelDelta}");

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
