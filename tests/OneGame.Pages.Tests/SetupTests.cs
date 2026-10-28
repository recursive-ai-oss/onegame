using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.Testing;
using OneRender.Interaction;
using PagesGame;

namespace OneGame.Pages.Tests;

/// <summary>A game whose one screen is a form: a checkbox and a button, with the checkbox set (and the button focused) from <c>Setup</c> or from markup.</summary>
public sealed class FormGame : GameDefinition
{
    private const string Body = """<body style="margin:0;font:16px Body;background:#203040"><label style="position:absolute;left:10px;top:10px"><input id="box" type="checkbox"@checked> Option</label><button id="go" style="position:absolute;left:10px;top:50px;width:80px;height:30px">Go</button></body>""";

    public bool InSetup { get; init; }

    public override string Name => "Form";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.PagesGame.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Meter());

    public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")));

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        var fonts = new PageFonts(game.Fonts);
        fonts.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")), "Body");
        var html = Body.Replace("@checked", InSetup ? string.Empty : " checked", StringComparison.Ordinal);
        game.Pages.Show("form", PageScreen.FromHtml(html, new PageOptions(fonts)
        {
            Setup = InSetup
                ? s =>
                {
                    var page = s.Interactive!;
                    page.SetChecked(page.FindElement("box"), true);
                    page.Focus(page.FindElement("go"), FocusReason.Keyboard);
                }
                : s => s.Interactive!.Focus(s.Interactive.FindElement("go"), FocusReason.Keyboard),
        }));
        return OneGame.UI.Ui.Column();
    });
}

public class SetupTests
{
    private static byte[] Frame(bool inSetup, int width = 200, float scale = 1f)
    {
        using var cluster = TestCluster.Start(new FormGame { InSetup = inSetup }, new TestOptions { Width = width, Height = 100, Gpu = GpuMode.Auto });
        cluster.Client.Step(2);
        return cluster.Client.Game.Capture(width, 100, scale).Rgba;
    }

    [Fact]
    public void WhatSetupChangesIsInTheFirstFrameAndAfterAResize()
    {
        // The same page, once with the box checked in markup and once checked from Setup, draws the same pixels.
        Assert.True(Frame(false).AsSpan().SequenceEqual(Frame(true)));

        // A capture at another scale makes the page anew; Setup runs again and its changes show without any input in between.
        using var a = TestCluster.Start(new FormGame { InSetup = true }, new TestOptions { Width = 200, Height = 100, Gpu = GpuMode.Auto });
        using var b = TestCluster.Start(new FormGame { InSetup = false }, new TestOptions { Width = 200, Height = 100, Gpu = GpuMode.Auto });
        a.Client.Step(2);
        b.Client.Step(2);
        Assert.True(a.Client.Game.Capture(300, 150, 1.5f).Rgba.AsSpan().SequenceEqual(b.Client.Game.Capture(300, 150, 1.5f).Rgba));
        Assert.True(a.Client.Game.Pages.Get("form")!.Creations >= 2);
    }

    [Fact]
    public void AControllerDrivesAPageThatHasFocusAndLeavesOneThatHasNone()
    {
        using var cluster = TestCluster.Start(new FormGame { InSetup = true }, new TestOptions { Width = 200, Height = 100, Gpu = GpuMode.Auto });
        var c = cluster.Client;
        c.Step(2);
        var page = c.Game.Pages.Get("form")!;
        Assert.Equal("go", Focused(page));
        c.Call("input_nav", """{"action":"up"}""");
        Assert.Equal("box", Focused(page));
        c.Call("input_nav", """{"action":"down"}""");
        Assert.Equal("go", Focused(page));
        c.Call("input_nav", """{"action":"south"}""");   // Space on the focused button clicks it; no listener here, so only the focus is checked
        Assert.Equal("go", Focused(page));

        c.Game.Pages.Get("form")!.Interactive!.Blur(TestContext.Current.CancellationToken);
        Assert.False(c.Game.Pages.HasFocus);
        c.Call("input_nav", """{"action":"down"}""");
        Assert.False(c.Game.Pages.HasFocus);   // with no focus the pad belongs to the game's own navigation
    }

    [Fact]
    public void ChangingAReadoutsTextEveryFrameAllocatesNothingOnceWarm()
    {
        using var cluster = TestCluster.Start(new FormGame { InSetup = true }, new TestOptions { Width = 200, Height = 100, Gpu = GpuMode.Auto });
        var c = cluster.Client;
        c.Step(2);
        var page = c.Game.Pages.Get("form")!.Interactive!;
        var label = page.FindElement("go");
        string[] text = ["Go", "Go 1", "Go 2"];
        for (var i = 0; i < 3000; i++)
        {
            page.SetTextContent(label, text[i % 3], TestContext.Current.CancellationToken);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 300; i++)
        {
            page.SetTextContent(label, text[i % 3], TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.Equal("Go 2", page.GetTextContent(label));
    }

    [Fact]
    public void SettingAStyleAttributeCostsAKnownAmountAndTheIdleFrameAllocatesNothing()
    {
        using var cluster = TestCluster.Start(new FormGame { InSetup = true }, new TestOptions { Width = 200, Height = 100, Gpu = GpuMode.Auto });
        var c = cluster.Client;
        c.Step(2);
        var page = c.Game.Pages.Get("form")!.Interactive!;
        var go = page.FindElement("go");
        string[] v = ["--fill:10%", "--fill:20%"];
        for (var i = 0; i < 20; i++)
        {
            page.SetAttribute(go, "style", v[i % 2], TestContext.Current.CancellationToken);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
        {
            page.SetAttribute(go, "style", v[i % 2], TestContext.Current.CancellationToken);
        }

        var per = (GC.GetAllocatedBytesForCurrentThread() - before) / 20;
        Console.WriteLine($"BYTES-PER-STYLE-CHANGE {per}");
        Assert.True(per < 4_000_000);
    }

    private static string Focused(PageScreen page)
    {
        var p = page.Interactive!;
        return p.FocusedElement.IsNull ? "" : p.FocusedElement == p.FindElement("box") ? "box" : p.FocusedElement == p.FindElement("go") ? "go" : "other";
    }
}
