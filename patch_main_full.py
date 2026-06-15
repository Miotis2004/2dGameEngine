import re

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "r") as f:
    content = f.read()

# 1. Inspector Grid Replacements
content = content.replace("private readonly ListView _inspectorList;", "private readonly PropertyGrid _inspectorGrid;")

old_list_view = """        _inspectorList = new ListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            View = View.Details,
        };
        _inspectorList.Columns.Add("Property", 130);
        _inspectorList.Columns.Add("Value", 220);"""

new_grid = """        _inspectorGrid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            PropertySort = PropertySort.CategorizedAlphabetical,
        };
        _inspectorGrid.PropertyValueChanged += OnInspectorPropertyValueChanged;"""
content = content.replace(old_list_view, new_grid)
content = content.replace("panel.Controls.Add(_inspectorList);", "panel.Controls.Add(_inspectorGrid);")

start_idx = content.find("private void ShowInspector(object? selected)")
end_idx = content.find("private void OnEngineUpdated", start_idx)
new_show_inspector = """    private void ShowInspector(object? selected)
    {
        _inspectorGrid.SelectedObject = null;

        switch (selected)
        {
            case Scene scene:
                _inspectorHeader.Text = scene.Name;
                _inspectorGrid.SelectedObject = scene;
                break;
            case Entity[] entities:
                _inspectorHeader.Text = $"{entities.Length} Entities";
                _inspectorGrid.SelectedObjects = entities;
                break;
            case Entity entity:
                _inspectorHeader.Text = entity.Name;
                _inspectorGrid.SelectedObject = new EntityWrapper(entity);
                break;
            case EditorPackageInfo package:
                _inspectorHeader.Text = package.Manifest.DisplayName;
                _inspectorGrid.SelectedObject = package;
                break;
            case EditorExtensionInfo extension:
                _inspectorHeader.Text = extension.Manifest.DisplayName;
                _inspectorGrid.SelectedObject = extension;
                break;
            case AssetMetadata asset:
                _inspectorHeader.Text = Path.GetFileName(asset.RelativePath);
                _inspectorGrid.SelectedObject = asset;
                break;
            case Component component:
                _inspectorHeader.Text = component.GetType().Name;
                _inspectorGrid.SelectedObject = component;
                break;
            default:
                _inspectorHeader.Text = "Inspector";
                break;
        }
    }

    private void OnInspectorPropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
    {
        if (!EnsureEditMode())
        {
            return;
        }
        _sceneEditorViewport.Invalidate();
        _gameViewport.Invalidate();
    }

    private class EntityWrapper
    {
        private readonly Entity _entity;

        public EntityWrapper(Entity entity)
        {
            _entity = entity;
        }

        [System.ComponentModel.Category("Entity")]
        public string Name
        {
            get => _entity.Name;
            set => _entity.Name = value;
        }

        [System.ComponentModel.Category("Entity")]
        public bool IsEnabled
        {
            get => _entity.IsEnabled;
            set => _entity.IsEnabled = value;
        }

        [System.ComponentModel.Category("Transform")]
        [System.ComponentModel.TypeConverter(typeof(Vector2Converter))]
        public Vector2 Position
        {
            get => _entity.Transform.Value.Position;
            set => _entity.Transform.Value.Position = value;
        }

        [System.ComponentModel.Category("Transform")]
        public float Rotation
        {
            get => _entity.Transform.Value.Rotation;
            set => _entity.Transform.Value.Rotation = value;
        }

        [System.ComponentModel.Category("Transform")]
        [System.ComponentModel.TypeConverter(typeof(Vector2Converter))]
        public Vector2 Scale
        {
            get => _entity.Transform.Value.Scale;
            set => _entity.Transform.Value.Scale = value;
        }
    }

"""
content = content[:start_idx] + new_show_inspector + content[end_idx:]

# 2. Context Menu additions
content = content.replace(
    'menu.Items.Add(CreateMenuItem("Add Rectangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Rectangle, _lastSceneContextPoint)));',
    'menu.Items.Add(CreateMenuItem("Create Empty Entity", (_, _) => AddEmptyEntity(_lastSceneContextPoint)));\n        menu.Items.Add(CreateMenuItem("Add Rectangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Rectangle, _lastSceneContextPoint)));'
)
content = content.replace(
    'menu.Items.Add(CreateMenuItem("Add Rectangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Rectangle, Point.Empty)));',
    'menu.Items.Add(CreateMenuItem("Create Empty Entity", (_, _) => AddEmptyEntity(Point.Empty)));\n        menu.Items.Add(CreateMenuItem("Add Rectangle Sprite", (_, _) => AddPrimitiveSprite(SpritePrimitiveType.Rectangle, Point.Empty)));'
)
add_empty_func = """    private void AddEmptyEntity(Point viewportPoint)
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
        Entity entity = scene.CreateEntity(GetUniqueEntityName(scene, "Entity"));
        entity.Transform.Value.Position = viewportPoint == Point.Empty ? _renderer.Camera.Position : ScreenToWorld(viewportPoint, _sceneEditorViewport.ClientSize);

        LogToConsole($"Added empty entity '{entity.Name}'.");
        PopulateHierarchy(_editScene);
        PopulateEffectsEditor();
        UpdatePlayModeControls();
        SelectEntity(entity);
        PushSceneCommand("Add empty entity", before);
    }

"""
add_primitive_idx = content.find("private void AddPrimitiveSprite")
content = content[:add_primitive_idx] + add_empty_func + content[add_primitive_idx:]

new_opening = """        menu.Opening += (_, _) =>
        {
            Point clientPoint = _hierarchyTree.PointToClient(Cursor.Position);
            TreeNode? node = _hierarchyTree.GetNodeAt(clientPoint);
            if (node is not null)
            {
                _hierarchyTree.SelectedNode = node;
            }

            bool isComponent = node?.Tag is Component;
            foreach (ToolStripItem item in menu.Items)
            {
                if (item.Text == "Remove Component")
                {
                    item.Visible = isComponent;
                }
                else
                {
                    item.Visible = !isComponent;
                }
            }
        };"""
content = content.replace("""        menu.Opening += (_, _) =>
        {
            Point clientPoint = _hierarchyTree.PointToClient(Cursor.Position);
            TreeNode? node = _hierarchyTree.GetNodeAt(clientPoint);
            if (node is not null)
            {
                _hierarchyTree.SelectedNode = node;
            }
        };""", new_opening)

content = content.replace(
    'menu.Items.Add(CreateMenuItem("Delete", OnDeleteClicked));',
    'menu.Items.Add(CreateMenuItem("Delete", OnDeleteClicked));\n        menu.Items.Add(CreateMenuItem("Remove Component", OnRemoveComponentClicked));'
)
remove_component_method = """    private void OnRemoveComponentClicked(object? sender, EventArgs e)
    {
        if (!EnsureEditMode()) return;
        if (_hierarchyTree.SelectedNode?.Tag is Component component && component.Entity is not null)
        {
            string before = CaptureSceneSnapshot();
            component.Entity.RemoveComponent(component);
            LogToConsole($"Removed component {component.GetType().Name} from '{component.Entity.Name}'.");
            PopulateHierarchy(_editScene);
            ShowInspector(component.Entity);
            PushSceneCommand("Remove component", before);
        }
    }
"""
idx_del = content.find("private void OnDeleteClicked")
content = content[:idx_del] + remove_component_method + content[idx_del:]

# 3. Gizmo + Grid Snapping
content = content.replace("private Vector2 _dragOffset;", "private Vector2 _dragOffset;\n    private GizmoAxis _activeGizmoAxis = GizmoAxis.None;\n    private bool _isDraggingGizmo;")
gizmo_enum = """    private enum GizmoAxis
    {
        None,
        X,
        Y,
        Center
    }
"""
content = content.replace("private sealed class DockPanelSlot", gizmo_enum + "\n    private sealed class DockPanelSlot")
draw_gizmo = """
    private GizmoAxis HitTestGizmo(PointF screenPosition, PointF gizmoCenter, out float distance)
    {
        float xDist = MathF.Abs(screenPosition.X - gizmoCenter.X);
        float yDist = MathF.Abs(screenPosition.Y - gizmoCenter.Y);

        if (xDist <= 8.0f && yDist <= 8.0f)
        {
            distance = 0;
            return GizmoAxis.Center;
        }

        distance = float.MaxValue;
        GizmoAxis axis = GizmoAxis.None;

        if (screenPosition.X > gizmoCenter.X && screenPosition.X <= gizmoCenter.X + 46.0f && yDist <= 6.0f)
        {
            axis = GizmoAxis.X;
            distance = yDist;
        }

        if (screenPosition.Y < gizmoCenter.Y && screenPosition.Y >= gizmoCenter.Y - 46.0f && xDist <= 6.0f)
        {
            if (distance == float.MaxValue || xDist < distance)
            {
                axis = GizmoAxis.Y;
                distance = xDist;
            }
        }

        return axis;
    }
"""
content = content.replace("using Pen xAxis = new(Color.OrangeRed, 2.0f);", "using Pen xAxis = new(_activeGizmoAxis == GizmoAxis.X ? Color.Yellow : Color.OrangeRed, 2.0f);")
content = content.replace("using Pen yAxis = new(Color.LimeGreen, 2.0f);", "using Pen yAxis = new(_activeGizmoAxis == GizmoAxis.Y ? Color.Yellow : Color.LimeGreen, 2.0f);")
content = content.replace("using SolidBrush brush = new(Color.DeepSkyBlue);", "using SolidBrush brush = new(_activeGizmoAxis == GizmoAxis.Center ? Color.Yellow : Color.DeepSkyBlue);")
content = content.replace("graphics.DrawLine(xAxis, center.X, center.Y, center.X + 42.0f, center.Y);", "graphics.DrawLine(xAxis, center.X, center.Y, center.X + 42.0f, center.Y);\n        graphics.FillPolygon(new SolidBrush(xAxis.Color), new PointF[] { new PointF(center.X + 42.0f, center.Y - 4.0f), new PointF(center.X + 42.0f, center.Y + 4.0f), new PointF(center.X + 50.0f, center.Y) });")
content = content.replace("graphics.DrawLine(yAxis, center.X, center.Y, center.X, center.Y - 42.0f);", "graphics.DrawLine(yAxis, center.X, center.Y, center.X, center.Y - 42.0f);\n        graphics.FillPolygon(new SolidBrush(yAxis.Color), new PointF[] { new PointF(center.X - 4.0f, center.Y - 42.0f), new PointF(center.X + 4.0f, center.Y - 42.0f), new PointF(center.X, center.Y - 50.0f) });")
idx_draw_overlay = content.find("private void DrawSelectionOverlay")
content = content[:idx_draw_overlay] + draw_gizmo + content[idx_draw_overlay:]

new_mouse_down_logic = """
            if (_selectedEntities.Count > 0)
            {
                PointF gizmoCenter = _renderer.Camera.WorldToScreen(GetSelectionPivot(), _sceneEditorViewport.ClientSize);
                GizmoAxis hitAxis = HitTestGizmo(new PointF(e.X, e.Y), gizmoCenter, out _);
                if (hitAxis != GizmoAxis.None)
                {
                    _isDraggingGizmo = true;
                    _activeGizmoAxis = hitAxis;
                    _dragUndoSnapshot = CaptureSceneSnapshot();
                    _dragOffset = GetSelectionPivot() - world;
                    return;
                }
            }

            Entity? hit = HitTestEntity(world);
"""
content = content.replace("Entity? hit = HitTestEntity(world);", new_mouse_down_logic)

new_mouse_up_logic = """        if (e.Button == MouseButtons.Left)
        {
            if ((_isDraggingSelection || _isDraggingGizmo) && _dragUndoSnapshot is not null)
            {
                PushSceneCommand("Move selection", _dragUndoSnapshot);
                _dragUndoSnapshot = null;
            }

            _isDraggingSelection = false;
            _isDraggingGizmo = false;
        }"""
content = content.replace("""        if (e.Button == MouseButtons.Left)
        {
            if (_isDraggingSelection && _dragUndoSnapshot is not null)
            {
                PushSceneCommand("Move selection", _dragUndoSnapshot);
                _dragUndoSnapshot = null;
            }

            _isDraggingSelection = false;
        }""", new_mouse_up_logic)


# Snapping stuff
content = content.replace("private readonly ToolStripButton _redoButton;", "private readonly ToolStripButton _redoButton;\n    private readonly ToolStripButton _snapToGridButton;")
btn_init = """        _snapToGridButton = new ToolStripButton("Snap: 16px")
        {
            CheckOnClick = true,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _snapToGridButton.CheckedChanged += (s, e) => { _sceneEditorViewport.Invalidate(); };
"""
idx_btn = content.find("        _addSpriteButton = new ToolStripButton(\"Add Sprite\")")
content = content[:idx_btn] + btn_init + content[idx_btn:]

content = content.replace("toolStrip.Items.Add(_addSpriteButton);", "toolStrip.Items.Add(_snapToGridButton);\n        toolStrip.Items.Add(new ToolStripSeparator());\n        toolStrip.Items.Add(_addSpriteButton);")


start_mouse_move = content.find("private void OnViewportMouseMove(object? sender, MouseEventArgs e)")
end_mouse_move = content.find("private void OnViewportMouseWheel(object? sender, MouseEventArgs e)")
new_mouse_move_method = """private void OnViewportMouseMove(object? sender, MouseEventArgs e)
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

        if (sender == _sceneEditorViewport && !_isPlayMode)
        {
            if (_selectedEntities.Count > 0 && !_isDraggingSelection && !_isDraggingGizmo)
            {
                PointF gizmoCenter = _renderer.Camera.WorldToScreen(GetSelectionPivot(), _sceneEditorViewport.ClientSize);
                GizmoAxis hitAxis = HitTestGizmo(new PointF(e.X, e.Y), gizmoCenter, out _);
                if (hitAxis != _activeGizmoAxis)
                {
                    _activeGizmoAxis = hitAxis;
                    _sceneEditorViewport.Invalidate();
                }
            }

            if (_isDraggingGizmo && _selectedEntities.Count > 0)
            {
                Vector2 targetPosition = ScreenToWorld(e.Location, _sceneEditorViewport.ClientSize) + _dragOffset;
                if (_snapToGridButton.Checked)
                {
                    targetPosition.X = MathF.Round(targetPosition.X / 16.0f) * 16.0f;
                    targetPosition.Y = MathF.Round(targetPosition.Y / 16.0f) * 16.0f;
                }
                Vector2 pivot = GetSelectionPivot();
                Vector2 delta = Vector2.Zero;

                if (_activeGizmoAxis == GizmoAxis.X)
                {
                    delta = new Vector2(targetPosition.X - pivot.X, 0);
                }
                else if (_activeGizmoAxis == GizmoAxis.Y)
                {
                    delta = new Vector2(0, targetPosition.Y - pivot.Y);
                }
                else if (_activeGizmoAxis == GizmoAxis.Center)
                {
                    delta = targetPosition - pivot;
                }

                foreach (Entity entity in ActiveSelection())
                {
                    entity.Transform.Value.Position += delta;
                }

                ShowInspector(_selectedEntity);
                PopulateEffectsEditor();
                _sceneEditorViewport.Invalidate();
                _gameViewport.Invalidate();
                return;
            }
        }

        if (sender == _sceneEditorViewport && !_isPlayMode && _isDraggingSelection && _selectedEntity is not null && !_isDraggingGizmo)
        {
            Vector2 targetPosition = ScreenToWorld(e.Location, _sceneEditorViewport.ClientSize) + _dragOffset;
            if (_snapToGridButton.Checked)
            {
                targetPosition.X = MathF.Round(targetPosition.X / 16.0f) * 16.0f;
                targetPosition.Y = MathF.Round(targetPosition.Y / 16.0f) * 16.0f;
            }
            Vector2 delta = targetPosition - _selectedEntity.Transform.Value.Position;
            foreach (Entity entity in ActiveSelection())
            {
                entity.Transform.Value.Position += delta;
            }

            ShowInspector(_selectedEntity);
            PopulateEffectsEditor();
            _sceneEditorViewport.Invalidate();
            _gameViewport.Invalidate();
        }
    }

    """
content = content[:start_mouse_move] + new_mouse_move_method + content[end_mouse_move:]

content = content.replace("Snap 16px\", Font, brush", "Snap 16px\" + (_snapToGridButton.Checked ? \" (ON)\" : \" (OFF)\"), Font, brush")

# 4. Drag Drop Asset
setup_dragdrop = """        _sceneEditorViewport = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(24, 28, 36),
            Dock = DockStyle.Fill,
            AllowDrop = true,
        };
        _sceneEditorViewport.DragEnter += OnViewportDragEnter;
        _sceneEditorViewport.DragDrop += OnViewportDragDrop;"""
content = content.replace("""        _sceneEditorViewport = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(24, 28, 36),
            Dock = DockStyle.Fill,
        };""", setup_dragdrop)

drag_handlers = """
    private void OnViewportDragEnter(object? sender, DragEventArgs e)
    {
        if (!EnsureEditMode()) return;
        if (e.Data is not null && e.Data.GetDataPresent(typeof(TreeNode)))
        {
            TreeNode node = (TreeNode)e.Data.GetData(typeof(TreeNode))!;
            if (node.Tag is AssetMetadata asset && asset.Kind == AssetKind.Texture)
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }
        }
        e.Effect = DragDropEffects.None;
    }

    private void OnViewportDragDrop(object? sender, DragEventArgs e)
    {
        if (!EnsureEditMode()) return;
        if (e.Data is not null && e.Data.GetDataPresent(typeof(TreeNode)))
        {
            TreeNode node = (TreeNode)e.Data.GetData(typeof(TreeNode))!;
            if (node.Tag is AssetMetadata asset && asset.Kind == AssetKind.Texture)
            {
                Point clientPoint = _sceneEditorViewport.PointToClient(new Point(e.X, e.Y));
                Vector2 worldPosition = ScreenToWorld(clientPoint, _sceneEditorViewport.ClientSize);
                if (_snapToGridButton.Checked)
                {
                    worldPosition.X = MathF.Round(worldPosition.X / 16.0f) * 16.0f;
                    worldPosition.Y = MathF.Round(worldPosition.Y / 16.0f) * 16.0f;
                }

                Scene? scene = _engine.ActiveScene;
                if (scene is null) return;

                string before = CaptureSceneSnapshot();
                Entity entity = scene.CreateEntity(GetUniqueEntityName(scene, Path.GetFileNameWithoutExtension(asset.RelativePath)));
                entity.Transform.Value.Position = worldPosition;

                Vector2 size = new Vector2(asset.Width ?? 64.0f, asset.Height ?? 64.0f);

                entity.AddComponent(new SpriteRenderer(size, Color.White)
                {
                    Frame = new _2dGameEngine.Content.SpriteFrame("dragged", _assets.LoadTexture(asset.RelativePath), new Rectangle(0, 0, (int)size.X, (int)size.Y)),
                    SortingOrder = 5
                });

                LogToConsole($"Added sprite entity '{entity.Name}' from dragged texture.");
                PopulateHierarchy(_editScene);
                PopulateEffectsEditor();
                UpdatePlayModeControls();
                SelectEntity(entity);
                PushSceneCommand("Drop texture as sprite", before);
            }
        }
    }
"""
idx_down = content.find("private void OnViewportMouseDown")
content = content[:idx_down] + drag_handlers + content[idx_down:]

content = content.replace(
    "_projectAssetsTree.AfterSelect += OnProjectAssetSelectionChanged;",
    "_projectAssetsTree.AfterSelect += OnProjectAssetSelectionChanged;\n        _projectAssetsTree.ItemDrag += OnProjectAssetItemDrag;"
)
item_drag = """
    private void OnProjectAssetItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode node && node.Tag is AssetMetadata)
        {
            DoDragDrop(node, DragDropEffects.Copy);
        }
    }
"""
idx_proj_sel = content.find("private void OnProjectAssetSelectionChanged")
content = content[:idx_proj_sel] + item_drag + content[idx_proj_sel:]

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "w") as f:
    f.write(content)
