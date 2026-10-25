namespace OneGame.Steam.Input;

/// <summary>Maps Steam's action origins to the kit's gamepad glyph names. Steam extends the origin enum with every new controller, so the table covers one family
/// (the Xbox One layout, origins 114 to 147) and callers translate other pads to it first with <c>TranslateActionOrigin</c>.</summary>
public static class SteamOrigins
{
    /// <summary>The first origin number of the Xbox One family (its A button).</summary>
    public const int XboxOneFirst = 114;

    /// <summary>The kit glyph name for an Xbox One family origin.</summary>
    /// <param name="xboxOneOrigin">An origin number from 114 to 147.</param>
    /// <returns>A name accepted by <c>InputGlyphs.Bind</c> (<c>a</c>, <c>lb</c>, <c>dpad-up</c>...), or null when the origin is outside the family or has no kit picture (grips, share).</returns>
    public static string? KitGlyphName(int xboxOneOrigin) => xboxOneOrigin switch
    {
        114 => "a",
        115 => "b",
        116 => "x",
        117 => "y",
        118 => "lb",
        119 => "rb",
        120 => "start",
        121 => "select",
        122 or 123 => "lt",
        124 or 125 => "rt",
        >= 126 and <= 131 => "ls",
        >= 132 and <= 137 => "rs",
        138 => "dpad-up",
        139 => "dpad-down",
        140 => "dpad-left",
        141 => "dpad-right",
        142 => "dpad",
        _ => null,
    };
}
