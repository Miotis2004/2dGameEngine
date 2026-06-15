# Phase 30 Release Hardening Guide

Phase 30 turns Unity 2 Clone into a milestone release candidate by pairing the editor with starter templates, a complete public-workflow sample game, onboarding guidance, and repeatable regression checks.

## Starter templates

The New Project dialog exposes four templates:

| Template | Purpose | Starter content |
| --- | --- | --- |
| Blank | Custom projects that need no assumptions | Empty `Main.scene.json` and a basic C# script |
| Platformer | Side-scrolling action games | Player, Ground, and Goal markers plus movement comments |
| TopDown | Adventure, twin-stick, or RPG prototypes | Player and CameraTarget markers |
| UiHeavy | Menu, HUD, and interface-first projects | MainMenuCanvas marker and UI asset folders |

Every template keeps gameplay authored in C#, creates first-run onboarding notes, and includes a regression checklist under `ProjectSettings`.

## First-run tutorial

1. Create a Platformer project from the New Project dialog.
2. Open `Scenes/Main.scene.json` and confirm Player, Ground, and Goal entities exist.
3. Import art into `Assets/Sprites`, refresh assets, and validate metadata.
4. Edit `src/<Project>.Game/Scripts/PlayerController.cs` to add gameplay behavior.
5. Press Play, Pause, Step, and Stop to confirm play-mode isolation.
6. Use Build Project and inspect the generated manifest before launching the player.

## Manual page index

- Core systems: scenes, entities, transforms, play mode, and serialization.
- Scripting: C#-only scripts, hot reload settings, debug launch profiles, and component authoring.
- Asset import: imported content folders, sidecar metadata, validation, and preview behavior.
- Builds: desktop build settings, generated bootstrap files, build diagnostics, and artifact layout.
- Troubleshooting: missing solution files, invalid scene schema, failed script builds, and package or extension validation failures.

## API reference generation

Generate XML documentation during release builds and publish it beside the generated API index. Public APIs should include XML summaries before a Phase 30 release candidate is tagged.

## Regression checklist

Use the sample project in `Samples/PlatformerSample` for release validation:

- Import: refresh sample assets and validate metadata.
- Edit: move the Goal marker, save the scene, reload it, and confirm the edit persists.
- Play: enter play mode, pause, step one frame, stop, and confirm edit-state restoration.
- Build: create a release desktop build and confirm warnings are actionable.
- Launch: run the built player and confirm sample startup instructions are visible.
