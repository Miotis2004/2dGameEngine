using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _2dGameEngine;

/// <summary>
/// Describes a project package that can add editor tooling, runtime assets, or both.
/// </summary>
public sealed record EditorPackageManifest
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Version { get; init; } = "1.0.0";

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<string> ExtensionIds { get; init; } = [];

    public IReadOnlyList<string> AssetRoots { get; init; } = [];
}

/// <summary>
/// Describes a C# editor extension discovered from the project Extensions folder.
/// </summary>
public sealed record EditorExtensionManifest
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string EntryPoint { get; init; } = string.Empty;

    public string Version { get; init; } = "1.0.0";

    public bool EnabledByDefault { get; init; } = true;

    public IReadOnlyList<string> Menus { get; init; } = [];
}

/// <summary>
/// Captures the enabled extension set and package registry for a project.
/// </summary>
public sealed record EditorExtensibilitySettings
{
    public IReadOnlyList<string> EnabledExtensions { get; init; } = [];

    public IReadOnlyList<string> InstalledPackages { get; init; } = [];
}

public sealed record EditorPackageInfo(EditorPackageManifest Manifest, string PackageDirectory);

public sealed record EditorExtensionInfo(EditorExtensionManifest Manifest, string ExtensionDirectory, bool Enabled);

public sealed record EditorExtensibilityCatalog(IReadOnlyList<EditorPackageInfo> Packages, IReadOnlyList<EditorExtensionInfo> Extensions);

/// <summary>
/// Discovers packages and editor extensions and persists their enabled state.
/// </summary>
public sealed class EditorExtensibilityService
{
    private const string PackageManifestFileName = "package.json";
    private const string ExtensionManifestFileName = "extension.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly CreatedProject _project;

    public EditorExtensibilityService(CreatedProject project)
    {
        _project = project;
        Directory.CreateDirectory(_project.PackagesDirectory);
        Directory.CreateDirectory(_project.ExtensionsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
    }

    public string SettingsPath => Path.Combine(_project.ProjectDirectory, "ProjectSettings", "EditorExtensibility.json");

    public EditorExtensibilityCatalog Refresh()
    {
        EditorExtensibilitySettings settings = LoadSettings();
        List<EditorPackageInfo> packages = DiscoverPackages();
        HashSet<string> enabled = new(settings.EnabledExtensions, StringComparer.OrdinalIgnoreCase);
        List<EditorExtensionInfo> extensions = DiscoverExtensions()
            .Select(extension => new EditorExtensionInfo(extension.Manifest, extension.ExtensionDirectory, enabled.Contains(extension.Manifest.Id) || (settings.EnabledExtensions.Count == 0 && extension.Manifest.EnabledByDefault)))
            .ToList();

        SaveSettings(settings with
        {
            InstalledPackages = packages.Select(package => package.Manifest.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(),
            EnabledExtensions = extensions.Where(extension => extension.Enabled).Select(extension => extension.Manifest.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(),
        });

        return new EditorExtensibilityCatalog(packages, extensions);
    }

    public EditorPackageInfo InstallLocalPackage(string sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Package folder '{sourceDirectory}' does not exist.");
        }

        string manifestPath = Path.Combine(sourceDirectory, PackageManifestFileName);
        EditorPackageManifest manifest = File.Exists(manifestPath)
            ? ReadJson<EditorPackageManifest>(manifestPath)
            : CreatePackageManifest(Path.GetFileName(sourceDirectory));
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            manifest = manifest with { Id = SanitizeId(Path.GetFileName(sourceDirectory)) };
        }

        string targetDirectory = Path.Combine(_project.PackagesDirectory, manifest.Id);
        if (Directory.Exists(targetDirectory)) Directory.Delete(targetDirectory, true);
        CopyDirectory(sourceDirectory, targetDirectory);
        WriteJson(Path.Combine(targetDirectory, PackageManifestFileName), manifest);

        EditorExtensibilitySettings settings = LoadSettings();
        SaveSettings(settings with { InstalledPackages = settings.InstalledPackages.Append(manifest.Id).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray() });
        return new EditorPackageInfo(manifest, targetDirectory);
    }

    public EditorExtensionInfo SetExtensionEnabled(string extensionId, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
        EditorExtensibilitySettings settings = LoadSettings();
        HashSet<string> enabledIds = new(settings.EnabledExtensions, StringComparer.OrdinalIgnoreCase);
        if (enabled) enabledIds.Add(extensionId); else enabledIds.Remove(extensionId);
        SaveSettings(settings with { EnabledExtensions = enabledIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray() });
        return Refresh().Extensions.First(extension => string.Equals(extension.Manifest.Id, extensionId, StringComparison.OrdinalIgnoreCase));
    }

    public EditorExtensibilitySettings LoadSettings() => File.Exists(SettingsPath) ? ReadJson<EditorExtensibilitySettings>(SettingsPath) : new EditorExtensibilitySettings();

    private List<EditorPackageInfo> DiscoverPackages() => Discover(_project.PackagesDirectory, PackageManifestFileName, ReadPackage).ToList();

    private List<EditorExtensionInfo> DiscoverExtensions() => Discover(_project.ExtensionsDirectory, ExtensionManifestFileName, ReadExtension).ToList();

    private static IEnumerable<T> Discover<T>(string root, string manifestFileName, Func<string, T> reader)
    {
        if (!Directory.Exists(root)) yield break;
        foreach (string manifestPath in Directory.GetFiles(root, manifestFileName, SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            yield return reader(manifestPath);
        }
    }

    private static EditorPackageInfo ReadPackage(string manifestPath) => new(ReadJson<EditorPackageManifest>(manifestPath), Path.GetDirectoryName(manifestPath)!);

    private static EditorExtensionInfo ReadExtension(string manifestPath) => new(ReadJson<EditorExtensionManifest>(manifestPath), Path.GetDirectoryName(manifestPath)!, false);

    private void SaveSettings(EditorExtensibilitySettings settings) => WriteJson(SettingsPath, settings);

    private static T ReadJson<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Could not read {typeof(T).Name} from '{path}'.");

    private static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
    }

    private static EditorPackageManifest CreatePackageManifest(string name) => new() { Id = SanitizeId(name), DisplayName = name, Description = "Local editor package" };

    private static string SanitizeId(string value) => new(value.Where(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_').ToArray()).Trim('.', '-', '_').ToLowerInvariant();

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.OrdinalIgnoreCase));
        }

        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.OrdinalIgnoreCase), true);
        }
    }
}
