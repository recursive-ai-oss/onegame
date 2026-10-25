using OneGame.Host;

namespace OneGame.Scene3D.Tests;

public class AnchorTests
{
    // Label background in the test game (0x203040).
    private static int LabelPixels(CapturedFrame frame)
    {
        var n = 0;
        for (var i = 0; i + 3 < frame.Rgba.Length; i += 4)
        {
            if (frame.Rgba[i] == 0x20 && frame.Rgba[i + 1] == 0x30 && frame.Rgba[i + 2] == 0x40)
            {
                n++;
            }
        }

        return n;
    }

    private static (int Left, int Right, int Bottom) LabelExtent(CapturedFrame frame)
    {
        int left = int.MaxValue, right = -1, bottom = -1;
        for (var i = 0; i + 3 < frame.Rgba.Length; i += 4)
        {
            if (frame.Rgba[i] == 0x20 && frame.Rgba[i + 1] == 0x30 && frame.Rgba[i + 2] == 0x40)
            {
                var pixel = i / 4;
                var x = pixel % frame.Width;
                left = Math.Min(left, x);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, (pixel / frame.Width) + 1);
            }
        }

        return (left, right, bottom);
    }

    private static (double X, double Y) Project(TestCluster cluster, double x, double y, double z)
    {
        var p = Fixture.Json(cluster.Client.Call("project", FormattableString.Invariant($"{{\"point\":[{x},{y},{z}]}}")));
        return (p.GetProperty("x").GetDouble(), p.GetProperty("y").GetDouble());
    }

    [Fact]
    public void ALabelTracksAMovingEntityAndHidesBehindTheCamera()
    {
        using var cluster = Fixture.Start();
        var game = cluster.Client.Game;
        var crate = (Scene3DGame.Crate)game.World.Find(new OneGame.Core.EntityId(3))!;

        var seen = new List<(double X, double Y)>();
        for (var round = 0; round < 4; round++)
        {
            cluster.Client.Step(15);
            var frame = cluster.Client.Capture("640x360");
            Assert.True(LabelPixels(frame) > 200, $"round {round}: the label is drawn");
            var tag = cluster.Client.Find("tag")!;
            Assert.True(tag.IsVisible);

            // The label's bottom centre sits on the point one unit above the box: check where its pixels are, which includes the pivot.
            var p = crate.Transform.Position;
            var want = Project(cluster, p.X.ToDouble(), p.Y.ToDouble() + 1, p.Z.ToDouble());
            var (left, right, bottom) = LabelExtent(frame);
            Assert.InRange((left + right) / 2.0, want.X - 2, want.X + 2);
            Assert.InRange(bottom, want.Y - 2, want.Y + 2);
            seen.Add(want);
        }

        Assert.True(Math.Abs(seen[0].X - seen[3].X) + Math.Abs(seen[0].Y - seen[3].Y) > 20, "the label moved with the box");

        // Turn the camera round: the point is behind it, so the label is hidden and none of its pixels are drawn.
        cluster.Client.Call("camera", """{"position":[0,3,8],"target":[0,3,20]}""");
        var behind = cluster.Client.Capture("640x360");
        Assert.False(cluster.Client.Find("tag")!.IsVisible);
        Assert.Equal(0, LabelPixels(behind));

        // Looking back at the scene shows it again; looking so far aside that the point leaves the screen hides it too.
        cluster.Client.Call("camera", """{"reset":true}""");
        Assert.True(LabelPixels(cluster.Client.Capture("640x360")) > 200);
        cluster.Client.Call("camera", """{"position":[0,3,8],"target":[40,3,8]}""");
        Assert.Equal(0, LabelPixels(cluster.Client.Capture("640x360")));
    }
}
