using System.Drawing;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Describes how a 2D renderer should shade and composite a sprite or tile.
/// </summary>
public sealed class Material2D
{
    /// <summary>
    /// Gets or sets the display name used by editor tooling.
    /// </summary>
    public string Name { get; set; } = "Default 2D Material";

    /// <summary>
    /// Gets or sets the material tint multiplied with the renderer color.
    /// </summary>
    public Color Tint { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the alpha compositing mode requested by the material.
    /// </summary>
    public MaterialBlendMode BlendMode { get; set; } = MaterialBlendMode.AlphaBlend;

    /// <summary>
    /// Gets or sets an optional texture asset path used by editor pipeline tools.
    /// Runtime sprites can still override the rendered frame directly.
    /// </summary>
    public string? TexturePath { get; set; }

    /// <summary>
    /// Gets or sets an optional normal-map asset path used by lighting-aware importers.
    /// </summary>
    public string? NormalMapPath { get; set; }

    /// <summary>
    /// Gets or sets the shader profile name used by custom render pipeline tooling.
    /// </summary>
    public string Shader { get; set; } = "Sprites/Lit";

    /// <summary>
    /// Gets or sets a value indicating whether scene lights should affect this material.
    /// </summary>
    public bool ReceivesLighting { get; set; } = true;

    /// <summary>
    /// Creates a shallow copy that can be safely assigned to another renderer.
    /// </summary>
    public Material2D Clone() => new()
    {
        Name = Name,
        Tint = Tint,
        BlendMode = BlendMode,
        TexturePath = TexturePath,
        NormalMapPath = NormalMapPath,
        Shader = Shader,
        ReceivesLighting = ReceivesLighting,
    };
}

/// <summary>
/// Supported 2D sprite material blend modes.
/// </summary>
public enum MaterialBlendMode
{
    Opaque,
    AlphaBlend,
    Additive,
    Multiply,
}
