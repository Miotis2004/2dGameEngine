with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "r") as f:
    content = f.read()

content = content.replace(
    "_snapToGridButton.CheckedChanged += (s, e) => { _sceneEditorViewport.Invalidate(); };",
    "_snapToGridButton.CheckedChanged += (s, e) => { _sceneEditorViewport?.Invalidate(); };"
)

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "w") as f:
    f.write(content)
