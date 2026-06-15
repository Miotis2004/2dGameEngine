using System.Collections.Generic;
using System.Drawing;
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
    public static void GetColliders(Scene scene, List<Collider2D> results)
    {
        results.Clear();
        for (int i = 0; i < scene.Entities.Count; i++)
        {
            Entity entity = scene.Entities[i];
            if (!entity.IsEnabled) continue;
            for (int c = 0; c < entity.Components.Count; c++)
            {
                if (entity.Components[c] is Collider2D { IsEnabled: true } collider)
                {
                    results.Add(collider);
                }
            }
        }
    }


    /// <summary>
    /// Enumerates every enabled collider in the scene. Prefer the overload that fills a caller-owned list in hot paths.
    /// </summary>
    public static IEnumerable<Collider2D> GetColliders(Scene scene)
    {
        for (int i = 0; i < scene.Entities.Count; i++)
        {
            Entity entity = scene.Entities[i];
            if (!entity.IsEnabled) continue;
            for (int c = 0; c < entity.Components.Count; c++)
            {
                if (entity.Components[c] is Collider2D { IsEnabled: true } collider)
                {
                    yield return collider;
                }
            }
        }
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
