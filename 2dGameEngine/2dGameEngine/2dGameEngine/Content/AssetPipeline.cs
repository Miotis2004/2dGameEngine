using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace _2dGameEngine.Content;

/// <summary>
/// Imports source files into a project asset folder and maintains sidecar metadata for editor workflows.
/// </summary>
public sealed class AssetPipeline
{
    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase) { ".wav", ".mp3", ".ogg", ".flac" };

    public AssetPipeline(string assetsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetsRoot);
        AssetsRoot = Path.GetFullPath(assetsRoot);
        Directory.CreateDirectory(AssetsRoot);
    }

    public string AssetsRoot { get; }

    public AssetMetadata Import(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Import source file was not found.", sourcePath);
        }

        AssetKind kind = Classify(sourcePath);
        string folder = kind switch
        {
            AssetKind.Texture => "Sprites",
            AssetKind.Audio => "Audio",
            _ => "Imported",
        };

        string destinationDirectory = Path.Combine(AssetsRoot, folder);
        Directory.CreateDirectory(destinationDirectory);
        string destinationPath = GetAvailablePath(Path.Combine(destinationDirectory, Path.GetFileName(sourcePath)));
        File.Copy(sourcePath, destinationPath);

        AssetMetadata metadata = CreateMetadata(destinationPath, kind, sourcePath);
        SaveMetadata(metadata);
        return metadata;
    }

    public IReadOnlyList<AssetMetadata> Refresh()
    {
        Directory.CreateDirectory(AssetsRoot);
        List<AssetMetadata> assets = [];
        foreach (string path in Directory.EnumerateFiles(AssetsRoot, "*", SearchOption.AllDirectories)
                     .Where(path => !path.EndsWith(AssetMetadata.MetadataExtension, StringComparison.OrdinalIgnoreCase)))
        {
            AssetMetadata metadata = LoadMetadataForAsset(path) ?? CreateMetadata(path, Classify(path), null);
            SaveMetadata(metadata);
            assets.Add(metadata);
        }

        return assets.OrderBy(asset => asset.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<AssetValidationResult> Validate()
    {
        return Refresh().Select(ValidateAsset).ToArray();
    }

    public AssetMetadata? LoadMetadataForAsset(string assetPath)
    {
        string metadataPath = GetMetadataPath(assetPath);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AssetMetadata>(File.ReadAllText(metadataPath), JsonOptions);
    }

    public string GetMetadataPath(string assetPath) => assetPath + AssetMetadata.MetadataExtension;

    private AssetValidationResult ValidateAsset(AssetMetadata metadata)
    {
        string fullPath = Path.Combine(AssetsRoot, metadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return new AssetValidationResult(metadata, false, "Asset file is missing.");
        }

        try
        {
            if (metadata.Kind == AssetKind.Texture)
            {
                using Image image = Image.FromFile(fullPath);
                if (metadata.Width != image.Width || metadata.Height != image.Height)
                {
                    metadata = metadata with { Width = image.Width, Height = image.Height };
                    SaveMetadata(metadata);
                    return new AssetValidationResult(metadata, true, "Texture dimensions updated.");
                }
            }

            return new AssetValidationResult(metadata, true, "OK");
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException)
        {
            return new AssetValidationResult(metadata, false, ex.Message);
        }
    }

    private AssetMetadata CreateMetadata(string assetPath, AssetKind kind, string? sourcePath)
    {
        int? width = null;
        int? height = null;
        int sliceWidth = 0;
        int sliceHeight = 0;
        if (kind == AssetKind.Texture)
        {
            using Image image = Image.FromFile(assetPath);
            width = image.Width;
            height = image.Height;
            sliceWidth = Math.Min(32, image.Width);
            sliceHeight = Math.Min(32, image.Height);
        }

        return new AssetMetadata(
            Id: Guid.NewGuid(),
            RelativePath: Path.GetRelativePath(AssetsRoot, assetPath).Replace('\\', '/'),
            Kind: kind,
            SourcePath: sourcePath,
            ImportedAtUtc: DateTimeOffset.UtcNow,
            Width: width,
            Height: height,
            SliceWidth: sliceWidth,
            SliceHeight: sliceHeight);
    }

    private void SaveMetadata(AssetMetadata metadata)
    {
        string assetPath = Path.Combine(AssetsRoot, metadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(GetMetadataPath(assetPath), JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private static AssetKind Classify(string path)
    {
        string extension = Path.GetExtension(path);
        if (TextureExtensions.Contains(extension)) return AssetKind.Texture;
        if (AudioExtensions.Contains(extension)) return AssetKind.Audio;
        return AssetKind.Unknown;
    }

    private static string GetAvailablePath(string path)
    {
        if (!File.Exists(path)) return path;
        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int index = 2;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $"{name}-{index}{extension}");
            index++;
        }
        while (File.Exists(candidate));
        return candidate;
    }

    private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}

public enum AssetKind
{
    Unknown,
    Texture,
    Audio,
}

public sealed record AssetMetadata(Guid Id, string RelativePath, AssetKind Kind, string? SourcePath, DateTimeOffset ImportedAtUtc, int? Width, int? Height, int SliceWidth, int SliceHeight)
{
    public const string MetadataExtension = ".asset.json";
}

public sealed record AssetValidationResult(AssetMetadata Metadata, bool IsValid, string Message);
