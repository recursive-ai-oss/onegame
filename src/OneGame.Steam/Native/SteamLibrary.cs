using System.Runtime.InteropServices;

namespace OneGame.Steam.Native;

/// <summary>Finds and loads the Steam redistributable. Search order: the file named by <c>ONEGAME_STEAM_LIBRARY</c>; the
/// executable's directory; the current directory; a redistributable tree named by <c>ONEGAME_STEAM_REDIST</c> (laid out as
/// <c>linux-x64/</c>, <c>win-x64/</c>, <c>osx/</c>, used by tests and development); then the operating system's own search path.
/// Loading never throws: a missing library is reported as a reason string.</summary>
internal static class SteamLibrary
{
    /// <summary>Environment variable holding the full path of the library file.</summary>
    internal const string LibraryPathVariable = "ONEGAME_STEAM_LIBRARY";

    /// <summary>Environment variable holding a directory of redistributables, one folder per platform.</summary>
    internal const string RedistDirectoryVariable = "ONEGAME_STEAM_REDIST";

    private static readonly Lock Gate = new();
    private static nint _handle;
    private static string? _failure;
    private static bool _tried;

    /// <summary>The library file name Steam ships for this operating system.</summary>
    internal static string FileName =>
        OperatingSystem.IsWindows() ? "steam_api64.dll" :
        OperatingSystem.IsMacOS() ? "libsteam_api.dylib" :
        "libsteam_api.so";

    /// <summary>The folder name of this platform inside a redistributable tree.</summary>
    internal static string PlatformFolder =>
        OperatingSystem.IsWindows() ? "win-x64" :
        OperatingSystem.IsMacOS() ? "osx" :
        "linux-x64";

    /// <summary>Loads the library once and returns its handle, or zero with the reason it could not be loaded.</summary>
    internal static bool TryLoad(out nint handle, out string? failure)
    {
        lock (Gate)
        {
            if (!_tried)
            {
                _tried = true;
                (_handle, _failure) = Load(Candidates(), searchSystemPath: true);
                if (_handle != 0)
                {
                    NativeLibrary.SetDllImportResolver(typeof(SteamLibrary).Assembly, Resolve);
                }
            }

            handle = _handle;
            failure = _failure;
            return _handle != 0;
        }
    }

    private static nint Resolve(string name, System.Reflection.Assembly assembly, DllImportSearchPath? path) =>
        name == SteamApi.Library ? _handle : 0;

    internal static (nint Handle, string? Failure) Load(IEnumerable<string> candidates, bool searchSystemPath)
    {
        if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) || !Environment.Is64BitProcess)
        {
            return (0, "Steam is only supported on 64-bit Windows, Linux and macOS.");
        }

        var tried = new List<string>();
        foreach (var candidate in candidates)
        {
            tried.Add(candidate);
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
            {
                return (handle, null);
            }
        }

        if (searchSystemPath && NativeLibrary.TryLoad(FileName, out var system))
        {
            return (system, null);
        }

        return (0, $"The Steam library '{FileName}' was not found (looked in: {string.Join(", ", tried)}).");
    }

    private static IEnumerable<string> Candidates()
    {
        var explicitPath = Environment.GetEnvironmentVariable(LibraryPathVariable);
        if (!string.IsNullOrEmpty(explicitPath))
        {
            yield return explicitPath;
        }

        yield return Path.Combine(AppContext.BaseDirectory, FileName);
        yield return Path.Combine(Environment.CurrentDirectory, FileName);

        var redist = Environment.GetEnvironmentVariable(RedistDirectoryVariable);
        if (!string.IsNullOrEmpty(redist))
        {
            yield return Path.Combine(redist, PlatformFolder, FileName);
        }
    }
}
