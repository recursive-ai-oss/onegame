namespace OneGame.Generators.Tests;

/// <summary>Tweens are view-only: a callback written inside view code is view code, so it cannot assign synced state; sim code cannot use the tween API.</summary>
public class TweenRuleTests
{
    private const string Usings = "using OneGame.Tweens;\n";

    [Fact]
    public async Task ATweenCallbackInViewCodeCannotAssignSyncedState()
    {
        var diagnostics = await Harness.Diagnose(Usings + Harness.Entity("[View] public void Pop(TweenSet tweens) { tweens.Number(0, 1, 1, v => N = (int)v); }"));
        Assert.Contains(diagnostics, d => d.Id == "OG0008");
    }

    [Fact]
    public async Task ACompletionCallbackInViewCodeCannotSendThroughAHandler()
    {
        var diagnostics = await Harness.Diagnose(Usings + Harness.Entity("[View] public void Pop(TweenSet tweens) { tweens.Number(0, 1, 1, v => { }).OnComplete(() => L.Add(1)); }"));
        Assert.Contains(diagnostics, d => d.Id == "OG0008");
    }

    [Fact]
    public async Task ATweenCallbackInViewCodeMayTouchViewOnlyState()
    {
        var diagnostics = await Harness.Diagnose(Usings + Harness.Entity("private float _glow; [View] public void Pop(TweenSet tweens) { tweens.Number(0, 1, 1, v => _glow = v).OnComplete(() => _glow = 0); }"));
        Assert.DoesNotContain(diagnostics, d => d.Id == "OG0008");
    }

    [Fact]
    public async Task SimCodeThatStartsATweenIsFlagged()
    {
        var all = await Harness.DiagnoseAll(Usings + Harness.Entity("protected override void OnTick() { var s = new TweenSet(); s.Advance(1); }"));
        Assert.Contains(all, d => d.Id == "OG0015");
    }
}
