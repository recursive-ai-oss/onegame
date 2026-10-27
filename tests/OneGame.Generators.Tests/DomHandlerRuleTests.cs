namespace OneGame.Generators.Tests;

/// <summary>DOM event handlers on a OneRender page run from input dispatch, so they are view code wherever they are registered.</summary>
public class DomHandlerRuleTests
{
    // The analyzer keys on the public type OneRender.Interaction.EventListener, so a stand-in with the same shape is enough.
    private const string Stub = "namespace OneRender.Interaction { public class DomEvent { } public delegate void EventListener(DomEvent e); "
        + "public class EventDispatcher { public void AddEventListener(object target, string type, EventListener listener) { } } }\n";

    private const string Usings = "using OneRender.Interaction;\n";

    private static Task<List<Microsoft.CodeAnalysis.Diagnostic>> Run(string members, string extra = "")
        => Harness.Diagnose(Usings + Harness.Entity(members, extra), Stub);

    [Fact]
    public async Task ALambdaInAViewMemberCannotAssignSyncedState()
        => Assert.Contains(await Run("[View] public void Wire(EventDispatcher ev) { ev.AddEventListener(this, \"click\", e => N = 3); }"), d => d.Id == "OG0008");

    [Fact]
    public async Task ALocalFunctionInAViewMemberCannotAssignSyncedState()
        => Assert.Contains(await Run("[View] public void Wire(EventDispatcher ev) { void On(DomEvent e) { N++; } ev.AddEventListener(this, \"click\", On); }"), d => d.Id == "OG0008");

    [Fact]
    public async Task AMethodGroupInAViewMemberCannotAssignSyncedState()
        => Assert.Contains(await Run("void Plain(DomEvent e) { N = 1; } [View] public void Wire(EventDispatcher ev) { ev.AddEventListener(this, \"click\", Plain); }"), d => d.Id == "OG0008");

    [Fact]
    public async Task AHandlerRegisteredFromAScreenBuilderCannotMutateSyncedCollections()
    {
        const string screen = "public class Screen { public void Build(EventDispatcher ev, T.E hero) { ev.AddEventListener(hero, \"click\", e => hero.L.Add(1)); } }";
        Assert.Contains(await Run("", screen), d => d.Id == "OG0008");
    }

    [Fact]
    public async Task AHandlerRegisteredInGameSetupCannotCallACommandHandler()
    {
        const string setup = "public static class Setup { public static void Run(EventDispatcher ev, T.E hero) { ev.AddEventListener(hero, \"click\", e => hero.Bump()); } }";
        Assert.Contains(await Run("", setup), d => d.Id == "OG0008");
    }

    [Fact]
    public async Task AMethodGroupRegisteredInSetupCannotWriteState()
    {
        const string setup = "public static class Setup { static void On(DomEvent e) { _hero!.N = 2; } static T.E? _hero; public static void Run(EventDispatcher ev) { ev.AddEventListener(null!, \"click\", On); } }";
        Assert.Contains(await Run("", setup), d => d.Id == "OG0008");
    }

    [Fact]
    public async Task ALambdaStoredInAVariableFirstIsStillAHandler()
        => Assert.Contains(await Run("public void Wire(EventDispatcher ev) { EventListener l = e => { N = 1; }; ev.AddEventListener(this, \"click\", l); }"), d => d.Id == "OG0008");

    [Fact]
    public async Task AHandlerThatSendsACommandOrTouchesViewOnlyStateIsClean()
    {
        const string setup = "public static class Setup { public static int Clicks; public static void Run(EventDispatcher ev, T.E hero) { ev.AddEventListener(hero, \"click\", e => { Clicks++; hero.Send.Bump(); }); } }";
        var diagnostics = await Run("private int _hover; [View] public void Wire(EventDispatcher ev) { ev.AddEventListener(this, \"x\", e => _hover = 1); }", setup);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnUnrelatedLambdaOutsideViewCodeIsNotAHandler()
    {
        const string setup = "public static class Setup { public static void Run(T.E hero) { Action a = () => hero.N = 1; a(); } }";
        Assert.Empty(await Run("", setup));
    }
}
