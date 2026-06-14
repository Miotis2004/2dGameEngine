using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace _2dGameEngine;

/// <summary>
/// Creates the on-disk structure for new Unity 2 C# game projects.
/// </summary>
public static class EditorProjectScaffolder
{
    private static readonly Regex InvalidIdentifierCharacters = new("[^A-Za-z0-9_]", RegexOptions.Compiled);

    /// <summary>
    /// Creates a complete C# solution for a new game project.
    /// </summary>
    /// <param name="rootDirectory">Directory where the project folder will be created.</param>
    /// <param name="projectName">Human-readable project name.</param>
    /// <returns>Information about the created project.</returns>
    public static CreatedProject CreateProject(string rootDirectory, string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        string safeName = ToSafeName(projectName);
        string projectDirectory = Path.Combine(rootDirectory, safeName);
        Directory.CreateDirectory(projectDirectory);

        string sourceDirectory = Path.Combine(projectDirectory, "src");
        string coreDirectory = Path.Combine(sourceDirectory, $"{safeName}.Core");
        string gameDirectory = Path.Combine(sourceDirectory, $"{safeName}.Game");
        string editorDirectory = Path.Combine(sourceDirectory, $"{safeName}.Editor");
        string assetsDirectory = Path.Combine(projectDirectory, "Assets");
        string scenesDirectory = Path.Combine(projectDirectory, "Scenes");

        Directory.CreateDirectory(coreDirectory);
        Directory.CreateDirectory(gameDirectory);
        Directory.CreateDirectory(Path.Combine(gameDirectory, "Scripts"));
        Directory.CreateDirectory(Path.Combine(projectDirectory, "ProjectSettings"));
        Directory.CreateDirectory(editorDirectory);
        Directory.CreateDirectory(Path.Combine(assetsDirectory, "Sprites"));
        Directory.CreateDirectory(Path.Combine(assetsDirectory, "Audio"));
        Directory.CreateDirectory(scenesDirectory);

        string coreProjectId = Guid.NewGuid().ToString("B").ToUpperInvariant();
        string gameProjectId = Guid.NewGuid().ToString("B").ToUpperInvariant();
        string editorProjectId = Guid.NewGuid().ToString("B").ToUpperInvariant();
        string solutionId = Guid.NewGuid().ToString("B").ToUpperInvariant();

        WriteFile(Path.Combine(projectDirectory, $"{safeName}.sln"), CreateSolution(safeName, coreProjectId, gameProjectId, editorProjectId, solutionId));
        WriteFile(Path.Combine(projectDirectory, "README.md"), CreateReadme(projectName));
        WriteFile(Path.Combine(coreDirectory, $"{safeName}.Core.csproj"), CreateClassLibraryProject());
        WriteFile(Path.Combine(coreDirectory, "GameSettings.cs"), CreateGameSettings(safeName, projectName));
        WriteFile(Path.Combine(gameDirectory, $"{safeName}.Game.csproj"), CreateGameProject(safeName));
        WriteFile(Path.Combine(gameDirectory, "GameBootstrapper.cs"), CreateGameBootstrapper(safeName));
        WriteFile(Path.Combine(gameDirectory, "Scripts", "CSharpBehaviour.cs"), CreateCSharpBehaviour(safeName));
        WriteFile(Path.Combine(gameDirectory, "Scripts", "PlayerController.cs"), CreateStarterScript(safeName));
        WriteFile(Path.Combine(editorDirectory, $"{safeName}.Editor.csproj"), CreateEditorProject(safeName));
        WriteFile(Path.Combine(editorDirectory, "Program.cs"), CreateEditorProgram(safeName));
        WriteFile(Path.Combine(scenesDirectory, "Main.scene.json"), CreateDefaultScene(projectName));
        WriteFile(Path.Combine(projectDirectory, "ProjectSettings", "Unity2Project.json"), CreateProjectSettings(projectName));
        WriteFile(Path.Combine(assetsDirectory, "README.md"), "# Assets\n\nPlace sprites, audio, fonts, and other imported content in this folder. Runtime behavior is authored only in C#.\n");

        return new CreatedProject(projectName, safeName, projectDirectory, Path.Combine(projectDirectory, $"{safeName}.sln"), assetsDirectory, scenesDirectory);
    }



    /// <summary>
    /// Loads metadata for an existing 2dGameEngine project folder.
    /// </summary>
    public static CreatedProject LoadProject(string projectDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        string fullProjectDirectory = Path.GetFullPath(projectDirectory);
        if (!Directory.Exists(fullProjectDirectory))
        {
            throw new DirectoryNotFoundException($"Project directory '{fullProjectDirectory}' does not exist.");
        }

        string[] solutions = Directory.GetFiles(fullProjectDirectory, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(fullProjectDirectory, "*.slnx", SearchOption.TopDirectoryOnly))
            .ToArray();
        if (solutions.Length == 0)
        {
            throw new InvalidDataException($"Project directory '{fullProjectDirectory}' does not contain a solution file.");
        }

        string safeName = Path.GetFileNameWithoutExtension(solutions[0]);
        string assetsDirectory = Path.Combine(fullProjectDirectory, "Assets");
        string scenesDirectory = Path.Combine(fullProjectDirectory, "Scenes");
        Directory.CreateDirectory(assetsDirectory);
        Directory.CreateDirectory(scenesDirectory);

        return new CreatedProject(safeName, safeName, fullProjectDirectory, solutions[0], assetsDirectory, scenesDirectory);
    }

    private static string ToSafeName(string projectName)
    {
        string safeName = InvalidIdentifierCharacters.Replace(projectName.Trim(), string.Empty);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "NewGameProject";
        }

        if (char.IsDigit(safeName[0]))
        {
            safeName = $"Game{safeName}";
        }

        return safeName;
    }

    private static void WriteFile(string path, string contents)
    {
        File.WriteAllText(path, contents, new UTF8Encoding(false));
    }

    private static string CreateSolution(string safeName, string coreProjectId, string gameProjectId, string editorProjectId, string solutionId) => $$"""
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{safeName}}.Core", "src\{{safeName}}.Core\{{safeName}}.Core.csproj", "{{coreProjectId}}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{safeName}}.Game", "src\{{safeName}}.Game\{{safeName}}.Game.csproj", "{{gameProjectId}}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{safeName}}.Editor", "src\{{safeName}}.Editor\{{safeName}}.Editor.csproj", "{{editorProjectId}}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{coreProjectId}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{coreProjectId}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{coreProjectId}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{coreProjectId}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{gameProjectId}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{gameProjectId}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{gameProjectId}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{gameProjectId}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{editorProjectId}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{editorProjectId}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{editorProjectId}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{editorProjectId}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {{solutionId}}
	EndGlobalSection
EndGlobal
""";

    private static string CreateClassLibraryProject() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""";

    private static string CreateGameProject(string safeName) => $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\{{safeName}}.Core\{{safeName}}.Core.csproj" />
  </ItemGroup>
</Project>
""";

    private static string CreateEditorProject(string safeName) => $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\{{safeName}}.Core\{{safeName}}.Core.csproj" />
    <ProjectReference Include="..\{{safeName}}.Game\{{safeName}}.Game.csproj" />
  </ItemGroup>
</Project>
""";

    private static string CreateGameSettings(string safeName, string projectName) => $$"""
namespace {{safeName}}.Core;

public sealed record GameSettings(string Title, int Width, int Height)
{
    public static GameSettings Default { get; } = new("{{projectName.Replace("\"", "\\\"")}}", 1280, 720);
}
""";

    private static string CreateGameBootstrapper(string safeName) => $$"""
using {{safeName}}.Core;

namespace {{safeName}}.Game;

public static class GameBootstrapper
{
    public static GameSettings CreateDefaultSettings() => GameSettings.Default;
}
""";

    private static string CreateCSharpBehaviour(string safeName) => $$"""
namespace {{safeName}}.Game.Scripts;

public abstract class CSharpBehaviour
{
    public string DisplayName { get; set; } = string.Empty;

    public virtual void Awake()
    {
    }

    public virtual void Start()
    {
    }

    public virtual void Update(float deltaTime)
    {
    }
}
""";

    private static string CreateStarterScript(string safeName) => $$"""
namespace {{safeName}}.Game.Scripts;

public sealed class PlayerController : CSharpBehaviour
{
    public string DisplayName { get; set; } = "Player Controller";

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

    private static string CreateEditorProgram(string safeName) => $$"""
using {{safeName}}.Game;

GameBootstrapper.CreateDefaultSettings();
Console.WriteLine("{{safeName}} editor host is ready.");
""";

    private static string CreateReadme(string projectName) => $$"""
# {{projectName}}

Created with Unity 2 Clone. This project uses C# as its only gameplay scripting language.

## Structure

* `src` - C# solution projects for core gameplay, game code, editor host, and authored scripts.
* `ProjectSettings/Unity2Project.json` - Project manifest that locks the scripting backend to C#.
* `Assets` - Imported game content.
* `Scenes` - Scene files.
""";

    private static string CreateProjectSettings(string projectName) => $$"""
{
  "productName": "{{projectName.Replace("\"", "\\\"")}}",
  "editor": "Unity 2 Clone",
  "dimension": "2D",
  "scriptingBackend": "CSharpOnly",
  "supportedLanguages": ["CSharp"]
}
""";

    private static string CreateDefaultScene(string projectName) => $$"""
{
  "schemaVersion": 1,
  "name": "Main",
  "entities": []
}
""";
}

/// <summary>
/// Describes a project created from the editor shell.
/// </summary>
public sealed record CreatedProject(string DisplayName, string SafeName, string ProjectDirectory, string SolutionPath, string AssetsDirectory, string ScenesDirectory);
