using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Serialization;

namespace _2dGameEngine.Prefabs;

/// <summary>
/// High-level prefab authoring operations used by the editor and tooling.
/// </summary>
public static class PrefabUtility
{
    public static PrefabAsset CreatePrefab(Entity root, string name, AssetManager? assets = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PrefabAsset(name, SceneSerializer.CloneEntity(root, assets));
    }

    public static PrefabAsset CreateVariant(PrefabAsset basePrefab, string name, params PrefabOverride[] overrides)
    {
        ArgumentNullException.ThrowIfNull(basePrefab);
        PrefabAsset variant = new(name, SceneSerializer.CloneEntity(basePrefab.Root));
        variant.BasePrefabPath = basePrefab.AssetPath;
        foreach (PrefabOverride prefabOverride in overrides)
        {
            variant.AddVariantOverride(prefabOverride);
            ApplyOverride(variant.Root, prefabOverride);
        }

        return variant;
    }

    public static Entity Instantiate(PrefabAsset prefab, AssetManager? assets = null)
    {
        ArgumentNullException.ThrowIfNull(prefab);
        Entity instance = SceneSerializer.CloneEntity(prefab.Root, assets);
        foreach (PrefabOverride prefabOverride in prefab.VariantOverrides)
        {
            ApplyOverride(instance, prefabOverride);
        }

        if (!string.IsNullOrWhiteSpace(prefab.AssetPath))
        {
            instance.AddComponent(new PrefabInstanceComponent(prefab.AssetPath));
        }

        return instance;
    }

    public static Entity Instantiate(string prefabPath, AssetManager? assets = null)
    {
        PrefabAsset prefab = PrefabSerializer.Load(prefabPath, assets);
        return Instantiate(prefab, assets);
    }

    public static void Unpack(Entity instance)
    {
        PrefabInstanceComponent component = RequireInstance(instance);
        component.Unpack();
    }

    public static void Reconnect(Entity instance, string prefabPath)
    {
        RequireInstance(instance).Reconnect(prefabPath);
    }

    public static void RevertOverrides(Entity instance, AssetManager? assets = null)
    {
        PrefabInstanceComponent component = RequireInstance(instance);
        if (!component.IsConnected)
        {
            throw new InvalidOperationException("Unpacked prefab instances cannot be reverted.");
        }

        PrefabAsset prefab = PrefabSerializer.Load(component.PrefabPath, assets);
        Entity replacement = Instantiate(prefab, assets);
        CopyAuthoredState(replacement, instance);
        component.ClearOverrides();
    }

    public static void ApplyOverridesToAsset(Entity instance, AssetManager? assets = null)
    {
        PrefabInstanceComponent component = RequireInstance(instance);
        if (!component.IsConnected)
        {
            throw new InvalidOperationException("Unpacked prefab instances cannot apply overrides.");
        }

        PrefabAsset prefab = CreatePrefab(instance, Path.GetFileNameWithoutExtension(component.PrefabPath), assets);
        prefab.AssetPath = component.PrefabPath;
        PrefabSerializer.Save(prefab, component.PrefabPath);
        component.ClearOverrides();
    }

    public static void RecordOverride(Entity instance, PrefabOverride prefabOverride)
    {
        PrefabInstanceComponent component = RequireInstance(instance);
        component.RecordOverride(prefabOverride);
        ApplyOverride(instance, prefabOverride);
    }

    public static bool ApplyOverride(Entity root, PrefabOverride prefabOverride)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(prefabOverride);
        Entity? target = FindByPath(root, prefabOverride.EntityPath);
        if (target is null)
        {
            return false;
        }

        string value = prefabOverride.Value ?? string.Empty;
        switch (prefabOverride.PropertyPath)
        {
            case "name":
                target.Name = value;
                return true;
            case "isEnabled":
                target.IsEnabled = bool.Parse(value);
                return true;
            case "transform.position":
                target.Transform.Value.Position = ParseVector2(value);
                return true;
            case "transform.rotation":
                target.Transform.Value.Rotation = float.Parse(value, CultureInfo.InvariantCulture);
                return true;
            case "transform.scale":
                target.Transform.Value.Scale = ParseVector2(value);
                return true;
            default:
                return false;
        }
    }

    private static PrefabInstanceComponent RequireInstance(Entity instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return instance.GetComponent<PrefabInstanceComponent>()
            ?? throw new InvalidOperationException("The entity is not a prefab instance root.");
    }

    private static Entity? FindByPath(Entity root, string path)
    {
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts[0] != root.Name)
        {
            return null;
        }

        Entity current = root;
        foreach (string part in parts.Skip(1))
        {
            current = current.Children.FirstOrDefault(child => child.Name == part)!;
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private static Vector2 ParseVector2(string value)
    {
        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new FormatException("Vector2 override values must use 'x,y' format.");
        }

        return new Vector2(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    private static void CopyAuthoredState(Entity source, Entity destination)
    {
        destination.Name = source.Name;
        destination.IsEnabled = source.IsEnabled;
        destination.Transform.Value.Position = source.Transform.Value.Position;
        destination.Transform.Value.Rotation = source.Transform.Value.Rotation;
        destination.Transform.Value.Scale = source.Transform.Value.Scale;
    }
}
