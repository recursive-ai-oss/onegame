using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>
/// What an Android game needs to sell things through Google Play: the Billing Library (Google's own Java code, which Play requires), the
/// one small Java class of ours that wraps it (embedded in the tool), and the library's few dependencies, shrunk into one
/// <c>classes.dex</c>. This runs at export time only; the libraries are build inputs fetched once from Google's Maven repository with pinned
/// hashes, never committed to the repository.
/// </summary>
internal static class PlayBillingKit
{
    /// <summary>The Billing Library version the bridge was written against.</summary>
    public const string BillingVersion = "9.1.0";

    private const string Repository = "https://dl.google.com/android/maven2/";

    // The Billing Library and the dependencies its code reaches at run time (its published pom lists more, such as the location services,
    // which the library never loads). Each file is pinned by SHA-256.
    private static readonly (string Path, string Sha256)[] Artifacts =
    [
        ("com/android/billingclient/billing/9.1.0/billing-9.1.0.aar", "d28286d4e4c18725510980de01211d9b72501e7146d96817fa0c9999af9b0d22"),
        ("androidx/core/core/1.15.0/core-1.15.0.aar", "432b85a1974076e14b487ece4a28c59a84f1b9efc3fc8be72cd7f05d32055e51"),
        ("androidx/activity/activity/1.2.3/activity-1.2.3.aar", "1dce0705c334a6b2ef03382418dc7586f4e57ee23817267b403ea8cfc36c824e"),
        ("com/google/android/datatransport/transport-api/3.0.0/transport-api-3.0.0.aar", "4e6983c0703b357df6f1c6ceacb1b5dfc2c5006a789c799fec2298b2b5337466"),
        ("com/google/android/datatransport/transport-runtime/3.1.8/transport-runtime-3.1.8.aar", "cb9353ef1791ae17097d878ca711e25a9c32cec9042adc49b00cadfee1a7290b"),
        ("com/google/android/datatransport/transport-backend-cct/3.1.8/transport-backend-cct-3.1.8.aar", "e17edd1ef7fd475c90baa4e39422332f27087d34bcb46cb48ce86af9a54a612e"),
    ];

    private const string Rules = """
        -keep class dev.onegame.store.PlayBridge { public static *; }
        -keep class com.android.billingclient.api.ProxyBillingActivity { *; }
        -keep class com.android.billingclient.api.ProxyBillingActivityV2 { *; }
        -keep class com.android.vending.billing.** { *; }
        -keep class com.google.android.apps.play.billingtestcompanion.aidl.** { *; }
        -dontwarn **
        -dontobfuscate
        """;

    /// <summary>The folder the libraries are kept in: <c>ONEGAME_PLAY_BILLING_DIR</c>, else <c>play-billing</c> under the user's local application data.</summary>
    public static string CacheDirectory() =>
        Environment.GetEnvironmentVariable("ONEGAME_PLAY_BILLING_DIR") is { Length: > 0 } dir
            ? dir
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "onegame", "play-billing");

    /// <summary>Writes the embedded Java wrapper source under <paramref name="work"/> and returns the source folder.</summary>
    public static string ExtractBridgeSources(string work)
    {
        var root = Directory.CreateDirectory(Path.Combine(work, "bridge-src")).FullName;
        var assembly = typeof(PlayBillingKit).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("java/", StringComparison.Ordinal)))
        {
            var target = Path.Combine(root, name["java/".Length..].Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var input = assembly.GetManifestResourceStream(name)!;
            using var output = File.Create(target);
            input.CopyTo(output);
        }

        return root;
    }

    /// <summary>
    /// Builds <c>classes.dex</c> into <paramref name="work"/> and returns its path. Fetches the libraries when they are not in the cache,
    /// and fails with a message that says what to install when a tool is missing.
    /// </summary>
    public static string BuildDex(AndroidSdk sdk, string javaSources, string work)
    {
        var jars = new List<string>();
        var cache = Directory.CreateDirectory(CacheDirectory()).FullName;
        foreach (var (path, sha) in Artifacts)
        {
            var file = Path.Combine(cache, Path.GetFileName(path));
            if (!File.Exists(file) || Hash(file) != sha)
            {
                Download(Repository + path, file, sha);
            }

            // An .aar is a zip whose classes.jar is the code.
            var jar = Path.Combine(work, Path.GetFileNameWithoutExtension(file) + ".jar");
            using var zip = ZipFile.OpenRead(file);
            (zip.GetEntry("classes.jar") ?? throw new CliException(ExitCodes.RuntimeError, "play_billing_package", $"{Path.GetFileName(file)} has no classes.jar.")).ExtractToFile(jar, true);
            jars.Add(jar);
        }

        var classes = Directory.CreateDirectory(Path.Combine(work, "bridge-classes")).FullName;
        var sources = Directory.GetFiles(javaSources, "*.java", SearchOption.AllDirectories);
        if (sources.Length == 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "play_billing_sources", $"No Java sources under {javaSources}.");
        }

        var separator = Path.PathSeparator;
        var javac = Tool("javac");
        Run(javac, ["--release", "8", "-Xlint:-options", "-cp", sdk.AndroidJar + separator + string.Join(separator, jars), "-d", classes, .. sources], work, "javac_failed");

        // R8 reads class files from jars, so the bridge's classes travel as one.
        var bridge = Path.Combine(work, "bridge.jar");
        ZipFile.CreateFromDirectory(classes, bridge);
        var rules = Path.Combine(work, "play-billing.pro");
        File.WriteAllText(rules, Rules);
        var billingRules = Path.Combine(work, "billing-consumer.pro");
        using (var zip = ZipFile.OpenRead(Path.Combine(cache, "billing-" + BillingVersion + ".aar")))
        {
            zip.GetEntry("proguard.txt")?.ExtractToFile(billingRules, true);
        }

        var output = Directory.CreateDirectory(Path.Combine(work, "dex")).FullName;
        List<string> r8 = ["-cp", Path.Combine(sdk.BuildTools, "lib", "d8.jar"), "com.android.tools.r8.R8", "--release", "--min-api", "26", "--lib", sdk.AndroidJar, "--pg-conf", rules];
        if (File.Exists(billingRules))
        {
            r8.AddRange(["--pg-conf", billingRules]);
        }

        r8.AddRange(["--output", output, bridge, .. jars]);
        Run(Tool("java"), r8, work, "r8_failed");

        var dex = Path.Combine(output, "classes.dex");
        return File.Exists(dex) ? dex : throw new CliException(ExitCodes.RuntimeError, "r8_failed", "R8 did not produce classes.dex.");
    }

    private static string Tool(string name)
    {
        var java = Environment.GetEnvironmentVariable("JAVA_HOME");
        return !string.IsNullOrEmpty(java) && File.Exists(Path.Combine(java, "bin", name)) ? Path.Combine(java, "bin", name) : name;
    }

    private static void Run(string file, IEnumerable<string> args, string dir, string code)
    {
        var run = Proc.Run(file, args, dir);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, code, $"{Path.GetFileName(file)} failed (exit {run.ExitCode}): {string.Join(" | ", (run.Output + "\n" + run.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(6))}");
        }
    }

    private static string Hash(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static void Download(string url, string target, string sha256)
    {
        byte[] data;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            data = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CliException(ExitCodes.RuntimeError, "play_billing_download",
                $"Could not fetch {url}: {ex.Message}. Put {Path.GetFileName(target)} (SHA-256 {sha256}) in {Path.GetDirectoryName(target)} (set ONEGAME_PLAY_BILLING_DIR to use another folder) and run the export again.");
        }

        if (Convert.ToHexStringLower(SHA256.HashData(data)) != sha256)
        {
            throw new CliException(ExitCodes.RuntimeError, "play_billing_hash", $"{url} does not match the pinned SHA-256 {sha256}; refusing to use it.");
        }

        File.WriteAllBytes(target, data);
    }
}
