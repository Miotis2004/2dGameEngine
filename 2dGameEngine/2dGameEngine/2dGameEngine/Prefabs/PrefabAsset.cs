using System;
using System.Collections.Generic;
using _2dGameEngine.Core;

namespace _2dGameEngine.Prefabs;

/// <summary>
/// Represents a reusable entity hierarchy stored as a prefab asset.
/// </summary>
public sealed class PrefabAsset
{
    private readonly List<PrefabOverride> _variantOverrides = [];

    public PrefabAsset(string name, Entity root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(root);
        Name = name;
        Root = root;
    }

    public string Name { get; }

    public string? AssetPath { get; internal set; }

    public string? BasePrefabPath { get; internal set; }

    public Entity Root { get; }

    public IReadOnlyList<PrefabOverride> VariantOverrides => _variantOverrides;

    public void AddVariantOverride(PrefabOverride prefabOverride)
    {
        ArgumentNullException.ThrowIfNull(prefabOverride);
        _variantOverrides.RemoveAll(existing => existing.EntityPath == prefabOverride.EntityPath && existing.PropertyPath == prefabOverride.PropertyPath);
        _variantOverrides.Add(prefabOverride);
    }

    internal void SetVariantOverrides(IEnumerable<PrefabOverride> overrides)
    {
        _variantOverrides.Clear();
        _variantOverrides.AddRange(overrides);
    }
}
