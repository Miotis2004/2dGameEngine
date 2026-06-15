import re

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "r") as f:
    content = f.read()

content = content.replace(
    'menu.Items.Add(CreateMenuItem("Create Empty Entity", (_, _) => AddEmptyEntity(_lastSceneContextPoint)));',
    'menu.Items.Add(CreateMenuItem("Create Empty Entity", (_, _) => AddEmptyEntity((Point?)_lastSceneContextPoint)));'
)

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "w") as f:
    f.write(content)
