using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Serialization;

namespace _2dGameEngine.Prefabs;

/// <summary>
/// Saves and restores prefab assets as human-readable JSON documents.
/// </summary>
public static class PrefabSerializer
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(PrefabAsset prefab)
    {
        ArgumentNullException.ThrowIfNull(prefab);
        string sceneJson = SceneSerializer.Serialize(new Scene(prefab.Name).AddRoot(prefab.Root));
        PrefabDocument document = new(CurrentSchemaVersion, prefab.Name, prefab.BasePrefabPath, sceneJson, prefab.VariantOverrides.ToArray());
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public static void Save(PrefabAsset prefab, string path)
    {
        ArgumentNullException.ThrowIfNull(prefab);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        prefab.AssetPath = path;
        File.WriteAllText(path, Serialize(prefab));
    }

    public static PrefabAsset Deserialize(string json, AssetManager? assets = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        PrefabDocument document = JsonSerializer.Deserialize<PrefabDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("Prefab document is empty or invalid.");
        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported prefab schema version '{document.SchemaVersion}'. Expected '{CurrentSchemaVersion}'.");
        }

        Scene scene = SceneSerializer.Deserialize(document.RootSceneJson, assets);
        if (scene.Entities.Count != 1)
        {
            throw new InvalidDataException("Prefab documents must contain exactly one root entity.");
        }

        PrefabAsset prefab = new(document.Name, scene.Entities[0])
        {
            BasePrefabPath = document.BasePrefabPath,
        };
        prefab.SetVariantOverrides(document.VariantOverrides ?? []);
        return prefab;
    }

    public static PrefabAsset Load(string path, AssetManager? assets = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        PrefabAsset prefab = Deserialize(File.ReadAllText(path), assets);
        prefab.AssetPath = path;
        return prefab;
    }

    private sealed record PrefabDocument(int SchemaVersion, string Name, string? BasePrefabPath, string RootSceneJson, PrefabOverride[]? VariantOverrides);

    private static Scene AddRoot(this Scene scene, Entity root)
    {
        scene.AddEntity(root);
        return scene;
    }
}
