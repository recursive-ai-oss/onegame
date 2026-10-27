// Generated from docs/pages.md by DocSnippetTests. Do not edit.
#pragma warning disable
namespace PageDocs.Block1
{
    using OneGame.Host;
    using OneGame.Pages;
    using OneGame.UI;
    using OneRender.Interaction;
    using PagesGame;   // Meter: the game's own entity, with an Add command

    public static class MenuScreen
    {
        private const string Html = """
            <body style="margin:0;font:16px Body;color:#e8ecf4">
              <h1 style="margin:10px">Menu</h1>
              <button id="start" style="width:120px;height:36px">Start</button>
            </body>
            """;

        public static Element Build(GameInstance game)
        {
            var fonts = new PageFonts(game.Fonts);                       // the same faces OneGame.Text draws with
            fonts.Add(File.ReadAllBytes("fonts/Body.ttf"), "Body");      // the page says: font: 16px Body

            game.Pages.Show("menu", PageScreen.FromHtml(Html, new PageOptions(fonts)
            {
                // Runs every time the page is made, which includes after a window resize or a UI scale change.
                Setup = s => s.Interactive!.Events.AddEventListener(
                    EventTarget.Of(s.Interactive.FindElement("start")), "click", _ => Meter.Of(game.World).Send.Add(1)),
            }));
            return Ui.Column();   // the screen's OneGame.UI side: empty, or the felt behind the page
        }
    }
}

namespace PageDocs.Block2
{
    using OneGame.Pages;
    using OneRender.Interaction;

    public static class Handlers
    {
        public static void Wire(InteractivePage page, Action<int> setMusic, Action back)
        {
            page.Events.AddEventListener(EventTarget.Of(page.FindElement("music")), "input", _ => setMusic((int)page.GetRangeValue(page.FindElement("music"))));
            page.Events.AddEventListener(EventTarget.Of(page.FindElement("back")), "click", _ => back());
        }
    }
}

namespace PageDocs.Block3
{
    using OneGame.Host;
    using OneGame.Pages;
    using OneRender.Style;
    using OneRender.Tree;

    public static class HudScreen
    {
        public static void Show(GameInstance game, PageFonts fonts)
        {
            var tree = new StyledTree();
            var root = tree.AddRoot("div", new StyleDeclaration().Set("width", "320px").Set("height", "40px").Set("font", "18px Body").Set("color", "#ffd040"));
            var gold = tree.AddElement(root, "span");
            tree.AddText(gold, "Gold 0");
            tree.SetName(gold, "gold");
            game.Pages.Show("hud", PageScreen.FromTree(tree, new PageOptions(fonts)));
        }

        public static void Update(PageSet pages, int gold)
        {
            var tree = pages.Get("hud")!.Live!.Styled;
            tree.SetText(tree.FindByName("gold"), $"Gold {gold}");
        }
    }
}

namespace PageDocs.Block4
{
    using OneGame.Host;
    using OneGame.Pages;

    public static class Art
    {
        public static PageOptions Options(GameInstance game, PageFonts fonts)
        {
            var images = new PageImages(game.Art);
            images.Add("coin.png", File.ReadAllBytes("art/coin.png"));
            images.Add("heart.svg", File.ReadAllBytes("art/heart.svg"));
            return new PageOptions(fonts) { Images = images };   // <img src="coin.png"> and background-image: url(heart.svg)
        }
    }
}

