using OneGame.Steam.Native;
using OneGame.Stores;

namespace OneGame.Steam.Tests;

public class CallbackTests
{
    private static readonly SteamPacking Pack = SteamPacking.Small;

    private static byte[] Completed(ulong call, int id, uint size) => Wire.Pack(4, call, id, size);

    [Fact]
    public void PumpRunsAFrameAndFreesEveryCallbackItTook()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var seen = new List<UserStatsStored>();
        callbacks.Subscribe<UserStatsStored>(seen.Add);

        fake.QueueCallback(1102, Wire.Pack(4, 480UL, 1));
        fake.QueueCallback(9999, [1, 2, 3]); // nobody listens; must still be freed
        fake.QueueCallback(1102, Wire.Pack(4, 480UL, 8));
        callbacks.Pump();

        Assert.Equal(1, fake.RunFrameCount);
        Assert.Equal(3, fake.FreeCount);
        Assert.Equal([new UserStatsStored(480, 1), new UserStatsStored(480, 8)], seen);
    }

    [Fact]
    public void ATruncatedCallbackIsDroppedAndTheLoopContinues()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var count = 0;
        callbacks.Subscribe<UserStatsStored>(_ => count++);
        fake.QueueCallback(1102, [1, 2, 3]);
        fake.QueueCallback(1102, Wire.Pack(4, 480UL, 1));
        callbacks.Pump();
        Assert.Equal(1, count);
        Assert.Equal(2, fake.FreeCount);
    }

    [Fact]
    public void ACallbackIsFreedEvenWhenTheHandlerThrows()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        callbacks.Subscribe<UserStatsStored>(_ => throw new InvalidOperationException("boom"));
        fake.QueueCallback(1102, Wire.Pack(4, 480UL, 1));
        Assert.Throws<InvalidOperationException>(callbacks.Pump);
        Assert.Equal(1, fake.FreeCount);
    }

    [Fact]
    public void ACallResultIsMatchedByHandleAndDeliveredOnce()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var results = new List<(NumberOfCurrentPlayers, bool)>();
        callbacks.Track<NumberOfCurrentPlayers>(77, (v, failed) => results.Add((v, failed)));
        Assert.Equal(1, callbacks.PendingCalls);

        var payload = Wire.Pack(4, (byte)1, 4321);
        fake.SetResult(77, 1107, payload);
        fake.QueueCallback(703, Completed(55, 1107, 8)); // someone else's call: ignored
        fake.QueueCallback(703, Completed(77, 1107, 8));
        fake.QueueCallback(703, Completed(77, 1107, 8)); // duplicate completion: nothing pending any more
        callbacks.Pump();

        Assert.Equal([(new NumberOfCurrentPlayers(true, 4321), false)], results);
        Assert.Equal(0, callbacks.PendingCalls);
        Assert.Equal(3, fake.FreeCount);
    }

    [Fact]
    public void TwoCallsInFlightAreMatchedToTheirOwnHandlers()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var a = new List<int>();
        var b = new List<int>();
        callbacks.Track<NumberOfCurrentPlayers>(1, (v, _) => a.Add(v.Players));
        callbacks.Track<NumberOfCurrentPlayers>(2, (v, _) => b.Add(v.Players));
        fake.SetResult(1, 1107, Wire.Pack(4, (byte)1, 10));
        fake.SetResult(2, 1107, Wire.Pack(4, (byte)1, 20));
        fake.QueueCallback(703, Completed(2, 1107, 8));
        fake.QueueCallback(703, Completed(1, 1107, 8));
        callbacks.Pump();
        Assert.Equal([10], a);
        Assert.Equal([20], b);
    }

    [Fact]
    public void AFailedCallReportsFailureWithADefaultValue()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        (NumberOfCurrentPlayers, bool)? got = null;
        callbacks.Track<NumberOfCurrentPlayers>(5, (v, f) => got = (v, f));
        fake.SetResult(5, 1107, Wire.Pack(4, (byte)1, 9), failed: true);
        fake.QueueCallback(703, Completed(5, 1107, 8));
        callbacks.Pump();
        Assert.Equal((default(NumberOfCurrentPlayers), true), got);
    }

    [Fact]
    public void ARefusedCallHandleFailsImmediately()
    {
        var callbacks = new SteamCallbacks(new FakeNative(), Pack);
        var failed = false;
        callbacks.Track<NumberOfCurrentPlayers>(0, (_, f) => failed = f);
        Assert.True(failed);
        Assert.Equal(0, callbacks.PendingCalls);
    }

    [Fact]
    public void AnAbsurdResultSizeIsNotAllocatedFor()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var failed = false;
        callbacks.Track<NumberOfCurrentPlayers>(9, (_, f) => failed = f);
        fake.QueueCallback(703, Completed(9, 1107, uint.MaxValue));
        callbacks.Pump();
        Assert.True(failed);
        Assert.Equal(0, callbacks.PendingCalls);
    }

    [Fact]
    public void ACallThatTheLibraryCannotProduceTheResultOfReportsFailure()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var failed = false;
        callbacks.Track<NumberOfCurrentPlayers>(9, (_, f) => failed = f);
        fake.QueueCallback(703, Completed(9, 1107, 8)); // no result registered: GetCallResult returns false
        callbacks.Pump();
        Assert.True(failed);
    }

    [Fact]
    public void AHandlerMaySubscribeAnotherHandlerWhileTheLoopRuns()
    {
        var fake = new FakeNative();
        var callbacks = new SteamCallbacks(fake, Pack);
        var late = 0;
        callbacks.Subscribe<UserStatsStored>(_ => callbacks.Subscribe<UserStatsStored>(_ => late++));
        fake.QueueCallback(1102, Wire.Pack(4, 480UL, 1));
        callbacks.Pump();
        Assert.Equal(1, late); // the new handler also sees the callback being dispatched
    }

    [Fact]
    public void PlatformDecodesWithThePackingOfTheRunningOs()
    {
        // The fake queues bytes in the packing of the running OS; a platform built through Start decodes them the same way.
        var fake = new FakeNative();
        using var steam = (SteamStorePlatform)SteamPlatform.Start(fake, new SteamOptions());
        UserStatsReceived? got = null;
        steam.Callbacks.Subscribe<UserStatsReceived>(v => got = v);
        fake.QueueCallback(1101, Wire.Pack((int)SteamLayout.Current, 480UL, 1, 76561198000000001UL));
        steam.Pump();
        Assert.Equal(new UserStatsReceived(480, 1, 76561198000000001UL), got);
    }

    [Fact]
    public void NumberOfPlayersRoundTripsThroughThePlatform()
    {
        var fake = new FakeNative { NextCall = 123 };
        using var steam = (SteamStorePlatform)SteamPlatform.Start(fake, new SteamOptions());
        NumberOfCurrentPlayers? got = null;
        steam.RequestNumberOfCurrentPlayers((v, failed) => got = failed ? null : v);
        fake.SetResult(123, 1107, Wire.Pack((int)SteamLayout.Current, (byte)1, 42));
        fake.QueueCallback(703, Wire.Pack((int)SteamLayout.Current, 123UL, 1107, 8u));
        steam.Pump();
        Assert.Equal(new NumberOfCurrentPlayers(true, 42), got);
    }
}
