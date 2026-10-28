using OneGame.Core;

namespace OneGame.Cli.Verbs;

/// <summary>
/// Development keys for the commands that run a server or connect to one, so local play needs no key ceremony. The keys are made on
/// first use in a per-user folder outside every project (never in a game folder, so no export or commit takes them), marked as
/// development keys, and handed over exactly the way a pinned key would be: the server gets <c>--key</c>, the client <c>--server-key</c>
/// and <c>--identity</c>. A real server is run with a key from <c>onegame keys</c>; a real client pins that server's public key.
/// </summary>
internal static class DevKeys
{
    private const string Scope = "default";

    private static string Folder => KeyStore.DevelopmentFolder(Scope);

    /// <summary>Adds <c>--key</c> to a server's arguments unless it already has a key, a configuration with a security section, or <c>--insecure</c>.</summary>
    public static void AddServerArgs(List<string> args)
    {
        if (args.Contains("--key") || args.Contains("--insecure") || ConfigHasSecurity(args))
        {
            return;
        }

        args.AddRange(["--key", KeyStore.Ensure(Folder, "server", development: true).PrivateFile]);
    }

    /// <summary>Adds <c>--server-key</c> and <c>--identity</c> to a game's arguments when it connects to a server and was not given its own.</summary>
    public static void AddClientArgs(List<string> args)
    {
        if (!args.Any(a => a == "--connect" || a.StartsWith("--connect=", StringComparison.Ordinal)) || args.Contains("--insecure"))
        {
            return;
        }

        if (!args.Any(a => a == "--server-key" || a.StartsWith("--server-key=", StringComparison.Ordinal)))
        {
            args.AddRange(["--server-key", KeyStore.Ensure(Folder, "server", development: true).PublicFile]);
        }

        if (!args.Any(a => a == "--identity" || a.StartsWith("--identity=", StringComparison.Ordinal)))
        {
            args.AddRange(["--identity", KeyStore.Ensure(Folder, "player", development: true).PrivateFile]);
        }
    }

    private static bool ConfigHasSecurity(List<string> args)
    {
        var i = args.IndexOf("--config");
        if (i < 0 || i + 1 >= args.Count)
        {
            return false;
        }

        try
        {
            return File.ReadLines(args[i + 1]).Any(l => l.StartsWith("security:", StringComparison.Ordinal));
        }
        catch (IOException)
        {
            return false;
        }
    }
}
