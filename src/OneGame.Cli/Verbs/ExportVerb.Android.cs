using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>The Android SDK and NDK pieces the tool uses at build time: where they are, and how to run them.</summary>
internal sealed class AndroidSdk
{
    private AndroidSdk(string home, string buildTools, string androidJar, string ndk)
    {
        Home = home;
        BuildTools = buildTools;
        AndroidJar = androidJar;
        Ndk = ndk;
    }

    public string Home { get; }

    public string BuildTools { get; }

    public string AndroidJar { get; }

    public string Ndk { get; }

    /// <summary>The platform-tools folder holding <c>adb</c>.</summary>
    public string PlatformTools => Path.Combine(Home, "platform-tools");

    public string Adb => File.Exists(Path.Combine(PlatformTools, "adb")) ? Path.Combine(PlatformTools, "adb") : "adb";

    /// <summary>Finds the SDK from <paramref name="option"/>, <c>ANDROID_HOME</c> or <c>ANDROID_SDK_ROOT</c> and the newest build tools, platform and NDK in it.</summary>
    public static AndroidSdk Find(string? option, bool needNdk = true)
    {
        var home = option ?? Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (string.IsNullOrEmpty(home) || !Directory.Exists(home))
        {
            throw new CliException(ExitCodes.RuntimeError, "android_sdk_missing",
                "The Android SDK was not found. Set ANDROID_HOME (or pass --sdk <folder>) to a folder with build-tools, platforms, platform-tools and an ndk folder; the command-line tools' sdkmanager installs them.");
        }

        var buildTools = Newest(Path.Combine(home, "build-tools"))
            ?? throw new CliException(ExitCodes.RuntimeError, "android_sdk_incomplete", $"{home} has no build-tools. Install them with: sdkmanager \"build-tools;35.0.0\".");
        var platform = Newest(Path.Combine(home, "platforms"), "android-")
            ?? throw new CliException(ExitCodes.RuntimeError, "android_sdk_incomplete", $"{home} has no platform. Install one with: sdkmanager \"platforms;android-35\".");
        var ndk = Environment.GetEnvironmentVariable("ANDROID_NDK") ?? Newest(Path.Combine(home, "ndk")) ?? "";
        if (needNdk && !File.Exists(Path.Combine(ndk, "toolchains", "llvm", "prebuilt", "linux-x86_64", "bin", "clang")))
        {
            throw new CliException(ExitCodes.RuntimeError, "android_ndk_missing", $"No Android NDK under {home}/ndk (or ANDROID_NDK). Install one with: sdkmanager \"ndk;27.2.12479018\". Cross-building for Android needs a Linux x86-64 host.");
        }

        return new AndroidSdk(home, buildTools, Path.Combine(platform, "android.jar"), ndk);
    }

    public string BuildTool(string name) => Path.Combine(BuildTools, name);

    // The highest version-numbered subfolder; "android-35" style names compare by their number.
    private static string? Newest(string folder, string prefix = "")
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        return Directory.EnumerateDirectories(folder)
            .Select(d => (Path: d, Version: ParseVersion(Path.GetFileName(d), prefix)))
            .Where(x => x.Version is not null)
            .OrderByDescending(x => x.Version)
            .Select(x => x.Path)
            .FirstOrDefault();
    }

    private static Version? ParseVersion(string name, string prefix)
    {
        if (!name.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var text = name[prefix.Length..];
        if (!text.Contains('.', StringComparison.Ordinal))
        {
            text += ".0";
        }

        return Version.TryParse(text, out var v) ? v : null;
    }
}

/// <summary>What <c>onegame export --target android</c> printed.</summary>
internal sealed record AndroidExportResult(
    bool Ok, string Game, string GameVersion, string EngineVersion, string Target, string Package, string Apk, long Size, string Sha256, string[] Abis,
    bool DevTools, bool Debuggable, string Signing, ExportFile[] Libraries, ExportWarning[] Warnings, double Seconds);

internal sealed partial class ExportVerb
{
    // Android ABI name to .NET runtime identifier.
    private static readonly (string Abi, string Rid)[] AndroidAbis = [("arm64-v8a", "linux-bionic-arm64"), ("x86_64", "linux-bionic-x64")];

    private static int RunAndroidExport(ProjectArgs a, bool dev, bool noAot, bool json, TextWriter output)
    {
        if (noAot)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "--no-aot does not apply to Android: games are NativeAOT shared libraries there.");
        }

        if (!OperatingSystem.IsLinux())
        {
            throw new CliException(ExitCodes.RuntimeError, "android_host", "Exporting for Android runs NativeAOT with the NDK's Linux toolchain, so it needs a Linux x86-64 host.");
        }

        var sdk = AndroidSdk.Find(a.Get("--sdk"));
        var abis = (a.Get("--abi") ?? string.Join(',', AndroidAbis.Select(x => x.Abi))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var abi in abis)
        {
            if (Array.FindIndex(AndroidAbis, x => x.Abi == abi) < 0)
            {
                throw new CliException(ExitCodes.Usage, "bad_abi", $"Unknown ABI '{abi}'. ABIs: {string.Join(", ", AndroidAbis.Select(x => x.Abi))}.");
            }
        }

        var project = Projects.Find(a.Get("--project"));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var props = ReadProperties(project);
        var package = a.Get("--package") ?? "dev.onegame." + new string([.. props.AssemblyName.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit)]);
        if (!System.Text.RegularExpressions.Regex.IsMatch(package, "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)+$"))
        {
            throw new CliException(ExitCodes.Usage, "bad_package", $"'{package}' is not an Android package name (lower-case words joined by dots, such as com.example.game).");
        }

        var label = a.Get("--label") ?? project.Name;
        var outRoot = Path.GetFullPath(a.Get("--out") ?? Path.Combine(project.Directory, "export"));
        var work = Path.Combine(outRoot, ".android-" + project.Name);
        if (Directory.Exists(work))
        {
            Directory.Delete(work, true);
        }

        Directory.CreateDirectory(work);

        var libraries = new List<(string Abi, string Path)>();
        foreach (var abi in abis)
        {
            var rid = AndroidAbis.First(x => x.Abi == abi).Rid;
            var folder = Path.Combine(work, abi);
            PublishAndroid(project, rid, folder, dev, sdk);
            var so = Path.Combine(folder, props.AssemblyName + ".so");
            if (!File.Exists(so))
            {
                throw new CliException(ExitCodes.BuildFailed, "no_library", $"The publish for {rid} did not produce {props.AssemblyName}.so.");
            }

            libraries.Add((abi, so));
        }

        var manifest = Path.Combine(work, "AndroidManifest.xml");
        File.WriteAllText(manifest, AndroidManifest(package, label, props.Version, props.AssemblyName, dev, a.Get("--orientation") ?? "unspecified"), new UTF8Encoding(false));

        var versionCode = VersionCode(props.Version);
        var baseApk = Path.Combine(work, "base.apk");
        RunTool(sdk.BuildTool("aapt2"), ["link", "-o", baseApk, "-I", sdk.AndroidJar, "--manifest", manifest, "--min-sdk-version", "26", "--target-sdk-version", "35",
            "--version-code", versionCode.ToString(System.Globalization.CultureInfo.InvariantCulture), "--version-name", props.Version], work, "aapt2_failed");

        using (var zip = ZipFile.Open(baseApk, ZipArchiveMode.Update))
        {
            foreach (var (abi, so) in libraries)
            {
                zip.CreateEntryFromFile(so, $"lib/{abi}/lib{props.AssemblyName}.so", CompressionLevel.Optimal);
            }
        }

        var aligned = Path.Combine(work, "aligned.apk");
        RunTool(sdk.BuildTool("zipalign"), ["-f", "4", baseApk, aligned], work, "zipalign_failed");

        var keystore = a.Get("--keystore") ?? EnsureDebugKeystore(out _);
        Directory.CreateDirectory(outRoot);
        var apk = Path.Combine(outRoot, $"{project.Name}-{props.Version}-android{(dev ? "-dev" : "")}.apk");
        RunTool(sdk.BuildTool("apksigner"), ["sign", "--ks", keystore, "--ks-pass", "pass:android", "--key-pass", "pass:android", "--out", apk, aligned], work, "apksigner_failed");
        RunTool(sdk.BuildTool("apksigner"), ["verify", apk], work, "apksigner_verify_failed");

        var libs = libraries.Select(l => Describe(Path.GetDirectoryName(l.Path)!, Path.GetFileName(l.Path)) with { Path = $"lib/{l.Abi}/lib{props.AssemblyName}.so" }).ToArray();
        var warnings = new List<ExportWarning>
        {
            new("debug_signed", "The APK is signed with a debug key: fine for installing on a device or emulator, not for a store. A release key and an AAB for Play come with the store binding."),
        };
        if (!dev)
        {
            warnings.Add(new("no_dev_tools", "Release build: no live control and no scenario options. Pass --dev for a build that agents can drive (adb forward, onegame android run)."));
        }

        var sha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(apk)));
        var size = new FileInfo(apk).Length;
        Directory.Delete(work, true);
        var result = new AndroidExportResult(true, project.Name, props.Version, Program.GetVersion(), "android", package, apk, size, sha, abis, dev, dev, "debug-key", libs, [.. warnings],
            Math.Round(clock.Elapsed.TotalSeconds, 1));
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.AndroidExportResult));
        }
        else
        {
            output.WriteLine($"exported {project.Name} {props.Version} for Android ({string.Join(", ", abis)}): {apk} ({size / 1024} KiB)");
            output.WriteLine($"  package: {package}   install: onegame android install {apk}");
            foreach (var w in warnings)
            {
                output.WriteLine($"  warning {w.Code}: {w.Message}");
            }
        }

        return ExitCodes.Ok;
    }

    private static void PublishAndroid(ProjectRef project, string rid, string folder, bool dev, AndroidSdk sdk)
    {
        // A Release build either way: the development tools are chosen by OneGameDev, not by the configuration.
        List<string> args = ["publish", project.File, "-c", "Release", "-r", rid, "--self-contained", "true", "-o", folder, "--nologo", "-v", "q", "-clp:NoSummary",
            $"-p:OneGameDev={(dev ? "true" : "false")}", "-p:DebugType=none", "-p:DebugSymbols=false", "-p:GenerateFullPaths=true", "-p:AndroidNdkPath=" + sdk.Ndk];
        var run = Proc.Run(Proc.Dotnet, args, project.Directory);
        if (run.ExitCode != 0)
        {
            var errors = BuildOutput.Parse(run.Output + "\n" + run.Error).Where(d => d.Severity == "error").Take(8)
                .Select(d => d.File is null ? $"{d.Code}: {d.Message}" : $"{d.File}({d.Line}): {d.Code}: {d.Message}");
            var text = string.Join(" | ", errors);
            if (text.Length == 0)
            {
                text = string.Join(" | ", (run.Output + "\n" + run.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(6));
            }

            throw new CliException(ExitCodes.BuildFailed, "publish_failed", $"dotnet publish for {rid} failed (exit {run.ExitCode}): {text}");
        }
    }

    internal static string AndroidManifest(string package, string label, string version, string library, bool dev, string orientation)
    {
        static string X(string s) => System.Security.SecurityElement.Escape(s) ?? "";
        _ = version;
        var debuggable = dev ? "true" : "false";

        // The game is native code only. The activity is the platform's own NativeActivity, which loads the library named below, so the app
        // has no Java or Kotlin of its own (hasCode=false). The live-control listener of a dev build is a loopback socket, which needs INTERNET.
        var lines = new List<string>
        {
            "<?xml version='1.0' encoding='utf-8'?>",
            $"<manifest xmlns:android='http://schemas.android.com/apk/res/android' package='{X(package)}'>",
            "  <uses-sdk android:minSdkVersion='26' android:targetSdkVersion='35'/>",
            "  <uses-feature android:name='android.hardware.vulkan.level' android:version='0' android:required='true'/>",
            "  <uses-feature android:name='android.hardware.touchscreen' android:required='false'/>",
            "  <uses-feature android:name='android.hardware.gamepad' android:required='false'/>",
        };
        if (dev)
        {
            lines.Add("  <uses-permission android:name='android.permission.INTERNET'/>");
        }

        lines.Add($"  <application android:label='{X(label)}' android:hasCode='false' android:allowBackup='false' android:debuggable='{debuggable}'>");
        lines.Add($"    <activity android:name='android.app.NativeActivity' android:exported='true' android:launchMode='singleTask' android:screenOrientation='{X(orientation)}'");
        lines.Add("        android:theme='@android:style/Theme.Black.NoTitleBar.Fullscreen'");
        lines.Add("        android:configChanges='orientation|screenSize|screenLayout|smallestScreenSize|keyboard|keyboardHidden|navigation|density|uiMode|fontScale|layoutDirection|locale'>");
        lines.Add($"      <meta-data android:name='android.app.lib_name' android:value='{X(library)}'/>");
        lines.Add("      <intent-filter><action android:name='android.intent.action.MAIN'/><category android:name='android.intent.category.LAUNCHER'/></intent-filter>");
        lines.Add("    </activity>");
        lines.Add("  </application>");
        lines.Add("</manifest>");
        return string.Join('\n', lines) + "\n";
    }

    // 1.2.3 becomes 10203, so a newer version always has a higher code.
    internal static int VersionCode(string version)
    {
        var core = version.Split('-', '+')[0].Split('.');
        var n = 0;
        for (var i = 0; i < 3; i++)
        {
            var part = i < core.Length && int.TryParse(core[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var v) ? Math.Min(v, 99) : 0;
            n = (n * 100) + part;
        }

        return Math.Max(n, 1);
    }

    private static void RunTool(string file, IEnumerable<string> args, string dir, string code)
    {
        var run = Proc.Run(file, args, dir);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, code, $"{Path.GetFileName(file)} failed (exit {run.ExitCode}): {(run.Output + run.Error).Trim()}");
        }
    }

    /// <summary>The debug key Android tooling uses by default (<c>debug.keystore</c> in the Android user folder), created with <c>keytool</c> when it is missing.</summary>
    internal static string EnsureDebugKeystore(out bool created)
    {
        created = false;
        var home = Environment.GetEnvironmentVariable("ANDROID_USER_HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android");
        }

        var path = Path.Combine(home, "debug.keystore");
        if (File.Exists(path))
        {
            return path;
        }

        Directory.CreateDirectory(home);
        var keytool = Environment.GetEnvironmentVariable("JAVA_HOME") is { Length: > 0 } java && File.Exists(Path.Combine(java, "bin", "keytool")) ? Path.Combine(java, "bin", "keytool") : "keytool";
        RunTool(keytool, ["-genkeypair", "-keystore", path, "-storepass", "android", "-keypass", "android", "-alias", "androiddebugkey", "-keyalg", "RSA", "-keysize", "2048", "-validity", "10000",
            "-dname", "CN=Android Debug,O=Android,C=US"], home, "keytool_failed");
        created = true;
        return path;
    }
}
