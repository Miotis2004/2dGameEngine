using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Animation;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;
using _2dGameEngine.Input;
using _2dGameEngine.Physics;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 10 platformer validation project inside the editor runtime preview.
/// </summary>
public sealed class MainForm : Form
{
    private readonly AssetManager _assets;
    private readonly Engine _engine;
    private readonly Entity _playerEntity;
    private readonly RigidBody2D _playerBody;
    private readonly Entity _goalEntity;
    private readonly Vector2 _playerStartPosition;
    private bool _levelComplete;
    private readonly Renderer2D _renderer;
    private readonly TreeView _hierarchyTree;
    private readonly ListView _inspectorList;
    private readonly Label _inspectorHeader;
    private readonly Label _runtimeStatusLabel;
    private readonly Label _viewportOverlayLabel;
    private readonly ToolStripButton _playPauseButton;
    private readonly ToolStripStatusLabel _statusStripLabel;
    private readonly Panel _viewport;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
    {
        Text = "2dGameEngine - Phase 10 Platformer Validation";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 600);
        ClientSize = new Size(1280, 720);
        KeyPreview = true;

        _renderer = new Renderer2D
        {
            ClearColor = Color.FromArgb(14, 18, 28),
        };
        _renderer.Camera.Zoom = 1.0f;

        _assets = new AssetManager(Path.Combine(AppContext.BaseDirectory, "Content"));
        Scene scene = CreateValidationScene(_assets, out _playerEntity, out _playerBody, out _goalEntity, out _playerStartPosition);

        _engine = new Engine();
        _engine.SetActiveScene(scene);
        _engine.Updated += OnEngineUpdated;

        ToolStrip toolStrip = new()
        {
            GripStyle = ToolStripGripStyle.Hidden,
        };
        _playPauseButton = new ToolStripButton("Pause Preview")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _playPauseButton.Click += OnPlayPauseClicked;
        toolStrip.Items.Add(new ToolStripLabel("2dGameEngine Editor"));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(_playPauseButton);
        toolStrip.Items.Add(new ToolStripLabel("Scene Preview"));

        StatusStrip statusStrip = new();
        _statusStripLabel = new ToolStripStatusLabel("Runtime preview ready");
        statusStrip.Items.Add(_statusStripLabel);

        _hierarchyTree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
        };
        _hierarchyTree.AfterSelect += OnHierarchySelectionChanged;

        _inspectorHeader = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Top,
            Font = new Font(Font.FontFamily, 11.0f, FontStyle.Bold),
            Height = 36,
            Padding = new Padding(8, 10, 8, 0),
            Text = "Inspector",
        };
        _inspectorList = new ListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            View = View.Details,
        };
        _inspectorList.Columns.Add("Property", 130);
        _inspectorList.Columns.Add("Value", 220);

        _runtimeStatusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 9.5f),
            Padding = new Padding(8),
            Text = "Starting runtime preview...",
        };

        _viewport = new DoubleBufferedPanel
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
        };
        _viewport.Paint += OnViewportPaint;
        _viewport.MouseDown += OnViewportMouseDown;
        _viewport.MouseUp += OnViewportMouseUp;
        _viewport.MouseMove += OnViewportMouseMove;
        _viewport.MouseWheel += OnViewportMouseWheel;

        _viewportOverlayLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.FromArgb(170, 20, 24, 32),
            Dock = DockStyle.Top,
            ForeColor = Color.White,
            Height = 42,
            Padding = new Padding(10, 6, 10, 4),
            Text = "Platformer Validation - reach the gold flag (A/D or arrows move, Space/W/Up jump, R reset, mouse tracked)",
        };
        _viewport.Controls.Add(_viewportOverlayLabel);

        SplitContainer rootSplit = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 260,
        };

        SplitContainer centerSplit = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 760,
        };

        SplitContainer viewportRuntimeSplit = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 470,
        };

        rootSplit.Panel1.Controls.Add(CreateDockPanel("Hierarchy", _hierarchyTree));
        rootSplit.Panel2.Controls.Add(centerSplit);
        centerSplit.Panel1.Controls.Add(viewportRuntimeSplit);
        centerSplit.Panel2.Controls.Add(CreateInspectorPanel());
        viewportRuntimeSplit.Panel1.Controls.Add(CreateDockPanel("Viewport", _viewport));
        viewportRuntimeSplit.Panel2.Controls.Add(CreateDockPanel("Runtime Preview", _runtimeStatusLabel));

        Controls.Add(rootSplit);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);

        PopulateHierarchy(scene);
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
        _playPauseButton.Click -= OnPlayPauseClicked;
        _hierarchyTree.AfterSelect -= OnHierarchySelectionChanged;
        _viewport.Paint -= OnViewportPaint;
        _viewport.MouseDown -= OnViewportMouseDown;
        _viewport.MouseUp -= OnViewportMouseUp;
        _viewport.MouseMove -= OnViewportMouseMove;
        _viewport.MouseWheel -= OnViewportMouseWheel;
        _assets.Dispose();
        base.OnFormClosed(e);
    }

    private static Panel CreateDockPanel(string title, Control content)
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
        };
        Label header = new()
        {
            BackColor = Color.FromArgb(38, 44, 56),
            Dock = DockStyle.Top,
            ForeColor = Color.White,
            Height = 28,
            Padding = new Padding(8, 6, 8, 0),
            Text = title,
        };
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Controls.Add(header);
        return panel;
    }

    private Control CreateInspectorPanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
        };
        panel.Controls.Add(_inspectorList);
        panel.Controls.Add(_inspectorHeader);
        return CreateDockPanel("Inspector", panel);
    }

    private static Scene CreateValidationScene(AssetManager assets, out Entity playerEntity, out RigidBody2D playerBody, out Entity goalEntity, out Vector2 playerStartPosition)
    {
        SpriteSheetAsset tileSprites = assets.LoadSpriteSheet("Assets/demo-tiles.spritesheet.json");
        AnimationClip playerIdle = assets.LoadAnimationClip("Assets/player-idle.animation.json");

        Scene scene = new("Phase 10 Platformer Validation Level");
        playerStartPosition = new Vector2(-460.0f, 64.0f);
        playerEntity = scene.CreateEntity("Player Controller");
        playerEntity.Transform.Value.Position = playerStartPosition;
        playerBody = playerEntity.AddComponent(new RigidBody2D());
        playerEntity.AddComponent(new BoxCollider2D(new Vector2(42.0f, 58.0f)));
        playerEntity.AddComponent(new PlatformerMovementComponent(285.0f, 610.0f));
        playerEntity.AddComponent(new SpriteRenderer(new Vector2(42.0f, 58.0f), Color.CornflowerBlue) { SortingOrder = 10 });
        playerEntity.AddComponent(new AnimationPlayer(playerIdle));

        Entity level = scene.CreateEntity("Validation Tilemap Level");
        level.Transform.Value.Position = new Vector2(-640.0f, -96.0f);
        Tilemap tilemap = level.AddComponent(new Tilemap(48, 14, new Vector2(32.0f, 32.0f))
        {
            SortingOrder = -10,
        });
        tilemap.SetDefinition(new TileDefinition(1, Color.ForestGreen) { Frame = tileSprites.GetFrame("grass") });
        tilemap.SetDefinition(new TileDefinition(2, Color.SaddleBrown) { Frame = tileSprites.GetFrame("dirt") });
        tilemap.SetDefinition(new TileDefinition(3, Color.Orange) { Frame = tileSprites.GetFrame("platform") });

        for (int x = 0; x < tilemap.Width; x++)
        {
            tilemap.SetTile(x, 12, 1);
            tilemap.SetTile(x, 13, 2);
        }

        AddPlatform(tilemap, 6, 9, 6);
        AddPlatform(tilemap, 15, 7, 5);
        AddPlatform(tilemap, 25, 8, 7);
        AddPlatform(tilemap, 36, 6, 6);
        AddPlatform(tilemap, 42, 10, 4);

        for (int y = 9; y < 12; y++)
        {
            tilemap.SetTile(30, y, 2);
        }

        level.AddComponent(new TilemapCollider2D());

        goalEntity = scene.CreateEntity("Goal Flag");
        goalEntity.Transform.Value.Position = new Vector2(840.0f, 68.0f);
        goalEntity.AddComponent(new BoxCollider2D(new Vector2(44.0f, 96.0f)) { IsTrigger = true });
        goalEntity.AddComponent(new SpriteRenderer(new Vector2(44.0f, 96.0f), Color.Gold) { OutlineColor = Color.OrangeRed, SortingOrder = 8 });

        return scene;
    }

    private static void AddPlatform(Tilemap tilemap, int startX, int y, int width)
    {
        for (int x = startX; x < startX + width; x++)
        {
            tilemap.SetTile(x, y, 3);
        }
    }

    private void ResetValidationLevel()
    {
        _playerEntity.Transform.Value.Position = _playerStartPosition;
        _playerBody.Velocity = Vector2.Zero;
        _levelComplete = false;
        SpriteRenderer? goalSprite = _goalEntity.GetComponent<SpriteRenderer>();
        if (goalSprite is not null)
        {
            goalSprite.Color = Color.Gold;
            goalSprite.OutlineColor = Color.OrangeRed;
        }
    }

    private void UpdateGoalState()
    {
        if (_levelComplete)
        {
            return;
        }

        BoxCollider2D? playerCollider = _playerEntity.GetComponent<BoxCollider2D>();
        BoxCollider2D? goalCollider = _goalEntity.GetComponent<BoxCollider2D>();
        if (playerCollider is null || goalCollider is null || !CollisionWorld.Intersects(playerCollider.GetBounds(), goalCollider.GetBounds()))
        {
            return;
        }

        _levelComplete = true;
        SpriteRenderer? goalSprite = _goalEntity.GetComponent<SpriteRenderer>();
        if (goalSprite is not null)
        {
            goalSprite.Color = Color.LimeGreen;
            goalSprite.OutlineColor = Color.White;
        }
    }

    private void PopulateHierarchy(Scene scene)
    {
        _hierarchyTree.BeginUpdate();
        _hierarchyTree.Nodes.Clear();
        TreeNode sceneNode = new(scene.Name)
        {
            Tag = scene,
        };

        foreach (Entity entity in scene.Entities)
        {
            TreeNode entityNode = new(entity.Name)
            {
                Tag = entity,
            };

            foreach (Component component in entity.Components)
            {
                entityNode.Nodes.Add(new TreeNode(component.GetType().Name)
                {
                    Tag = component,
                });
            }

            sceneNode.Nodes.Add(entityNode);
        }

        _hierarchyTree.Nodes.Add(sceneNode);
        sceneNode.ExpandAll();
        _hierarchyTree.SelectedNode = sceneNode.Nodes.Count > 0 ? sceneNode.Nodes[0] : sceneNode;
        _hierarchyTree.EndUpdate();
    }

    private void OnHierarchySelectionChanged(object? sender, TreeViewEventArgs e)
    {
        ShowInspector(e.Node?.Tag);
    }

    private void ShowInspector(object? selected)
    {
        _inspectorList.Items.Clear();

        switch (selected)
        {
            case Scene scene:
                _inspectorHeader.Text = scene.Name;
                AddInspectorRow("Type", "Scene");
                AddInspectorRow("Entities", scene.Entities.Count.ToString());
                break;
            case Entity entity:
                Vector2 position = entity.Transform.Value.Position;
                Vector2 scale = entity.Transform.Value.Scale;
                _inspectorHeader.Text = entity.Name;
                AddInspectorRow("Type", "Entity");
                AddInspectorRow("Enabled", entity.IsEnabled.ToString());
                AddInspectorRow("Position", FormattableString.Invariant($"{position.X:0.##}, {position.Y:0.##}"));
                AddInspectorRow("Rotation", FormattableString.Invariant($"{entity.Transform.Value.Rotation:0.###} rad"));
                AddInspectorRow("Scale", FormattableString.Invariant($"{scale.X:0.##}, {scale.Y:0.##}"));
                AddInspectorRow("Components", entity.Components.Count.ToString());
                break;
            case Component component:
                _inspectorHeader.Text = component.GetType().Name;
                AddInspectorRow("Type", component.GetType().Name);
                AddInspectorRow("Enabled", component.IsEnabled.ToString());
                AddInspectorRow("Entity", component.Entity?.Name ?? "<detached>");
                break;
            default:
                _inspectorHeader.Text = "Inspector";
                AddInspectorRow("Selection", "None");
                break;
        }
    }

    private void AddInspectorRow(string name, string value)
    {
        _inspectorList.Items.Add(new ListViewItem(new[] { name, value }));
    }

    private void OnEngineUpdated(object? sender, EngineUpdatedEventArgs args)
    {
        Vector2 position = _playerEntity.Transform.Value.Position;
        InputState input = args.Input;
        if (input.WasKeyPressed(Keys.R) || position.Y > 440.0f)
        {
            ResetValidationLevel();
            position = _playerEntity.Transform.Value.Position;
        }

        _renderer.Camera.Position = Vector2.Lerp(_renderer.Camera.Position, new Vector2(position.X + 160.0f, position.Y - 30.0f), 0.18f);
        UpdateGoalState();
        Point mouseDelta = input.MouseDelta;
        string sceneName = args.Scene?.Name ?? "<none>";
        string runtimeState = _engine.IsRunning ? "Playing" : "Paused";

        _runtimeStatusLabel.Text = FormattableString.Invariant(
            $"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nScene: {sceneName}\nEntity: {_playerEntity.Name}\nObjective: {(_levelComplete ? "Complete" : "Reach the gold flag")}\nPosition: ({position.X:0.00}, {position.Y:0.00})\nVelocity: ({_playerBody.Velocity.X:0.00}, {_playerBody.Velocity.Y:0.00})\nGrounded: {_playerBody.IsGrounded}\nRuntime: {runtimeState}\nMouse: ({input.MousePosition.X}, {input.MousePosition.Y}) Δ({mouseDelta.X}, {mouseDelta.Y}) Wheel: {input.MouseWheelDelta}");
        _statusStripLabel.Text = FormattableString.Invariant($"{args.Scene?.Entities.Count ?? 0} entities | Frame {args.Time.FrameCount} | Preview {runtimeState.ToLowerInvariant()} | Goal {(_levelComplete ? "complete" : "active")}");
        _viewport.Invalidate();
    }

    private void OnViewportPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _engine.ActiveScene, _viewport.ClientSize);
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_engine.IsRunning)
        {
            _engine.Stop();
            _playPauseButton.Text = "Play Preview";
            _statusStripLabel.Text = "Runtime preview paused";
            return;
        }

        _engine.Start();
        _playPauseButton.Text = "Pause Preview";
        _statusStripLabel.Text = "Runtime preview playing";
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
