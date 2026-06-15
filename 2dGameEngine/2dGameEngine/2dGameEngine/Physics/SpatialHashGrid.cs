using System;
using System.Collections.Generic;
using System.Drawing;

namespace _2dGameEngine.Physics;

/// <summary>
/// Broad-phase spatial hash for reducing collider pair checks before narrow-phase AABB tests.
/// </summary>
public sealed class SpatialHashGrid
{
    private readonly Dictionary<(int X, int Y), List<Collider2D>> _cells = [];

    public float CellSize { get; set; } = 128.0f;

    public void Rebuild(IReadOnlyList<Collider2D> colliders)
    {
        _cells.Clear();
        for (int i = 0; i < colliders.Count; i++)
        {
            Collider2D collider = colliders[i];
            RectangleF bounds = collider.GetBounds();
            foreach ((int x, int y) in EnumerateCells(bounds))
            {
                if (!_cells.TryGetValue((x, y), out List<Collider2D>? bucket))
                {
                    bucket = [];
                    _cells.Add((x, y), bucket);
                }
                bucket.Add(collider);
            }
        }
    }

    public void Query(RectangleF bounds, List<Collider2D> results)
    {
        results.Clear();
        for (int x = ToCell(bounds.Left); x <= ToCell(bounds.Right); x++)
        {
            for (int y = ToCell(bounds.Top); y <= ToCell(bounds.Bottom); y++)
            {
                if (!_cells.TryGetValue((x, y), out List<Collider2D>? bucket)) continue;
                for (int i = 0; i < bucket.Count; i++)
                {
                    Collider2D collider = bucket[i];
                    if (!results.Contains(collider)) results.Add(collider);
                }
            }
        }
    }

    private IEnumerable<(int X, int Y)> EnumerateCells(RectangleF bounds)
    {
        for (int x = ToCell(bounds.Left); x <= ToCell(bounds.Right); x++)
        for (int y = ToCell(bounds.Top); y <= ToCell(bounds.Bottom); y++)
            yield return (x, y);
    }

    private int ToCell(float coordinate) => (int)MathF.Floor(coordinate / MathF.Max(1.0f, CellSize));
}
