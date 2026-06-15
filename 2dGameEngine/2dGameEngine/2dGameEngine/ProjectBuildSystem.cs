using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace _2dGameEngine;

public enum BuildTargetPlatform
{
    WindowsDesktop,
}

public enum BuildConfiguration
{
    Debug,
    Release,
}

public sealed record BuildSettings(
    BuildTargetPlatform TargetPlatform,
    BuildConfiguration Configuration,
    string StartupScene,
    string OutputDirectory,
    string ProductName,
    string Version,
    string IconPath,
    bool RunnableFolderExport)
{
    public static BuildSettings CreateDefault(CreatedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        string startupScene = Directory.GetFiles(project.ScenesDirectory, "*.scene.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? Path.Combine(project.ScenesDirectory, "Main.scene.json");
        return new BuildSettings(
            BuildTargetPlatform.WindowsDesktop,
            BuildConfiguration.Release,
            startupScene,
            Path.Combine(project.ProjectDirectory, "Builds", "Windows"),
            project.DisplayName,
            "1.0.0",
            string.Empty,
            true);
    }
}

public sealed record BuildDiagnostic(BuildDiagnosticSeverity Severity, string Message, string? Path = null);

public enum BuildDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record BuildArtifact(string Path, string Kind, long SizeInBytes, string Sha256);

public sealed record BuildProfile(string Name, BuildConfiguration Configuration, bool Deterministic, bool DebugSymbols)
{
    public static BuildProfile Debug { get; } = new("Debug", BuildConfiguration.Debug, true, true);
    public static BuildProfile Release { get; } = new("Release", BuildConfiguration.Release, true, false);
}

public sealed record BuildResult(bool Succeeded, string OutputDirectory, IReadOnlyList<BuildDiagnostic> Diagnostics, IReadOnlyList<BuildArtifact> Artifacts);

public static class ProjectBuildSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static BuildResult Build(CreatedProject project, BuildSettings settings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(settings);

        List<BuildDiagnostic> diagnostics = [];
        List<BuildArtifact> artifacts = [];
        string outputDirectory = Path.GetFullPath(settings.OutputDirectory);
        string contentDirectory = Path.Combine(outputDirectory, "Content");
        string scenesDirectory = Path.Combine(contentDirectory, "Scenes");
        string assetsDirectory = Path.Combine(contentDirectory, "Assets");

        Validate(project, settings, diagnostics);
        if (diagnostics.Any(diagnostic => diagnostic.Severity == BuildDiagnosticSeverity.Error))
        {
            return new BuildResult(false, outputDirectory, diagnostics, artifacts);
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(scenesDirectory);
        Directory.CreateDirectory(assetsDirectory);

        string startupSceneName = Path.GetFileName(settings.StartupScene);
        foreach (string scenePath in Directory.GetFiles(project.ScenesDirectory, "*.scene.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            CopyArtifact(scenePath, Path.Combine(scenesDirectory, Path.GetFileName(scenePath)), "Scene", artifacts);
        }

        if (Directory.Exists(project.AssetsDirectory))
        {
            foreach (string assetPath in Directory.EnumerateFiles(project.AssetsDirectory, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                string relativePath = Path.GetRelativePath(project.AssetsDirectory, assetPath);
                if (relativePath.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CopyArtifact(assetPath, Path.Combine(assetsDirectory, relativePath), "Asset", artifacts);
            }
        }

        string bootstrapPath = Path.Combine(outputDirectory, "player-bootstrap.json");
        WriteJsonArtifact(bootstrapPath, new
        {
            productName = settings.ProductName,
            version = settings.Version,
            configuration = settings.Configuration.ToString(),
            targetPlatform = settings.TargetPlatform.ToString(),
            deterministic = true,
            msBuildProperties = CreateMSBuildProperties(settings),
            startupScene = $"Content/Scenes/{startupSceneName}",
            contentRoot = "Content",
            runnableFolderExport = settings.RunnableFolderExport,
        }, "Bootstrap", artifacts);

        string manifestPath = Path.Combine(outputDirectory, "build-manifest.json");
        WriteJsonArtifact(manifestPath, new
        {
            project = project.SafeName,
            settings.ProductName,
            settings.Version,
            scenes = artifacts.Where(artifact => artifact.Kind == "Scene").Select(artifact => ToManifestEntry(outputDirectory, artifact)).ToArray(),
            assets = artifacts.Where(artifact => artifact.Kind == "Asset").Select(artifact => ToManifestEntry(outputDirectory, artifact)).ToArray(),
            generatedAtUtc = DateTime.UnixEpoch,
        }, "Manifest", artifacts);

        string readmePath = Path.Combine(outputDirectory, "RUN_GAME.txt");
        File.WriteAllText(readmePath, $"{settings.ProductName} {settings.Version}{Environment.NewLine}Open this folder with the Unity 2 player runtime and load player-bootstrap.json.{Environment.NewLine}", Encoding.UTF8);
        artifacts.Add(CreateArtifact(readmePath, "RunnableFolderInstructions"));

        WriteTextArtifact(Path.Combine(outputDirectory, "Directory.Build.props"), CreateDirectoryBuildProps(settings), "MSBuildProperties", artifacts);
        diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Info, $"Built deterministic {artifacts.Count} artifact(s) for {settings.TargetPlatform}.", outputDirectory));
        return new BuildResult(true, outputDirectory, diagnostics, artifacts.OrderBy(artifact => artifact.Path, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static void Validate(CreatedProject project, BuildSettings settings, List<BuildDiagnostic> diagnostics)
    {
        if (!Directory.Exists(project.ScenesDirectory)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Error, "Scenes directory is missing.", project.ScenesDirectory));
        if (!Directory.Exists(project.AssetsDirectory)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Warning, "Assets directory is missing; build will contain no assets.", project.AssetsDirectory));
        if (string.IsNullOrWhiteSpace(settings.ProductName)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Error, "Product name is required."));
        if (string.IsNullOrWhiteSpace(settings.Version)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Error, "Version is required."));
        if (string.IsNullOrWhiteSpace(settings.OutputDirectory)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Error, "Output directory is required."));
        if (!File.Exists(settings.StartupScene)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Error, "Startup scene does not exist.", settings.StartupScene));
        if (!string.IsNullOrWhiteSpace(settings.IconPath) && !File.Exists(settings.IconPath)) diagnostics.Add(new BuildDiagnostic(BuildDiagnosticSeverity.Warning, "Icon path was not found; default player icon will be used.", settings.IconPath));
    }

    private static void CopyArtifact(string source, string destination, string kind, List<BuildArtifact> artifacts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, overwrite: true);
        artifacts.Add(CreateArtifact(destination, kind));
    }

    private static void WriteJsonArtifact(string path, object value, string kind, List<BuildArtifact> artifacts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8);
        artifacts.Add(CreateArtifact(path, kind));
    }

    private static void WriteTextArtifact(string path, string contents, string kind, List<BuildArtifact> artifacts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents, Encoding.UTF8);
        artifacts.Add(CreateArtifact(path, kind));
    }

    private static BuildArtifact CreateArtifact(string path, string kind)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);
        return new BuildArtifact(path, kind, new FileInfo(path).Length, Convert.ToHexString(hash).ToLowerInvariant());
    }

    private static object CreateMSBuildProperties(BuildSettings settings) => new
    {
        Configuration = settings.Configuration.ToString(),
        ContinuousIntegrationBuild = true,
        Deterministic = true,
        DebugType = settings.Configuration == BuildConfiguration.Debug ? "portable" : "none",
        PublishTrimmed = settings.Configuration == BuildConfiguration.Release,
        RuntimeIdentifier = GetRuntimeIdentifier(settings.TargetPlatform),
    };

    private static string CreateDirectoryBuildProps(BuildSettings settings) =>
        $"""
        <Project>
          <PropertyGroup>
            <Configuration>{settings.Configuration}</Configuration>
            <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
            <Deterministic>true</Deterministic>
            <DebugType>{(settings.Configuration == BuildConfiguration.Debug ? "portable" : "none")}</DebugType>
            <PublishTrimmed>{(settings.Configuration == BuildConfiguration.Release).ToString().ToLowerInvariant()}</PublishTrimmed>
            <RuntimeIdentifier>{GetRuntimeIdentifier(settings.TargetPlatform)}</RuntimeIdentifier>
          </PropertyGroup>
        </Project>
        """;

    private static string GetRuntimeIdentifier(BuildTargetPlatform targetPlatform) => targetPlatform switch
    {
        BuildTargetPlatform.WindowsDesktop => "win-x64",
        _ => "win-x64",
    };

    private static object ToManifestEntry(string outputDirectory, BuildArtifact artifact) => new
    {
        path = Path.GetRelativePath(outputDirectory, artifact.Path).Replace(Path.DirectorySeparatorChar, '/'),
        artifact.Kind,
        artifact.SizeInBytes,
        artifact.Sha256,
    };
}
