using OneGame.Host;

namespace OneGame.Control;

/// <summary>Result of the <c>overlay</c> operation.</summary>
/// <param name="Visible">Whether the debug panel is shown.</param>
/// <param name="Key">The toggle key, or null for none.</param>
/// <param name="Chord">The toggle buttons on a gamepad, or empty for none.</param>
/// <param name="Masked">Whether machine-dependent figures show as <c>--</c>.</param>
/// <param name="Lines">The panel's text now.</param>
public sealed record OverlayResult(bool Visible, string? Key, string[] Chord, bool Masked, string[] Lines);

public sealed partial class ControlSurface
{
    /// <summary>The debug panel of this game.</summary>
    public DebugOverlay Overlay { get; }

    private OverlayResult OverlayOp(Args a)
    {
        var o = Overlay;
        if (a.Has("visible"))
        {
            o.Visible = a.Bool("visible", false);
        }

        if (a.Bool("toggle", false))
        {
            o.Visible = !o.Visible;
        }

        if (a.Has("masked"))
        {
            o.Masked = a.Bool("masked", true);
        }

        if (a.Str("key") is { } key)
        {
            o.ToggleKey = key.Equals("none", StringComparison.OrdinalIgnoreCase)
                ? null
                : OneGame.Host.KeyNames.TryParse(key, out var k) ? k : throw new ControlException("bad_args", $"'{key}' is not a key name (F4, Backquote, none).");
        }

        if (a.Str("chord") is { } chord)
        {
            try
            {
                o.ToggleChord = DebugOverlay.ParseChord(chord);
            }
            catch (FormatException ex)
            {
                throw new ControlException("bad_args", ex.Message, ex);
            }
        }

        return new OverlayResult(o.Visible, o.ToggleKey?.ToString(), [.. o.ToggleChord.Select(b => b.ToString())], o.IsMasked, [.. o.Refresh()]);
    }
}
