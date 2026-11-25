using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>The key an Android export is signed with, and the child-process environment that hands its passwords to the signing tools.</summary>
internal sealed record AndroidSigning(string Keystore, string Alias, bool IsDebug, string StorePassword, string KeyPassword)
{
    internal const string StoreVariable = "ONEGAME_SIGN_STORE_PASS";
    internal const string KeyVariable = "ONEGAME_SIGN_KEY_PASS";

    /// <summary>The variables the signing tools read their passwords from. Passwords never go on a command line, where other processes can read them.</summary>
    public IReadOnlyDictionary<string, string> ToolEnvironment => new Dictionary<string, string> { [StoreVariable] = StorePassword, [KeyVariable] = KeyPassword };

    /// <summary>Removes the passwords from text that may be shown to the user.</summary>
    public string Redact(string text)
    {
        foreach (var secret in new[] { StorePassword, KeyPassword })
        {
            if (secret.Length > 0)
            {
                text = text.Replace(secret, "***", StringComparison.Ordinal);
            }
        }

        return text;
    }
}

internal sealed partial class ExportVerb
{
    private const string BundletoolVersion = "1.18.1";
    private const string BundletoolSha256 = "675786493983787ffa11550bdb7c0715679a44e1643f3ff980a529e9c822595c";

    /// <summary>
    /// Chooses the signing key. A development build uses the debug key unless a keystore is given; any other build needs the game's own keystore
    /// (<c>--keystore</c> and <c>--key-alias</c>, or <c>ONEGAME_ANDROID_KEYSTORE</c> and <c>ONEGAME_ANDROID_KEY_ALIAS</c>) with its passwords in
    /// <c>ONEGAME_ANDROID_STORE_PASSWORD</c> and <c>ONEGAME_ANDROID_KEY_PASSWORD</c> (default: the store password), or an explicit <c>--debug-key</c>.
    /// </summary>
    internal static AndroidSigning ResolveSigning(string? keystore, string? alias, bool dev, bool debugKey, Func<string, string?> env, Func<string> debugKeystore)
    {
        keystore ??= env("ONEGAME_ANDROID_KEYSTORE");
        alias ??= env("ONEGAME_ANDROID_KEY_ALIAS");
        if (debugKey && !string.IsNullOrEmpty(keystore))
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "--debug-key and --keystore ask for different keys. Use one.");
        }

        if (string.IsNullOrEmpty(keystore))
        {
            if (!dev && !debugKey)
            {
                throw new CliException(ExitCodes.Usage, "keystore_required",
                    "A release export needs the game's signing key: pass --keystore <file> and --key-alias <name> (or set ONEGAME_ANDROID_KEYSTORE and ONEGAME_ANDROID_KEY_ALIAS), with the passwords in "
                    + "ONEGAME_ANDROID_STORE_PASSWORD and ONEGAME_ANDROID_KEY_PASSWORD (default: the store password). For a build that only has to install on a test device, add --dev or --debug-key.");
            }

            return new AndroidSigning(debugKeystore(), "androiddebugkey", true, "android", "android");
        }

        if (!File.Exists(keystore))
        {
            throw new CliException(ExitCodes.Usage, "keystore_missing", $"The keystore {keystore} does not exist.");
        }

        if (string.IsNullOrEmpty(alias))
        {
            throw new CliException(ExitCodes.Usage, "key_alias_required", "A keystore needs the key's name: pass --key-alias <name> or set ONEGAME_ANDROID_KEY_ALIAS.");
        }

        var store = env("ONEGAME_ANDROID_STORE_PASSWORD");
        if (string.IsNullOrEmpty(store))
        {
            throw new CliException(ExitCodes.Usage, "keystore_password_required", "Set ONEGAME_ANDROID_STORE_PASSWORD to the keystore's password (and ONEGAME_ANDROID_KEY_PASSWORD if the key has its own). Passwords are never taken from the command line.");
        }

        var key = env("ONEGAME_ANDROID_KEY_PASSWORD");
        return new AndroidSigning(Path.GetFullPath(keystore), alias, false, store, string.IsNullOrEmpty(key) ? store : key);
    }

    /// <summary>The version code from <c>--version-code</c> or the project, else derived from the version; Google Play accepts 1 to 2100000000 and refuses a repeat.</summary>
    internal static int ResolveVersionCode(string? text, string version)
    {
        if (string.IsNullOrEmpty(text))
        {
            return VersionCode(version);
        }

        if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var code) || code is < 1 or > 2_100_000_000)
        {
            throw new CliException(ExitCodes.Usage, "bad_version_code", $"'{text}' is not a version code (a whole number from 1 to 2100000000).");
        }

        return code;
    }

    /// <summary>Signs an APK (already zip-aligned) and checks the result; returns the signing certificate's SHA-256.</summary>
    private static string SignApk(AndroidSdk sdk, AndroidSigning signing, string input, string output, string work)
    {
        string[] common = ["--ks", signing.Keystore, "--ks-key-alias", signing.Alias, "--ks-pass", "env:" + AndroidSigning.StoreVariable, "--key-pass", "env:" + AndroidSigning.KeyVariable];
        RunSecretTool(sdk.BuildTool("apksigner"), ["sign", .. common, "--out", output, input], work, "apksigner_failed", signing);
        var verify = Proc.Run(sdk.BuildTool("apksigner"), ["verify", "--print-certs", output], work);
        if (verify.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "apksigner_verify_failed", $"apksigner verify failed: {signing.Redact(verify.Output + verify.Error).Trim()}");
        }

        foreach (var line in verify.Output.Split('\n'))
        {
            var at = line.IndexOf("certificate SHA-256 digest:", StringComparison.Ordinal);
            if (at >= 0)
            {
                return line[(at + "certificate SHA-256 digest:".Length)..].Trim().ToLowerInvariant();
            }
        }

        throw new CliException(ExitCodes.RuntimeError, "apksigner_verify_failed", "apksigner verify --print-certs did not print the signer's certificate.");
    }

    private static void RunSecretTool(string file, IEnumerable<string> args, string dir, string code, AndroidSigning signing)
    {
        var run = Proc.Run(file, args, dir, environment: signing.ToolEnvironment);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, code, $"{Path.GetFileName(file)} failed (exit {run.ExitCode}): {signing.Redact(run.Output + run.Error).Trim()}");
        }
    }

    /// <summary>
    /// Turns the linked APK (binary resources) into an Android App Bundle: aapt2 converts the resources to the bundle's protobuf form, the
    /// parts are laid out as the base module, <c>bundletool build-bundle</c> packs them with ABI splits and uncompressed, page-aligned native
    /// libraries, and <c>jarsigner</c> signs the bundle with the game's key (Play re-signs with its own key for devices).
    /// </summary>
    private static void BuildBundle(AndroidSdk sdk, string linkedApk, string bundletool, AndroidSigning signing, string output, string work)
    {
        var proto = Path.Combine(work, "proto.apk");
        RunTool(sdk.BuildTool("aapt2"), ["convert", "--output-format", "proto", "-o", proto, linkedApk], work, "aapt2_failed");

        var extracted = Path.Combine(work, "bundle-x");
        var module = Path.Combine(work, "bundle-base");
        ZipFile.ExtractToDirectory(proto, extracted);
        Directory.CreateDirectory(Path.Combine(module, "manifest"));
        File.Move(Path.Combine(extracted, "AndroidManifest.xml"), Path.Combine(module, "manifest", "AndroidManifest.xml"));
        File.Move(Path.Combine(extracted, "resources.pb"), Path.Combine(module, "resources.pb"));
        foreach (var name in new[] { "res", "assets", "lib" })
        {
            if (Directory.Exists(Path.Combine(extracted, name)))
            {
                Directory.Move(Path.Combine(extracted, name), Path.Combine(module, name));
            }
        }

        var dexFolder = Path.Combine(module, "dex");
        foreach (var dex in Directory.GetFiles(extracted, "classes*.dex"))
        {
            Directory.CreateDirectory(dexFolder);
            File.Move(dex, Path.Combine(dexFolder, Path.GetFileName(dex)));
        }

        var moduleZip = Path.Combine(work, "base.zip");
        ZipFile.CreateFromDirectory(module, moduleZip, CompressionLevel.Optimal, includeBaseDirectory: false);

        // Split by ABI only: the game has no translated or density-specific resources. Libraries stay uncompressed and 16 KiB aligned so
        // devices map them straight from the package (Android 15 on 16 KiB-page devices needs that alignment).
        var config = Path.Combine(work, "bundle-config.json");
        File.WriteAllText(config, """
            {"optimizations":{"splitsConfig":{"splitDimension":[{"value":"ABI","negate":false},{"value":"LANGUAGE","negate":true},{"value":"SCREEN_DENSITY","negate":true}]},
            "uncompressNativeLibraries":{"enabled":true,"alignment":"PAGE_ALIGNMENT_16K"}}}
            """, new UTF8Encoding(false));

        File.Delete(output);
        var run = Proc.Run(JavaTool("java"), ["-jar", bundletool, "build-bundle", "--modules=" + moduleZip, "--config=" + config, "--output=" + output], work);
        if (run.ExitCode != 0 || !File.Exists(output))
        {
            throw new CliException(ExitCodes.RuntimeError, "bundletool_failed", $"bundletool build-bundle failed (exit {run.ExitCode}): {string.Join(" | ", (run.Output + "\n" + run.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(6))}");
        }

        RunSecretTool(JavaTool("jarsigner"), ["-digestalg", "SHA-256", "-keystore", signing.Keystore, "-storepass:env", AndroidSigning.StoreVariable, "-keypass:env", AndroidSigning.KeyVariable, output, signing.Alias], work, "jarsigner_failed", signing);
        var verify = Proc.Run(JavaTool("jarsigner"), ["-verify", output], work);
        if (verify.ExitCode != 0 || !verify.Output.Contains("jar verified", StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.RuntimeError, "jarsigner_verify_failed", $"The signed bundle does not verify: {(verify.Output + verify.Error).Trim()}");
        }
    }

    /// <summary>The SHA-256 of the certificate a bundle is signed with, read with <c>keytool</c>, lower-case hex.</summary>
    private static string BundleCertificate(string bundle, string work)
    {
        var run = Proc.Run(JavaTool("keytool"), ["-printcert", "-jarfile", bundle], work);
        foreach (var line in run.Output.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("SHA256:", StringComparison.Ordinal))
            {
                return t["SHA256:".Length..].Replace(":", "", StringComparison.Ordinal).Trim().ToLowerInvariant();
            }
        }

        throw new CliException(ExitCodes.RuntimeError, "keytool_failed", "keytool -printcert did not print the bundle's certificate: " + (run.Output + run.Error).Trim());
    }

    private static string JavaTool(string name)
    {
        var java = Environment.GetEnvironmentVariable("JAVA_HOME");
        return !string.IsNullOrEmpty(java) && File.Exists(Path.Combine(java, "bin", name)) ? Path.Combine(java, "bin", name) : name;
    }

    /// <summary>
    /// Finds Google's bundletool (the program Play itself uses to build device APKs from a bundle): <c>--bundletool</c>, <c>ONEGAME_BUNDLETOOL</c>,
    /// <c>BUNDLETOOL</c>, a <c>bundletool*.jar</c> in the Android SDK folder, or the pinned release kept in the tool's cache, fetched on first use
    /// and checked against its SHA-256. It is a build-time tool and nothing from it ships.
    /// </summary>
    internal static string FindBundletool(string? option, AndroidSdk sdk)
    {
        foreach (var candidate in new[] { option, Environment.GetEnvironmentVariable("ONEGAME_BUNDLETOOL"), Environment.GetEnvironmentVariable("BUNDLETOOL") })
        {
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            return File.Exists(candidate) ? Path.GetFullPath(candidate)
                : throw new CliException(ExitCodes.Usage, "bundletool_missing", $"bundletool was named as {candidate}, which is not a file.");
        }

        if (Directory.GetFiles(sdk.Home, "bundletool*.jar").OrderDescending().FirstOrDefault() is { } inSdk)
        {
            return inSdk;
        }

        var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "onegame", "bundletool");
        var jar = Path.Combine(cache, $"bundletool-all-{BundletoolVersion}.jar");
        if (File.Exists(jar) && FileHash(jar) == BundletoolSha256)
        {
            return jar;
        }

        var url = $"https://github.com/google/bundletool/releases/download/{BundletoolVersion}/bundletool-all-{BundletoolVersion}.jar";
        byte[] data;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            data = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CliException(ExitCodes.RuntimeError, "bundletool_download",
                $"Could not fetch {url}: {ex.Message}. Download bundletool-all-{BundletoolVersion}.jar (SHA-256 {BundletoolSha256}) and pass it as --bundletool <file> or set ONEGAME_BUNDLETOOL.");
        }

        if (Convert.ToHexStringLower(SHA256.HashData(data)) != BundletoolSha256)
        {
            throw new CliException(ExitCodes.RuntimeError, "bundletool_hash", $"{url} does not match the pinned SHA-256 {BundletoolSha256}; refusing to use it.");
        }

        Directory.CreateDirectory(cache);
        File.WriteAllBytes(jar, data);
        return jar;
    }

    private static string FileHash(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>
    /// The smallest alignment among an ELF64 library's loadable segments, or null when the file is not a little-endian ELF64. Android 15
    /// devices with 16 KiB pages map segments at page boundaries, so Play requires at least 16384 for apps that target Android 15 and later.
    /// </summary>
    internal static long? LoadSegmentAlignment(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> head = stackalloc byte[64];
        if (stream.Read(head) < 64 || !head[..4].SequenceEqual(new byte[] { 0x7f, (byte)'E', (byte)'L', (byte)'F' }) || head[4] != 2 || head[5] != 1)
        {
            return null;
        }

        var phoff = BitConverter.ToInt64(head[32..]);
        var phentsize = BitConverter.ToUInt16(head[54..]);
        var phnum = BitConverter.ToUInt16(head[56..]);
        if (phentsize < 56 || phnum == 0 || phoff <= 0 || phoff + ((long)phentsize * phnum) > stream.Length)
        {
            return null;
        }

        long? smallest = null;
        var entry = new byte[phentsize];
        for (var i = 0; i < phnum; i++)
        {
            stream.Position = phoff + ((long)i * phentsize);
            stream.ReadExactly(entry);
            if (BitConverter.ToUInt32(entry, 0) == 1)
            {
                var align = BitConverter.ToInt64(entry, 48);
                smallest = smallest is null ? align : Math.Min(smallest.Value, align);
            }
        }

        return smallest;
    }

    /// <summary>
    /// The <c>application</c> meta-data entries the Billing Library's own manifest declares, read from the library at export time so they cannot
    /// drift from the version that is bundled. Play reads the library version from <c>com.google.android.play.billingclient.version</c>.
    /// </summary>
    internal static (string Name, string Value)[] BillingMetaData()
    {
        var aar = Path.Combine(PlayBillingKit.CacheDirectory(), "billing-" + PlayBillingKit.BillingVersion + ".aar");
        using var zip = ZipFile.OpenRead(aar);
        var entry = zip.GetEntry("AndroidManifest.xml") ?? throw new CliException(ExitCodes.RuntimeError, "play_billing_package", "billing-" + PlayBillingKit.BillingVersion + ".aar has no AndroidManifest.xml.");
        using var stream = entry.Open();
        var ns = XNamespace.Get("http://schemas.android.com/apk/res/android");
        var doc = XDocument.Load(stream);
        var found = doc.Root?.Element("application")?.Elements("meta-data")
            .Select(m => (Name: (string?)m.Attribute(ns + "name") ?? "", Value: (string?)m.Attribute(ns + "value") ?? ""))
            .Where(m => m.Name.Length > 0)
            .ToArray() ?? [];
        return found.Any(m => m.Name == BillingVersionKey && m.Value == PlayBillingKit.BillingVersion)
            ? found
            : throw new CliException(ExitCodes.RuntimeError, "play_billing_package", $"The Billing Library's manifest does not declare {BillingVersionKey} {PlayBillingKit.BillingVersion}.");
    }

    /// <summary>The meta-data name Google Play reads the Billing Library version from.</summary>
    internal const string BillingVersionKey = "com.google.android.play.billingclient.version";

    /// <summary>Zips the debug files of each ABI into the layout Play's crash reporting reads: <c>&lt;abi&gt;/lib&lt;name&gt;.so.dbg</c>.</summary>
    private static string WriteSymbolsArchive(string symbolsFolder, string library, string[] abis)
    {
        var path = Path.Combine(symbolsFolder, "native-debug-symbols.zip");
        File.Delete(path);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var abi in abis)
        {
            var dbg = Path.Combine(symbolsFolder, abi, library + ".so.dbg");
            if (File.Exists(dbg))
            {
                zip.CreateEntryFromFile(dbg, $"{abi}/lib{library}.so.dbg", CompressionLevel.Optimal);
            }
        }

        return path;
    }
}
