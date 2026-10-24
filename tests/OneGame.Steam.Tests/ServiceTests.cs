using System.Text;
using OneGame.Steam.Native;
using OneGame.Stores;

namespace OneGame.Steam.Tests;

public class ServiceTests
{
    private static SteamStorePlatform Connect(FakeNative fake) => (SteamStorePlatform)SteamPlatform.Start(fake, new SteamOptions());

    private static byte[] Wire4(params object[] values) => Wire.Pack((int)SteamLayout.Current, values);

    // ---- lifecycle ----

    [Fact]
    public void StartConnectsWhenInitSucceeds()
    {
        var fake = new FakeNative();
        using var store = SteamPlatform.Start(fake, new SteamOptions());
        Assert.True(store.IsAvailable);
        Assert.Equal("steam", store.Name);
        Assert.Null(store.UnavailableReason);
        Assert.Equal(["init"], fake.Calls);
    }

    [Theory]
    [InlineData(SteamInitStatus.NoSteamClient, "Cannot create IPC pipe")]
    [InlineData(SteamInitStatus.FailedGeneric, "app id")]
    [InlineData(SteamInitStatus.VersionMismatch, "older")]
    public void FailedInitGivesNoOpServicesWithTheReason(SteamInitStatus status, string message)
    {
        var fake = new FakeNative { InitStatus = status, InitMessage = message };
        using var store = SteamPlatform.Start(fake, new SteamOptions());
        Assert.False(store.IsAvailable);
        Assert.Equal("none", store.Name);
        Assert.Contains(status.ToString(), store.UnavailableReason);
        Assert.Contains(message, store.UnavailableReason);
        Assert.False(store.RelaunchRequested);
        Assert.False(fake.ShutDown); // init failed; there is nothing to shut down
        AssertAllServicesAreInert(store);
    }

    [Fact]
    public void RelaunchIsOnlyAskedForWhenOptedInWithAnAppId()
    {
        var fake = new FakeNative { RestartResult = true };
        using (var a = SteamPlatform.Start(fake, new SteamOptions { AppId = 480 }))
        {
            Assert.True(a.IsAvailable); // not opted in: carries on
        }

        using (var b = SteamPlatform.Start(fake, new SteamOptions { RelaunchThroughSteam = true }))
        {
            Assert.True(b.IsAvailable); // no app id: cannot ask
        }

        Assert.DoesNotContain("restart:480", fake.Calls);

        using var c = SteamPlatform.Start(fake, new SteamOptions { AppId = 480, RelaunchThroughSteam = true });
        Assert.True(c.RelaunchRequested);
        Assert.False(c.IsAvailable);
        Assert.Contains("restart:480", fake.Calls);
        Assert.Equal(2, fake.Calls.Count(x => x == "init")); // the relaunching start never initializes
    }

    [Fact]
    public void RelaunchNotNeededGoesOnToInit()
    {
        var fake = new FakeNative { RestartResult = false };
        using var store = SteamPlatform.Start(fake, new SteamOptions { AppId = 480, RelaunchThroughSteam = true });
        Assert.True(store.IsAvailable);
        Assert.Equal(["restart:480", "init"], fake.Calls);
    }

    [Fact]
    public void DisposeShutsDownOnceAndTurnsServicesInert()
    {
        var fake = new FakeNative();
        var store = Connect(fake);
        store.Dispose();
        store.Dispose();
        Assert.True(fake.ShutDown);
        Assert.False(store.IsAvailable);
        AssertAllServicesAreInert(store);
        store.Pump(); // no-op, must not touch the library
        Assert.Equal(0, fake.RunFrameCount);
    }

    private static void AssertAllServicesAreInert(IStorePlatform s)
    {
        Assert.Equal(StoreResult.Unavailable, s.Achievements.Unlock("ACH_WIN"));
        Assert.Equal(StoreResult.Unavailable, s.Achievements.TryGetUnlocked("ACH_WIN", out var u));
        Assert.False(u);
        Assert.Equal(StoreResult.Unavailable, s.Achievements.ShowProgress("ACH_TEN", 1, 10));
        Assert.Equal(StoreResult.Unavailable, s.Achievements.Clear("ACH_WIN"));
        Assert.Equal(StoreResult.Unavailable, s.Stats.SetInt("wins", 1));
        Assert.Equal(StoreResult.Unavailable, s.Stats.SetFloat("distance", 1));
        Assert.Equal(StoreResult.Unavailable, s.Stats.TryGetInt("wins", out _));
        Assert.Equal(StoreResult.Unavailable, s.Stats.TryGetFloat("distance", out _));
        Assert.Equal(StoreResult.Unavailable, s.Stats.Commit());
        Assert.Equal(StoreResult.Unavailable, s.Cloud.Write("a", [1]));
        Assert.Equal(StoreResult.Unavailable, s.Cloud.Read("a", out var data));
        Assert.Empty(data);
        Assert.False(s.Cloud.Exists("a"));
        Assert.Equal(StoreResult.Unavailable, s.Cloud.Delete("a"));
        Assert.Empty(s.Cloud.List());
        Assert.Equal(StoreResult.Unavailable, s.Cloud.TryGetQuota(out _));
        Assert.False(s.Cloud.IsEnabled);
        Assert.Equal(StoreResult.Unavailable, s.Presence.Update("status", "x"));
        Assert.Equal(StoreResult.Unavailable, s.Presence.Clear());
        Assert.Equal(StoreResult.Unavailable, s.Overlay.Open(OverlayPage.Friends));
        Assert.Equal(StoreResult.Unavailable, s.Overlay.OpenWebPage("https://example.com"));
        Assert.Equal(StoreResult.Unavailable, s.Overlay.OpenStorePage(480));
        Assert.False(s.Overlay.IsEnabled);
        Assert.False(s.Overlay.IsActive);
        Assert.False(s.Ownership.OwnsApp);
        Assert.False(s.Ownership.HasDlc(1));
        Assert.Equal(0u, s.Ownership.AppId);
        Assert.Equal(0, s.Ownership.BuildId);
        Assert.Equal(0UL, s.Identity.UserId);
        Assert.Equal("", s.Identity.DisplayName);
        Assert.Equal("", s.Environment.Language);
        Assert.Equal("", s.Environment.LanguageTag);
        Assert.False(s.Environment.IsHandheld);
        Assert.False(s.Environment.IsCouchMode);
        s.Pump();
    }

    // ---- identity, ownership, environment ----

    [Fact]
    public void IdentityOwnershipAndEnvironmentReadThroughToSteam()
    {
        var fake = new FakeNative { Deck = true, BigPicture = true };
        fake.InstalledDlc.Add(999);
        using var store = Connect(fake);
        Assert.Equal(76561198000000001UL, store.Identity.UserId);
        Assert.Equal("Ada", store.Identity.DisplayName);
        Assert.Equal(480u, store.Ownership.AppId);
        Assert.True(store.Ownership.OwnsApp);
        Assert.True(store.Ownership.HasDlc(999));
        Assert.False(store.Ownership.HasDlc(1000));
        Assert.False(store.Ownership.HasDlc(0));
        Assert.Equal(1234, store.Ownership.BuildId);
        Assert.Equal("german", store.Environment.Language);
        Assert.Equal("de", store.Environment.LanguageTag);
        Assert.True(store.Environment.IsHandheld);
        Assert.True(store.Environment.IsCouchMode);
        Assert.True(store.Overlay.IsEnabled);
    }

    [Theory]
    [InlineData("schinese", "zh-CN")]
    [InlineData("brazilian", "pt-BR")]
    [InlineData("latam", "es-419")]
    [InlineData("koreana", "ko")]
    [InlineData("klingon", "")]
    public void LanguageNamesMapToTags(string steamName, string tag)
    {
        var fake = new FakeNative { GameLanguage = steamName };
        using var store = Connect(fake);
        Assert.Equal(tag, store.Environment.LanguageTag);
    }

    [Fact]
    public void LanguageFallsBackToTheClientLanguage()
    {
        var fake = new FakeNative { GameLanguage = "" };
        using var store = Connect(fake);
        Assert.Equal("english", store.Environment.Language);
        Assert.Equal("en", store.Environment.LanguageTag);
    }

    // ---- achievements and stats ----

    [Fact]
    public void UnlockSetsThenStoresAndIsIdempotent()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        Assert.Equal(StoreResult.Ok, store.Achievements.Unlock("ACH_WIN"));
        Assert.Equal(["init", "set-ach:ACH_WIN", "store"], fake.Calls);

        Assert.Equal(StoreResult.Ok, store.Achievements.TryGetUnlocked("ACH_WIN", out var unlocked));
        Assert.True(unlocked);

        fake.Calls.Clear();
        Assert.Equal(StoreResult.Ok, store.Achievements.Unlock("ACH_WIN"));
        Assert.Empty(fake.Calls); // already unlocked: no second pop-up request

        Assert.Equal(StoreResult.Ok, store.Achievements.Clear("ACH_WIN"));
        Assert.Equal(StoreResult.Ok, store.Achievements.TryGetUnlocked("ACH_WIN", out unlocked));
        Assert.False(unlocked);
    }

    [Fact]
    public void UnknownAndMalformedAchievementIdsAreRejectedWithoutCallingSteam()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        Assert.Equal(StoreResult.Rejected, store.Achievements.Unlock("NOPE"));
        Assert.Equal(StoreResult.NotFound, store.Achievements.TryGetUnlocked("NOPE", out _));
        fake.Calls.Clear();
        Assert.Equal(StoreResult.InvalidArgument, store.Achievements.Unlock(""));
        Assert.Equal(StoreResult.InvalidArgument, store.Achievements.Unlock(new string('a', 129)));
        Assert.Equal(StoreResult.InvalidArgument, store.Achievements.Unlock("A\0B"));
        Assert.Equal(StoreResult.InvalidArgument, store.Achievements.Unlock(new string('é', 65))); // 130 bytes in UTF-8
        Assert.Empty(fake.Calls);
    }

    [Fact]
    public void ProgressNeedsCurrentBelowMax()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        Assert.Equal(StoreResult.Ok, store.Achievements.ShowProgress("ACH_TEN", 3, 10));
        Assert.Contains("progress:ACH_TEN:3/10", fake.Calls);
        Assert.Equal(StoreResult.InvalidArgument, store.Achievements.ShowProgress("ACH_TEN", 10, 10));
        Assert.Equal(StoreResult.InvalidArgument, store.Achievements.ShowProgress("ACH_TEN", 0, 0));
        Assert.Equal(StoreResult.Rejected, store.Achievements.ShowProgress("NOPE", 1, 10));
    }

    [Fact]
    public void StoredCallbackRaisesTheAchievementEventOnlyForFullUnlocks()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        var stored = new List<string>();
        store.Achievements.Stored += stored.Add;

        fake.QueueCallback(1103, Wire4(480UL, (byte)0, "ACH_TEN", 3u, 10u)); // progress notification
        fake.QueueCallback(1103, Wire4(480UL, (byte)0, "ACH_WIN", 0u, 0u));
        store.Pump();
        Assert.Equal(["ACH_WIN"], stored);
    }

    [Fact]
    public void StatsSetGetAndCommit()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        Assert.Equal(StoreResult.Ok, store.Stats.SetInt("wins", 9));
        Assert.Equal(StoreResult.Ok, store.Stats.TryGetInt("wins", out var wins));
        Assert.Equal(9, wins);
        Assert.Equal(StoreResult.Ok, store.Stats.SetFloat("distance", 2.25f));
        Assert.Equal(StoreResult.Ok, store.Stats.TryGetFloat("distance", out var d));
        Assert.Equal(2.25f, d);
        Assert.Equal(StoreResult.Rejected, store.Stats.SetInt("unknown", 1));
        Assert.Equal(StoreResult.NotFound, store.Stats.TryGetInt("unknown", out _));
        Assert.Equal(StoreResult.InvalidArgument, store.Stats.SetInt("", 1));
        Assert.Equal(StoreResult.Ok, store.Stats.Commit());
        Assert.Contains("store", fake.Calls);
    }

    [Fact]
    public void CommitEventReportsTheServerOutcome()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        var outcomes = new List<bool>();
        store.Stats.Committed += outcomes.Add;
        fake.QueueCallback(1102, Wire4(480UL, 1));
        fake.QueueCallback(1102, Wire4(480UL, 8)); // server rejected a value and sent corrections
        store.Pump();
        Assert.Equal([true, false], outcomes);
    }

    // ---- cloud ----

    [Fact]
    public void CloudWriteReadListDeleteAndQuota()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        var save = Encoding.UTF8.GetBytes("{\"level\":3}");
        Assert.True(store.Cloud.IsEnabled);
        Assert.Equal(StoreResult.Ok, store.Cloud.Write("saves/slot1.json", save));
        Assert.Equal(StoreResult.Ok, store.Cloud.Write("settings.bin", [1, 2, 3]));
        Assert.True(store.Cloud.Exists("saves/slot1.json"));

        Assert.Equal(StoreResult.Ok, store.Cloud.Read("saves/slot1.json", out var back));
        Assert.Equal(save, back);

        Assert.Equal([new CloudFile("saves/slot1.json", save.Length), new CloudFile("settings.bin", 3)], store.Cloud.List());

        Assert.Equal(StoreResult.Ok, store.Cloud.TryGetQuota(out var quota));
        Assert.Equal(new CloudQuota(1000, 1000 - (ulong)save.Length - 3), quota);

        Assert.Equal(StoreResult.Ok, store.Cloud.Write("settings.bin", [])); // an empty overwrite is a valid file
        Assert.Equal(StoreResult.Ok, store.Cloud.Delete("settings.bin"));
        Assert.Equal(StoreResult.NotFound, store.Cloud.Delete("settings.bin"));
        Assert.Equal(StoreResult.NotFound, store.Cloud.Read("settings.bin", out var gone));
        Assert.Empty(gone);
    }

    [Fact]
    public void CloudReadChecksTheSizeBeforeAllocating()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        store.Cloud.Write("big.bin", new byte[2000]);
        Assert.Equal(StoreResult.TooLarge, store.Cloud.Read("big.bin", out var data, maxBytes: 1999));
        Assert.Empty(data);
        Assert.Equal(StoreResult.Ok, store.Cloud.Read("big.bin", out data, maxBytes: 2000));
        Assert.Equal(2000, data.Length);
        Assert.Equal(StoreResult.InvalidArgument, store.Cloud.Read("big.bin", out _, maxBytes: -1));
    }

    [Fact]
    public void CloudReadFailsWhenSteamReturnsFewerBytesThanItReportedAsTheSize()
    {
        var fake = new FakeNative { ShortRead = true };
        using var store = Connect(fake);
        fake.Cloud["x"] = new byte[10];
        Assert.Equal(StoreResult.Failed, store.Cloud.Read("x", out var data));
        Assert.Empty(data);
    }

    [Fact]
    public void CloudRejectsBadNamesAndOversizeWritesWithoutCallingSteam()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        foreach (var bad in new[] { "", "/abs", "a\\b", "../x", "a/../b", "a\0b", new string('n', 261) })
        {
            Assert.Equal(StoreResult.InvalidArgument, store.Cloud.Write(bad, [1]));
            Assert.False(store.Cloud.Exists(bad));
        }

        Assert.Empty(fake.Cloud);
        Assert.Equal(StoreResult.TooLarge, store.Cloud.Write("huge", new byte[ICloudSaves.MaxFileBytes + 1]));
    }

    [Fact]
    public void CloudWriteReportsDisabledAndFailedStates()
    {
        var fake = new FakeNative { CloudEnabled = false };
        using var store = Connect(fake);
        Assert.False(store.Cloud.IsEnabled);
        Assert.Equal(StoreResult.Disabled, store.Cloud.Write("a", [1]));

        fake.CloudEnabled = true;
        fake.CloudFailWrites = 1;
        Assert.Equal(StoreResult.Failed, store.Cloud.Write("a", [1]));
        Assert.Equal(StoreResult.Ok, store.Cloud.Write("a", [1]));
    }

    // ---- presence and overlay ----

    [Fact]
    public void RichPresenceValidatesLengthsAndClears()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        Assert.Equal(StoreResult.Ok, store.Presence.Update("status", "In the garden"));
        Assert.Equal("In the garden", fake.Presence["status"]);
        Assert.Equal(StoreResult.Ok, store.Presence.Update("status", ""));
        Assert.Empty(fake.Presence);
        Assert.Equal(StoreResult.InvalidArgument, store.Presence.Update("", "x"));
        Assert.Equal(StoreResult.InvalidArgument, store.Presence.Update(new string('k', 65), "x"));
        Assert.Equal(StoreResult.InvalidArgument, store.Presence.Update("k", new string('v', 257)));
        Assert.Equal(StoreResult.Ok, store.Presence.Update("k", new string('v', 256)));
        Assert.Equal(StoreResult.Ok, store.Presence.Clear());
        Assert.Empty(fake.Presence);
    }

    [Fact]
    public void OverlayOpensPagesAndOnlyWebUrls()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        Assert.Equal(StoreResult.Ok, store.Overlay.Open(OverlayPage.OfficialGameGroup));
        Assert.Equal(StoreResult.Ok, store.Overlay.Open(OverlayPage.Achievements));
        Assert.Equal(StoreResult.InvalidArgument, store.Overlay.Open((OverlayPage)99));
        Assert.Equal(StoreResult.Ok, store.Overlay.OpenWebPage("https://example.com/manual"));
        Assert.Equal(StoreResult.InvalidArgument, store.Overlay.OpenWebPage("file:///etc/passwd"));
        Assert.Equal(StoreResult.InvalidArgument, store.Overlay.OpenWebPage("steam://run/480"));
        Assert.Equal(StoreResult.Ok, store.Overlay.OpenStorePage(480));
        Assert.Equal(StoreResult.InvalidArgument, store.Overlay.OpenStorePage(0));
        Assert.Equal(
            ["init", "overlay:officialgamegroup", "overlay:achievements", "overlay-web:https://example.com/manual", "overlay-store:480"],
            fake.Calls);
    }

    [Fact]
    public void OverlayStateFollowsTheActivationCallback()
    {
        var fake = new FakeNative();
        using var store = Connect(fake);
        var changes = new List<bool>();
        store.Overlay.ActiveChanged += changes.Add;
        Assert.False(store.Overlay.IsActive);
        fake.QueueCallback(331, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        store.Pump();
        Assert.True(store.Overlay.IsActive);
        fake.QueueCallback(331, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        store.Pump();
        Assert.False(store.Overlay.IsActive);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void NullStoreCanBeUsedLikeAnyOther()
    {
        using var none = new NullStorePlatform("test");
        Assert.False(none.IsAvailable);
        Assert.Equal("test", none.UnavailableReason);
        AssertAllServicesAreInert(none);
        none.Overlay.ActiveChanged += _ => { };
        none.Achievements.Stored += _ => { };
        none.Stats.Committed += _ => { };
    }
}
