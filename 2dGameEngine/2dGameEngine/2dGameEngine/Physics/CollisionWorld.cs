using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using _2dGameEngine.Core;

namespace _2dGameEngine.Physics;

/// <summary>
/// Provides collision queries over colliders in a scene.
/// </summary>
public sealed class CollisionWorld
{
    /// <summary>
    /// Gets every enabled collider in the scene.
    /// </summary>
    /// <param name="scene">The scene to query.</param>
    /// <returns>The enabled colliders.</returns>
    public static IEnumerable<Collider2D> GetColliders(Scene scene)
    {
        return scene.Entities.Where(entity => entity.IsEnabled)
            .SelectMany(entity => entity.Components.OfType<Collider2D>())
            .Where(collider => collider.IsEnabled);
    }

    /// <summary>
    /// Gets a value indicating whether two axis-aligned bounds overlap.
    /// </summary>
    /// <param name="first">The first bounds.</param>
    /// <param name="second">The second bounds.</param>
    /// <returns><see langword="true" /> when the bounds overlap.</returns>
    public static bool Intersects(RectangleF first, RectangleF second)
    {
        return first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;
    }
}
