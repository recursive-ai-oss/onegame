using System.Numerics;
using OneGame.Tweens;

namespace OneGame.UI.Tests;

public class TweenEngineTests
{
    private static List<float> Record(out Action<float> sink)
    {
        var list = new List<float>();
        sink = list.Add;
        return list;
    }

    [Fact]
    public void ANumberTweenMovesFromStartToEndAndEndsExactly()
    {
        var set = new TweenSet();
        float v = -1;
        var done = 0;
        var t = set.Number(0, 10, 1, x => v = x).OnComplete(() => done++);
        Assert.True(t.IsActive);
        set.Advance(0);
        Assert.Equal(0f, v);
        set.Advance(0.25);
        Assert.Equal(2.5f, v, 1e-5f);
        Assert.Equal(0.25, t.Progress, 1e-9);
        set.Advance(0.25);
        Assert.Equal(5f, v, 1e-5f);
        Assert.Equal(0, done);
        set.Advance(0.5);
        Assert.Equal(10f, v);
        Assert.Equal(1, done);
        Assert.False(t.IsActive);
        Assert.Equal(0, set.ActiveCount);
        set.Advance(1);
        Assert.Equal(1, done);
    }

    [Fact]
    public void AZeroLengthTweenJumpsToItsEndOnTheFirstAdvance()
    {
        var set = new TweenSet();
        float v = 0;
        set.Number(3, 7, 0, x => v = x);
        set.Advance(0.001);
        Assert.Equal(7f, v);
        Assert.Equal(0, set.ActiveCount);
    }

    [Fact]
    public void DelayHoldsEveryValueUntilItsOver()
    {
        var set = new TweenSet();
        float v = -99;
        set.Number(0, 1, 1, x => v = x).Delay(0.5f);
        set.Advance(0.4);
        Assert.Equal(-99f, v);
        set.Advance(0.6);
        Assert.Equal(0.5f, v, 1e-5f);
        set.Advance(0.5);
        Assert.Equal(1f, v);
    }

    [Fact]
    public void EasingShapesTheValue()
    {
        var set = new TweenSet();
        float v = 0;
        set.Number(0, 1, 1, x => v = x).Ease(Easing.OutQuad);
        set.Advance(0.5);
        Assert.Equal(0.75f, v, 1e-5f);
        var set2 = new TweenSet();
        set2.Number(0, 1, 1, x => v = x).Ease("steps(4)");
        set2.Advance(0.6);
        Assert.Equal(0.5f, v);
    }

    [Fact]
    public void VectorsAndColorsBlendComponentWise()
    {
        var set = new TweenSet();
        Vector2 a = default;
        Vector3 b = default;
        Vector4 c = default;
        Color col = default;
        Color fade = default;
        set.Vector2(new Vector2(0, 10), new Vector2(10, 0), 1, v => a = v);
        set.Vector3(new Vector3(1, 2, 3), new Vector3(3, 2, 1), 1, v => b = v);
        set.Vector4(Vector4.Zero, new Vector4(4, 8, 12, 16), 1, v => c = v);
        set.Color(Color.White, Color.Black, 1, v => col = v);
        set.Color(new Color(255, 0, 0), new Color(255, 0, 0, 0), 1, v => fade = v);
        set.Advance(0.5);
        Assert.Equal(new Vector2(5, 5), a);
        Assert.Equal(new Vector3(2, 2, 2), b);
        Assert.Equal(new Vector4(2, 4, 6, 8), c);
        Assert.Equal(new Color(128, 128, 128), col);
        // CSS blends premultiplied: half-transparent red is still pure red, not pink or brown.
        Assert.Equal(new Color(255, 0, 0, 128), fade);
    }

    [Fact]
    public void RepeatAndYoyoPlayTheRightNumberOfTimes()
    {
        var set = new TweenSet();
        var seen = Record(out var sink);
        var done = 0;
        set.Number(0, 1, 1, sink).Repeat(2, yoyo: true).OnComplete(() => done++);
        foreach (var dt in new[] { 0.5, 0.5, 0.5, 0.5 })
        {
            set.Advance(dt);
        }

        // Up to 1 at t=1, back down to 0 at t=2, and over.
        Assert.Equal([0.5f, 1f, 0.5f, 0f], seen);
        Assert.Equal(1, done);
        Assert.Equal(0, set.ActiveCount);

        var plain = new TweenSet();
        var seen2 = Record(out var sink2);
        plain.Number(0, 1, 1, sink2).Repeat(2);
        foreach (var dt in new[] { 0.5, 0.5, 0.5, 0.5 })
        {
            plain.Advance(dt);
        }

        // The step that crosses t = 1 shows the end of the first play, then the start of the second.
        Assert.Equal([0.5f, 1f, 0f, 0.5f, 1f], seen2);
    }

    [Fact]
    public void ForeverRunsUntilCancelledAndNeverCompletes()
    {
        var set = new TweenSet();
        var done = 0;
        float v = 0;
        var t = set.Number(0, 1, 1, x => v = x).RepeatForever(yoyo: true).OnComplete(() => done++);
        for (var i = 0; i < 1000; i++)
        {
            set.Advance(0.1);
        }

        Assert.True(t.IsActive);
        Assert.Equal(-1, ListOne(set).Duration);
        Assert.Equal(double.PositiveInfinity, t.TotalDuration);
        t.Cancel();
        Assert.False(t.IsActive);
        Assert.Equal(0, done);
        Assert.Equal(0, set.ActiveCount);
        Assert.InRange(v, 0f, 1f);
    }

    private static TweenInfo ListOne(TweenSet set)
    {
        var l = new List<TweenInfo>();
        set.Snapshot(l);
        return Assert.Single(l);
    }

    [Fact]
    public void ASequenceRunsChildrenInOrderAndTouchesNothingEarly()
    {
        var set = new TweenSet();
        float a = -99, b = -99;
        var order = new List<string>();
        var done = 0;
        var seq = set.Sequence(
            set.Number(0, 1, 1, x => a = x),
            set.Call(() => order.Add("mid")),
            set.Delay(0.5f),
            set.Number(0, 2, 1, x => b = x).OnComplete(() => order.Add("b")))
            .OnComplete(() => { order.Add("seq"); done++; });
        Assert.Equal(2.5, seq.TotalDuration, 1e-9);
        set.Advance(0.5);
        Assert.Equal(0.5f, a, 1e-5f);
        Assert.Equal(-99f, b);
        set.Advance(0.75); // t = 1.25: A is done, the call ran, the delay is running
        Assert.Equal(1f, a);
        Assert.Equal(-99f, b);
        Assert.Equal(["mid"], order);
        set.Advance(0.75); // t = 2.0: B is halfway
        Assert.Equal(1f, b, 1e-5f);
        set.Advance(0.5);
        Assert.Equal(2f, b);
        Assert.Equal(["mid", "b", "seq"], order);
        Assert.Equal(1, done);
        Assert.Equal(0, set.ActiveCount);
    }

    [Fact]
    public void AParallelGroupEndsWithItsLongestChild()
    {
        var set = new TweenSet();
        float a = 0, b = 0;
        var done = 0;
        var p = set.Parallel(set.Number(0, 1, 1, x => a = x), set.Number(0, 1, 2, x => b = x)).OnComplete(() => done++);
        Assert.Equal(2, p.TotalDuration, 1e-9);
        set.Advance(1);
        Assert.Equal(1f, a);
        Assert.Equal(0.5f, b, 1e-5f);
        Assert.Equal(0, done);
        set.Advance(1);
        Assert.Equal(1f, b);
        Assert.Equal(1, done);
    }

    [Fact]
    public void GroupsNestAndRepeat()
    {
        var set = new TweenSet();
        var log = new List<string>();
        var inner = set.Sequence(set.Call(() => log.Add("start")), set.Delay(1));
        var outer = set.Sequence(inner, set.Call(() => log.Add("end"))).Repeat(2);
        for (var i = 0; i < 40; i++)
        {
            set.Advance(0.1);
        }

        Assert.Equal(["start", "end", "start", "end"], log);
        Assert.False(outer.IsActive);
    }

    [Fact]
    public void ASequenceThatYoyosReplaysItsCallsAndRestoresStartValues()
    {
        var set = new TweenSet();
        float a = 0, b = 0;
        var seq = set.Sequence(set.Number(0, 1, 1, x => a = x), set.Number(10, 20, 1, x => b = x)).Repeat(2, yoyo: true);
        Assert.Equal(4, seq.TotalDuration, 1e-9);
        set.Advance(1.5);
        Assert.Equal(1f, a);
        Assert.Equal(15f, b, 1e-5f);
        set.Advance(1); // t = 2.5: reversing, B is at 1.5 s of its reverse pass = 5/10 back from 20
        Assert.Equal(1f, a);
        Assert.Equal(15f, b, 1e-5f);
        set.Advance(1); // t = 3.5: B is back at its start, A has run half of the way back
        Assert.Equal(10f, b);
        Assert.Equal(0.5f, a, 1e-5f);
        set.Advance(1);
        Assert.Equal(0f, a);
        Assert.Equal(10f, b);
    }

    [Fact]
    public void ValuesDependOnlyOnTimeNotOnHowItIsSteppedTo()
    {
        float Final(double[] steps, double checkpoint)
        {
            var set = new TweenSet();
            float v = 0, w = 0;
            set.Sequence(set.Number(0, 100, 2, x => v = x).Ease(Easing.InOutCubic), set.Number(0, 50, 1, x => w = x).Delay(0.25f)).Repeat(3, yoyo: true);
            var t = 0.0;
            foreach (var s in steps)
            {
                set.Advance(s);
                t += s;
            }

            Assert.Equal(checkpoint, t, 1e-9);
            return v + (w * 1000);
        }

        foreach (var at in new[] { 0.5, 1.75, 3.25, 5.0, 9.0, 12.0 })
        {
            var fine = Final(Enumerable.Repeat(at / 50, 50).ToArray(), at);
            var coarse = Final([at], at);
            var uneven = Final([at * 0.3, at * 0.01, at * 0.69], at);
            Assert.Equal(coarse, fine, 0.05f);
            Assert.Equal(coarse, uneven, 0.05f);
        }
    }

    [Fact]
    public void PauseTimeScaleAndAPausedSetHoldOrSlowTime()
    {
        var set = new TweenSet();
        float v = 0;
        var t = set.Number(0, 1, 1, x => v = x);
        set.Advance(0.2);
        t.Pause();
        set.Advance(5);
        Assert.Equal(0.2f, v, 1e-5f);
        t.Resume();
        set.TimeScale = 0.5f;
        set.Advance(0.4);
        Assert.Equal(0.4f, v, 1e-5f);
        set.Paused = true;
        set.Advance(10);
        Assert.Equal(0.4f, v, 1e-5f);
        set.Paused = false;
        set.TimeScale = 1;
        set.Advance(0.6);
        Assert.Equal(1f, v);
        Assert.Throws<ArgumentOutOfRangeException>(() => set.TimeScale = -1);
    }

    [Fact]
    public void CompleteAppliesTheEndAndCallsBackCancelDoesNot()
    {
        var set = new TweenSet();
        float v = 0, w = 0;
        var done = 0;
        var a = set.Number(0, 9, 100, x => v = x).OnComplete(() => done++);
        var b = set.Number(0, 9, 100, x => w = x).OnComplete(() => done += 10);
        set.Advance(1);
        a.Complete();
        b.Cancel();
        Assert.Equal(9f, v);
        Assert.Equal(0.09f, w, 1e-5f);
        Assert.Equal(1, done);
        Assert.Equal(0, set.ActiveCount);
        Assert.Throws<TweenException>(() => b.Cancel());
        Assert.Throws<TweenException>(() => a.Ease(Easing.Linear));
        Assert.Throws<TweenException>(() => set.Number(0, 1, 1, _ => { }).RepeatForever().Complete());
    }

    [Fact]
    public void SeekScrubsBackAndForthAndReplaysCallbacks()
    {
        var set = new TweenSet();
        float v = 0;
        var done = 0;
        var t = set.Number(0, 10, 2, x => v = x).OnComplete(() => done++).Repeat(1);
        t.Seek(1);
        Assert.Equal(5f, v, 1e-5f);
        t.Seek(0.5);
        Assert.Equal(2.5f, v, 1e-5f);
        t.Seek(2);
        Assert.Equal(10f, v);
        Assert.Equal(1, done);
        Assert.False(t.IsActive);
    }

    [Fact]
    public void CallbacksMayStartMoreTweensWhichWaitForTheNextAdvance()
    {
        var set = new TweenSet();
        float v = 0;
        var chain = 0;
        set.Number(0, 1, 1, _ => { }).OnComplete(() =>
        {
            chain++;
            set.Number(0, 1, 1, x => v = x);
        });
        set.Advance(1);
        Assert.Equal(1, chain);
        Assert.Equal(0f, v);
        Assert.Equal(1, set.ActiveCount);
        set.Advance(0.5);
        Assert.Equal(0.5f, v, 1e-5f);
    }

    [Fact]
    public void ValueCallbacksMayCancelOtherTweensSafely()
    {
        var set = new TweenSet();
        float other = 0;
        var victim = set.Number(0, 1, 5, x => other = x);
        set.Number(0, 1, 1, _ => { if (victim.IsActive) { victim.Cancel(); } });
        set.Advance(0.5);
        Assert.False(victim.IsActive);
        Assert.Equal(1, set.ActiveCount);
        set.Advance(0.6);
        Assert.Equal(0, set.ActiveCount);
        Assert.Equal(0.1f, other, 1e-5f);
    }

    [Fact]
    public void MisuseThrowsTypedExceptionsWithTheFix()
    {
        var set = new TweenSet();
        var a = set.Number(0, 1, 1, _ => { });
        var seq = set.Sequence(a);
        var ex = Assert.Throws<TweenException>(() => a.Cancel());
        Assert.Contains("group", ex.Message, StringComparison.Ordinal);
        Assert.Throws<TweenException>(() => a.Pause());
        Assert.Throws<TweenException>(() => a.RepeatForever());
        Assert.Throws<TweenException>(() => set.Sequence(a));                              // already in a group
        var forever = set.Number(0, 1, 1, _ => { }).RepeatForever();
        Assert.Throws<TweenException>(() => set.Parallel(forever));                         // would never end
        var started = set.Number(0, 1, 1, _ => { });
        set.Advance(0.1);
        Assert.Throws<TweenException>(() => set.Sequence(started));                         // already running
        Assert.Throws<TweenException>(() => seq.Add(set.Number(0, 1, 1, _ => { })));        // group already started
        Assert.Throws<TweenException>(() => set.Number(0, 1, 1, _ => { }).Add(set.Delay(1)));       // not a group
        Assert.Throws<ArgumentOutOfRangeException>(() => set.Number(0, 1, -1, _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.Number(0, 1, float.NaN, _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.Delay(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.Advance(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.Number(0, 1, 1, _ => { }).Repeat(0));
        Assert.Throws<ArgumentNullException>(() => set.Number(0, 1, 1, null!));
        Assert.Throws<FormatException>(() => set.Number(0, 1, 1, _ => { }).Ease("wobbly"));
        Assert.False(default(Tween).IsActive);
        Assert.Throws<TweenException>(() => default(Tween).Cancel());
    }

    [Fact]
    public void GroupsDeeperThanTheLimitAreRefused()
    {
        var set = new TweenSet();
        var g = set.Sequence(set.Delay(1));
        var depth = 1;
        Assert.Throws<TweenException>((Action)(() =>
        {
            while (true)
            {
                g = set.Sequence(g);
                depth++;
            }
        }));
        Assert.InRange(depth, 2, 12);
    }

    [Fact]
    public void AdvanceFromInsideAValueCallbackIsRefused()
    {
        var set = new TweenSet();
        TweenException? caught = null;
        set.Number(0, 1, 1, _ =>
        {
            try
            {
                set.Advance(1);
            }
            catch (TweenException ex)
            {
                caught = ex;
            }
        });
        set.Advance(0.5);
        Assert.NotNull(caught);
    }

    [Fact]
    public void TheSnapshotListsWhatIsRunning()
    {
        var set = new TweenSet();
        set.Number(0, 1, 2, _ => { }).Named("fade");
        set.Sequence(set.Delay(1), set.Delay(1));
        set.Advance(0.5);
        var list = new List<TweenInfo>();
        set.Snapshot(list);
        Assert.Equal(2, list.Count);
        Assert.Equal("value", list[0].Kind);
        Assert.Equal("fade", list[0].Name);
        Assert.Equal(0.5, list[0].Elapsed, 1e-9);
        Assert.Equal(0.25, list[0].Progress, 1e-9);
        Assert.Equal("sequence", list[1].Kind);
        Assert.Equal(2, list[1].Children);
        Assert.Equal(2, list[1].Duration, 1e-9);
        Assert.True(list[0].Id < list[1].Id);
        set.CancelAll();
        set.Snapshot(list);
        Assert.Empty(list);
    }

    [Fact]
    public void RunningTweensAllocateNothingPerFrame()
    {
        var set = new TweenSet();
        var sink = new float[8];
        var v2 = Vector2.Zero;
        var col = Color.Black;
        Action<float> numberSink = x => sink[0] = x;
        Action<Vector2> v2Sink = v => v2 = v;
        var colorCalls = 0;
        Action<Color> colorSink = c => { col = c; colorCalls++; };
        for (var i = 0; i < 100; i++)
        {
            set.Number(0, 1, 1, numberSink).RepeatForever(yoyo: true).Ease(Easing.InOutCubic);
            set.Vector2(Vector2.Zero, Vector2.One, 2, v2Sink).RepeatForever();
            set.Color(Color.White, Color.Black, 0.7f, colorSink).Repeat(1_000_000, yoyo: true).Ease(Easing.OutBack);
            set.Sequence(set.Number(0, 1, 0.3f, numberSink), set.Delay(0.1f), set.Number(1, 0, 0.3f, numberSink)).Repeat(1_000_000, yoyo: true);
            set.Parallel(set.Number(0, 1, 0.5f, numberSink), set.Vector2(Vector2.One, Vector2.Zero, 0.5f, v2Sink)).Repeat(1_000_000);
        }

        Assert.Equal(500, set.ActiveCount);
        for (var i = 0; i < 100; i++)
        {
            set.Advance(1 / 60.0);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 600; i++)
        {
            set.Advance(1 / 60.0);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(500, set.ActiveCount);
        Assert.True(colorCalls > 0);
    }

    [Fact]
    public void StartingAndFinishingTweensReusesThePoolWithoutAllocating()
    {
        var set = new TweenSet();
        float v = 0;
        Action<float> sink = x => v = x;
        Action done = () => { };
        for (var i = 0; i < 50; i++)
        {
            set.Number(0, 1, 0.1f, sink).OnComplete(done);
        }

        for (var i = 0; i < 10; i++)
        {
            set.Advance(0.1);
        }

        Assert.Equal(0, set.ActiveCount);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var round = 0; round < 20; round++)
        {
            for (var i = 0; i < 50; i++)
            {
                set.Number(0, 1, 0.1f, sink).Ease(Easing.OutCubic).OnComplete(done);
            }

            for (var i = 0; i < 10; i++)
            {
                set.Advance(0.1);
            }
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.Equal(1f, v);
    }
}
