using System.Text;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>What <c>onegame export --target ios-simulator|ios</c> printed.</summary>
internal sealed record IosExportResult(
    bool Ok, string Game, string GameVersion, string EngineVersion, string Target, string BundleId, string App, long Size, string Sha256, bool DevTools,
    string Signing, string[] SigningNeeds, string? Entitlements, ExportFile[] Files, ExportWarning[] Warnings, double Seconds);

internal sealed partial class ExportVerb
{
    private static int RunIosExport(ProjectArgs a, bool simulator, bool dev, bool noAot, bool json, TextWriter output)
    {
        if (noAot)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "--no-aot does not apply to iOS: games are NativeAOT executables there.");
        }

        if (!OperatingSystem.IsMacOS())
        {
            throw new CliException(ExitCodes.RuntimeError, "ios_host", "Exporting for iOS links with Xcode's toolchain and the iOS SDK, so it needs a Mac with Xcode (xcode-select -p must point at it).");
        }

        var sdkName = simulator ? "iphonesimulator" : "iphoneos";
        var sdk = Proc.Run("xcrun", ["--sdk", sdkName, "--show-sdk-version"], Directory.GetCurrentDirectory());
        if (sdk.ExitCode != 0 || sdk.Output.Trim().Length == 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "xcode_missing", $"xcrun could not find the {sdkName} SDK. Install Xcode, open it once to accept the licence, and run: xcode-select -s /Applications/Xcode.app/Contents/Developer. {(sdk.Output + sdk.Error).Trim()}");
        }

        var sdkVersion = sdk.Output.Trim();
        var project = Projects.Find(a.Get("--project"));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var props = ReadProperties(project);
        var settings = ReadIosSettings(project);
        var bundleId = a.Get("--bundle-id") ?? settings.GetValueOrDefault("IosBundleId") ?? "dev.onegame." + new string([.. props.AssemblyName.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit)]);
        if (!System.Text.RegularExpressions.Regex.IsMatch(bundleId, "^[A-Za-z0-9-]+(\\.[A-Za-z0-9-]+)+$"))
        {
            throw new CliException(ExitCodes.Usage, "bad_bundle_id", $"'{bundleId}' is not a bundle identifier (words of letters, digits and hyphens joined by dots, such as com.example.game).");
        }

        var label = a.Get("--label") ?? settings.GetValueOrDefault("IosLabel") ?? project.Name;
        var orientation = a.Get("--orientation") ?? settings.GetValueOrDefault("IosOrientation") ?? "all";
        if (orientation is not ("all" or "landscape" or "portrait"))
        {
            throw new CliException(ExitCodes.Usage, "bad_orientation", $"--orientation is all, landscape or portrait, got '{orientation}'.");
        }

        var target = simulator ? "ios-simulator" : "ios";
        var rid = simulator ? "iossimulator-arm64" : "ios-arm64";
        var outRoot = Path.GetFullPath(a.Get("--out") ?? Path.Combine(project.Directory, "export"));
        var folderName = $"{project.Name}-{props.Version}-{target}{(dev ? "-dev" : "")}";
        var folder = Path.Combine(outRoot, folderName);
        var work = Path.Combine(outRoot, ".ios-" + project.Name);
        foreach (var dir in new[] { folder, work })
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        Directory.CreateDirectory(work);
        Directory.CreateDirectory(folder);

        var symbols = a.Get("--symbols");
        PublishIos(project, rid, work, dev, symbols is not null);
        var executable = Path.Combine(work, props.AssemblyName);
        if (!File.Exists(executable))
        {
            throw new CliException(ExitCodes.BuildFailed, "no_executable", $"The publish for {rid} did not produce {props.AssemblyName}.");
        }

        if (symbols is not null)
        {
            // The dSYM turns a crash report's addresses into method names (symbolicatecrash, lldb).
            var target2 = Directory.CreateDirectory(Path.GetFullPath(symbols)).FullName;
            foreach (var dsym in Directory.GetDirectories(work, "*.dSYM"))
            {
                CopyDirectory(dsym, Path.Combine(target2, Path.GetFileName(dsym)));
            }
        }

        // The bundle: the program, its property list and the game's content folders (art, fonts, data), which the game finds beside the program.
        var app = Path.Combine(folder, props.AssemblyName + ".app");
        Directory.CreateDirectory(app);
        var contentWarnings = new List<ExportWarning>();
        ExportAssets.Copy(project.Directory, app, [], contentWarnings);
        File.Copy(executable, Path.Combine(app, props.AssemblyName));
        File.SetUnixFileMode(Path.Combine(app, props.AssemblyName), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        File.WriteAllText(Path.Combine(app, "Info.plist"), IosInfoPlist(bundleId, label, props.Version, props.AssemblyName, orientation, simulator, dev, sdkName, sdkVersion), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(app, "PkgInfo"), "APPL????", new UTF8Encoding(false));

        var warnings = new List<ExportWarning>(contentWarnings);
        string signing;
        string[] needs = [];
        string? entitlements = null;
        if (simulator)
        {
            // The simulator runs ad-hoc signed code (an identity of "-"): no account, no profile.
            RunTool("codesign", ["--force", "--sign", "-", "--timestamp=none", app], work, "codesign_failed");
            RunTool("codesign", ["--verify", "--deep", "--strict", app], work, "codesign_verify_failed");
            signing = "ad-hoc";
        }
        else
        {
            signing = "unsigned";
            entitlements = Path.Combine(folder, props.AssemblyName + ".entitlements");
            File.WriteAllText(entitlements, IosEntitlements(bundleId, dev), new UTF8Encoding(false));
            needs =
            [
                "An Apple Developer account and a signing certificate (Apple Development for a test device, Apple Distribution for the store) in the Mac's keychain.",
                $"A provisioning profile for the bundle id '{bundleId}' that lists the device: copy it into the bundle as {props.AssemblyName}.app/embedded.mobileprovision. If the id is taken or the profile is for another one, export again with --bundle-id.",
                $"The entitlements file written next to the bundle ({Path.GetFileName(entitlements)}): replace TEAMID with the profile's team identifier.",
                $"Sign: codesign --force --sign \"<identity>\" --entitlements {Path.GetFileName(entitlements)} --timestamp=none {props.AssemblyName}.app",
                $"Install on a connected device: xcrun devicectl device install app --device <device id> {props.AssemblyName}.app",
            ];
            warnings.Add(new("unsigned", "The bundle for a device is not signed: iOS will not run it until it is signed with a certificate and a provisioning profile. See signingNeeds."));
        }

        if (!dev)
        {
            warnings.Add(new("no_dev_tools", "Release build: no live control and no scenario options. Pass --dev for a build you can drive from outside (onegame ios run, onegame ios live)."));
        }

        var executableInfo = Describe(app, props.AssemblyName);
        var files = Directory.EnumerateFiles(app, "*", SearchOption.AllDirectories).Select(f => Describe(app, Path.GetRelativePath(app, f).Replace('\\', '/'))).Order(Comparer<ExportFile>.Create((x, y) => string.CompareOrdinal(x.Path, y.Path))).ToArray();
        long size = 0;
        foreach (var f in files)
        {
            size += f.Size;
        }

        Directory.Delete(work, true);
        var result = new IosExportResult(true, project.Name, props.Version, Program.GetVersion(), target, bundleId, app, size, executableInfo.Sha256, dev, signing, needs, entitlements, files, [.. warnings],
            Math.Round(clock.Elapsed.TotalSeconds, 1));
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.IosExportResult));
        }
        else
        {
            output.WriteLine($"exported {project.Name} {props.Version} for iOS ({target}, {signing}): {app} ({size / 1024} KiB)");
            output.WriteLine(simulator ? $"  bundle id: {bundleId}   install: onegame ios install {app}" : $"  bundle id: {bundleId}");
            foreach (var n in needs)
            {
                output.WriteLine($"  signing: {n}");
            }

            foreach (var w in warnings)
            {
                output.WriteLine($"  warning {w.Code}: {w.Message}");
            }
        }

        return ExitCodes.Ok;
    }

    // The project can set IosBundleId, IosLabel and IosOrientation; the command line wins.
    private static Dictionary<string, string> ReadIosSettings(ProjectRef project)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        var run = Proc.Run(Proc.Dotnet, ["msbuild", project.File, "-getProperty:IosBundleId", "-getProperty:IosLabel", "-getProperty:IosOrientation", "-nologo"], project.Directory);
        if (run.ExitCode == 0)
        {
            using var doc = JsonDocument.Parse(run.Output);
            foreach (var property in doc.RootElement.GetProperty("Properties").EnumerateObject())
            {
                if (property.Value.GetString() is { Length: > 0 } value)
                {
                    settings[property.Name] = value;
                }
            }
        }

        return settings;
    }

    private static void PublishIos(ProjectRef project, string rid, string folder, bool dev, bool keepSymbols)
    {
        // A Release build either way: the development tools are chosen by OneGameDev, not by the configuration.
        List<string> args = ["publish", project.File, "-c", "Release", "-r", rid, "-o", folder, "--nologo", "-v", "q", "-clp:NoSummary", $"-p:OneGameDev={(dev ? "true" : "false")}", "-p:GenerateFullPaths=true"];
        if (!keepSymbols)
        {
            args.AddRange(["-p:DebugType=none", "-p:DebugSymbols=false"]);
        }

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

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    // A "1.2.3-beta" version becomes "1.2.3": a bundle version is up to three dot-separated integers.
    internal static string BundleVersion(string version)
    {
        var core = version.Split('-', '+')[0].Split('.');
        var parts = new List<string>();
        foreach (var p in core)
        {
            if (int.TryParse(p, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n))
            {
                parts.Add(n.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        return parts.Count == 0 ? "1.0.0" : string.Join('.', parts.Take(3));
    }

    internal static string IosInfoPlist(string bundleId, string label, string version, string executable, string orientation, bool simulator, bool dev, string sdkName, string sdkVersion)
    {
        static string X(string s) => System.Security.SecurityElement.Escape(s) ?? "";
        var v = BundleVersion(version);
        var phoneOrientations = orientation switch
        {
            "landscape" => new[] { "UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight" },
            "portrait" => ["UIInterfaceOrientationPortrait"],
            _ => ["UIInterfaceOrientationPortrait", "UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight"],
        };
        var padOrientations = orientation switch
        {
            "landscape" => phoneOrientations,
            "portrait" => ["UIInterfaceOrientationPortrait", "UIInterfaceOrientationPortraitUpsideDown"],
            _ => [.. phoneOrientations, "UIInterfaceOrientationPortraitUpsideDown"],
        };

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n");
        sb.Append("<plist version=\"1.0\">\n<dict>\n");
        void Str(string key, string value) => sb.Append($"  <key>{key}</key><string>{X(value)}</string>\n");
        void Bool(string key, bool value) => sb.Append($"  <key>{key}</key><{(value ? "true" : "false")}/>\n");
        void Strings(string key, IEnumerable<string> values) => sb.Append($"  <key>{key}</key>\n  <array>{string.Concat(values.Select(s => $"<string>{X(s)}</string>"))}</array>\n");

        Str("CFBundleIdentifier", bundleId);
        Str("CFBundleExecutable", executable);
        Str("CFBundleName", label);
        Str("CFBundleDisplayName", label);
        Str("CFBundlePackageType", "APPL");
        Str("CFBundleInfoDictionaryVersion", "6.0");
        Str("CFBundleDevelopmentRegion", "en");
        Str("CFBundleVersion", v);
        Str("CFBundleShortVersionString", v);
        Str("MinimumOSVersion", "15.0");
        Strings("CFBundleSupportedPlatforms", [simulator ? "iPhoneSimulator" : "iPhoneOS"]);
        Str("DTPlatformName", sdkName);
        Str("DTSDKName", sdkName + sdkVersion);
        Str("DTPlatformVersion", sdkVersion);
        sb.Append("  <key>UIDeviceFamily</key>\n  <array><integer>1</integer><integer>2</integer></array>\n");
        Strings("UIRequiredDeviceCapabilities", ["arm64", "metal"]);
        Strings("UISupportedInterfaceOrientations", phoneOrientations);
        Strings("UISupportedInterfaceOrientations~ipad", padOrientations);
        Bool("UIRequiresFullScreen", true);
        Bool("UIStatusBarHidden", true);
        Bool("UIViewControllerBasedStatusBarAppearance", true);
        Bool("UIApplicationSupportsIndirectInputEvents", true);
        // An empty launch screen is what tells iOS the app draws the whole screen; without the key it runs letter-boxed.
        sb.Append("  <key>UILaunchScreen</key>\n  <dict/>\n");
        // The scene lifecycle: the system creates a window scene and hands it to the delegate class IosUi registers at start-up.
        sb.Append("  <key>UIApplicationSceneManifest</key>\n  <dict>\n");
        sb.Append("    <key>UIApplicationSupportsMultipleScenes</key><false/>\n");
        sb.Append("    <key>UISceneConfigurations</key>\n    <dict>\n      <key>UIWindowSceneSessionRoleApplication</key>\n      <array><dict>\n");
        sb.Append("        <key>UISceneConfigurationName</key><string>Default</string>\n");
        sb.Append("        <key>UISceneDelegateClassName</key><string>OneGameSceneDelegate</string>\n");
        sb.Append("      </dict></array>\n    </dict>\n  </dict>\n");
        if (dev)
        {
            // The live listener is a TCP socket: on a device the system asks before letting a program on the local network be reached.
            Str("NSLocalNetworkUsageDescription", "Development builds accept connections from the onegame tools on this network.");
            sb.Append("  <key>NSAppTransportSecurity</key>\n  <dict><key>NSAllowsLocalNetworking</key><true/></dict>\n");
        }

        sb.Append("</dict>\n</plist>\n");
        return sb.ToString();
    }

    internal static string IosEntitlements(string bundleId, bool dev) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
        + "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
        + "<plist version=\"1.0\">\n<dict>\n"
        + $"  <key>application-identifier</key><string>TEAMID.{System.Security.SecurityElement.Escape(bundleId)}</string>\n"
        + "  <key>com.apple.developer.team-identifier</key><string>TEAMID</string>\n"
        + $"  <key>get-task-allow</key><{(dev ? "true" : "false")}/>\n"
        + "</dict>\n</plist>\n";
}
