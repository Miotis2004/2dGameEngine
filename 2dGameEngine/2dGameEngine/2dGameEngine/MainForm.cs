using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Animation;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;
using _2dGameEngine.Input;
using _2dGameEngine.Physics;
using _2dGameEngine.Serialization;
using _2dGameEngine.Scripting;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 15 scene editing workspace with isolated play-mode runtime preview panes.
/// </summary>
public sealed class MainForm : Form
{
    private readonly AssetManager _assets;
    private readonly Engine _engine;
    private Entity _playerEntity = null!;
    private RigidBody2D _playerBody = null!;
    private Entity _goalEntity = null!;
    private Vector2 _playerStartPosition;
    private bool _levelComplete;
    private readonly Renderer2D _renderer;
    private readonly TreeView _hierarchyTree;
    private readonly ListView _inspectorList;
    private readonly Label _inspectorHeader;
    private readonly Label _runtimeStatusLabel;
    private readonly Label _viewportOverlayLabel;
    private readonly ToolStripButton _playButton;
    private readonly ToolStripButton _pauseButton;
    private readonly ToolStripButton _stopButton;
    private readonly ToolStripButton _stepButton;
    private readonly ToolStripButton _undoButton;
    private readonly ToolStripButton _redoButton;
    private readonly ToolStripStatusLabel _statusStripLabel;
    private readonly Panel _sceneEditorViewport;
    private readonly Panel _gameViewport;
    private readonly TreeView _projectAssetsTree;
    private readonly ListBox _consoleList;
    private readonly ToolStripButton _addSpriteButton;
    private readonly ToolStripButton _addTilemapButton;
    private readonly ToolStripButton _tilePaintButton;
    private readonly ToolStripButton _duplicateButton;
    private readonly ToolStripButton _deleteButton;
    private readonly ToolStripButton _saveSceneButton;
    private readonly ToolStripButton _loadSceneButton;
    private readonly ToolStripButton _importAssetButton;
    private readonly ToolStripButton _refreshAssetsButton;
    private readonly ToolStripButton _validateAssetsButton;
    private readonly ToolStripDropDownButton _addComponentButton;
    private readonly ToolStripButton _newScriptButton;
    private readonly PictureBox _assetPreviewBox;
    private readonly ListBox _tilePaletteList;
    private readonly ContextMenuStrip _sceneContextMenu;
    private readonly ContextMenuStrip _hierarchyContextMenu;
    private Point _lastSceneContextPoint;
    private Entity? _selectedEntity;
    private readonly List<Entity> _selectedEntities = [];
    private bool _isDraggingSelection;
    private string? _dragUndoSnapshot;
    private string? _lastSavedSceneSnapshot;
    private readonly Stack<EditorSceneCommand> _undoStack = new();
    private readonly Stack<EditorSceneCommand> _redoStack = new();
    private bool _isRestoringEditorCommand;
    private bool _isSyncingHierarchySelection;
    private bool _isTilePaintMode;
    private int _selectedTileId = 1;
    private Vector2 _dragOffset;
    private readonly HashSet<Panel> _dockPanels = new();
    private readonly Dictionary<Control, DockPanelSlot> _dockSlotsByHost = new();
    private Panel? _draggedDockPanel;
    private DockPanelSlot? _dragSourceSlot;
    private Point _panelDragOffset;
    private CreatedProject? _currentProject;
    private AssetPipeline? _assetPipeline;
    private Scene _editScene = null!;
    private string? _playModeSnapshot;
    private bool _isPlayMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
    {
        Text = "Unity 2 Clone - C# 2D Editor";
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
        _editScene = CreateValidationScene(_assets);
        BindValidationSceneReferences(_editScene);

        _engine = new Engine();
        _engine.SetActiveScene(_editScene);
        _engine.Updated += OnEngineUpdated;
        _engine.ErrorOccurred += OnEngineErrorOccurred;

        ToolStrip toolStrip = new()
        {
            GripStyle = ToolStripGripStyle.Hidden,
        };
        ToolStripButton newProjectButton = new("New Project")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        ToolStripButton loadProjectButton = new("Load Project")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        newProjectButton.Click += OnNewProjectClicked;
        loadProjectButton.Click += OnLoadProjectClicked;
        _playButton = new ToolStripButton("Play")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _pauseButton = new ToolStripButton("Pause")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _stopButton = new ToolStripButton("Stop")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _undoButton = new ToolStripButton("Undo")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _redoButton = new ToolStripButton("Redo")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _stepButton = new ToolStripButton("Step")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _playButton.Click += OnPlayClicked;
        _pauseButton.Click += OnPauseClicked;
        _stopButton.Click += OnStopClicked;
        _stepButton.Click += OnStepClicked;
        _undoButton.Click += OnUndoClicked;
        _redoButton.Click += OnRedoClicked;
        _addSpriteButton = new ToolStripButton("Add Sprite")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _addTilemapButton = new ToolStripButton("Add Tilemap")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _tilePaintButton = new ToolStripButton("Tile Paint")
        {
            CheckOnClick = true,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _duplicateButton = new ToolStripButton("Duplicate")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _deleteButton = new ToolStripButton("Delete")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _saveSceneButton = new ToolStripButton("Save Scene")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _loadSceneButton = new ToolStripButton("Load Scene")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _importAssetButton = new ToolStripButton("Import Asset")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _refreshAssetsButton = new ToolStripButton("Refresh Assets")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _validateAssetsButton = new ToolStripButton("Validate Assets")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _addComponentButton = new ToolStripDropDownButton("Add Component")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        foreach (ComponentRecipe recipe in ComponentAuthoring.Recipes)
        {
            ToolStripMenuItem item = new($"{recipe.Category} / {recipe.Name}") { Tag = recipe };
            item.Click += OnAddComponentRecipeClicked;
            _addComponentButton.DropDownItems.Add(item);
        }

        _newScriptButton = new ToolStripButton("New Script")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _addSpriteButton.Click += OnAddSpriteClicked;
        _addTilemapButton.Click += OnAddTilemapClicked;
        _tilePaintButton.Click += OnTilePaintClicked;
        _duplicateButton.Click += OnDuplicateClicked;
        _deleteButton.Click += OnDeleteClicked;
        _saveSceneButton.Click += OnSaveSceneClicked;
        _loadSceneButton.Click += OnLoadSceneClicked;
        _importAssetButton.Click += OnImportAssetClicked;
        _refreshAssetsButton.Click += OnRefreshAssetsClicked;
        _validateAssetsButton.Click += OnValidateAssetsClicked;
        _newScriptButton.Click += OnNewScriptClicked;
        toolStrip.Items.Add(new ToolStripLabel("Unity 2 Clone"));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(newProjectButton);
        toolStrip.Items.Add(loadProjectButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(_playButton);
        toolStrip.Items.Add(_pauseButton);
        toolStrip.Items.Add(_stopButton);
        toolStrip.Items.Add(_stepButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(_undoButton);
        toolStrip.Items.Add(_redoButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(_addSpriteButton);
        toolStrip.Items.Add(_addTilemapButton);
        toolStrip.Items.Add(_tilePaintButton);
        toolStrip.Items.Add(_duplicateButton);
        toolStrip.Items.Add(_deleteButton);
        toolStrip.Items.Add(_saveSceneButton);
        toolStrip.Items.Add(_loadSceneButton);
        toolStrip.Items.Add(_addComponentButton);
        toolStrip.Items.Add(_newScriptButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(_importAssetButton);
        toolStrip.Items.Add(_refreshAssetsButton);
        toolStrip.Items.Add(_validateAssetsButton);

        StatusStrip statusStrip = new();
        _statusStripLabel = new ToolStripStatusLabel("Runtime preview ready");
        statusStrip.Items.Add(_statusStripLabel);

        _hierarchyTree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
        };
        _hierarchyTree.AfterSelect += OnHierarchySelectionChanged;

        _sceneContextMenu = CreateSceneContextMenu();
        _hierarchyContextMenu = CreateHierarchyContextMenu();
        _hierarchyTree.ContextMenuStrip = _hierarchyContextMenu;

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

        _sceneEditorViewport = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(24, 28, 36),
            Dock = DockStyle.Fill,
        };
        _sceneEditorViewport.Paint += OnSceneEditorPaint;
        _sceneEditorViewport.MouseDown += OnViewportMouseDown;
        _sceneEditorViewport.MouseUp += OnViewportMouseUp;
        _sceneEditorViewport.MouseMove += OnViewportMouseMove;
        _sceneEditorViewport.MouseWheel += OnViewportMouseWheel;

        _gameViewport = new DoubleBufferedPanel
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
        };
        _gameViewport.Paint += OnGameViewportPaint;
        _gameViewport.MouseDown += OnViewportMouseDown;
        _gameViewport.MouseEnter += OnGameViewportMouseEnter;
        _gameViewport.MouseUp += OnViewportMouseUp;
        _gameViewport.MouseMove += OnViewportMouseMove;
        _gameViewport.MouseWheel += OnViewportMouseWheel;

        _viewportOverlayLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.FromArgb(170, 20, 24, 32),
            Dock = DockStyle.Top,
            ForeColor = Color.White,
            Height = 42,
            Padding = new Padding(10, 6, 10, 4),
            Text = "Phase 19 scene tools - Ctrl/Shift click multi-selects, drag moves groups, Ctrl+Z/Y undo/redo, Ctrl+D duplicates",
        };
        _sceneEditorViewport.Controls.Add(_viewportOverlayLabel);

        _projectAssetsTree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
        };
        _projectAssetsTree.AfterSelect += OnProjectAssetSelectionChanged;
        _assetPreviewBox = new PictureBox
        {
            BackColor = Color.FromArgb(20, 24, 32),
            Dock = DockStyle.Bottom,
            Height = 120,
            SizeMode = PictureBoxSizeMode.Zoom,
        };
        _tilePaletteList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 9.0f),
        };
        _tilePaletteList.SelectedIndexChanged += OnTilePaletteSelectionChanged;
        PopulateTilePalette(null);
        PopulateProjectAssetsPane(null);

        _consoleList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 9.0f),
        };
        LogToConsole("Editor workspace initialized.");

        SplitContainer rootSplit = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 260,
        };

        SplitContainer leftSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 330,
        };

        SplitContainer centerRightSplit = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 760,
        };

        SplitContainer editorGameSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 310,
        };

        SplitContainer bottomSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 140,
        };

        SplitContainer inspectorConsoleSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 430,
        };

        leftSplit.Panel1.Controls.Add(CreateDockPanel("Hierarchy", _hierarchyTree));
        Panel assetsPanel = new() { Dock = DockStyle.Fill };
        assetsPanel.Controls.Add(_projectAssetsTree);
        assetsPanel.Controls.Add(_assetPreviewBox);
        leftSplit.Panel2.Controls.Add(CreateDockPanel("Project / Assets", assetsPanel));
        rootSplit.Panel1.Controls.Add(leftSplit);
        rootSplit.Panel2.Controls.Add(centerRightSplit);
        centerRightSplit.Panel1.Controls.Add(bottomSplit);
        centerRightSplit.Panel2.Controls.Add(inspectorConsoleSplit);
        bottomSplit.Panel1.Controls.Add(editorGameSplit);
        SplitContainer statusTileSplit = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 360,
        };
        statusTileSplit.Panel1.Controls.Add(CreateDockPanel("Runtime Status", _runtimeStatusLabel));
        statusTileSplit.Panel2.Controls.Add(CreateDockPanel("Tile Palette", _tilePaletteList));
        bottomSplit.Panel2.Controls.Add(statusTileSplit);
        editorGameSplit.Panel1.Controls.Add(CreateDockPanel("Scene Editor", _sceneEditorViewport));
        editorGameSplit.Panel2.Controls.Add(CreateDockPanel("Rendered Game", _gameViewport));
        inspectorConsoleSplit.Panel1.Controls.Add(CreateInspectorPanel());
        inspectorConsoleSplit.Panel2.Controls.Add(CreateDockPanel("Console", _consoleList));

        RegisterDockPanelSlots(rootSplit);

        Controls.Add(rootSplit);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);

        PopulateHierarchy(_editScene);
        _lastSavedSceneSnapshot = CaptureSceneSnapshot();
        UpdatePlayModeControls();
        KeyDown += OnFormKeyDown;
        KeyUp += OnFormKeyUp;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _engine.Stop();
        _engine.Updated -= OnEngineUpdated;
        _engine.ErrorOccurred -= OnEngineErrorOccurred;
        KeyDown -= OnFormKeyDown;
        KeyUp -= OnFormKeyUp;
        _playButton.Click -= OnPlayClicked;
        _pauseButton.Click -= OnPauseClicked;
        _stopButton.Click -= OnStopClicked;
        _stepButton.Click -= OnStepClicked;
        _addSpriteButton.Click -= OnAddSpriteClicked;
        _addTilemapButton.Click -= OnAddTilemapClicked;
        _tilePaintButton.Click -= OnTilePaintClicked;
        _tilePaletteList.SelectedIndexChanged -= OnTilePaletteSelectionChanged;
        _duplicateButton.Click -= OnDuplicateClicked;
        _deleteButton.Click -= OnDeleteClicked;
        _saveSceneButton.Click -= OnSaveSceneClicked;
        _loadSceneButton.Click -= OnLoadSceneClicked;
        _importAssetButton.Click -= OnImportAssetClicked;
        _refreshAssetsButton.Click -= OnRefreshAssetsClicked;
        _validateAssetsButton.Click -= OnValidateAssetsClicked;
        _newScriptButton.Click -= OnNewScriptClicked;
        _undoButton.Click -= OnUndoClicked;
        _redoButton.Click -= OnRedoClicked;
        foreach (ToolStripItem item in _addComponentButton.DropDownItems)
        {
            item.Click -= OnAddComponentRecipeClicked;
        }
        _projectAssetsTree.AfterSelect -= OnProjectAssetSelectionChanged;
        _hierarchyTree.AfterSelect -= OnHierarchySelectionChanged;
        _sceneEditorViewport.Paint -= OnSceneEditorPaint;
        _gameViewport.Paint -= OnGameViewportPaint;
        _sceneEditorViewport.MouseDown -= OnViewportMouseDown;
        _sceneEditorViewport.MouseUp -= OnViewportMouseUp;
        _sceneEditorViewport.MouseMove -= OnViewportMouseMove;
        _sceneEditorViewport.MouseWheel -= OnViewportMouseWheel;
        _gameViewport.MouseDown -= OnViewportMouseDown;
        _gameViewport.MouseEnter -= OnGameViewportMouseEnter;
        _gameViewport.MouseUp -= OnViewportMouseUp;
        _gameViewport.MouseMove -= OnViewportMouseMove;
        _gameViewport.MouseWheel -= OnViewportMouseWheel;
        _assets.Dispose();
        base.OnFormClosed(e);
    }

    private Panel CreateDockPanel(string title, Control content)
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
        };
        Label header = new()
        {
            BackColor = Color.FromArgb(38, 44, 56),
            Cursor = Cursors.SizeAll,
            Dock = DockStyle.Top,
            ForeColor = Color.White,
            Height = 28,
            Padding = new Padding(8, 6, 8, 0),
            Text = title,
        };
        header.MouseDown += OnDockPanelHeaderMouseDown;
        header.MouseMove += OnDockPanelHeaderMouseMove;
        header.MouseUp += OnDockPanelHeaderMouseUp;
        _dockPanels.Add(panel);
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Controls.Add(header);
        return panel;
    }


    private void OnDockPanelHeaderMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control header || header.Parent is not Panel panel)
        {
            return;
        }

        BeginDockPanelDrag(panel, header, e.Location);
    }

    private void OnDockPanelHeaderMouseMove(object? sender, MouseEventArgs e)
    {
        if (_draggedDockPanel is null || sender is not Control header || !header.Capture)
        {
            return;
        }

        Point cursorInForm = PointToClient(header.PointToScreen(e.Location));
        Point targetLocation = new(cursorInForm.X - _panelDragOffset.X, cursorInForm.Y - _panelDragOffset.Y);
        _draggedDockPanel.Location = ClampDockPanelLocation(_draggedDockPanel, targetLocation);
    }

    private void OnDockPanelHeaderMouseUp(object? sender, MouseEventArgs e)
    {
        if (sender is Control header)
        {
            header.Capture = false;
        }

        if (_draggedDockPanel is not null)
        {
            CompleteDockPanelDrag(_draggedDockPanel);
        }

        _draggedDockPanel = null;
        _dragSourceSlot = null;
    }

    private void BeginDockPanelDrag(Panel panel, Control header, Point headerMouseLocation)
    {
        _draggedDockPanel = panel;
        _dragSourceSlot = GetDockPanelSlot(panel);
        _panelDragOffset = panel.PointToClient(header.PointToScreen(headerMouseLocation));

        if (_dragSourceSlot is not null)
        {
            FloatDockPanel(panel, _dragSourceSlot);
        }

        panel.BringToFront();
        header.Capture = true;
    }


    private void RegisterDockPanelSlots(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Panel panel && _dockPanels.Contains(panel) && panel.Parent is not null)
            {
                _dockSlotsByHost[panel.Parent] = new DockPanelSlot(panel.Parent, panel);
            }

            RegisterDockPanelSlots(child);
        }
    }

    private DockPanelSlot? GetDockPanelSlot(Panel panel)
    {
        if (panel.Parent is not null && _dockSlotsByHost.TryGetValue(panel.Parent, out DockPanelSlot? parentSlot) && parentSlot.Panel == panel)
        {
            return parentSlot;
        }

        return _dockSlotsByHost.Values.FirstOrDefault(slot => slot.Panel == panel);
    }

    private void FloatDockPanel(Panel panel, DockPanelSlot sourceSlot)
    {
        Rectangle floatingBounds = RectangleToClient(panel.RectangleToScreen(panel.ClientRectangle));

        sourceSlot.Host.Controls.Remove(panel);
        sourceSlot.Host.Controls.Add(sourceSlot.Placeholder);
        sourceSlot.Panel = null;

        panel.Dock = DockStyle.None;
        panel.Bounds = floatingBounds;
        Controls.Add(panel);
    }

    private void CompleteDockPanelDrag(Panel panel)
    {
        Point cursorInForm = PointToClient(Cursor.Position);
        DockPanelSlot? targetSlot = FindDockPanelDropSlot(cursorInForm) ?? _dragSourceSlot;
        if (targetSlot is null)
        {
            return;
        }

        Panel? swappedPanel = targetSlot.Panel;
        SnapDockPanelToSlot(panel, targetSlot);

        if (swappedPanel is not null && swappedPanel != panel && _dragSourceSlot is not null)
        {
            SnapDockPanelToSlot(swappedPanel, _dragSourceSlot);
        }
    }

    private DockPanelSlot? FindDockPanelDropSlot(Point cursorInForm)
    {
        DockPanelSlot? nearestOpenSlot = null;
        double nearestOpenDistance = double.MaxValue;

        foreach (DockPanelSlot slot in _dockSlotsByHost.Values)
        {
            Rectangle slotBounds = RectangleToClient(slot.Host.RectangleToScreen(slot.Host.ClientRectangle));
            if (slotBounds.Contains(cursorInForm))
            {
                return slot;
            }

            if (slot.Panel is null)
            {
                double distance = GetDistanceSquared(cursorInForm, slotBounds);
                if (distance < nearestOpenDistance)
                {
                    nearestOpenSlot = slot;
                    nearestOpenDistance = distance;
                }
            }
        }

        return nearestOpenSlot;
    }

    private void SnapDockPanelToSlot(Panel panel, DockPanelSlot slot)
    {
        if (panel.Parent is not null)
        {
            panel.Parent.Controls.Remove(panel);
        }

        slot.Host.Controls.Remove(slot.Placeholder);
        panel.Dock = DockStyle.Fill;
        slot.Host.Controls.Add(panel);
        slot.Panel = panel;
    }

    private static double GetDistanceSquared(Point point, Rectangle bounds)
    {
        int closestX = Math.Clamp(point.X, bounds.Left, bounds.Right);
        int closestY = Math.Clamp(point.Y, bounds.Top, bounds.Bottom);
        int deltaX = point.X - closestX;
        int deltaY = point.Y - closestY;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private Point ClampDockPanelLocation(Control panel, Point targetLocation)
    {
        int maxX = Math.Max(0, ClientSize.Width - panel.Width);
        int maxY = Math.Max(0, ClientSize.Height - panel.Height);
        return new Point(
            Math.Clamp(targetLocation.X, 0, maxX),
            Math.Clamp(targetLocation.Y, 0, maxY));
    }


    private sealed class DockPanelSlot
    {
        public DockPanelSlot(Control host, Panel panel)
        {
            Host = host;
            Panel = panel;
            Placeholder = new Panel
            {
                BackColor = Color.FromArgb(30, 34, 44),
                Dock = DockStyle.Fill,
            };
        }

        public Control Host { get; }

        public Panel? Panel { get; set; }

        public Panel Placeholder { get; }
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



    private ContextMenuStrip CreateSceneContextMenu()
    {
        ContextMenuStrip menu = new();
        menu.Items.Add(CreateMenuItem("Add Rectangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Rectangle, _lastSceneContextPoint)));
        menu.Items.Add(CreateMenuItem("Add Circle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Circle, _lastSceneContextPoint)));
        menu.Items.Add(CreateMenuItem("Add Triangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Triangle, _lastSceneContextPoint)));
        menu.Items.Add(CreateMenuItem("Add Tilemap", (_, _) => AddTilemap(_lastSceneContextPoint)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateMenuItem("Duplicate Selected", OnDuplicateClicked));
        menu.Items.Add(CreateMenuItem("Delete Selected", OnDeleteClicked));
        return menu;
    }

    private ContextMenuStrip CreateHierarchyContextMenu()
    {
        ContextMenuStrip menu = new();
        menu.Opening += (_, _) =>
        {
            Point clientPoint = _hierarchyTree.PointToClient(Cursor.Position);
            TreeNode? node = _hierarchyTree.GetNodeAt(clientPoint);
            if (node is not null)
            {
                _hierarchyTree.SelectedNode = node;
            }
        };
        menu.Items.Add(CreateMenuItem("Add Rectangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Rectangle, Point.Empty)));
        menu.Items.Add(CreateMenuItem("Add Circle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Circle, Point.Empty)));
        menu.Items.Add(CreateMenuItem("Add Triangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Triangle, Point.Empty)));
        menu.Items.Add(CreateMenuItem("Add Tilemap", (_, _) => AddTilemap(Point.Empty)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateMenuItem("Duplicate", OnDuplicateClicked));
        menu.Items.Add(CreateMenuItem("Delete", OnDeleteClicked));
        return menu;
    }

    private static ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick)
    {
        ToolStripMenuItem item = new(text);
        item.Click += onClick;
        return item;
    }

    private static Scene CreateValidationScene(AssetManager assets)
    {
        SpriteSheetAsset tileSprites = assets.LoadSpriteSheet("Assets/demo-tiles.spritesheet.json");
        AnimationClip playerIdle = assets.LoadAnimationClip("Assets/player-idle.animation.json");

        Scene scene = new("Phase 10 Platformer Validation Level");
        Vector2 playerStartPosition = new(-460.0f, 64.0f);
        Entity playerEntity = scene.CreateEntity("Player Controller");
        playerEntity.Transform.Value.Position = playerStartPosition;
        playerEntity.AddComponent(new RigidBody2D());
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

        Entity goalEntity = scene.CreateEntity("Goal Flag");
        goalEntity.Transform.Value.Position = new Vector2(840.0f, 68.0f);
        goalEntity.AddComponent(new BoxCollider2D(new Vector2(44.0f, 96.0f)) { IsTrigger = true });
        goalEntity.AddComponent(new SpriteRenderer(new Vector2(44.0f, 96.0f), Color.Gold) { OutlineColor = Color.OrangeRed, SortingOrder = 8 });

        return scene;
    }

    private void BindValidationSceneReferences(Scene scene)
    {
        _playerEntity = scene.Entities.First(entity => entity.Name == "Player Controller");
        _playerBody = _playerEntity.GetComponent<RigidBody2D>() ?? throw new InvalidDataException("Validation player is missing RigidBody2D.");
        _goalEntity = scene.Entities.First(entity => entity.Name == "Goal Flag");
        _playerStartPosition = _playerEntity.Transform.Value.Position;
    }

    private void TryBindValidationSceneReferences(Scene scene)
    {
        Entity? player = scene.Entities.FirstOrDefault(entity => entity.Name == "Player Controller");
        Entity? goal = scene.Entities.FirstOrDefault(entity => entity.Name == "Goal Flag");
        RigidBody2D? body = player?.GetComponent<RigidBody2D>();
        if (player is null || goal is null || body is null)
        {
            _levelComplete = false;
            return;
        }

        _playerEntity = player;
        _playerBody = body;
        _goalEntity = goal;
        _playerStartPosition = player.Transform.Value.Position;
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
        _hierarchyTree.SelectedNode = FindEntityNode(sceneNode, _selectedEntity) ?? (sceneNode.Nodes.Count > 0 ? sceneNode.Nodes[0] : sceneNode);
        _hierarchyTree.EndUpdate();
    }



    private static TreeNode? FindEntityNode(TreeNode root, Entity? entity)
    {
        if (entity is null)
        {
            return null;
        }

        foreach (TreeNode node in root.Nodes)
        {
            if (ReferenceEquals(node.Tag, entity))
            {
                return node;
            }
        }

        return null;
    }

    private void SelectEntity(Entity? entity, bool additive = false)
    {
        if (!additive)
        {
            _selectedEntities.Clear();
        }

        if (entity is not null)
        {
            if (additive && _selectedEntities.Contains(entity))
            {
                _selectedEntities.Remove(entity);
            }
            else if (!_selectedEntities.Contains(entity))
            {
                _selectedEntities.Add(entity);
            }
        }

        _selectedEntity = _selectedEntities.LastOrDefault();
        TreeNode? match = _hierarchyTree.Nodes.Count > 0 ? FindEntityNode(_hierarchyTree.Nodes[0], _selectedEntity) : null;
        if (match is not null && !ReferenceEquals(_hierarchyTree.SelectedNode, match))
        {
            _isSyncingHierarchySelection = true;
            try
            {
                _hierarchyTree.SelectedNode = match;
            }
            finally
            {
                _isSyncingHierarchySelection = false;
            }
        }
        else
        {
            ShowInspector(_selectedEntities.Count > 1 ? _selectedEntities.ToArray() : _selectedEntity);
        }

        _deleteButton.Enabled = !_isPlayMode && _selectedEntities.Count > 0;
        _duplicateButton.Enabled = !_isPlayMode && _selectedEntities.Count > 0;
        _addComponentButton.Enabled = !_isPlayMode && _selectedEntity is not null;
        _newScriptButton.Enabled = !_isPlayMode && _selectedEntity is not null;
        _sceneEditorViewport.Invalidate();
    }

    private IEnumerable<Entity> ActiveSelection() => _selectedEntities.Count == 0 && _selectedEntity is not null ? [_selectedEntity] : _selectedEntities;

    private string CaptureSceneSnapshot() => SceneSerializer.Serialize(_editScene);

    private bool IsSceneDirty => _lastSavedSceneSnapshot is not null && CaptureSceneSnapshot() != _lastSavedSceneSnapshot;

    private void PushSceneCommand(string name, string beforeSnapshot)
    {
        if (_isRestoringEditorCommand || _isPlayMode)
        {
            return;
        }

        string afterSnapshot = CaptureSceneSnapshot();
        if (beforeSnapshot == afterSnapshot)
        {
            return;
        }

        _undoStack.Push(new EditorSceneCommand(name, beforeSnapshot, afterSnapshot));
        _redoStack.Clear();
        LogToConsole($"Recorded editor command: {name}.");
        UpdatePlayModeControls();
    }

    private void RestoreSceneSnapshot(string snapshot)
    {
        string? selectedName = _selectedEntity?.Name;
        _isRestoringEditorCommand = true;
        try
        {
            _editScene = SceneSerializer.Deserialize(snapshot, _assets);
            _engine.SetActiveScene(_editScene);
            TryBindValidationSceneReferences(_editScene);
            _selectedEntities.Clear();
            Entity? restoredSelection = string.IsNullOrWhiteSpace(selectedName) ? null : _editScene.Entities.FirstOrDefault(entity => entity.Name == selectedName);
            PopulateHierarchy(_editScene);
            SelectEntity(restoredSelection);
            _sceneEditorViewport.Invalidate();
            _gameViewport.Invalidate();
        }
        finally
        {
            _isRestoringEditorCommand = false;
        }
    }

    private void OnUndoClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode() || _undoStack.Count == 0)
        {
            return;
        }

        EditorSceneCommand command = _undoStack.Pop();
        _redoStack.Push(command);
        RestoreSceneSnapshot(command.BeforeSnapshot);
        LogToConsole($"Undo: {command.Name}.");
        UpdatePlayModeControls();
    }

    private void OnRedoClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode() || _redoStack.Count == 0)
        {
            return;
        }

        EditorSceneCommand command = _redoStack.Pop();
        _undoStack.Push(command);
        RestoreSceneSnapshot(command.AfterSnapshot);
        LogToConsole($"Redo: {command.Name}.");
        UpdatePlayModeControls();
    }

    private RectangleF GetSelectionBounds()
    {
        RectangleF bounds = GetEntityBounds(ActiveSelection().First());
        foreach (Entity entity in ActiveSelection().Skip(1))
        {
            bounds = RectangleF.Union(bounds, GetEntityBounds(entity));
        }

        return bounds;
    }

    private Vector2 GetSelectionPivot()
    {
        RectangleF bounds = GetSelectionBounds();
        return new Vector2(bounds.X + bounds.Width / 2.0f, bounds.Y + bounds.Height / 2.0f);
    }

    private void OnAddSpriteClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        Scene? scene = _engine.ActiveScene;
        if (scene is null)
        {
            return;
        }

        AddPrimitiveSprite(SpritePrimitiveType.Rectangle, Point.Empty);
    }



    private void AddPrimitiveSprite(SpritePrimitiveType primitiveType, Point viewportPoint)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        Scene? scene = _engine.ActiveScene;
        if (scene is null)
        {
            return;
        }

        string before = CaptureSceneSnapshot();
        string primitiveName = primitiveType == SpritePrimitiveType.Rectangle ? "Sprite Entity" : $"{primitiveType} Sprite";
        Entity entity = scene.CreateEntity(GetUniqueEntityName(scene, primitiveName));
        entity.Transform.Value.Position = viewportPoint == Point.Empty ? _renderer.Camera.Position : ScreenToWorld(viewportPoint, _sceneEditorViewport.ClientSize);
        entity.AddComponent(new SpriteRenderer(new Vector2(64.0f, 64.0f), GetPrimitiveColor(primitiveType))
        {
            OutlineColor = Color.White,
            SortingOrder = 5,
            PrimitiveType = primitiveType,
        });

        LogToConsole($"Added {primitiveType.ToString().ToLowerInvariant()} sprite entity '{entity.Name}'.");
        PopulateHierarchy(_editScene);
        UpdatePlayModeControls();
        SelectEntity(entity);
        PushSceneCommand($"Add {primitiveType} sprite", before);
    }

    private static Color GetPrimitiveColor(SpritePrimitiveType primitiveType) => primitiveType switch
    {
        SpritePrimitiveType.Circle => Color.MediumSeaGreen,
        SpritePrimitiveType.Triangle => Color.Coral,
        _ => Color.MediumPurple,
    };


    private void OnAddTilemapClicked(object? sender, EventArgs e)
    {
        AddTilemap(Point.Empty);
    }

    private void OnTilePaintClicked(object? sender, EventArgs e)
    {
        _isTilePaintMode = _tilePaintButton.Checked && _selectedEntity?.GetComponent<Tilemap>() is not null;
        _statusStripLabel.Text = _isTilePaintMode ? "Tile paint mode: left paints, right erases" : "Tile paint mode disabled";
        _sceneEditorViewport.Invalidate();
    }

    private void OnTilePaletteSelectionChanged(object? sender, EventArgs e)
    {
        if (_tilePaletteList.SelectedItem is TilePaletteItem item)
        {
            _selectedTileId = item.TileId;
            _statusStripLabel.Text = $"Selected tile {item.TileId}: {item.Name}";
        }
    }

    private void AddTilemap(Point viewportPoint)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        Scene? scene = _engine.ActiveScene;
        if (scene is null)
        {
            return;
        }

        string before = CaptureSceneSnapshot();
        Entity entity = scene.CreateEntity(GetUniqueEntityName(scene, "Tilemap Level"));
        entity.Transform.Value.Position = viewportPoint == Point.Empty ? _renderer.Camera.Position - new Vector2(320.0f, 160.0f) : ScreenToWorld(viewportPoint, _sceneEditorViewport.ClientSize);
        Tilemap tilemap = entity.AddComponent(new Tilemap(20, 12, new Vector2(32.0f, 32.0f)) { SortingOrder = -5 });
        AddDefaultTileDefinitions(tilemap);
        entity.AddComponent(new TilemapCollider2D());
        PopulateTilePalette(tilemap);
        PopulateHierarchy(_editScene);
        SelectEntity(entity);
        _tilePaintButton.Checked = true;
        _isTilePaintMode = true;
        LogToConsole($"Added editable tilemap '{entity.Name}'.");
        PushSceneCommand("Add tilemap", before);
    }

    private static void AddDefaultTileDefinitions(Tilemap tilemap)
    {
        tilemap.SetDefinition(new TileDefinition(1, Color.ForestGreen, true));
        tilemap.SetDefinition(new TileDefinition(2, Color.SaddleBrown, true));
        tilemap.SetDefinition(new TileDefinition(3, Color.Orange, true));
        tilemap.SetDefinition(new TileDefinition(4, Color.DeepSkyBlue, false));
    }

    private void PopulateTilePalette(Tilemap? tilemap)
    {
        _tilePaletteList.Items.Clear();
        _tilePaletteList.Items.Add(new TilePaletteItem(0, "Eraser / Empty"));
        foreach (TileDefinition definition in (tilemap?.Definitions.Values ?? Enumerable.Empty<TileDefinition>()).OrderBy(definition => definition.Id))
        {
            _tilePaletteList.Items.Add(new TilePaletteItem(definition.Id, definition.IsSolid ? "Solid" : "Decor"));
        }

        _tilePaletteList.SelectedIndex = _tilePaletteList.Items.Count > 1 ? 1 : 0;
    }

    private void OnDuplicateClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        Scene? scene = _engine.ActiveScene;
        if (scene is null || _selectedEntity is null)
        {
            return;
        }

        string before = CaptureSceneSnapshot();
        List<Entity> duplicates = [];
        foreach (Entity entity in ActiveSelection().ToArray())
        {
            Entity duplicate = DuplicateEntity(scene, entity);
            duplicates.Add(duplicate);
            LogToConsole($"Duplicated '{entity.Name}' as '{duplicate.Name}'.");
        }

        PopulateHierarchy(_editScene);
        UpdatePlayModeControls();
        SelectEntity(null);
        foreach (Entity duplicate in duplicates)
        {
            SelectEntity(duplicate, true);
        }

        PushSceneCommand("Duplicate selection", before);
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        Scene? scene = _engine.ActiveScene;
        if (scene is null || _selectedEntity is null)
        {
            return;
        }

        string before = CaptureSceneSnapshot();
        List<Entity> deleted = ActiveSelection().ToList();
        foreach (Entity entity in deleted)
        {
            scene.RemoveEntity(entity);
            LogToConsole($"Deleted entity '{entity.Name}'.");
        }

        _selectedEntity = null;
        _selectedEntities.Clear();
        _isDraggingSelection = false;
        PopulateHierarchy(_editScene);
        UpdatePlayModeControls();
        ShowInspector(scene);
        _sceneEditorViewport.Invalidate();
        PushSceneCommand("Delete selection", before);
    }

    private void OnSaveSceneClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        Scene? scene = _engine.ActiveScene;
        if (scene is null)
        {
            return;
        }

        string sceneDirectory = _currentProject?.ScenesDirectory ?? Path.Combine(AppContext.BaseDirectory, "Scenes");
        string scenePath = Path.Combine(sceneDirectory, $"{SanitizeFileName(scene.Name)}.scene.json");
        try
        {
            SceneSerializer.Save(scene, scenePath);
            PopulateProjectAssetsPane(_currentProject);
            LogToConsole($"Saved scene to {scenePath}");
            _lastSavedSceneSnapshot = CaptureSceneSnapshot();
            _statusStripLabel.Text = $"Scene saved: {scenePath}";
        }
        catch (Exception ex)
        {
            LogToConsole($"Scene save failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Scene Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }



    private void OnLoadSceneClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        using OpenFileDialog dialog = new()
        {
            Filter = "Scene files|*.scene.json;*.json|All files|*.*",
            InitialDirectory = _currentProject?.ScenesDirectory ?? Path.Combine(AppContext.BaseDirectory, "Scenes"),
            Title = "Load Scene",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            Scene scene = SceneSerializer.Load(dialog.FileName, _assets);
            _engine.Stop();
            _editScene = scene;
            _engine.SetActiveScene(_editScene);
            TryBindValidationSceneReferences(_editScene);
            SelectEntity(null);
            PopulateHierarchy(_editScene);
            ShowInspector(_editScene);
            _sceneEditorViewport.Invalidate();
            _gameViewport.Invalidate();
            LogToConsole($"Loaded scene '{scene.Name}' from {dialog.FileName}");
            _undoStack.Clear();
            _redoStack.Clear();
            _lastSavedSceneSnapshot = CaptureSceneSnapshot();
            _statusStripLabel.Text = $"Scene loaded: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            LogToConsole($"Scene load failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Scene Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Scene" : safe;
    }

    private static string GetUniqueEntityName(Scene scene, string baseName)
    {
        if (!scene.Entities.Any(entity => entity.Name == baseName))
        {
            return baseName;
        }

        int suffix = 2;
        while (scene.Entities.Any(entity => entity.Name == $"{baseName} {suffix}"))
        {
            suffix++;
        }

        return $"{baseName} {suffix}";
    }

    private static Entity DuplicateEntity(Scene scene, Entity source)
    {
        Entity duplicate = scene.CreateEntity(GetUniqueEntityName(scene, $"{source.Name} Copy"));
        duplicate.IsEnabled = source.IsEnabled;
        duplicate.Transform.Value.Position = source.Transform.Value.Position + new Vector2(32.0f, -32.0f);
        duplicate.Transform.Value.Rotation = source.Transform.Value.Rotation;
        duplicate.Transform.Value.Scale = source.Transform.Value.Scale;

        foreach (Component component in source.Components.Where(component => component is not TransformComponent))
        {
            if (CloneComponent(component) is Component clone)
            {
                clone.IsEnabled = component.IsEnabled;
                duplicate.AddComponent(clone);
            }
        }

        return duplicate;
    }

    private static Component? CloneComponent(Component component)
    {
        return component switch
        {
            SpriteRenderer sprite => new SpriteRenderer(sprite.Size, sprite.Color) { OutlineColor = sprite.OutlineColor, SortingOrder = sprite.SortingOrder, Frame = sprite.Frame, PrimitiveType = sprite.PrimitiveType },
            BoxCollider2D box => new BoxCollider2D(box.Size) { Offset = box.Offset, IsTrigger = box.IsTrigger },
            RigidBody2D body => new RigidBody2D { Velocity = body.Velocity, GravityScale = body.GravityScale, IsKinematic = body.IsKinematic },
            EntityMotionComponent motion => new EntityMotionComponent(motion.Velocity),
            EntityInputMovementComponent movement => new EntityInputMovementComponent(movement.Speed),
            PlatformerMovementComponent platformer => new PlatformerMovementComponent(platformer.MoveSpeed, platformer.JumpSpeed),
            TilemapCollider2D collider => new TilemapCollider2D { Offset = collider.Offset, IsTrigger = collider.IsTrigger },
            Tilemap tilemap => CloneTilemap(tilemap),
            AuthoredScriptComponent script => new AuthoredScriptComponent(script.ClassName, script.ScriptPath) { Description = script.Description },
            _ => null,
        };
    }



    private static Tilemap CloneTilemap(Tilemap source)
    {
        Tilemap clone = new(source.Width, source.Height, source.TileSize)
        {
            SortingOrder = source.SortingOrder,
        };
        foreach (TileDefinition definition in source.Definitions.Values)
        {
            clone.SetDefinition(new TileDefinition(definition.Id, definition.Color, definition.IsSolid) { Frame = definition.Frame });
        }
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                clone.SetTile(x, y, source.GetTile(x, y));
            }
        }
        return clone;
    }

    private void OnAddComponentRecipeClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        if (_selectedEntity is null || sender is not ToolStripMenuItem { Tag: ComponentRecipe recipe })
        {
            return;
        }

        Component component = recipe.Factory();
        string before = CaptureSceneSnapshot();
        _selectedEntity.AddComponent(component);
        LogToConsole($"Added {recipe.Name} to '{_selectedEntity.Name}'.");
        PopulateHierarchy(_engine.ActiveScene!);
        ShowInspector(component);
        PushSceneCommand($"Add {recipe.Name} component", before);
    }

    private void OnNewScriptClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }

        if (_selectedEntity is null)
        {
            return;
        }

        if (_currentProject is null)
        {
            MessageBox.Show(this, "Create a project before authoring scripts so the source file has a game assembly.", "No Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string gameSourceDirectory = Path.Combine(_currentProject.ProjectDirectory, "src", $"{_currentProject.SafeName}.Game");
        string before = CaptureSceneSnapshot();
        AuthoredScriptComponent script = ComponentAuthoring.CreateScript(gameSourceDirectory, _currentProject.SafeName, $"{_selectedEntity.Name}Behavior");
        script.Properties["TargetEntity"] = _selectedEntity.Name;
        _selectedEntity.AddComponent(script);
        PopulateProjectAssetsPane(_currentProject);
        PopulateHierarchy(_engine.ActiveScene!);
        ShowInspector(script);
        LogToConsole("C# is the only supported scripting language; no visual scripting or alternate language assets were generated.");
        LogToConsole($"Created script '{script.ClassName}' and attached it to '{_selectedEntity.Name}'.");
        _statusStripLabel.Text = $"Script created: {script.ScriptPath}";
        PushSceneCommand("Create C# script component", before);
    }

    private void OnNewProjectClicked(object? sender, EventArgs e)
    {
        using NewProjectDialog dialog = new();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _currentProject = EditorProjectScaffolder.CreateProject(dialog.ProjectRootDirectory, dialog.ProjectName);
            _assetPipeline = new AssetPipeline(_currentProject.AssetsDirectory);
            _assetPipeline.Refresh();
            PopulateProjectAssetsPane(_currentProject);
            LogToConsole($"Created project '{_currentProject.DisplayName}' at {_currentProject.ProjectDirectory}");
            _statusStripLabel.Text = $"Project created: {_currentProject.SafeName}";
        }
        catch (Exception ex)
        {
            LogToConsole($"Project creation failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Project Creation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }



    private void OnLoadProjectClicked(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select a Unity 2 C# project folder",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _currentProject = EditorProjectScaffolder.LoadProject(dialog.SelectedPath);
            _assetPipeline = new AssetPipeline(_currentProject.AssetsDirectory);
            _assetPipeline.Refresh();
            PopulateProjectAssetsPane(_currentProject);
            LogToConsole($"Loaded project '{_currentProject.DisplayName}' from {_currentProject.ProjectDirectory}");
            _statusStripLabel.Text = $"Project loaded: {_currentProject.SafeName}";
        }
        catch (Exception ex)
        {
            LogToConsole($"Project load failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Project Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateProjectAssetsPane(CreatedProject? project)
    {
        _projectAssetsTree.Nodes.Clear();
        TreeNode root = new(project?.DisplayName ?? "No project loaded");
        if (project is not null)
        {
            root.Nodes.Add(new TreeNode($"Solution: {Path.GetFileName(project.SolutionPath)}") { Tag = project.SolutionPath });
            root.Nodes.Add(new TreeNode("Scenes") { Tag = project.ScenesDirectory });
            TreeNode assetsNode = new("Assets") { Tag = project.AssetsDirectory };
            foreach (AssetMetadata asset in (_assetPipeline ??= new AssetPipeline(project.AssetsDirectory)).Refresh())
            {
                assetsNode.Nodes.Add(new TreeNode($"{asset.Kind}: {asset.RelativePath}") { Tag = asset });
            }

            root.Nodes.Add(assetsNode);
        }
        else
        {
            root.Nodes.Add("Use New Project to create a Unity-style 2D C# solution.");
        }

        _projectAssetsTree.Nodes.Add(root);
        root.ExpandAll();
    }

    private void OnImportAssetClicked(object? sender, EventArgs e)
    {
        if (_currentProject is null)
        {
            MessageBox.Show(this, "Create a project before importing assets.", "No Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using OpenFileDialog dialog = new()
        {
            Filter = "Supported assets|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.wav;*.mp3;*.ogg;*.flac|All files|*.*",
            Multiselect = true,
            Title = "Import Assets",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _assetPipeline ??= new AssetPipeline(_currentProject.AssetsDirectory);
        foreach (string fileName in dialog.FileNames)
        {
            AssetMetadata asset = _assetPipeline.Import(fileName);
            LogToConsole($"Imported {asset.Kind} asset '{asset.RelativePath}' with metadata.");
        }

        PopulateProjectAssetsPane(_currentProject);
        _statusStripLabel.Text = "Asset import complete";
    }

    private void OnRefreshAssetsClicked(object? sender, EventArgs e)
    {
        if (_currentProject is null) return;
        _assetPipeline ??= new AssetPipeline(_currentProject.AssetsDirectory);
        int count = _assetPipeline.Refresh().Count;
        PopulateProjectAssetsPane(_currentProject);
        LogToConsole($"Refreshed {count} asset(s) and metadata files.");
    }

    private void OnValidateAssetsClicked(object? sender, EventArgs e)
    {
        if (_currentProject is null) return;
        _assetPipeline ??= new AssetPipeline(_currentProject.AssetsDirectory);
        foreach (AssetValidationResult result in _assetPipeline.Validate())
        {
            LogToConsole($"{(result.IsValid ? "OK" : "Invalid")}: {result.Metadata.RelativePath} - {result.Message}");
        }

        PopulateProjectAssetsPane(_currentProject);
    }

    private void OnProjectAssetSelectionChanged(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not AssetMetadata asset) return;
        ShowInspector(asset);
        _assetPreviewBox.Image?.Dispose();
        _assetPreviewBox.Image = null;
        if (_currentProject is not null && asset.Kind == AssetKind.Texture)
        {
            string fullPath = Path.Combine(_currentProject.AssetsDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            using Image image = Image.FromFile(fullPath);
            _assetPreviewBox.Image = new Bitmap(image);
        }
    }

    private void LogToConsole(string message)
    {
        _consoleList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        _consoleList.TopIndex = Math.Max(0, _consoleList.Items.Count - 1);
    }

    private void OnHierarchySelectionChanged(object? sender, TreeViewEventArgs e)
    {
        if (_isSyncingHierarchySelection)
        {
            ShowInspector(_selectedEntities.Count > 1 ? _selectedEntities.ToArray() : _selectedEntity);
            return;
        }

        _selectedEntity = e.Node?.Tag as Entity;
        _selectedEntities.Clear();
        if (_selectedEntity is not null)
        {
            _selectedEntities.Add(_selectedEntity);
        }

        _deleteButton.Enabled = !_isPlayMode && _selectedEntities.Count > 0;
        _duplicateButton.Enabled = !_isPlayMode && _selectedEntities.Count > 0;
        _addComponentButton.Enabled = !_isPlayMode && _selectedEntity is not null;
        _newScriptButton.Enabled = !_isPlayMode && _selectedEntity is not null;
        ShowInspector(e.Node?.Tag);
        PopulateTilePalette(_selectedEntity?.GetComponent<Tilemap>());
        UpdatePlayModeControls();
        _sceneEditorViewport.Invalidate();
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
            case Entity[] entities:
                _inspectorHeader.Text = $"{entities.Length} Entities";
                AddInspectorRow("Type", "Multi-selection");
                AddInspectorRow("Entities", string.Join(", ", entities.Select(entity => entity.Name)));
                AddInspectorRow("Pivot", FormattableString.Invariant($"{GetSelectionPivot().X:0.##}, {GetSelectionPivot().Y:0.##}"));
                AddInspectorRow("Tools", "Move / Rotate / Scale / Rect / Collider gizmos");
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
            case AssetMetadata asset:
                _inspectorHeader.Text = Path.GetFileName(asset.RelativePath);
                AddInspectorRow("Type", $"Asset ({asset.Kind})");
                AddInspectorRow("Path", asset.RelativePath);
                AddInspectorRow("Metadata", asset.RelativePath + AssetMetadata.MetadataExtension);
                AddInspectorRow("Imported", asset.ImportedAtUtc.ToLocalTime().ToString("g"));
                if (asset.Width is not null && asset.Height is not null)
                {
                    AddInspectorRow("Size", $"{asset.Width} x {asset.Height}");
                    AddInspectorRow("Slice", $"{asset.SliceWidth} x {asset.SliceHeight}");
                }

                break;
            case AuthoredScriptComponent script:
                _inspectorHeader.Text = script.ClassName;
                AddInspectorRow("Type", "C# MonoBehaviour Script");
                AddInspectorRow("Language", "C# only");
                AddInspectorRow("Enabled", script.IsEnabled.ToString());
                AddInspectorRow("Entity", script.Entity?.Name ?? "<detached>");
                AddInspectorRow("Source", script.ScriptPath);
                AddInspectorRow("Properties", script.Properties.Count.ToString());
                break;
            case Tilemap tilemap:
                _inspectorHeader.Text = "Tilemap";
                AddInspectorRow("Type", "Tilemap");
                AddInspectorRow("Enabled", tilemap.IsEnabled.ToString());
                AddInspectorRow("Size", $"{tilemap.Width} x {tilemap.Height}");
                AddInspectorRow("Tile Size", FormattableString.Invariant($"{tilemap.TileSize.X:0.##}, {tilemap.TileSize.Y:0.##}"));
                AddInspectorRow("Definitions", tilemap.Definitions.Count.ToString());
                AddInspectorRow("Sorting Order", tilemap.SortingOrder.ToString());
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
        if (args.Scene is null || !args.Scene.Entities.Contains(_playerEntity))
        {
            string genericState = _engine.IsRunning ? "Playing" : "Paused";
            _runtimeStatusLabel.Text = FormattableString.Invariant($"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nScene: {args.Scene?.Name ?? "<none>"}\nEntities: {args.Scene?.Entities.Count ?? 0}\nRuntime: {genericState}");
            _statusStripLabel.Text = FormattableString.Invariant($"{args.Scene?.Entities.Count ?? 0} entities | Frame {args.Time.FrameCount} | Preview {genericState.ToLowerInvariant()}{(IsSceneDirty ? " | Unsaved" : string.Empty)}");
            _sceneEditorViewport.Invalidate();
            _gameViewport.Invalidate();
            return;
        }

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
        _statusStripLabel.Text = FormattableString.Invariant($"{args.Scene?.Entities.Count ?? 0} entities | Frame {args.Time.FrameCount} | Preview {runtimeState.ToLowerInvariant()} | Goal {(_levelComplete ? "complete" : "active")}{(IsSceneDirty ? " | Unsaved" : string.Empty)}");
        _sceneEditorViewport.Invalidate();
        _gameViewport.Invalidate();
    }

    private void OnSceneEditorPaint(object? sender, PaintEventArgs e)
    {
        DrawViewportGrid(e.Graphics, _sceneEditorViewport.ClientSize);
        _renderer.Render(e.Graphics, _engine.ActiveScene, _sceneEditorViewport.ClientSize);
        DrawSelectionOverlay(e.Graphics, _sceneEditorViewport.ClientSize);
        DrawTilemapEditingOverlay(e.Graphics, _sceneEditorViewport.ClientSize);
    }

    private void OnGameViewportPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _engine.ActiveScene, _gameViewport.ClientSize);
    }

    private static void DrawViewportGrid(System.Drawing.Graphics graphics, Size size)
    {
        using Pen majorPen = new(Color.FromArgb(55, 76, 91, 112));
        using Pen minorPen = new(Color.FromArgb(28, 76, 91, 112));
        for (int x = 0; x < size.Width; x += 32)
        {
            graphics.DrawLine(x % 128 == 0 ? majorPen : minorPen, x, 0, x, size.Height);
        }

        for (int y = 0; y < size.Height; y += 32)
        {
            graphics.DrawLine(y % 128 == 0 ? majorPen : minorPen, 0, y, size.Width, y);
        }
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!_isPlayMode)
            {
                _playModeSnapshot = SceneSerializer.Serialize(_editScene);
                Scene runtimeScene = SceneSerializer.Deserialize(_playModeSnapshot, _assets);
                _engine.SetActiveScene(runtimeScene);
                TryBindValidationSceneReferences(runtimeScene);
                SelectEntity(null);
                PopulateHierarchy(runtimeScene);
                _isPlayMode = true;
                LogToConsole("Play mode started from an isolated scene snapshot.");
            }

            _engine.Start();
            _gameViewport.Focus();
            _statusStripLabel.Text = "Play mode running in isolated runtime scene";
        }
        catch (Exception ex)
        {
            ReportRuntimeError(ex);
        }

        UpdatePlayModeControls();
    }

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        _engine.Stop();
        LogToConsole(_isPlayMode ? "Play mode paused." : "Edit preview paused.");
        _statusStripLabel.Text = _isPlayMode ? "Play mode paused" : "Runtime preview paused";
        UpdatePlayModeControls();
    }

    private void OnStepClicked(object? sender, EventArgs e)
    {
        if (!_isPlayMode)
        {
            OnPlayClicked(sender, e);
            _engine.Stop();
        }

        _engine.Step();
        LogToConsole("Advanced play mode by one simulation frame.");
        _statusStripLabel.Text = "Play mode single-step advanced";
        UpdatePlayModeControls();
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _engine.Stop();
        if (_isPlayMode)
        {
            RestoreEditSceneAfterPlay();
            LogToConsole("Play mode stopped; restored the edit scene snapshot.");
        }
        else
        {
            ResetValidationLevel();
            LogToConsole("Runtime preview stopped and scene reset.");
        }

        _sceneEditorViewport.Invalidate();
        _gameViewport.Invalidate();
        _statusStripLabel.Text = "Play mode stopped; edit scene restored";
        UpdatePlayModeControls();
    }

    private bool EnsureEditMode()
    {
        if (!_isPlayMode)
        {
            return true;
        }

        LogToConsole("Edit operation blocked while play mode is active. Stop play mode to modify the scene.");
        _statusStripLabel.Text = "Stop play mode before editing the scene";
        return false;
    }

    private void RestoreEditSceneAfterPlay()
    {
        string? selectedName = _selectedEntity?.Name;
        _editScene = _playModeSnapshot is null ? _editScene : SceneSerializer.Deserialize(_playModeSnapshot, _assets);
        _playModeSnapshot = null;
        _isPlayMode = false;
        _engine.SetActiveScene(_editScene);
        TryBindValidationSceneReferences(_editScene);
        PopulateHierarchy(_editScene);
        SelectEntity(string.IsNullOrWhiteSpace(selectedName) ? null : _editScene.Entities.FirstOrDefault(entity => entity.Name == selectedName));
    }

    private void UpdatePlayModeControls()
    {
        _playButton.Enabled = !_engine.IsRunning;
        _undoButton.Enabled = !_isPlayMode && _undoStack.Count > 0;
        _redoButton.Enabled = !_isPlayMode && _redoStack.Count > 0;
        _pauseButton.Enabled = _engine.IsRunning;
        _stopButton.Enabled = _isPlayMode || _engine.IsRunning;
        _stepButton.Enabled = !_engine.IsRunning;
        _addSpriteButton.Enabled = !_isPlayMode;
        _addTilemapButton.Enabled = !_isPlayMode;
        _tilePaintButton.Enabled = !_isPlayMode && _selectedEntity?.GetComponent<Tilemap>() is not null;
        if (!_tilePaintButton.Enabled)
        {
            _tilePaintButton.Checked = false;
            _isTilePaintMode = false;
        }
        _duplicateButton.Enabled = !_isPlayMode && _selectedEntities.Count > 0;
        _deleteButton.Enabled = !_isPlayMode && _selectedEntities.Count > 0;
        _saveSceneButton.Enabled = !_isPlayMode;
        _loadSceneButton.Enabled = !_isPlayMode;
        _addComponentButton.Enabled = !_isPlayMode && _selectedEntity is not null;
        _newScriptButton.Enabled = !_isPlayMode && _selectedEntity is not null;
    }

    private void OnEngineErrorOccurred(object? sender, Exception ex)
    {
        ReportRuntimeError(ex);
    }

    private void ReportRuntimeError(Exception ex)
    {
        LogToConsole($"Runtime error: {ex.GetType().Name}: {ex.Message}");
        _statusStripLabel.Text = "Runtime error reported in console; play mode paused";
        UpdatePlayModeControls();
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            OnUndoClicked(sender, e);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.Y)
        {
            OnRedoClicked(sender, e);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D)
        {
            OnDuplicateClicked(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Delete)
        {
            OnDeleteClicked(sender, e);
            e.Handled = true;
            return;
        }

        if (_isPlayMode && _gameViewport.Focused)
        {
            _engine.Input.SetKeyDown(e.KeyCode);
        }
    }

    private void OnFormKeyUp(object? sender, KeyEventArgs e)
    {
        if (_isPlayMode && _gameViewport.Focused)
        {
            _engine.Input.SetKeyUp(e.KeyCode);
        }
    }

    private void OnViewportMouseDown(object? sender, MouseEventArgs e)
    {
        ((Control?)sender)?.Focus();
        if (sender == _gameViewport && _isPlayMode)
        {
            _engine.Input.SetMouseButtonDown(e.Button);
            _engine.Input.SetMousePosition(e.Location);
        }

        if (sender == _sceneEditorViewport && !_isPlayMode)
        {
            Vector2 world = ScreenToWorld(e.Location, _sceneEditorViewport.ClientSize);
            if (_isTilePaintMode && _selectedEntity?.GetComponent<Tilemap>() is Tilemap activeTilemap && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
            {
                PaintTile(activeTilemap, world, e.Button == MouseButtons.Right ? 0 : _selectedTileId);
                return;
            }

            Entity? hit = HitTestEntity(world);
            bool additiveSelection = (ModifierKeys & (Keys.Control | Keys.Shift)) != 0;
            SelectEntity(hit, additiveSelection);
            if (e.Button == MouseButtons.Left && hit is not null)
            {
                _isDraggingSelection = true;
                _dragUndoSnapshot = CaptureSceneSnapshot();
                _dragOffset = hit.Transform.Value.Position - world;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _lastSceneContextPoint = e.Location;
                _sceneContextMenu.Show(_sceneEditorViewport, e.Location);
            }
        }
    }

    private void OnViewportMouseUp(object? sender, MouseEventArgs e)
    {
        if (sender == _gameViewport && _isPlayMode)
        {
            _engine.Input.SetMouseButtonUp(e.Button);
            _engine.Input.SetMousePosition(e.Location);
        }
        if (e.Button == MouseButtons.Left)
        {
            if (_isDraggingSelection && _dragUndoSnapshot is not null)
            {
                PushSceneCommand("Move selection", _dragUndoSnapshot);
                _dragUndoSnapshot = null;
            }

            _isDraggingSelection = false;
        }
    }

    private void OnViewportMouseMove(object? sender, MouseEventArgs e)
    {
        if (sender == _gameViewport && _isPlayMode)
        {
            _engine.Input.SetMousePosition(e.Location);
        }

        if (sender == _sceneEditorViewport && !_isPlayMode && _isTilePaintMode && _selectedEntity?.GetComponent<Tilemap>() is Tilemap activeTilemap && e.Button == MouseButtons.Left)
        {
            PaintTile(activeTilemap, ScreenToWorld(e.Location, _sceneEditorViewport.ClientSize), _selectedTileId);
            return;
        }

        if (sender == _sceneEditorViewport && !_isPlayMode && _isDraggingSelection && _selectedEntity is not null)
        {
            Vector2 targetPosition = ScreenToWorld(e.Location, _sceneEditorViewport.ClientSize) + _dragOffset;
            Vector2 delta = targetPosition - _selectedEntity.Transform.Value.Position;
            foreach (Entity entity in ActiveSelection())
            {
                entity.Transform.Value.Position += delta;
            }

            ShowInspector(_selectedEntity);
            _sceneEditorViewport.Invalidate();
            _gameViewport.Invalidate();
        }
    }

    private void OnViewportMouseWheel(object? sender, MouseEventArgs e)
    {
        if (sender == _gameViewport && _isPlayMode)
        {
            _engine.Input.AddMouseWheelDelta(e.Delta);
            _engine.Input.SetMousePosition(e.Location);
        }
    }


    private void OnGameViewportMouseEnter(object? sender, EventArgs e)
    {
        _gameViewport.Focus();
    }


    private void PaintTile(Tilemap tilemap, Vector2 world, int tileId)
    {
        Vector2 origin = tilemap.Entity?.Transform.Value.Position ?? Vector2.Zero;
        int x = (int)MathF.Floor((world.X - origin.X) / tilemap.TileSize.X);
        int y = (int)MathF.Floor((world.Y - origin.Y) / tilemap.TileSize.Y);
        if (x < 0 || x >= tilemap.Width || y < 0 || y >= tilemap.Height)
        {
            return;
        }

        int previousTile = tilemap.GetTile(x, y);
        if (previousTile == tileId)
        {
            return;
        }

        string before = CaptureSceneSnapshot();
        tilemap.SetTile(x, y, tileId);
        PushSceneCommand(tileId == 0 ? "Erase tile" : "Paint tile", before);
        ShowInspector(tilemap);
        _statusStripLabel.Text = tileId == 0 ? $"Erased tile ({x}, {y})" : $"Painted tile {tileId} at ({x}, {y})";
        _sceneEditorViewport.Invalidate();
        _gameViewport.Invalidate();
    }

    private Vector2 ScreenToWorld(Point screenPosition, Size viewportSize)
    {
        float zoom = MathF.Max(0.01f, _renderer.Camera.Zoom);
        Vector2 viewportCenter = new(viewportSize.Width / 2.0f, viewportSize.Height / 2.0f);
        return (new Vector2(screenPosition.X, screenPosition.Y) - viewportCenter) / zoom + _renderer.Camera.Position;
    }

    private Entity? HitTestEntity(Vector2 worldPosition)
    {
        Scene? scene = _engine.ActiveScene;
        if (scene is null)
        {
            return null;
        }

        return scene.Entities.Reverse().FirstOrDefault(entity => entity.IsEnabled && GetEntityBounds(entity).Contains(worldPosition.X, worldPosition.Y));
    }

    private static RectangleF GetEntityBounds(Entity entity)
    {
        if (entity.GetComponent<SpriteRenderer>() is SpriteRenderer sprite)
        {
            Vector2 position = entity.Transform.Value.Position;
            Vector2 scale = entity.Transform.Value.Scale;
            float width = MathF.Max(1.0f, sprite.Size.X * scale.X);
            float height = MathF.Max(1.0f, sprite.Size.Y * scale.Y);
            return new RectangleF(position.X - width / 2.0f, position.Y - height / 2.0f, width, height);
        }

        if (entity.GetComponent<BoxCollider2D>() is BoxCollider2D box)
        {
            return box.GetBounds();
        }

        if (entity.GetComponent<Tilemap>() is Tilemap tilemap)
        {
            Vector2 origin = entity.Transform.Value.Position;
            return new RectangleF(origin.X, origin.Y, tilemap.Width * tilemap.TileSize.X, tilemap.Height * tilemap.TileSize.Y);
        }

        Vector2 fallback = entity.Transform.Value.Position;
        return new RectangleF(fallback.X - 12.0f, fallback.Y - 12.0f, 24.0f, 24.0f);
    }

    private void DrawSelectionOverlay(System.Drawing.Graphics graphics, Size viewportSize)
    {
        if (_selectedEntities.Count == 0)
        {
            return;
        }

        RectangleF worldBounds = GetSelectionBounds();
        PointF screenPosition = _renderer.Camera.WorldToScreen(new Vector2(worldBounds.X, worldBounds.Y), viewportSize);
        float zoom = MathF.Max(0.01f, _renderer.Camera.Zoom);
        RectangleF screenBounds = new(screenPosition.X, screenPosition.Y, worldBounds.Width * zoom, worldBounds.Height * zoom);
        using Pen selectionPen = new(Color.DeepSkyBlue, 2.0f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        graphics.DrawRectangle(selectionPen, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);

        PointF center = _renderer.Camera.WorldToScreen(GetSelectionPivot(), viewportSize);
        using SolidBrush brush = new(Color.DeepSkyBlue);
        graphics.FillEllipse(brush, center.X - 4.0f, center.Y - 4.0f, 8.0f, 8.0f);
        using Pen xAxis = new(Color.OrangeRed, 2.0f);
        using Pen yAxis = new(Color.LimeGreen, 2.0f);
        graphics.DrawLine(xAxis, center.X, center.Y, center.X + 42.0f, center.Y);
        graphics.DrawLine(yAxis, center.X, center.Y, center.X, center.Y - 42.0f);
        graphics.DrawString($"{_selectedEntities.Count} selected | Move gizmo | Global | Snap 16px", Font, brush, center.X + 8.0f, center.Y + 8.0f);
    }


    private void DrawTilemapEditingOverlay(System.Drawing.Graphics graphics, Size viewportSize)
    {
        if (_selectedEntity?.GetComponent<Tilemap>() is not Tilemap tilemap)
        {
            return;
        }

        float zoom = MathF.Max(0.01f, _renderer.Camera.Zoom);
        using Pen gridPen = new(_isTilePaintMode ? Color.Lime : Color.FromArgb(130, Color.DeepSkyBlue), 1.0f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
        for (int x = 0; x <= tilemap.Width; x++)
        {
            RectangleF bounds = tilemap.GetBounds();
            PointF top = _renderer.Camera.WorldToScreen(new Vector2(bounds.X + x * tilemap.TileSize.X, bounds.Y), viewportSize);
            graphics.DrawLine(gridPen, top.X, top.Y, top.X, top.Y + bounds.Height * zoom);
        }

        for (int y = 0; y <= tilemap.Height; y++)
        {
            RectangleF bounds = tilemap.GetBounds();
            PointF left = _renderer.Camera.WorldToScreen(new Vector2(bounds.X, bounds.Y + y * tilemap.TileSize.Y), viewportSize);
            graphics.DrawLine(gridPen, left.X, left.Y, left.X + bounds.Width * zoom, left.Y);
        }
    }

    private sealed record EditorSceneCommand(string Name, string BeforeSnapshot, string AfterSnapshot);

    private sealed record TilePaletteItem(int TileId, string Name)
    {
        public override string ToString() => TileId == 0 ? Name : $"{TileId}: {Name}";
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
