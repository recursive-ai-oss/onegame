namespace OneGame.UI.Kit.Tests;

/// <summary>A UI tree with the kit installed, a fixed viewport and helpers that drive it the way the harness does: through input events and frames.</summary>
internal sealed class Screen
{
    public Screen(float width = 1280, float height = 720, float scale = 1, ITextMeasurer? measurer = null)
    {
        Ui = new UiTree(measurer);
        Ui.SetViewport(width, height, scale);
        Host = new KitHost(Ui);
    }

    public UiTree Ui { get; }

    public KitHost Host { get; }

    public Element Root => Ui.Root;

    /// <summary>Runs frames until bindings, layout and the components' own follow-up work have settled.</summary>
    public void Settle(int frames = 4)
    {
        for (var i = 0; i < frames; i++)
        {
            Host.Advance(0);
            Ui.Frame();
        }
    }

    public Element Get(string id) => Ui.Find(id) ?? throw new InvalidOperationException($"No element with id '{id}'. Ids: " + string.Join(", ", Ui.Query("*").Where(e => e.Id is not null).Select(e => e.Id).Take(40)));

    public void Click(Element e)
    {
        var r = e.AbsoluteBounds;
        var x = r.X + (r.Width / 2);
        var y = r.Y + (r.Height / 2);
        Host.Dispatch(UiEvent.PointerMove(x, y));
        Host.Dispatch(UiEvent.PointerDown(x, y));
        Host.Dispatch(UiEvent.PointerUp(x, y));
        Settle();
    }

    public void Key(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Host.Dispatch(UiEvent.KeyDown(key, modifiers));
        Host.Dispatch(UiEvent.KeyUp(key, modifiers));
        Settle();
    }

    public void Type(string text)
    {
        Host.Dispatch(UiEvent.TextInput(text));
        Settle();
    }

    public void Nav(NavDirection d)
    {
        Host.Dispatch(UiEvent.Navigate(d));
        Settle();
    }

    public void Activate()
    {
        Host.Dispatch(UiEvent.Activate());
        Settle();
    }

    public void Cancel()
    {
        Host.Dispatch(UiEvent.Cancel());
        Settle();
    }

    public void Focus(Element e)
    {
        Assert.True(Ui.Focus(e), $"{e} cannot take focus");
        Settle();
    }

    /// <summary>Bytes allocated by the calling thread while running frames on an unchanged screen.</summary>
    public long AllocatedOverFrames(int frames)
    {
        Settle();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < frames; i++)
        {
            Host.Advance(0.016f);
            Ui.Frame();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
