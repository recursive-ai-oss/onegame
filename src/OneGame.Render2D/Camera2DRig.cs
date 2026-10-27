using System.Numerics;
using OneGame.UI;

namespace OneGame.Render2D;

/// <summary>
/// A camera that follows a point: optional smoothing, world bounds, zoom, and conversion between screen pixels and world points. It is
/// view state (it remembers where it is between frames) and is never part of the simulation. <see cref="Camera2D"/> remains the low-level
/// value the renderer takes; <see cref="Current"/> gives it.
/// </summary>
public sealed class Camera2DRig
{
    private Vector2 _position;
    private bool _placed;

    /// <summary>Physical pixels per world unit.</summary>
    public float Zoom { get; set; } = 1;

    /// <summary>Rotation in radians; positive turns the view clockwise.</summary>
    public float Rotation { get; set; }

    /// <summary>
    /// How quickly the camera catches up with its target, as a time constant in seconds (about 63% of the distance closes in that time).
    /// Zero follows exactly.
    /// </summary>
    public float Smoothing { get; set; }

    /// <summary>The world area the view may not leave, or null for none. A view larger than the bounds is centred on them.</summary>
    public Rect? Bounds { get; set; }

    /// <summary>Round sprite positions to whole pixels; see <see cref="Camera2D.PixelSnap"/>.</summary>
    public bool PixelSnap { get; set; }

    /// <summary>The world point at the centre of the screen.</summary>
    public Vector2 Position => _position;

    /// <summary>Puts the camera on a point at once, with no smoothing, and keeps it there until the next <see cref="Follow"/>.</summary>
    /// <param name="position">The world point to centre.</param>
    public void Snap(Vector2 position)
    {
        _position = position;
        _placed = true;
    }

    /// <summary>Moves toward a target. The first call, and any call after <see cref="Snap"/> was never made, starts exactly on it.</summary>
    /// <param name="target">The world point to centre on.</param>
    /// <param name="seconds">View time since the last call; with <see cref="Smoothing"/> above zero it sets how far the camera closes in.</param>
    public void Follow(Vector2 target, float seconds)
    {
        if (!_placed || Smoothing <= 0)
        {
            _position = target;
            _placed = true;
            return;
        }

        if (seconds > 0)
        {
            _position += (target - _position) * (1f - MathF.Exp(-seconds / Smoothing));
        }
    }

    /// <summary>The camera for a screen size, with the bounds applied.</summary>
    /// <param name="screenSize">Target size in pixels.</param>
    /// <returns>The value to pass to the renderer.</returns>
    public Camera2D Current(Vector2 screenSize) => new()
    {
        Position = Clamp(_position, screenSize),
        Zoom = Zoom,
        Rotation = Rotation,
        PixelSnap = PixelSnap,
    };

    /// <summary>Where a world point lands on the screen, in pixels.</summary>
    /// <param name="world">A world point.</param>
    /// <param name="screenSize">Target size in pixels.</param>
    /// <returns>Framebuffer pixels, origin top-left.</returns>
    public Vector2 WorldToScreen(Vector2 world, Vector2 screenSize) => Vector2.Transform(world, Current(screenSize).View(screenSize, Vector2.One));

    /// <summary>The world point under a screen pixel.</summary>
    /// <param name="screen">Framebuffer pixels, origin top-left.</param>
    /// <param name="screenSize">Target size in pixels.</param>
    /// <returns>A world point.</returns>
    public Vector2 ScreenToWorld(Vector2 screen, Vector2 screenSize) =>
        Matrix3x2.Invert(Current(screenSize).View(screenSize, Vector2.One), out var inverse) ? Vector2.Transform(screen, inverse) : _position;

    private Vector2 Clamp(Vector2 p, Vector2 screenSize)
    {
        if (Bounds is not { } b || !(Zoom > 0))
        {
            return p;
        }

        // The half extent of the visible area along each world axis, allowing for rotation.
        var hw = screenSize.X * 0.5f / Zoom;
        var hh = screenSize.Y * 0.5f / Zoom;
        var c = MathF.Abs(MathF.Cos(Rotation));
        var s = MathF.Abs(MathF.Sin(Rotation));
        var ex = (c * hw) + (s * hh);
        var ey = (s * hw) + (c * hh);
        return new Vector2(Axis(p.X, b.X, b.Right, ex), Axis(p.Y, b.Y, b.Bottom, ey));
    }

    private static float Axis(float v, float min, float max, float half) =>
        max - min <= 2 * half ? (min + max) * 0.5f : Math.Clamp(v, min + half, max - half);
}
