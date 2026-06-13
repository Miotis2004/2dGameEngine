using System.Drawing;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Describes how a tile id should be rendered and interpreted by tilemap systems.
/// </summary>
/// <param name="id">The non-zero tile id.</param>
/// <param name="color">The color used to render the tile.</param>
/// <param name="isSolid">A value indicating whether the tile blocks physics bodies.</param>
public sealed class TileDefinition(int id, Color color, bool isSolid = true)
{
    /// <summary>
    /// Gets the tile id. Id 0 is reserved for empty cells.
    /// </summary>
    public int Id { get; } = id > 0 ? id : throw new System.ArgumentOutOfRangeException(nameof(id), "Tile id must be greater than zero.");

    /// <summary>
    /// Gets or sets the tile render color.
    /// </summary>
    public Color Color { get; set; } = color;

    /// <summary>
    /// Gets or sets a value indicating whether this tile should be treated as solid by tilemap colliders.
    /// </summary>
    public bool IsSolid { get; set; } = isSolid;
}
