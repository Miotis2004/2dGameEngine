namespace _2dGameEngine.Prefabs;

/// <summary>
/// Describes a serialized property override on a prefab instance or variant.
/// </summary>
public sealed record PrefabOverride(string EntityPath, string PropertyPath, string? Value);
