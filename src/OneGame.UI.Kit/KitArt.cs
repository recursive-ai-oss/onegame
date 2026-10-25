using System.Text;

namespace OneGame.UI.Kit;

/// <summary>One piece of vector art the kit uses: the asset id, the SVG source, and the nine-slice inset (0 for none) in the SVG's own pixels.</summary>
/// <param name="Id">The asset id the theme and components refer to.</param>
/// <param name="Svg">The SVG file's bytes.</param>
/// <param name="Slice">The same inset on all four sides for nine-slice stretching, or 0.</param>
public readonly record struct KitArtAsset(string Id, byte[] Svg, float Slice);

/// <summary>
/// The SVG art that ships with the kit: a frame for <see cref="UiKit.Frame"/>, sort arrows, a drop-down chevron, a check mark, and input glyphs for keyboard, mouse and gamepad.
/// It is plain shapes in a neutral style, drawn for this engine. Register all of it with the art atlas once: <c>foreach (var a in KitArt.Assets) atlas.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));</c>
/// </summary>
public static class KitArt
{
    /// <summary>The nine-slice frame behind <see cref="UiKit.Frame"/>.</summary>
    public const string Frame = "kit/frame";

    /// <summary>The unstretched border of <see cref="Frame"/> in UI units, to pass as <c>slice</c> to <see cref="UiKit.Frame"/>.</summary>
    public const float FrameSlice = 12;

    /// <summary>The ascending sort arrow.</summary>
    public const string SortAscending = "kit/icon/sort-asc";

    /// <summary>The descending sort arrow.</summary>
    public const string SortDescending = "kit/icon/sort-desc";

    /// <summary>The drop-down chevron.</summary>
    public const string ChevronDown = "kit/icon/chevron-down";

    /// <summary>A check mark.</summary>
    public const string Check = "kit/icon/check";

    /// <summary>The blank key cap behind keyboard glyphs (nine-slice, <see cref="KeySlice"/>).</summary>
    public const string KeyCap = "kit/glyph/key";

    /// <summary>The unstretched border of <see cref="KeyCap"/> in UI units.</summary>
    public const float KeySlice = 8;

    private static readonly string[] PadNames = ["a", "b", "x", "y", "lb", "rb", "lt", "rt", "ls", "rs", "dpad", "dpad-up", "dpad-down", "dpad-left", "dpad-right", "start", "select"];
    private static readonly string[] MouseNames = ["left", "right", "middle", "move"];
    private static readonly string[] PictureKeys = ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"];

    private static IReadOnlyList<KitArtAsset>? _assets;

    /// <summary>Every asset, ready to register.</summary>
    public static IReadOnlyList<KitArtAsset> Assets => _assets ??= Build();

    /// <summary>The asset id of the glyph for a control: <c>kit/glyph/pad/a</c>, <c>kit/glyph/mouse/left</c>, or the key cap (or an arrow key's picture) for the keyboard.</summary>
    public static string GlyphId(InputDevice device, string name) => device switch
    {
        InputDevice.Gamepad => "kit/glyph/pad/" + name,
        InputDevice.Mouse => "kit/glyph/mouse/" + name,
        _ => IsPictureKey(name) ? "kit/glyph/key-" + name[5..].ToLowerInvariant() : KeyCap,
    };

    internal static bool IsPictureKey(string name) => Array.IndexOf(PictureKeys, name) >= 0;

    private const string Ink = "#e6e8ee";
    private const string Body = "#2b2f38";
    private const string Rim = "#8a93a6";
    private const string Hot = "#e0a63a";

    private static string Svg(int w, int h, int vb, string body) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {vb} {vb * h / w}\">{body}</svg>";

    private static string Stroke(string d, string color = Ink, double width = 5) =>
        $"<path d=\"{d}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{width}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>";

    private static string Disc(string letter) =>
        $"<circle cx=\"32\" cy=\"32\" r=\"28\" fill=\"{Body}\" stroke=\"{Rim}\" stroke-width=\"3\"/>{letter}";

    private static string Pad(string name) => name switch
    {
        "a" => Disc(Stroke("M22 46 L32 18 L42 46 M26 37 H38")),
        "b" => Disc(Stroke("M24 18 H33 A7 7 0 0 1 33 32 H24 M33 32 A7 7 0 0 1 33 46 H24 V18")),
        "x" => Disc(Stroke("M23 19 L41 45 M41 19 L23 45")),
        "y" => Disc(Stroke("M23 19 L32 33 L41 19 M32 33 V46")),
        "lb" => Bumper(true),
        "rb" => Bumper(false),
        "lt" => Trigger(true),
        "rt" => Trigger(false),
        "ls" => Stick(Stroke("M26 22 V42 H38", Hot, 4)),
        "rs" => Stick(Stroke("M26 42 V22 H34 A6 6 0 0 1 34 33 H26 M34 33 L40 42", Hot, 4)),
        "dpad" => Cross(null),
        "dpad-up" => Cross("up"),
        "dpad-down" => Cross("down"),
        "dpad-left" => Cross("left"),
        "dpad-right" => Cross("right"),
        "start" => Disc(Stroke("M21 24 H43 M21 32 H43 M21 40 H43", Ink, 4)),
        _ => Disc($"<rect x=\"18\" y=\"22\" width=\"20\" height=\"14\" rx=\"2\" fill=\"none\" stroke=\"{Ink}\" stroke-width=\"4\"/><rect x=\"26\" y=\"28\" width=\"20\" height=\"14\" rx=\"2\" fill=\"{Body}\" stroke=\"{Ink}\" stroke-width=\"4\"/>"),
    };

    private static string Bumper(bool left) =>
        $"<rect x=\"6\" y=\"20\" width=\"52\" height=\"24\" rx=\"10\" fill=\"{Body}\" stroke=\"{Rim}\" stroke-width=\"3\"/>" +
        $"<rect x=\"{(left ? 10 : 34)}\" y=\"25\" width=\"20\" height=\"14\" rx=\"6\" fill=\"{Hot}\"/>";

    private static string Trigger(bool left) =>
        $"<path d=\"M12 54 V26 Q12 8 32 8 Q52 8 52 26 V54 Z\" fill=\"{Body}\" stroke=\"{Rim}\" stroke-width=\"3\"/>" +
        $"<rect x=\"{(left ? 16 : 34)}\" y=\"14\" width=\"14\" height=\"30\" rx=\"6\" fill=\"{Hot}\"/>";

    private static string Stick(string letter) =>
        $"<circle cx=\"32\" cy=\"32\" r=\"28\" fill=\"{Body}\" stroke=\"{Rim}\" stroke-width=\"3\"/><circle cx=\"32\" cy=\"32\" r=\"17\" fill=\"#3a4050\" stroke=\"{Rim}\" stroke-width=\"3\"/>{letter}";

    private static string Cross(string? hot)
    {
        var arm = (string key, string rect) => $"<rect {rect} rx=\"3\" fill=\"{(hot == key ? Hot : Body)}\" stroke=\"{Rim}\" stroke-width=\"3\"/>";
        return arm("up", "x=\"22\" y=\"6\" width=\"20\" height=\"24\"") + arm("down", "x=\"22\" y=\"34\" width=\"20\" height=\"24\"") +
               arm("left", "x=\"6\" y=\"22\" width=\"24\" height=\"20\"") + arm("right", "x=\"34\" y=\"22\" width=\"24\" height=\"20\"") +
               $"<rect x=\"22\" y=\"22\" width=\"20\" height=\"20\" fill=\"{Body}\"/>";
    }

    private static string Mouse(string name)
    {
        var left = name == "left" ? Hot : "none";
        var right = name == "right" ? Hot : "none";
        var middle = name == "middle" ? Hot : Body;
        var body = $"<rect x=\"16\" y=\"6\" width=\"32\" height=\"52\" rx=\"16\" fill=\"{Body}\" stroke=\"{Rim}\" stroke-width=\"3\"/>";
        var buttons = $"<path d=\"M17.5 28 V22 A14.5 14.5 0 0 1 31 7.5 V28 Z\" fill=\"{left}\"/><path d=\"M46.5 28 V22 A14.5 14.5 0 0 0 33 7.5 V28 Z\" fill=\"{right}\"/>";
        var lines = $"<path d=\"M17 28 H47 M32 7 V28\" stroke=\"{Rim}\" stroke-width=\"3\" fill=\"none\"/><rect x=\"28.5\" y=\"12\" width=\"7\" height=\"12\" rx=\"3.5\" fill=\"{middle}\" stroke=\"{Rim}\" stroke-width=\"2\"/>";
        return body + buttons + lines;
    }

    private static string Key(string? arrow)
    {
        var cap = $"<rect x=\"1.5\" y=\"1.5\" width=\"29\" height=\"29\" rx=\"6\" fill=\"{Body}\" stroke=\"{Rim}\" stroke-width=\"3\"/>";
        return arrow switch
        {
            "up" => cap + $"<path d=\"M16 8 L24 20 H8 Z\" fill=\"{Ink}\"/>",
            "down" => cap + $"<path d=\"M16 24 L24 12 H8 Z\" fill=\"{Ink}\"/>",
            "left" => cap + $"<path d=\"M8 16 L20 8 V24 Z\" fill=\"{Ink}\"/>",
            "right" => cap + $"<path d=\"M24 16 L12 8 V24 Z\" fill=\"{Ink}\"/>",
            _ => cap,
        };
    }

    private static List<KitArtAsset> Build()
    {
        var list = new List<KitArtAsset>();

        void Add(string id, string svg, float slice = 0) => list.Add(new KitArtAsset(id, Encoding.UTF8.GetBytes(svg), slice));

        Add(Frame, Svg(48, 48, 48, "<rect x=\"1.5\" y=\"1.5\" width=\"45\" height=\"45\" rx=\"10\" fill=\"#20242c\" stroke=\"#8a93a6\" stroke-width=\"3\"/><rect x=\"6\" y=\"6\" width=\"36\" height=\"36\" rx=\"6\" fill=\"none\" stroke=\"#3a4050\" stroke-width=\"2\"/>"), FrameSlice);
        Add(SortAscending, Svg(16, 16, 16, $"<path d=\"M8 3 L14 12 H2 Z\" fill=\"{Ink}\"/>"));
        Add(SortDescending, Svg(16, 16, 16, $"<path d=\"M8 13 L14 4 H2 Z\" fill=\"{Ink}\"/>"));
        Add(ChevronDown, Svg(16, 16, 16, Stroke("M3 6 L8 11 L13 6", Ink, 2)));
        Add(Check, Svg(16, 16, 16, Stroke("M3 8.5 L6.5 12 L13 4.5", Ink, 2.5)));
        Add(KeyCap, Svg(32, 32, 32, Key(null)), KeySlice);
        foreach (var dir in new[] { "up", "down", "left", "right" })
        {
            Add("kit/glyph/key-" + dir, Svg(32, 32, 32, Key(dir)));
        }

        foreach (var name in PadNames)
        {
            Add("kit/glyph/pad/" + name, Svg(32, 32, 64, Pad(name)));
        }

        foreach (var name in MouseNames)
        {
            Add("kit/glyph/mouse/" + name, Svg(32, 32, 64, Mouse(name)));
        }

        return list;
    }
}
