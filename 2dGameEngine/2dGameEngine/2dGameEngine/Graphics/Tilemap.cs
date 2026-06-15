using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using _2dGameEngine.Core;

namespace _2dGameEngine.Graphics;

/// <summary>
/// Stores a grid of tile ids and the definitions used to render and collide with those tiles.
/// </summary>
public sealed class Tilemap : Component
{
    private readonly int[,] _tiles;
    private readonly Dictionary<int, TileDefinition> _definitions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Tilemap"/> class.
    /// </summary>
    /// <param name="width">The number of tile columns.</param>
    /// <param name="height">The number of tile rows.</param>
    /// <param name="tileSize">The size of each tile in world units.</param>
    public Tilemap(int width, int height, Vector2 tileSize)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Tilemap width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Tilemap height must be positive.");
        }

        if (tileSize.X <= 0.0f || tileSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(tileSize), "Tile size must be positive on both axes.");
        }

        Width = width;
        Height = height;
        TileSize = tileSize;
        _tiles = new int[width, height];
    }

    /// <summary>
    /// Gets the number of tile columns.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the number of tile rows.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the size of each tile in world units.
    /// </summary>
    public Vector2 TileSize { get; }

    /// <summary>
    /// Gets or sets the draw order. Higher values are rendered later.
    /// </summary>
    public int SortingOrder { get; set; }

    /// <summary>
    /// Gets or sets the render layer used by camera culling and 2D lights.
    /// </summary>
    public RenderLayerMask RenderLayer { get; set; } = RenderLayerMask.Default;

    /// <summary>
    /// Gets the material controlling tint, blend, and lighting behavior.
    /// </summary>
    public Material2D Material { get; set; } = new();

    /// <summary>
    /// Gets the registered tile definitions keyed by tile id.
    /// </summary>
    public IReadOnlyDictionary<int, TileDefinition> Definitions => _definitions;

    /// <summary>
    /// Registers or replaces a tile definition.
    /// </summary>
    /// <param name="definition">The tile definition to register.</param>
    public void SetDefinition(TileDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Id] = definition;
    }

    /// <summary>
    /// Writes a tile id into the requested cell. Use 0 to clear a tile.
    /// </summary>
    public void SetTile(int x, int y, int tileId)
    {
        ValidateCell(x, y);
        if (tileId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileId), "Tile id cannot be negative.");
        }

        _tiles[x, y] = tileId;
    }

    /// <summary>
    /// Gets the tile id at the requested cell.
    /// </summary>
    public int GetTile(int x, int y)
    {
        ValidateCell(x, y);
        return _tiles[x, y];
    }

    /// <summary>
    /// Gets a value indicating whether the requested cell exists and contains a solid tile definition.
    /// </summary>
    public bool IsSolidTile(int x, int y)
    {
        return IsInBounds(x, y) && _definitions.TryGetValue(_tiles[x, y], out TileDefinition? definition) && definition.IsSolid;
    }

    /// <summary>
    /// Gets every non-empty tile cell in row-major order.
    /// </summary>
    public IEnumerable<(int X, int Y, int TileId, TileDefinition Definition)> GetOccupiedTiles()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int tileId = _tiles[x, y];
                if (tileId > 0 && _definitions.TryGetValue(tileId, out TileDefinition? definition))
                {
                    yield return (x, y, tileId, definition);
                }
            }
        }
    }

    /// <summary>
    /// Gets a tile's world-space bounds.
    /// </summary>
    public RectangleF GetTileBounds(int x, int y)
    {
        ValidateCell(x, y);
        Vector2 origin = Entity?.Transform.Value.Position ?? Vector2.Zero;
        return new RectangleF(origin.X + x * TileSize.X, origin.Y + y * TileSize.Y, TileSize.X, TileSize.Y);
    }

    /// <summary>
    /// Gets the full world-space bounds of this tilemap.
    /// </summary>
    public RectangleF GetBounds()
    {
        Vector2 origin = Entity?.Transform.Value.Position ?? Vector2.Zero;
        return new RectangleF(origin.X, origin.Y, Width * TileSize.X, Height * TileSize.Y);
    }

    private bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    private void ValidateCell(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Tile coordinates must be within the tilemap bounds.");
        }
    }
}
