using System.Globalization;

namespace OneGame.Platform;

/// <summary>How a window is shown.</summary>
public enum WindowState : byte
{
    /// <summary>An ordinary window at its own size and position.</summary>
    Normal,

    /// <summary>Enlarged to fill the work area of its screen; the normal rectangle is kept for when it is restored.</summary>
    Maximized,

    /// <summary>Covering the whole screen.</summary>
    Fullscreen,
}

/// <summary>A rectangle in screen pixels, such as the usable area of a monitor.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    /// <summary>The right edge (exclusive).</summary>
    public int Right => X + Width;

    /// <summary>The bottom edge (exclusive).</summary>
    public int Bottom => Y + Height;

    internal long OverlapArea(ScreenRect other)
    {
        long w = Math.Min(Right, other.Right) - Math.Max(X, other.X);
        long h = Math.Min(Bottom, other.Bottom) - Math.Max(Y, other.Y);
        return w > 0 && h > 0 ? w * h : 0;
    }
}

/// <summary>
/// Where a window is and how it is shown, as a plain value a game can keep in its settings and apply to a later window.
/// <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/> and <see cref="Height"/> describe the <em>normal</em> (restored) client rectangle in
/// screen pixels, even while the window is maximized or fullscreen, so restoring from either state returns to where the player left it.
/// </summary>
/// <param name="X">Left edge of the client area on the virtual screen. Meaningful only when <paramref name="HasPosition"/>.</param>
/// <param name="Y">Top edge of the client area on the virtual screen. Meaningful only when <paramref name="HasPosition"/>.</param>
/// <param name="Width">Client width in pixels.</param>
/// <param name="Height">Client height in pixels.</param>
/// <param name="State">Normal, maximized or fullscreen.</param>
/// <param name="HasPosition">False where the window system does not let a program read or choose a window's position (Wayland, phones, browsers).</param>
public readonly record struct WindowPlacement(int X, int Y, int Width, int Height, WindowState State = WindowState.Normal, bool HasPosition = true)
{
    // Enough of the window must stay on a screen for the player to grab it: a title bar's worth of height and a little width.
    private const int MinVisibleWidth = 96, MinVisibleHeight = 48;

    /// <summary>A placement that fixes only size and state, for window systems that choose the position themselves.</summary>
    /// <param name="width">Client width in pixels.</param>
    /// <param name="height">Client height in pixels.</param>
    /// <param name="state">Normal, maximized or fullscreen.</param>
    /// <returns>The placement, with <see cref="HasPosition"/> false.</returns>
    public static WindowPlacement SizeOnly(int width, int height, WindowState state = WindowState.Normal) => new(0, 0, width, height, state, false);

    /// <summary>
    /// Moves and shrinks the normal rectangle so it can be reached on the given screens: when less than a title bar's worth of it is
    /// visible on any screen (a monitor that was unplugged, a resolution that dropped), it is fitted into the nearest screen's work area.
    /// A placement that is already reachable is returned unchanged. The first area is taken as the primary screen.
    /// </summary>
    /// <param name="workAreas">The usable rectangle of each connected screen. Empty leaves the placement as it is.</param>
    /// <returns>The placement to apply.</returns>
    public WindowPlacement ClampTo(IReadOnlyList<ScreenRect> workAreas)
    {
        var width = Math.Max(1, Width);
        var height = Math.Max(1, Height);
        if (workAreas.Count == 0)
        {
            return this with { Width = width, Height = height };
        }

        if (!HasPosition)
        {
            var primary = workAreas[0];
            return this with { Width = Math.Min(width, primary.Width), Height = Math.Min(height, primary.Height) };
        }

        var rect = new ScreenRect(X, Y, width, height);
        var best = workAreas[0];
        long bestArea = -1;
        foreach (var area in workAreas)
        {
            var overlap = rect.OverlapArea(area);
            if (overlap > bestArea)
            {
                best = area;
                bestArea = overlap;
            }
        }

        // A window may span screens, but not be larger than all of them together: its controls would be out of reach.
        int left = workAreas[0].X, top = workAreas[0].Y, right = workAreas[0].Right, bottom = workAreas[0].Bottom;
        foreach (var area in workAreas)
        {
            left = Math.Min(left, area.X);
            top = Math.Min(top, area.Y);
            right = Math.Max(right, area.Right);
            bottom = Math.Max(bottom, area.Bottom);
        }

        var visibleWidth = Math.Min(rect.Right, best.Right) - Math.Max(rect.X, best.X);
        var visibleHeight = Math.Min(rect.Bottom, best.Bottom) - Math.Max(rect.Y, best.Y);
        if (width <= right - left && height <= bottom - top
            && visibleWidth >= Math.Min(MinVisibleWidth, width) && visibleHeight >= Math.Min(MinVisibleHeight, height))
        {
            return this with { Width = width, Height = height };
        }

        var fittedWidth = Math.Min(width, best.Width);
        var fittedHeight = Math.Min(height, best.Height);
        var x = Math.Clamp(X, best.X, best.Right - fittedWidth);
        var y = Math.Clamp(Y, best.Y, best.Bottom - fittedHeight);
        return this with { X = x, Y = y, Width = fittedWidth, Height = fittedHeight };
    }

    /// <summary>Writes the placement as text for a settings file, such as <c>100,80,1280x720,maximized</c> or <c>-,-,1280x720,normal</c>.</summary>
    /// <returns>The text; <see cref="TryParse"/> reads it back.</returns>
    public string Format()
    {
        var inv = CultureInfo.InvariantCulture;
        var position = HasPosition ? string.Create(inv, $"{X},{Y}") : "-,-";
        return string.Create(inv, $"{position},{Width}x{Height},{State.ToString().ToLowerInvariant()}");
    }

    /// <summary>Reads text written by <see cref="Format"/>. Anything else, including absurd sizes, is refused.</summary>
    /// <param name="text">The text.</param>
    /// <param name="placement">The placement when the text is valid.</param>
    /// <returns>True when the text was valid.</returns>
    public static bool TryParse(string? text, out WindowPlacement placement)
    {
        placement = default;
        if (text is null)
        {
            return false;
        }

        var parts = text.Split(',');
        if (parts.Length != 4)
        {
            return false;
        }

        var size = parts[2].Split('x');
        var inv = CultureInfo.InvariantCulture;
        var hasPosition = parts[0] != "-" || parts[1] != "-";
        int x = 0, y = 0;
        if (hasPosition && (!int.TryParse(parts[0], NumberStyles.AllowLeadingSign, inv, out x) || !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, inv, out y)))
        {
            return false;
        }

        if (size.Length != 2
            || !int.TryParse(size[0], NumberStyles.None, inv, out var width) || !int.TryParse(size[1], NumberStyles.None, inv, out var height)
            || width is < 1 or > 65535 || height is < 1 or > 65535)
        {
            return false;
        }

        if (!Enum.TryParse<WindowState>(parts[3], true, out var state) || !Enum.IsDefined(state) || Math.Abs((long)x) > 1_000_000 || Math.Abs((long)y) > 1_000_000)
        {
            return false;
        }

        placement = new WindowPlacement(x, y, width, height, state, hasPosition);
        return true;
    }
}
