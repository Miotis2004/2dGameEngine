# 2dGameEngine# 2dGameEngine

A modern 2D game engine built with C# 14, .NET 10, and WinUI 3.

The goal of 2dGameEngine is to provide a fully managed, modern, extensible game development framework focused on 2D game creation while leveraging the latest Microsoft technologies. The engine is being developed as both a runtime and an editor, allowing games to be designed, tested, and deployed from a unified environment.

The first game created using the engine will be a 2D platformer. This game serves as a validation project that will drive the design and implementation of the engine's core systems.

---

# Project Goals

## Primary Goals

* Build a modern 2D game engine in C#
* Utilize .NET 10 as the minimum runtime
* Use WinUI 3 Packaged Applications as the editor platform
* Support C# as the native scripting language
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
* Plugin architecture
* Multiplayer framework
* Save/load framework

---

# Technology Stack

| Technology | Purpose             |
| ---------- | ------------------- |
| C# 14      | Primary Language    |
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

Provides game behavior implementation.

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

Project initialization complete.

Phase 1: Engine Foundation has been verified complete.

Development focus is ready to move to Phase 5: Tilemaps.
