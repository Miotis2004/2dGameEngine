# Unity 2 Clone

A Unity-inspired 2D game editor and runtime built entirely around C# authoring.

The goal of 2dGameEngine is to provide a fully managed, modern, extensible game development framework focused on 2D game creation while leveraging the latest Microsoft technologies. The engine is being developed as both a runtime and an editor, allowing games to be designed, tested, and deployed from a unified environment.

The first game created using the engine will be a 2D platformer. This game serves as a validation project that will drive the design and implementation of the engine's core systems.

---

# Project Goals

## Primary Goals

* Build a Unity-like 2D editor workflow in C#
* Utilize .NET 10 as the minimum runtime
* Use WinUI 3 Packaged Applications as the editor platform
* Support C# as the only gameplay scripting language
* Create an extensible Entity Component System architecture
* Develop a visual editor for scene creation
* Support tile-based and sprite-based games
* Provide a complete workflow for building 2D games

## Long-Term Goals

* Visual scene editor
* Animation editor
* Tilemap editor
* Audio management system
* Particle system
* UI system
* Physics system
* Asset pipeline
* Packaging and deployment tools
* C# editor extension points
* Multiplayer framework
* Save/load framework

---

# Technology Stack

| Technology | Purpose             |
| ---------- | ------------------- |
| C#         | Only scripting language |
| .NET 10    | Runtime             |
| WinUI 3    | Editor Framework    |
| Win2D      | Rendering Backend   |
| xUnit      | Unit Testing        |
| MSIX       | Packaging           |
| JSON       | Scene Serialization |

---

# Architecture

The engine is divided into multiple assemblies to maintain separation of concerns and support future expansion.

```text
2dGameEngine

├── 2dGameEngine.Core
├── 2dGameEngine.Graphics
├── 2dGameEngine.Physics
├── 2dGameEngine.Content
├── 2dGameEngine.Scripting
├── 2dGameEngine.Editor
└── 2dGameEngine.Platform.Windows
```

---

# Assembly Responsibilities

## 2dGameEngine.Core

Contains the fundamental engine systems.

Responsibilities:

* Game loop
* Scene management
* Entity management
* Component system
* Transform system
* Timing system
* Event system

Key Classes:

```text
Engine
Scene
Entity
Component
Transform2D
Time
```

---

## 2dGameEngine.Graphics

Responsible for all rendering operations.

Responsibilities:

* Rendering pipeline
* Camera system
* Sprite rendering
* Tilemap rendering
* Animation playback

Key Classes:

```text
Renderer2D
Camera2D
SpriteRenderer
TileMapRenderer
AnimationPlayer
```

---

## 2dGameEngine.Physics

Provides collision detection and movement systems.

Responsibilities:

* Gravity
* Collision detection
* Collision response
* Rigidbody simulation

Key Classes:

```text
RigidBody2D
Collider2D
BoxCollider2D
CollisionWorld
PhysicsSystem
```

---

## 2dGameEngine.Content

Handles asset management.

Responsibilities:

* Asset loading
* Texture management
* Sprite sheet loading
* Audio loading
* Resource caching

Key Classes:

```text
AssetManager
TextureLoader
SpriteSheetLoader
AudioLoader
```

---

## 2dGameEngine.Scripting

Provides C#-only game behavior implementation with Unity-style lifecycle methods (`Awake`, `Start`, and `Update`).

Responsibilities:

* Script execution
* Component scripting
* Engine API exposure

Key Classes:

```text
ScriptComponent
ScriptManager
EngineAPI
```

---

## 2dGameEngine.Editor

WinUI-based editor application.

Responsibilities:

* Scene editing
* Asset management
* Property editing
* Project configuration
* Runtime debugging

Future Features:

* Scene hierarchy
* Inspector panel
* Asset browser
* Animation editor
* Tilemap editor

---

## 2dGameEngine.Platform.Windows

Windows-specific services.

Responsibilities:

* File system integration
* Input handling
* Audio integration
* Native platform services

---

# Core Engine Principles

## Simplicity

The engine should remain approachable and understandable.

## Extensibility

All major systems should be replaceable or expandable.

## Performance

Performance is important, but maintainability takes priority during early development.

## Modern C#

The engine should utilize modern language features whenever appropriate.

Examples:

* Primary constructors
* Collection expressions
* Pattern matching
* File-scoped namespaces
* Nullable reference types

---

# Initial Development Roadmap

The roadmap is intentionally ordered so that every milestone builds directly upon previous functionality.

---

# Phase 1: Engine Foundation

Goal:

Establish the basic runtime framework.

Features:

* Engine startup
* Main game loop
* Time management
* Scene system
* Entity system
* Component system
* Transform2D

Deliverable:

A running application capable of updating entities every frame.

Status:

Completed. The WinUI host starts the engine, schedules a frame update loop, advances engine time, updates the active scene, and displays a moving demo entity to validate entity/component updates.

Verification:

* Engine startup is wired through the application entry point and runtime host.
* The main loop is driven by a 16 ms update timer.
* Time state tracks delta time, total time, and frame count.
* The active scene is updated every frame.
* Scenes can create and contain entities.
* Entities own and update enabled components.
* Every entity receives a Transform2D through a TransformComponent.
* The demo entity uses a motion component so the running application visibly proves per-frame entity/component updates.

---

# Phase 2: Rendering

Goal:

Display graphics on screen.

Features:

* Renderer initialization
* Render pipeline
* Camera2D
* Sprite rendering
* Layer ordering
* Basic texture loading

Deliverable:

Render sprites to the screen.

---

# Phase 3: Input

Goal:

Allow user interaction.

Features:

* Keyboard input
* Mouse input
* Input manager
* Action mapping system

Deliverable:

Move an object around the screen.

---

# Phase 4: Physics

Goal:

Support movement and collision.

Features:

* Rigidbody2D
* Gravity
* Collision detection
* Collision resolution
* Ground detection

Deliverable:

Basic platformer movement.

Status:

Completed. The runtime now includes a physics system that applies gravity to dynamic rigid bodies, resolves axis-aligned box collisions against static colliders, tracks grounded state, and drives a platformer demo player with horizontal movement and jumping.

Verification:

* Scenes own a PhysicsSystem and advance physics after component input updates each frame.
* Dynamic entities can use RigidBody2D for velocity, gravity scale, kinematic state, and grounded state.
* BoxCollider2D provides world-space bounds for collision detection.
* CollisionWorld exposes reusable collider queries and AABB intersection checks.
* PhysicsSystem integrates velocity, applies gravity, resolves collisions, and marks grounded bodies.
* The demo scene includes a player rigidbody, ground collider, raised platform, and platformer movement controls.

---

# Phase 5: Tilemaps

Goal:

Support level creation.

Features:

* Tile definitions
* Tilemap renderer
* Tile collisions
* Map loading

Deliverable:

Playable platformer level.

---

# Phase 6: Content System

Goal:

Load external assets.

Features:

* Asset manager
* Texture loading
* Sprite sheets
* Asset caching

Deliverable:

Assets loaded from project content folders.

Status:

Completed. The runtime now includes a content-root asset manager that loads external texture files, parses sprite sheet metadata, caches reusable assets, and renders sprite sheet frames through the existing sprite and tilemap renderer paths.

Verification:

* AssetManager loads content-relative textures from a project content folder and caches repeated requests.
* Sprite sheet metadata maps named frames to texture atlas rectangles.
* SpriteRenderer and TileDefinition can reference loaded sprite frames while preserving color fallback rendering.
* Renderer2D draws loaded texture frames for sprites and tilemap cells.
* The demo content folder contains a texture atlas and sprite sheet metadata that are copied to build output.
* The Phase 6 demo scene loads tile visuals from the external content folder instead of hard-coded tile colors.

---

# Phase 7: Animation

Goal:

Support animated entities.

Features:

* Animation clips
* Animation playback
* State transitions

Deliverable:

Animated player character.

---

# Phase 8: Scene Serialization

Goal:

Persist game worlds.

Features:

* Scene saving
* Scene loading
* JSON serialization

Deliverable:

Scenes can be edited and reloaded.

---

# Phase 9: Editor Foundation

Goal:

Begin creation of the visual editor.

Features:

* Main editor window
* Docking layout
* Scene viewport
* Runtime preview

Deliverable:

Visual scene preview inside the editor.

Status:

Completed. The application now starts as an editor foundation shell with docked hierarchy, viewport, inspector, and runtime preview panels. The viewport renders the live Phase 9 preview scene through the runtime renderer while toolbar controls allow the preview loop to be paused and resumed.

Verification:

* The main window is branded as the Phase 9 editor foundation.
* A docked layout separates scene hierarchy, viewport, inspector, and runtime preview output.
* The hierarchy lists the active scene, entities, and components.
* The inspector displays details for the selected scene, entity, or component.
* The scene viewport renders the live runtime preview scene.
* The toolbar exposes play/pause controls for the runtime preview.

---

# Phase 10: Platformer Validation Project

Goal:

Validate engine architecture through an actual game.

Features:

* Player controller
* Platform movement
* Jumping
* Camera follow
* Tilemap level
* Collision system
* Animation system

Deliverable:

A fully playable platformer level.

Status:

Completed. The editor runtime preview now launches a Phase 10 platformer validation level that combines player control, jumping, camera follow, tilemap collision, animation playback, reset behavior, and a goal flag objective.

Verification:

* The main window is branded as the Phase 10 platformer validation project.
* The preview scene creates a controllable player with rigidbody physics, a box collider, platformer movement, sprite rendering, and animation playback.
* A larger tilemap level includes ground, multiple raised platforms, and tilemap collision.
* The runtime camera follows the player during play.
* Pressing R or falling below the level resets the player to the start position.
* Reaching the gold goal flag marks the objective complete and updates the runtime status.

---

# Phase 11: Full Editor Workspace and Project Creation

Goal:

Turn the runtime validation shell into the starting point for a full game editor.

Features:

* Project creation workflow
* Complete generated C# solution for each new game project
* Scene editor pane
* Rendered game pane
* Hierarchy pane
* Project and assets pane
* Inspector pane
* Console pane
* Play, pause, and stop testing controls

Deliverable:

A full editor workspace shell that can create new game projects and host the core panes required for scene editing, asset management, inspection, console output, and runtime testing.

Status:

Completed. The application now launches as the Phase 11 editor workspace with split panes for hierarchy, project/assets, scene editing, rendered game preview, inspector, console output, and runtime status. The toolbar includes New Project plus Play, Pause, and Stop controls. Creating a project generates a complete C# solution folder with Core, Game, and Editor projects, starter source files, default scene data, asset folders, and a project README.

Verification:

* The main window is branded as the Phase 11 editor workspace.
* The editor shell exposes hierarchy, project/assets, scene editor, rendered game, inspector, console, and runtime status panes.
* The toolbar exposes New Project, Play, Pause, and Stop controls.
* New Project creates a C# solution containing separate Core, Game, and Editor projects.
* The generated project includes starter source files, asset folders, scene folders, a default scene file, and README documentation.

---

# Phase 12: Scene Editing Tools

Goal:

Add interactive scene-authoring operations to the editor workspace.

Planned Features:

* Entity creation and deletion from the hierarchy pane
* Transform gizmos for move, rotate, and scale
* Selection outlines and scene picking
* Scene save/load commands
* Undo and redo support for editor actions
* Editable inspector fields for entity and component properties

Deliverable:

A usable scene editor that can create, modify, save, and reload entity-based scenes.

---

# Phase 13: Asset Pipeline and Import Workflow

Goal:

Make the project/assets pane the central workflow for bringing content into a game project.

Planned Features:

* Asset import commands
* Asset metadata files
* Texture and sprite previewing
* Sprite slicing setup
* Audio asset registration
* Asset refresh and validation

Deliverable:

An asset pipeline that imports source files, tracks metadata, and exposes assets to scenes and runtime systems.

Status:

Completed. The editor now includes project asset import, refresh, and validation commands. Imported texture and audio files are copied into project asset folders, receive sidecar `.asset.json` metadata, appear in the project/assets tree, and texture assets can be selected for preview and metadata inspection.

---

# Phase 14: Component and Script Authoring

Goal:

Enable game-specific behavior authoring from inside the generated C# solution.

Planned Features:

* Add component command
* Script/component templates
* Generated project reload detection
* Runtime component discovery
* Inspector support for script fields
* Build diagnostics surfaced in the console pane

Deliverable:

A scripting workflow where users can add C# gameplay components, build them, and attach them to entities.

---

# Phase 15: Editor Play Mode Isolation

Goal:

Separate edit-time scene data from play-mode runtime state.

Planned Features:

* Scene state snapshot before play
* Restore edit scene on stop
* Pause and single-step simulation controls
* Runtime error reporting in the console
* Game pane input focus and capture handling

Deliverable:

Safe Play/Pause/Stop testing where runtime changes do not corrupt the open edit scene.

Status:

Completed. The editor now snapshots the edit scene before play, runs gameplay against an isolated runtime scene, restores the edit scene when play mode stops, supports pause and single-step simulation, reports runtime exceptions in the console, and routes keyboard/mouse input through the rendered game pane while play mode is active.

---


# Phase 16: Tilemap and Level Design Editor

Goal:

Turn tilemaps from runtime data into a production authoring workflow for building full 2D levels.

Planned Features:

* Tile palette pane sourced from imported sprite sheets
* Brush, rectangle, fill, erase, and eyedropper paint tools
* Multi-layer tilemaps with sorting layers and collision layers
* Per-tile collider, tag, material, and custom property editing
* Grid snapping, tile selection, and tile transform tools
* Large-map chunking and dirty-region rendering for editor performance

Deliverable:

A tilemap editor that lets designers create, edit, validate, save, and test multi-layer platformer levels without hand-authoring JSON.

---

# Phase 17: Animation Timeline and Animator Controller

Goal:

Provide editor tooling for sprite animation, state machines, and previewable character behavior.

Planned Features:

* Animation clip editor with keyframes, events, frame timing, and sprite swaps
* Timeline scrubbing, playback speed controls, looping, and onion-skin preview
* Animator controller graph with states, transitions, parameters, and blend rules
* Runtime animation event dispatch into C# components
* Inspector authoring for animator parameters and default states
* Import workflow for generating clips from sprite sheets

Deliverable:

A complete 2D animation authoring workflow that can build reusable clips and animator controllers for gameplay entities.

Status:

Completed. Phase 17 adds runtime-ready animation timelines, clip events, animator controllers, state transitions, parameters, scene serialization, and asset loading so editor-authored animation data can drive gameplay entities.

---

# Phase 18: Prefabs, Variants, and Nested Composition

Goal:

Allow reusable entity hierarchies to be authored once and instantiated consistently across scenes.

Planned Features:

* Create prefab assets from scene entities
* Instantiate, unpack, and reconnect prefab instances
* Property override tracking and revert/apply controls
* Prefab variants for specialized enemies, pickups, and environment props
* Nested prefab support with safe override propagation
* Prefab editing stage isolated from the open scene

Deliverable:

A prefab workflow comparable to modern editor expectations, enabling scalable reuse of characters, hazards, pickups, cameras, and UI objects.

Status:

Completed. Phase 18 adds entity hierarchy composition, prefab asset serialization, instance metadata, override recording, variant creation, instantiate/unpack/reconnect operations, and nested prefab-friendly scene serialization.

---

# Phase 19: Editor Undo, Selection, and Gizmo Maturity

Goal:

Make scene editing feel safe, predictable, and efficient across all major authoring actions.

Planned Features:

* Unified command history for hierarchy, transform, inspector, tilemap, asset, and prefab operations
* Multi-selection with group transform handles
* Move, rotate, scale, rect, and collider edit gizmos
* Pivot, local/global space, snapping, and precision input controls
* Clipboard copy/paste/duplicate for entities and components
* Dirty-state tracking with save prompts and recovery after editor crashes

Deliverable:

A reliable editor interaction layer where destructive or complex scene edits can be confidently undone, redone, duplicated, and saved.

Status:

Completed. Phase 19 adds a snapshot-backed editor command history with toolbar and keyboard undo/redo, multi-entity selection, group dragging, duplicate/delete shortcuts, tile paint history, save dirty-state tracking, and matured scene gizmo overlays for selection bounds, pivots, axis handles, snapping, and global-space feedback.

---

# Phase 20: Audio System and Mixer Editor

Goal:

Add production-ready audio playback and mixing for 2D games.

Planned Features:

* AudioClip asset import and previewing
* AudioSource and AudioListener components
* 2D spatial attenuation, panning, looping, and one-shot playback
* Audio mixer groups with volume, mute, solo, and exposed parameters
* Runtime control from C# scripts
* Audio diagnostics for missing clips, invalid formats, and clipping risk

Deliverable:

An integrated audio workflow for importing sounds, placing emitters, mixing channels, and controlling playback from gameplay scripts.

---

# Phase 21: 2D Lighting, Materials, and Render Pipeline Tools

Goal:

Expand the renderer beyond simple sprites into configurable 2D visual production features.

Planned Features:

* Sprite materials with tint, blend mode, texture, normal map, and shader settings
* 2D point, spot, global, and shape lights
* Sorting groups, render layers, and camera culling masks
* Post-processing stack for bloom, color grading, vignette, and pixel-perfect scaling
* Render pipeline asset for project-level visual configuration
* Scene view debug modes for overdraw, batches, colliders, and lighting

Deliverable:

A configurable 2D render pipeline that supports polished lighting, materials, camera effects, and editor diagnostics.

Status:

Completed. Phase 21 adds material metadata, render layers, camera culling masks, sorting groups, global/point/spot/shape 2D light components, render pipeline visual settings, scene serialization for lighting/material data, and editor add-component recipes for lighting tools. The validation scene now includes lit player/tile/goal materials plus global and point lights.

---

# Phase 22: UI Canvas and In-Game Interface Builder

Goal:

Support creation of menus, HUDs, dialogs, and editor-authored in-game interfaces.

Planned Features:

* Canvas, panel, image, text, button, slider, and layout components
* Anchors, pivots, responsive scaling, and safe-area preview
* UI event system with keyboard, mouse, and controller navigation
* Font import, text styling, and localization-ready string references
* Scene view UI editing with rect handles and alignment tools
* Runtime APIs for updating UI from C# gameplay scripts

Deliverable:

A UI authoring system that lets games build interactive menus and HUDs directly in the editor.

---

# Phase 23: Project Build, Packaging, and Deployment

Goal:

Turn editor projects into distributable games.

Planned Features:

* Build settings window for target platform, scenes, icons, versioning, and output path
* Deterministic content build pipeline with asset dependency collection
* Release/debug build profiles
* Windows desktop packaging and runnable folder export
* Build logs, warnings, errors, and clickable diagnostics in the console
* Generated player bootstrap that loads configured startup scenes and content bundles

Deliverable:

A one-click build workflow that produces a playable exported game from the current project.

Status:

Completed. Phase 23 adds editor build settings for Windows desktop exports, release/debug build profiles, deterministic scene and asset collection, generated player bootstrap and build manifest files, runnable folder instructions, and console diagnostics for build warnings, errors, and artifact output.

---

# Phase 24: C# Debugging, Hot Reload, and Script Tooling

Goal:

Make C# gameplay iteration fast, observable, and editor-integrated.

Planned Features:

* Project build button with incremental compilation
* Roslyn diagnostics surfaced in the console and inspector
* Script recompilation and domain reload while preserving edit-mode scene state
* Hot reload support for safe method-body changes during play mode
* Script execution order settings
* Breakpoint/debugger attach guidance and runtime stack traces linked to source files

Deliverable:

A productive C# authoring loop where code changes, diagnostics, and play-mode testing are integrated into the editor.

---

# Phase 25: Physics Authoring and Diagnostics

Goal:

Upgrade physics from a platformer validation system into an inspectable editor feature.

Planned Features:

* Physics material assets for friction, bounciness, density, and combine modes
* Collider shape editing for boxes, circles, capsules, polygons, and tilemap outlines
* Rigidbody constraints, triggers, layers, and collision matrix settings
* Physics debug overlay for contacts, normals, broadphase bounds, and sleeping bodies
* Joint components for hinges, distance links, springs, and motors
* Deterministic fixed-step simulation controls and replayable physics tests

Deliverable:

A robust 2D physics authoring workflow with visual debugging and enough component coverage for common platformer, puzzle, and action games.

---

# Phase 26: Particles and Visual Effects Editor

Goal:

Enable common 2D visual effects without requiring custom gameplay code.

Planned Features:

* ParticleSystem2D component with emitter shapes, bursts, lifetime, speed, color, size, and rotation curves
* Texture sheet animation for particle sprites
* Collision, trigger, and sub-emitter modules
* Effect preview window with pause, scrub, and restart controls
* Reusable VFX preset assets
* Runtime pooling and batching for particle performance

Deliverable:

An editor-driven 2D VFX system for smoke, sparks, pickups, explosions, ambient effects, and character feedback.

---

# Phase 27: Save Data, Localization, and Game Services

Goal:

Provide reusable systems needed by complete shipped games rather than isolated demos.

Planned Features:

* Save-game profile API with JSON and binary serializers
* Project settings for save locations, migration versions, and encryption hooks
* Localization tables for strings, fonts, sprites, and audio variants
* Runtime locale switching and missing-translation diagnostics
* Achievement/stat/event service abstractions
* Editor validation for save schemas and localized content coverage

Deliverable:

A set of game-service foundations that support persistence, localization, and platform-facing feature integrations.

---

# Phase 28: Editor Extensibility and Package Management

Goal:

Let engine users extend the editor and share reusable tooling or runtime systems.

Planned Features:

* Editor extension API for custom windows, inspectors, importers, menu items, and gizmos
* Package manifest format with dependencies, version ranges, samples, and documentation links
* Local package discovery and project package installation
* Template packages for components, editor tools, and runtime libraries
* Sandboxed extension loading and diagnostics
* Package update, remove, and validation workflows

Deliverable:

An extensibility model that allows teams to customize the editor and distribute reusable packages without modifying engine source.

---

# Phase 29: Performance Profiling and Memory Tools

Goal:

Expose runtime and editor performance data so projects can be optimized before release.

Planned Features:

* Frame profiler for update, physics, animation, rendering, scripting, and editor overhead
* Memory profiler for assets, scenes, textures, audio, and managed allocations
* Entity/component count dashboards and expensive-object highlighting
* Render stats for draw calls, batches, texture swaps, and overdraw
* Timeline capture export for bug reports and regression analysis
* Performance budgets and automated warnings in play mode and builds

Deliverable:

A profiling suite that makes bottlenecks visible and actionable during editor iteration and packaged-game testing.

---

# Phase 30: Sample Game, Templates, and Documentation Hardening

Goal:

Prove the editor by shipping a complete sample game and the documentation needed for new users to succeed.

Planned Features:

* Complete 2D platformer sample project built only with public editor workflows
* Additional project templates for blank, platformer, top-down, and UI-heavy games
* Guided first-run tutorial and interactive editor onboarding
* Manual pages for core systems, scripting, asset import, builds, and troubleshooting
* API reference generation for runtime and editor assemblies
* Regression checklist that validates the sample game through import, edit, play, build, and launch

Deliverable:

A Unity 2 milestone release candidate: a documented editor that can create, author, test, build, and ship a complete 2D game project.

---

# Coding Standards

## General Rules

* Nullable Reference Types Enabled
* Implicit Usings Enabled
* Warnings Treated As Errors
* XML Documentation Required For Public APIs
* Unit Tests Required For Core Systems

## Naming

### Classes

```csharp
PlayerController
SpriteRenderer
CollisionWorld
```

### Interfaces

```csharp
IRenderer
IAssetLoader
IComponent
```

### Private Fields

```csharp
_playerPosition
_velocity
_gravity
```

### Constants

```csharp
DefaultGravity
MaxJumpHeight
```

---

# Initial Success Criteria

The first major milestone will be considered complete when the engine can:

* Create a window
* Render sprites
* Accept keyboard input
* Apply gravity
* Detect collisions
* Render tilemaps
* Load assets
* Follow a player with a camera
* Run a playable platformer level

At that point, the engine will have successfully proven its foundational architecture and will be ready for editor expansion and advanced gameplay systems.

---

# Current Status

Project has completed through Phase 16: Tilemap and Level Design Editor, with additional editor, runtime, and workflow improvements beyond the original foundation milestone.

Development focus is ready to continue through the expanded editor roadmap toward a Unity 2 milestone release candidate. Phase 16 adds tilemap creation, tile palette selection, scene-viewport tile painting and erasing, tile grid overlays, tilemap duplication, and scene JSON persistence for designed levels.
