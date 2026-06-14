using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;
using _2dGameEngine.Physics;

namespace _2dGameEngine.Scripting;

/// <summary>
/// Provides editor-facing component factories and Unity-style C# script source generation helpers.
/// </summary>
public static class ComponentAuthoring
{
    private static readonly Regex InvalidIdentifierCharacters = new("[^A-Za-z0-9_]", RegexOptions.Compiled);

    /// <summary>
    /// Gets component recipes available from the editor's Add Component menu.
    /// </summary>
    public static IReadOnlyList<ComponentRecipe> Recipes { get; } =
    [
        new("Sprite Renderer", "Rendering", () => new SpriteRenderer(new Vector2(64.0f, 64.0f), System.Drawing.Color.White) { OutlineColor = System.Drawing.Color.LightSkyBlue }),
        new("Box Collider 2D", "Physics", () => new BoxCollider2D(new Vector2(64.0f, 64.0f))),
        new("Rigid Body 2D", "Physics", () => new RigidBody2D()),
        new("Entity Motion", "Core", () => new EntityMotionComponent(Vector2.Zero)),
        new("Input Movement", "Core", () => new EntityInputMovementComponent(180.0f)),
        new("Platformer Movement", "Physics", () => new PlatformerMovementComponent(285.0f, 610.0f)),
        new("Tilemap Collider 2D", "Physics", () => new TilemapCollider2D()),
    ];

    /// <summary>
    /// Creates a script source file in the project's game assembly and returns its attachable placeholder component.
    /// </summary>
    public static AuthoredScriptComponent CreateScript(string gameSourceDirectory, string projectSafeName, string requestedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameSourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectSafeName);
        string className = ToIdentifier(requestedName, "NewScript");
        Directory.CreateDirectory(Path.Combine(gameSourceDirectory, "Scripts"));
        string path = GetUniqueScriptPath(Path.Combine(gameSourceDirectory, "Scripts"), className);
        className = Path.GetFileNameWithoutExtension(path);
        File.WriteAllText(path, CreateScriptSource(projectSafeName, className), new UTF8Encoding(false));
        return new AuthoredScriptComponent(className, Path.Combine("src", $"{projectSafeName}.Game", "Scripts", Path.GetFileName(path)).Replace(Path.DirectorySeparatorChar, '/'));
    }

    /// <summary>
    /// Builds source code for a new gameplay script.
    /// </summary>
    public static string CreateScriptSource(string projectSafeName, string className) => $$"""
namespace {{projectSafeName}}.Game.Scripts;

public sealed class {{className}} : CSharpBehaviour
{
    public string DisplayName { get; set; } = "{{className}}";

    public override void Start()
    {
        // Initialize authored gameplay state here.
    }

    public override void Update(float deltaTime)
    {
        // Add C# gameplay behavior here.
    }
}
""";

    private static string GetUniqueScriptPath(string directory, string className)
    {
        string path = Path.Combine(directory, className + ".cs");
        int suffix = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(directory, $"{className}{suffix}.cs");
            suffix++;
        }

        return path;
    }

    private static string ToIdentifier(string value, string fallback)
    {
        string safe = InvalidIdentifierCharacters.Replace(value.Trim(), string.Empty);
        if (string.IsNullOrWhiteSpace(safe)) safe = fallback;
        if (char.IsDigit(safe[0])) safe = $"Script{safe}";
        return safe;
    }
}

/// <summary>
/// Describes a component template that can be added to a selected entity.
/// </summary>
public sealed record ComponentRecipe(string Name, string Category, Func<Component> Factory);
