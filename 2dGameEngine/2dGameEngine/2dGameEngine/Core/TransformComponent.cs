namespace _2dGameEngine.Core;

/// <summary>
/// Component wrapper around an entity's two-dimensional transform.
/// </summary>
public sealed class TransformComponent : Component
{
    /// <summary>
    /// Gets the transform data for the owning entity.
    /// </summary>
    public Transform2D Value { get; } = new();
}
