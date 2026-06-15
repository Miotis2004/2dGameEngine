namespace _2dGameEngine.Graphics;

/// <summary>
/// Selects the rendering backend. Win2D is the preferred hardware-accelerated path when hosted by a WinUI/Win2D surface; GDI remains the editor fallback.
/// </summary>
public enum RenderBackendKind
{
    SystemDrawingFallback,
    Win2D,
}
