using System;
using System.Collections.Generic;
using _2dGameEngine.Core;

namespace _2dGameEngine.Prefabs;

/// <summary>
/// Marks an entity hierarchy as an instance of a prefab asset and records local overrides.
/// </summary>
public sealed class PrefabInstanceComponent : Component
{
    private readonly List<PrefabOverride> _overrides = [];

    public PrefabInstanceComponent(string prefabPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefabPath);
        PrefabPath = prefabPath;
    }

    public string PrefabPath { get; private set; }

    public bool IsConnected { get; private set; } = true;

    public bool IsUnpacked => !IsConnected;

    public IReadOnlyList<PrefabOverride> Overrides => _overrides;

    public void RecordOverride(PrefabOverride prefabOverride)
    {
        ArgumentNullException.ThrowIfNull(prefabOverride);
        _overrides.RemoveAll(existing => existing.EntityPath == prefabOverride.EntityPath && existing.PropertyPath == prefabOverride.PropertyPath);
        _overrides.Add(prefabOverride);
    }

    public bool RemoveOverride(string entityPath, string propertyPath)
    {
        return _overrides.RemoveAll(existing => existing.EntityPath == entityPath && existing.PropertyPath == propertyPath) > 0;
    }

    public void ClearOverrides() => _overrides.Clear();

    public void Unpack()
    {
        IsConnected = false;
    }

    public void Reconnect(string prefabPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefabPath);
        PrefabPath = prefabPath;
        IsConnected = true;
    }

    internal void SetOverrides(IEnumerable<PrefabOverride> overrides)
    {
        _overrides.Clear();
        _overrides.AddRange(overrides);
    }
}
