using System.Collections.Generic;
using System.Drawing;
using _2dGameEngine.Graphics;

namespace _2dGameEngine.Physics;

/// <summary>
/// Exposes the solid cells of an attached <see cref="Tilemap"/> to the physics system.
/// </summary>
public sealed class TilemapCollider2D : Collider2D
{
    /// <inheritdoc />
    public override RectangleF GetBounds()
    {
        Tilemap? tilemap = Entity?.GetComponent<Tilemap>();
        return tilemap?.GetBounds() ?? RectangleF.Empty;
    }

    /// <summary>
    /// Gets solid tile bounds that overlap the supplied world-space area.
    /// </summary>
    public IEnumerable<RectangleF> GetSolidTileBounds(RectangleF area)
    {
        Tilemap? tilemap = Entity?.GetComponent<Tilemap>();
        if (tilemap is null)
        {
            yield break;
        }

        foreach ((int x, int y, _, _) in tilemap.GetOccupiedTiles())
        {
            if (!tilemap.IsSolidTile(x, y))
            {
                continue;
            }

            RectangleF bounds = tilemap.GetTileBounds(x, y);
            if (CollisionWorld.Intersects(area, bounds))
            {
                yield return bounds;
            }
        }
    }
}
