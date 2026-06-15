# Platformer Sample

This sample is a complete Phase 30 validation project built from the public Platformer template workflow. It is intentionally lightweight so it can be inspected, edited, played, built, and launched as part of every release-candidate regression pass.

## What it validates

- Template generation for a C#-only platformer project.
- Scene authoring with Player, Ground, and Goal entities.
- Asset folder conventions for sprites, audio, UI, and fonts.
- Script editing through `PlayerController.cs`.
- Build and launch readiness through the included regression checklist.

## Suggested smoke test

1. Load this folder as an existing project.
2. Refresh assets and validate metadata.
3. Open `Scenes/Main.scene.json` and move the Goal marker.
4. Enter Play, Pause, Step, and Stop.
5. Build a release desktop player and launch it from the output folder.
