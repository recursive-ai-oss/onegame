using System.Runtime.CompilerServices;
using OneGame.Core;
using OneGame.Host;
using OneGame.Pages;
using OneGame.UI;
using OneGame.UI.Kit;
using OneRender.Interaction;
using RecursiveAI.OneHtml;

namespace Solitaire.Views;

/// <summary>
/// The settings screen as a OneRender page: HTML and CSS, coloured from the same tokens as the kit screens. The controls are the page's native
/// radio buttons, checkbox and sliders. Their handlers are view code: they change the player's profile and save it, and never touch the table.
/// </summary>
[View]
internal static class SettingsPage
{
    private const string Name = "settings";

    private static readonly ConditionalWeakTable<GameInstance, PageFonts> Fonts = new();

    /// <summary>Shows the page. A page made earlier is replaced, so the controls start from the current profile.</summary>
    public static void Show(GameInstance game)
    {
        var s = Session.For(game);
        var fonts = Fonts.GetValue(game, static g =>
        {
            var f = new PageFonts(g.Fonts);
            f.Add(Assets.Read("fonts/NotoSans-Subset-Regular.ttf"), "Body");
            f.Add(Assets.Read("fonts/NotoSans-Subset-Bold-kern.ttf"), "Body");
            return f;
        });
        game.Pages.Show(Name, PageScreen.FromHtml(Html(s.Data.Description), new PageOptions(fonts) { Setup = page => Wire(s, page.Interactive!) }));
    }

    /// <summary>Stops showing the page; the other screens call this when they open.</summary>
    public static void Hide(GameInstance game) => game.Pages.Remove(Name);

    private static void Wire(Session s, InteractivePage page)
    {
        var p = s.Profile;
        Node Find(string id) => page.FindElement(id);

        // The controls show the profile as it is now. This runs again when the window is resized, which makes the page anew.
        page.SetChecked(Find(p.DrawMode == 3 ? "draw-3" : "draw-1"), true);
        page.SetChecked(Find("auto-finish"), p.AutoFinish);
        page.SetRangeValue(Find("music-volume"), p.MusicVolume);
        page.SetRangeValue(Find("effects-volume"), p.EffectsVolume);
        page.SetChecked(Find(p.Glyphs switch { GlyphStyle.Keyboard => "glyphs-keyboard", GlyphStyle.Gamepad => "glyphs-gamepad", _ => "glyphs-auto" }), true);

        void On(string id, string type, Action handler) =>
            page.Events.AddEventListener(EventTarget.Of(Find(id)), type, _ => handler());

        On("draw-1", "change", () => Choose("draw-1", () => p.DrawMode = 1));
        On("draw-3", "change", () => Choose("draw-3", () => p.DrawMode = 3));
        On("glyphs-auto", "change", () => Choose("glyphs-auto", () => p.Glyphs = GlyphStyle.Auto));
        On("glyphs-keyboard", "change", () => Choose("glyphs-keyboard", () => p.Glyphs = GlyphStyle.Keyboard));
        On("glyphs-gamepad", "change", () => Choose("glyphs-gamepad", () => p.Glyphs = GlyphStyle.Gamepad));
        On("auto-finish", "change", () =>
        {
            p.AutoFinish = page.IsChecked(Find("auto-finish"));
            s.SaveProfile();
        });
        On("music-volume", "input", () =>
        {
            p.MusicVolume = (int)Math.Round(page.GetRangeValue(Find("music-volume")));
            s.ApplyVolumes();
            s.SaveProfile();
        });
        On("effects-volume", "input", () =>
        {
            p.EffectsVolume = (int)Math.Round(page.GetRangeValue(Find("effects-volume")));
            s.ApplyVolumes();
            s.SaveProfile();
        });
        On("back", "click", s.Back);

        // Escape closes the screen. With a control focused the page takes the key; with none it reaches the UI tree's cancel handler.
        page.Events.AddEventListener(EventTarget.Of(page.FindElement("settings-root")), "keydown", e =>
        {
            if (e is KeyboardEvent { Key: "Escape" })
            {
                s.Back();
            }
        });

        // The first control has focus, as the kit screen's default focus did, so the keyboard and a controller work at once.
        page.Focus(Find(p.DrawMode == 3 ? "draw-3" : "draw-1"), FocusReason.Keyboard);

        void Choose(string id, Action set)
        {
            if (page.IsChecked(Find(id)))
            {
                set();
                s.SaveProfile();
            }
        }
    }

    private static string Hex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}";

    private static string Html(string location)
    {
        var t = KitTokens.Dark;
        // Placeholders keep the stylesheet readable: braces in CSS would need doubling in an interpolated string.
        var css = """
            html{background:transparent}
            body{margin:0;font:16px Body;color:@text}
            #settings-root{position:absolute;left:0;top:0;right:0;bottom:0;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px}
            h1{margin:0;font-size:34px;font-weight:700;color:@title}
            .card{box-sizing:border-box;width:620px;padding:12px;background:@panel;border:1px solid @border;border-radius:3px;display:flex;flex-direction:column;gap:14px}
            .row{display:flex;align-items:center;gap:14px;min-height:30px}
            .row>.label{width:170px}
            .row>.group{display:flex;align-items:center;gap:16px;flex:1}
            label.opt{display:flex;align-items:center;gap:6px}
            input{margin:0;accent-color:@accent}
            input[type=radio],input[type=checkbox]{width:18px;height:18px}
            input[type=range]{flex:1;height:20px}
            .end{color:@dim;font-size:13px}
            input:focus{outline:2px solid @focus;outline-offset:2px}
            #save-location{color:@dim}
            button{font:16px Body;padding:0 16px;height:34px;border:1px solid transparent;border-radius:3px;background:@accent;color:@accentText}
            button:hover{background:@accentHover}
            button:focus{outline:2px solid @focus;outline-offset:2px}
            """
            .Replace("@text", Hex(Theme.Text))
            .Replace("@title", Hex(Theme.Text))
            .Replace("@panel", Hex(t.Panel))
            .Replace("@border", Hex(t.Border))
            .Replace("@accentText", Hex(t.AccentText))
            .Replace("@accentHover", Hex(t.AccentHover))
            .Replace("@accent", Hex(t.Accent))
            .Replace("@dim", Hex(t.TextDim))
            .Replace("@focus", Hex(t.Focus));

        return $"""
            <style>{css}</style>
            <div id="settings-root">
              <h1>Settings</h1>
              <div class="card">
                <div class="row"><span class="label">New games deal</span>
                  <div class="group">
                    <label class="opt"><input type="radio" name="draw" id="draw-1">Draw 1</label>
                    <label class="opt"><input type="radio" name="draw" id="draw-3">Draw 3</label>
                  </div></div>
                <div class="row"><label class="opt"><input type="checkbox" id="auto-finish">Offer to finish the game when every card is face up</label></div>
                <div class="row"><span class="label">Music volume</span><span class="end">0</span>
                  <input type="range" id="music-volume" min="0" max="10" step="1"><span class="end">10</span></div>
                <div class="row"><span class="label">Effects volume</span><span class="end">0</span>
                  <input type="range" id="effects-volume" min="0" max="10" step="1"><span class="end">10</span></div>
                <div class="row"><span class="label">Input hints</span>
                  <div class="group">
                    <label class="opt"><input type="radio" name="glyphs" id="glyphs-auto">Automatic</label>
                    <label class="opt"><input type="radio" name="glyphs" id="glyphs-keyboard">Keyboard</label>
                    <label class="opt"><input type="radio" name="glyphs" id="glyphs-gamepad">Gamepad</label>
                  </div></div>
                <div id="save-location">Saved in: {System.Net.WebUtility.HtmlEncode(location)}</div>
              </div>
              <button id="back">Back</button>
            </div>
            """;
    }
}
