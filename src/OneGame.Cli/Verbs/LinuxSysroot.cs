using System.Net.Http;
using OneGame.Host;
using System.Security.Cryptography;

namespace OneGame.Cli.Verbs;

/// <summary>
/// The C library a Linux NativeAOT export links against. Steam runs Linux games in its Steam Linux Runtime 3 ("sniper", Debian 11, glibc 2.31),
/// and a program linked on a newer system asks for symbols that runtime lacks. The export therefore links against glibc 2.31 files, fetched once
/// from Debian's snapshot archive (pinned by SHA-256) into a per-user cache. They are a build tool: only the program's own symbol references
/// are taken from them, and nothing from them is copied into the game.
/// </summary>
internal static class LinuxSysroot
{
    /// <summary>The glibc the baseline runtime provides.</summary>
    public static readonly Version Baseline = new(2, 31);

    private const string Layout = "debian11-glibc2.31-1";

    // Debian 11 (bullseye) binaries from snapshot.debian.org, addressed by the archive's own file hash.
    private static readonly (string Name, string Sha1, string Sha256)[] Packages =
    [
        ("libc6_2.31-13+deb11u13_amd64.deb", "429f4b15a81de688b2a4cfa46a2c9f6705baf8a8", "22259b07ee4d6bc97fad5a828a9db5684e1d77db71464c4e3862ddef7d65310e"),
        ("libc6-dev_2.31-13+deb11u13_amd64.deb", "247ecad434d96644fdbd160f9bf79b85af6f6ae4", "c156953c41d1034e1b6ce186497542569509b0c5aeefa8b8681319d536474421"),
    ];

    private const long MaxDownload = 16 * 1024 * 1024;

    /// <summary>The folder that holds the cached sysroots: <c>$ONEGAME_CACHE</c>, else the user's cache folder.</summary>
    public static string CacheRoot()
    {
        var env = Environment.GetEnvironmentVariable("ONEGAME_CACHE");
        if (!string.IsNullOrEmpty(env))
        {
            return Path.GetFullPath(env);
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        var baseDir = !string.IsNullOrEmpty(xdg) ? xdg : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
        return Path.Combine(baseDir, "onegame");
    }

    /// <summary>Makes sure the baseline sysroot is in the cache (fetching it the first time) and returns its folder.</summary>
    /// <param name="log">Where progress lines go.</param>
    /// <returns>The sysroot folder.</returns>
    public static string Ensure(Action<string> log)
    {
        var root = Path.Combine(CacheRoot(), "sysroot", Layout);
        if (File.Exists(Path.Combine(root, ".complete")))
        {
            return root;
        }

        log($"Fetching the glibc {Baseline.Major}.{Baseline.Minor} build sysroot once into {root} (a build tool; nothing from it ships). Use --host-glibc to skip it.");
        var work = root + ".tmp" + Environment.ProcessId;
        try
        {
            if (Directory.Exists(work))
            {
                Directory.Delete(work, true);
            }

            Directory.CreateDirectory(work);
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            foreach (var (name, sha1, sha256) in Packages)
            {
                var deb = Download(http, $"https://snapshot.debian.org/file/{sha1}", sha256, name);
                ExtractDeb(deb, work, name);
            }

            RelinkAbsoluteLinks(work);
            File.WriteAllText(Path.Combine(work, ".complete"), Layout + "\n");
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(root)!);
            Directory.Move(work, root);
            return root;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            throw new CliException(ExitCodes.BuildFailed, "sysroot_unavailable",
                $"Could not prepare the glibc {Baseline.Major}.{Baseline.Minor} build sysroot: {e.Message} Retry with a network connection, or pass --host-glibc to link against this system's own C library (the program then runs only on systems at least as new).");
        }
        finally
        {
            if (Directory.Exists(work))
            {
                try
                {
                    Directory.Delete(work, true);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    /// <summary>MSBuild arguments that make a NativeAOT publish link against <paramref name="root"/> instead of the host's C library.</summary>
    /// <param name="root">A sysroot folder (from <see cref="Ensure"/>, or one the caller names).</param>
    /// <returns>Arguments for <c>dotnet publish</c>.</returns>
    public static string[] PublishArguments(string root)
    {
        // gcc and clang look for the C start files and libraries in the host's own folders whatever --sysroot says (a distribution's gcc
        // finds them relative to itself), so the sysroot's folders are named first; --sysroot makes the linker scripts inside resolve in it.
        var lib = Path.Combine(root, "usr", "lib", "x86_64-linux-gnu");
        var lib2 = Path.Combine(root, "lib", "x86_64-linux-gnu");
        // The file lives in the temp folder, named by the sysroot, so a read-only or shared sysroot is never written to.
        var file = Path.Combine(Path.GetTempPath(), "onegame-link-" + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(root)))[..16] + ".targets");
        File.WriteAllText(file,
            $"""
            <Project>
              <ItemGroup>
                <LinkerArg Include="-B&quot;{lib}/&quot;" />
                <LinkerArg Include="-L&quot;{lib}&quot;" />
                <LinkerArg Include="-L&quot;{lib2}&quot;" />
              </ItemGroup>
            </Project>

            """);
        return [$"-p:SysRoot={root}", $"-p:CustomAfterMicrosoftCommonTargets={file}"];
    }

    private static string Download(HttpClient http, string url, string sha256, string name)
    {
        using var response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxDownload)
        {
            throw new InvalidDataException($"{name} is larger than expected.");
        }

        using var stream = response.Content.ReadAsStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int n;
        while ((n = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            buffer.Write(chunk, 0, n);
            if (buffer.Length > MaxDownload)
            {
                throw new InvalidDataException($"{name} is larger than expected.");
            }
        }

        var bytes = buffer.ToArray();
        if (!Convert.ToHexStringLower(SHA256.HashData(bytes)).Equals(sha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{name} does not match its pinned SHA-256.");
        }

        return SaveTemp(bytes, name);
    }

    private static string SaveTemp(byte[] bytes, string name)
    {
        var path = Path.Combine(Path.GetTempPath(), "onegame-" + Environment.ProcessId + "-" + name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>Unpacks the data member of a .deb (an ar archive holding data.tar.xz) with the system's tar.</summary>
    private static void ExtractDeb(string deb, string into, string name)
    {
        var tarball = deb + ".data.tar.xz";
        try
        {
            using (var input = File.OpenRead(deb))
            {
                var magic = new byte[8];
                input.ReadExactly(magic);
                if (!magic.AsSpan().SequenceEqual("!<arch>\n"u8))
                {
                    throw new InvalidDataException($"{name} is not an ar archive.");
                }

                var header = new byte[60];
                var found = false;
                while (input.Position + 60 <= input.Length)
                {
                    input.ReadExactly(header);
                    var member = System.Text.Encoding.ASCII.GetString(header, 0, 16).Trim();
                    if (!long.TryParse(System.Text.Encoding.ASCII.GetString(header, 48, 10).Trim(), out var size) || size < 0 || input.Position + size > input.Length)
                    {
                        throw new InvalidDataException($"{name} has a damaged ar header.");
                    }

                    if (member.StartsWith("data.tar.xz", StringComparison.Ordinal))
                    {
                        using var output = File.Create(tarball);
                        var left = size;
                        var chunk = new byte[81920];
                        while (left > 0)
                        {
                            var read = input.Read(chunk, 0, (int)Math.Min(chunk.Length, left));
                            if (read <= 0)
                            {
                                throw new InvalidDataException($"{name} ends early.");
                            }

                            output.Write(chunk, 0, read);
                            left -= read;
                        }

                        found = true;
                        break;
                    }

                    input.Position += size + (size & 1);
                }

                if (!found)
                {
                    throw new InvalidDataException($"{name} has no data.tar.xz.");
                }
            }

            ProcessResult run;
            try
            {
                run = Proc.Run("tar", ["-xJf", tarball, "-C", into, "--no-same-owner"], into);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                throw new IOException("the 'tar' program (with xz support) is needed to unpack it.");
            }

            if (run.ExitCode != 0)
            {
                throw new IOException("unpacking failed: " + (run.Error.Length > 0 ? run.Error.Trim() : run.Output.Trim()));
            }
        }
        finally
        {
            File.Delete(deb);
            File.Delete(tarball);
        }
    }

    /// <summary>A package's links point at absolute paths (/lib/...); the linker would follow them out of the sysroot into the host, so they become relative.</summary>
    private static void RelinkAbsoluteLinks(string root)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories).ToArray())
        {
            var info = new FileInfo(path);
            if (info.LinkTarget is { } target && target.StartsWith('/'))
            {
                var relative = Path.GetRelativePath(Path.GetDirectoryName(path)!, Path.Combine(root, target.TrimStart('/')));
                info.Delete();
                File.CreateSymbolicLink(path, relative);
            }
        }
    }
}
