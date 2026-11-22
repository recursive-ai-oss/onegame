namespace OneGame.Platform.Tests;

public class PointerEdgeTests
{
    private static PlatformApp NewApp(bool emulate = false) =>
        PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false, TouchEmulatesPointer = emulate });

    [Fact]
    public void APressKeepsItsOwnPositionWhenThePointerMovesInTheSameFrame()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.PointerMove(w, 100.25f, 50.5f);
        app.Injector.PointerButton(w, MouseButton.Left, true);
        app.Injector.PointerMove(w, 160.75f, 50.5f);

        // The polled state is where the pointer ended up; the edge is where the press happened, fraction included.
        Assert.Equal(160.75f, app.Input.PointerX);
        var down = Assert.Single(app.Input.PointerEdges.ToArray(), e => e.Kind == PointerEdgeKind.Down);
        Assert.Equal((100.25f, 50.5f, MouseButton.Left, -1), (down.X, down.Y, down.Button, down.TouchId));
        Assert.Equal([PointerEdgeKind.Move, PointerEdgeKind.Down, PointerEdgeKind.Move], app.Input.PointerEdges.ToArray().Select(e => e.Kind));
    }

    [Fact]
    public void ALiftKeepsItsPositionWhenTheFingerMovedInTheSameFrame()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 2, 10, 10);
        app.Input.EndFrame();

        app.Injector.TouchMove(w, 2, 40, 90);
        app.Injector.TouchUp(w, 2, 44.5f, 91.25f);

        Assert.False(app.Input.TryGetTouch(2, out _, out _));
        var edges = app.Input.PointerEdges.ToArray();
        Assert.Equal([PointerEdgeKind.Move, PointerEdgeKind.Up], edges.Select(e => e.Kind));
        Assert.Equal((2, 44.5f, 91.25f), (edges[1].TouchId, edges[1].X, edges[1].Y));
    }

    [Fact]
    public void ATapThatBeganAndEndedInOneFrameIsStillListed()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 0, 300.5f, 200.5f);
        app.Injector.TouchUp(w, 0, 300.5f, 200.5f);

        Assert.Equal(0, app.Input.TouchCount);
        Assert.Equal([PointerEdgeKind.Down, PointerEdgeKind.Up], app.Input.PointerEdges.ToArray().Select(e => e.Kind));
    }

    [Fact]
    public void EndFrameClearsTheList()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.PointerMove(w, 1, 2);
        Assert.Equal(1, app.Input.PointerEdges.Length);
        app.Input.EndFrame();
        Assert.Equal(0, app.Input.PointerEdges.Length);
    }

    [Fact]
    public void MouseEdgesMadeFromTheFirstFingerAreMarkedAndTheStateSaysSo()
    {
        using var app = NewApp(emulate: true);
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 5, 20, 30);
        Assert.True(app.Input.PointerEmulatedFromTouch);
        var edges = app.Input.PointerEdges.ToArray();
        Assert.Equal(5, edges[0].TouchId);
        Assert.False(edges[0].FromTouch);
        Assert.All(edges.Skip(1), e => Assert.True(e.FromTouch && e.TouchId == -1));

        app.Injector.TouchUp(w, 5, 20, 30);
        app.Input.EndFrame();
        app.Injector.PointerMove(w, 400, 400);
        Assert.False(app.Input.PointerEmulatedFromTouch);
        Assert.False(app.Input.PointerEdges[0].FromTouch);
    }

    [Fact]
    public void AFrameThatNeverEndsKeepsTheListBoundedAndKeepsPressesAndLiftsOverMoves()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        for (var i = 0; i < 5000; i++)
        {
            app.Injector.PointerMove(w, i, i);
        }

        Assert.True(app.Input.PointerEdges.Length <= 1024);
        Assert.True(app.Input.DroppedPointerEdges > 0);
        app.Injector.PointerButton(w, MouseButton.Left, true);
        Assert.Equal(PointerEdgeKind.Down, app.Input.PointerEdges[^1].Kind);
    }
}
