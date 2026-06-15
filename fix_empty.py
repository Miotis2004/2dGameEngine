import re

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "r") as f:
    content = f.read()

# Change Point to Point? to avoid 0,0 issue for AddEmptyEntity

old_method = """    private void AddEmptyEntity(Point viewportPoint)
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
    }"""

new_method = """    private void AddEmptyEntity(Point? viewportPoint)
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
        entity.Transform.Value.Position = viewportPoint is null ? _renderer.Camera.Position : ScreenToWorld(viewportPoint.Value, _sceneEditorViewport.ClientSize);

        LogToConsole($"Added empty entity '{entity.Name}'.");
        PopulateHierarchy(_editScene);
        PopulateEffectsEditor();
        UpdatePlayModeControls();
        SelectEntity(entity);
        PushSceneCommand("Add empty entity", before);
    }"""
content = content.replace(old_method, new_method)

content = content.replace(
    'menu.Items.Add(CreateMenuItem("Create Empty Entity", (_, _) => AddEmptyEntity(Point.Empty)));',
    'menu.Items.Add(CreateMenuItem("Create Empty Entity", (_, _) => AddEmptyEntity(null)));'
)

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "w") as f:
    f.write(content)
