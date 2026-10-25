using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using OneGame.Steam.Native;
using OneGame.Stores;

namespace OneGame.Steam.Tests;

/// <summary>Tests against Valve's real redistributable. They need <c>ONEGAME_STEAM_REDIST</c> to point at a folder holding
/// <c>linux-x64/libsteam_api.so</c>, <c>win-x64/steam_api64.dll</c> and <c>osx/libsteam_api.dylib</c>, and skip without it. None of
/// them needs a Steam client: they cover loading, the export table and the documented failure when no client is running.</summary>
public partial class RealLibraryTests
{
    private static string? Redist => Environment.GetEnvironmentVariable(SteamLibrary.RedistDirectoryVariable);

    private static string[] BoundEntryPoints([CallerFilePath] string here = "")
    {
        var source = File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "src", "OneGame.Steam", "Native", "SteamApi.cs"));
        return [.. EntryPoint().Matches(source).Select(m => m.Groups[1].Value)];
    }

    [GeneratedRegex("EntryPoint = \"(\\w+)\"")]
    private static partial Regex EntryPoint();

    [Fact]
    public void EveryBoundFunctionIsListedOnce()
    {
        var names = BoundEntryPoints();
        Assert.True(names.Length > 50);
        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Fact]
    public void EveryBoundFunctionIsExportedByThisSystemsLibrary()
    {
        Assert.SkipUnless(Redist is not null, $"set {SteamLibrary.RedistDirectoryVariable}");
        var path = Path.Combine(Redist!, SteamLibrary.PlatformFolder, SteamLibrary.FileName);
        Assert.True(NativeLibrary.TryLoad(path, out var handle), $"cannot load {path}");
        try
        {
            var missing = BoundEntryPoints().Where(n => !NativeLibrary.TryGetExport(handle, n, out _)).ToArray();
            Assert.Empty(missing);
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [Theory]
    [InlineData("win-x64", "steam_api64.dll")]
    [InlineData("osx", "libsteam_api.dylib")]
    public void EveryBoundFunctionNameAppearsInTheOtherPlatformsBinaries(string folder, string file)
    {
        Assert.SkipUnless(Redist is not null, $"set {SteamLibrary.RedistDirectoryVariable}");
        // Names sit zero-terminated in the export (PE) or symbol (Mach-O, with a leading underscore) string tables.
        var bytes = File.ReadAllBytes(Path.Combine(Redist!, folder, file));
        var missing = BoundEntryPoints().Where(n => !ContainsSymbol(bytes, n)).ToArray();
        Assert.Empty(missing);
    }

    private static bool ContainsSymbol(byte[] haystack, string name)
    {
        var needle = Encoding.ASCII.GetBytes(name);
        var span = haystack.AsSpan();
        var from = 0;
        while (true)
        {
            var i = span[from..].IndexOf(needle);
            if (i < 0)
            {
                return false;
            }

            var at = from + i;
            var before = at == 0 ? (byte)0 : haystack[at - 1];
            var after = at + needle.Length < haystack.Length ? haystack[at + needle.Length] : (byte)0;
            if ((before == 0 || before == (byte)'_') && after == 0)
            {
                return true;
            }

            from = at + 1;
        }
    }

    [Fact]
    public void TheVersionedInterfaceAccessorsThisBindingTargetsAreExported()
    {
        Assert.SkipUnless(Redist is not null, $"set {SteamLibrary.RedistDirectoryVariable}");
        var names = BoundEntryPoints();
        foreach (var accessor in new[] { "SteamAPI_SteamUser_v023", "SteamAPI_SteamUserStats_v013", "SteamAPI_SteamFriends_v018",
                     "SteamAPI_SteamUtils_v010", "SteamAPI_SteamApps_v009", "SteamAPI_SteamRemoteStorage_v016" })
        {
            Assert.Contains(accessor, names);
        }
    }

    [Fact]
    public void LoadingTheRealLibraryAndStartingWithoutAClientFallsBackToNoOpServices()
    {
        Assert.SkipUnless(Redist is not null, $"set {SteamLibrary.RedistDirectoryVariable}");
        if (!(OperatingSystem.IsLinux() || OperatingSystem.IsWindows()))
        {
            Assert.Skip("The no-client message is checked against the Linux and Windows redistributables.");
        }

        using var store = SteamPlatform.Start(new SteamOptions());
        if (store.IsAvailable)
        {
            Assert.Skip("A Steam client is running on this machine; the absent-client path cannot be exercised.");
        }

        Assert.Equal("none", store.Name);
        Assert.False(store.RelaunchRequested);
        // The library's own documented failure for a missing client, not a crash. Where Steam is not installed at all (a Windows machine
        // without the registry entry for it) the library cannot find a client directory and says so with the generic failure.
        if (OperatingSystem.IsWindows())
        {
            Assert.Matches("NoSteamClient|FailedGeneric", store.UnavailableReason);
        }
        else
        {
            Assert.Contains("NoSteamClient", store.UnavailableReason);
            Assert.Contains("Steam is probably not running", store.UnavailableReason);
        }

        Assert.Equal(StoreResult.Unavailable, store.Achievements.Unlock("ACH_WIN"));
        Assert.Equal(StoreResult.Unavailable, store.Cloud.Write("a", [1]));
        store.Pump();
    }

    [Fact]
    public void ARealInitWithoutAClientReportsTheDocumentedResultAndMessage()
    {
        Assert.SkipUnless(Redist is not null, $"set {SteamLibrary.RedistDirectoryVariable}");
        if (!(OperatingSystem.IsLinux() || OperatingSystem.IsWindows()))
        {
            Assert.Skip("The no-client message is checked against the Linux and Windows redistributables.");
        }

        Assert.True(SteamLibrary.TryLoad(out _, out var failure), failure);
        var native = new SteamNativeApi();
        var status = native.Init(out var message);
        if (status == SteamInitStatus.Ok)
        {
            native.Shutdown();
            Assert.Skip("A Steam client is running on this machine.");
        }

        Assert.True(status == SteamInitStatus.NoSteamClient || (OperatingSystem.IsWindows() && status == SteamInitStatus.FailedGeneric), status.ToString());
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void AMissingLibraryIsReportedWithWhereItWasLookedFor()
    {
        var (handle, failure) = SteamLibrary.Load(["/nonexistent/dir/" + SteamLibrary.FileName], searchSystemPath: false);
        Assert.Equal(0, handle);
        Assert.Contains(SteamLibrary.FileName, failure);
        Assert.Contains("/nonexistent/dir", failure);
    }

    [Fact]
    public void ACorruptLibraryFileIsReportedNotThrown()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{SteamLibrary.FileName}");
        File.WriteAllBytes(file, [1, 2, 3, 4]);
        try
        {
            var (handle, failure) = SteamLibrary.Load([file], searchSystemPath: false);
            Assert.Equal(0, handle);
            Assert.NotNull(failure);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
