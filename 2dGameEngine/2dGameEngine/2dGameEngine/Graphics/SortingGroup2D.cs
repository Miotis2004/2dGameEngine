using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Offsets the sorting order and render layer for all renderers on the same entity.
/// </summary>
public sealed class SortingGroup2D : Component
{
    public int SortingOrderOffset { get; set; }
    public RenderLayerMask Layer { get; set; } = RenderLayerMask.Default;
}
