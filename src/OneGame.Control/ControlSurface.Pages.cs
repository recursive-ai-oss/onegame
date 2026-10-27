using System.Globalization;
using System.Text;
using OneGame.Pages;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    // A page element is addressed by its name: id "start" or selector "#start" (no other selector syntax reaches pages).
    private static string? PageNameOf(Args a)
    {
        if (a.Str("id") is { Length: > 0 } id)
        {
            return id;
        }

        return a.Str("selector") is { Length: > 1 } sel && sel[0] == '#' && sel.IndexOfAny([' ', '.', ':', '>', '[']) < 0 ? sel[1..] : null;
    }

    // Pages are created at the window's size on first use, so a query before the first frame sees them.
    private PageScreen? PageScreenWith(string name)
    {
        Game.Ui.Update();
        Game.Pages.Prepare(Game.Window.Width, Game.Window.Height, Game.UiScale);
        return Game.Pages.ScreenWith(name);
    }

    private string? PageElementJson(string selector)
    {
        if (selector.Length < 2 || selector[0] != '#')
        {
            return null;
        }

        var name = selector[1..];
        if (PageScreenWith(name) is not { } screen)
        {
            return null;
        }

        var sb = new StringBuilder("{\"kind\":\"page\",\"id\":");
        AppendString(sb, name);
        sb.Append(",\"screen\":");
        AppendString(sb, screen.Name);
        sb.Append(",\"text\":");
        AppendString(sb, screen.TextOf(name) ?? "");
        sb.Append(",\"states\":[");
        var first = true;
        foreach (var s in screen.StatesOf(name).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            sb.Append(first ? "" : ",");
            AppendString(sb, s);
            first = false;
        }

        sb.Append(']');
        if (screen.BoundsOf(name) is { } b)
        {
            sb.Append(CultureInfo.InvariantCulture, $",\"bounds\":{{\"x\":{b.X:0.##},\"y\":{b.Y:0.##},\"width\":{b.W:0.##},\"height\":{b.H:0.##}}}");
        }

        return sb.Append('}').ToString();
    }

    private InputResult PageClick(PageScreen screen, string name)
    {
        Game.Pages.Prepare(Game.Window.Width, Game.Window.Height, Game.UiScale);
        if (screen.BoundsOf(name) is not { } b)
        {
            // A builder page has no layout queries: clicking by name is not possible, only drawing and lookup.
            throw new ControlException("not_found", $"'{name}' is on a builder page, which has no pointer path by name. Use an HTML page, or call the tree's own handlers.");
        }

        var x = b.X + (b.W / 2);
        var y = b.Y + (b.H / 2);
        if (!screen.Covers(x, y))
        {
            throw new ControlException("obscured", $"The page element '{name}' is not reachable at its centre ({x:0.#}, {y:0.#}); nothing of the page is under the pointer there.");
        }

        ClickAt(x, y, OneGame.Platform.MouseButton.Left);
        return Done();
    }

    private string PagesOp()
    {
        Game.Pages.Prepare(Game.Window.Width, Game.Window.Height, Game.UiScale);
        var sb = new StringBuilder("{\"screens\":[");
        var first = true;
        foreach (var s in Game.Pages.All)
        {
            sb.Append(first ? "" : ",").Append("{\"name\":");
            AppendString(sb, s.Name);
            var (w, h) = s.Viewport;
            sb.Append(CultureInfo.InvariantCulture, $",\"kind\":\"{(s.Live is not null ? "tree" : "html")}\",\"order\":{s.Order},\"visible\":{(s.Visible ? "true" : "false")},\"creations\":{s.Creations},\"viewport\":{{\"width\":{w:0.##},\"height\":{h:0.##}}},\"animating\":{((s.Interactive?.IsAnimating ?? false) ? "true" : "false")}}}");
            first = false;
        }

        var t = PageStatsFor(Game);
        sb.Append(CultureInfo.InvariantCulture, $"],\"renderer\":{{\"passes\":{t.Passes},\"instances\":{t.Instances},\"layers\":{t.Layers},\"dropped\":{t.Dropped},\"missingImages\":{t.MissingImages},\"refusedGlyphs\":{t.RefusedGlyphs},\"pathBitmaps\":{t.PathBitmaps},\"artPages\":{t.ArtPages},\"glyphPages\":{t.GlyphPages}}},\"clockMs\":{Game.Pages.NowMs:0.###}}}");
        return sb.ToString();
    }

    private static PageStats PageStatsFor(OneGame.Host.GameInstance game) =>
        game.Stage is OneGame.Host.Render2DStage stage ? stage.PageStats : default;

    private static void AppendString(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case < ' ': sb.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:x4}"); break;
                default: sb.Append(c); break;
            }
        }

        sb.Append('"');
    }
}
