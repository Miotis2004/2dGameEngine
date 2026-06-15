using System.Drawing;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Project-level visual settings consumed by <see cref="Renderer2D"/>.
/// </summary>
public sealed class RenderPipelineAsset
{
    public Color AmbientLight { get; set; } = Color.FromArgb(255, 40, 40, 48);
    public bool EnableLighting { get; set; } = true;
    public bool PixelPerfect { get; set; } = true;
    public bool Bloom { get; set; }
    public bool ColorGrading { get; set; }
    public bool Vignette { get; set; }
    public RenderDebugMode DebugMode { get; set; } = RenderDebugMode.None;
    public RenderLayerMask CameraCullingMask { get; set; } = RenderLayerMask.Everything;
}

/// <summary>
/// Scene-view diagnostic overlays supported by the render pipeline.
/// </summary>
public enum RenderDebugMode
{
    None,
    Overdraw,
    Batches,
    Colliders,
    Lighting,
}

/// <summary>
/// Bitmask of render layers used by cameras, lights, sprites, and tilemaps.
/// </summary>
[System.Flags]
public enum RenderLayerMask
{
    None = 0,
    Default = 1 << 0,
    Background = 1 << 1,
    Gameplay = 1 << 2,
    Foreground = 1 << 3,
    Ui = 1 << 4,
    Everything = Default | Background | Gameplay | Foreground | Ui,
}
