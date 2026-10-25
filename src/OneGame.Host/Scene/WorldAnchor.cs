using System.Numerics;
using OneGame.Core;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>Pins UI elements to points in a 3D world.</summary>
public static class WorldAnchor
{
    /// <summary>
    /// Keeps an element at the screen position of a world point, and hides it while the point is behind the camera, beyond its near or far
    /// plane, or off screen. The point is read every UI frame, so a label can follow a moving entity. The element should be a direct
    /// child of the screen root, because it is placed against the screen.
    /// </summary>
    /// <param name="element">The element to pin.</param>
    /// <param name="view">The scene view whose camera projects the point.</param>
    /// <param name="game">The game being shown; its world is asked what the scene looks like, and its UI size is the screen.</param>
    /// <param name="worldPoint">Reads the point each frame; return null to hide the element (an entity that is gone).</param>
    /// <param name="pixelOffset">Extra shift in pixels (positive is right and down).</param>
    /// <param name="pivot">Which point of the element sits on the world point, from (0,0) top left to (1,1) bottom right. Default: bottom centre, so a label floats above.</param>
    /// <returns>The element.</returns>
    public static Element PinToWorld(this Element element, SceneView3D view, GameInstance game, Func<FVec3?> worldPoint, Vector2 pixelOffset = default, Vector2? pivot = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(worldPoint);
        var p = pivot ?? new Vector2(0.5f, 1f);
        var shown = false;
        var at = Vector2.Zero;
        return element
            .BindVisible(() =>
            {
                shown = false;
                if (worldPoint() is not { } point)
                {
                    return false;
                }

                var width = (int)MathF.Round(game.Ui.Width);
                var height = (int)MathF.Round(game.Ui.Height);
                view.Prepare(game.World, game.World.Tick, game.FrameAlpha);
                if (!view.TryProject(SceneWriter.ToVector(point), width, height, out var pixel, out _))
                {
                    return false;
                }

                // Off screen by more than a pixel or two means nothing to show; a label half over the edge still is.
                if (pixel.X < -1f || pixel.Y < -1f || pixel.X > width + 1f || pixel.Y > height + 1f)
                {
                    return false;
                }

                at = pixel + pixelOffset;
                shown = true;
                return true;
            })
            .BindStyle(s =>
            {
                if (shown)
                {
                    s.Anchor(AnchorPoint.TopLeft, Length.Px(MathF.Round(at.X)), Length.Px(MathF.Round(at.Y)), toScreen: true)
                        .Translate(Length.Percent(-100f * p.X), Length.Percent(-100f * p.Y));
                }
            });
    }
}
