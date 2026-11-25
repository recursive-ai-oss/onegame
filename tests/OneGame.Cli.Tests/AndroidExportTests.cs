using OneGame.Host;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

public class AndroidExportTests
{
    [Fact]
    public void ManifestIsNativeOnlyAndNamesTheLibrary()
    {
        var xml = ExportVerb.AndroidManifest("com.example.game", "My <Game>", "1.2.3", "MyGame", dev: false, "sensorLandscape");
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var ns = System.Xml.Linq.XNamespace.Get("http://schemas.android.com/apk/res/android");
        var app = doc.Root!.Element("application")!;
        Assert.Equal("false", (string?)app.Attribute(ns + "hasCode"));
        Assert.Equal("false", (string?)app.Attribute(ns + "debuggable"));
        Assert.Equal("My <Game>", (string?)app.Attribute(ns + "label"));
        var activity = app.Element("activity")!;
        Assert.Equal("android.app.NativeActivity", (string?)activity.Attribute(ns + "name"));
        Assert.Equal("sensorLandscape", (string?)activity.Attribute(ns + "screenOrientation"));
        Assert.Equal("MyGame", (string?)activity.Element("meta-data")!.Attribute(ns + "value"));
        Assert.DoesNotContain("INTERNET", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void DevBuildsAreDebuggableAndMayListen()
    {
        var xml = ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: true, "unspecified");
        Assert.Contains("android.permission.INTERNET", xml, StringComparison.Ordinal);
        Assert.Contains("android:debuggable='true'", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayBillingAddsCodePermissionQueriesAndPurchaseActivities()
    {
        var xml = ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified", playBilling: true);
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var ns = System.Xml.Linq.XNamespace.Get("http://schemas.android.com/apk/res/android");
        var app = doc.Root!.Element("application")!;
        Assert.Equal("true", (string?)app.Attribute(ns + "hasCode"));
        Assert.Contains(doc.Root.Elements("uses-permission"), e => (string?)e.Attribute(ns + "name") == "com.android.vending.BILLING");
        Assert.Contains(doc.Root.Element("queries")!.Elements("intent").Select(i => (string?)i.Element("action")!.Attribute(ns + "name")), n => n == "com.android.vending.billing.InAppBillingService.BIND");
        var names = app.Elements("activity").Select(a => (string?)a.Attribute(ns + "name")).ToArray();
        Assert.Contains("android.app.NativeActivity", names);
        Assert.Contains("com.android.billingclient.api.ProxyBillingActivity", names);
        Assert.Contains("com.android.billingclient.api.ProxyBillingActivityV2", names);
        Assert.DoesNotContain("BILLING", ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified"), StringComparison.Ordinal);
    }

    [Fact]
    public void NetworkedReleaseBuildsAskForInternetAndStayNotDebuggable()
    {
        var xml = ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified", internet: true);
        Assert.Contains("android.permission.INTERNET", xml, StringComparison.Ordinal);
        Assert.Contains("android:debuggable='false'", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetSdkDefaultsTo36CanBeSetAndWarnsBelow()
    {
        Assert.Contains("android:targetSdkVersion='36'", ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified"), StringComparison.Ordinal);
        Assert.Contains("android:targetSdkVersion='37'", ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified", targetSdk: 37), StringComparison.Ordinal);
        Assert.Equal(36, ExportVerb.ResolveTargetSdk(null, out var none));
        Assert.Null(none);
        Assert.Equal(35, ExportVerb.ResolveTargetSdk("35", out var low));
        Assert.Equal("target_sdk_low", low!.Code);
        Assert.Throws<CliException>(() => ExportVerb.ResolveTargetSdk("abc", out _));
    }

    private static string? NoEnv(string name) => null;

    [Fact]
    public void AReleaseExportWithoutAKeyIsAUsageError()
    {
        var ex = Assert.Throws<CliException>(() => ExportVerb.ResolveSigning(null, null, dev: false, debugKey: false, NoEnv, () => "debug.keystore"));
        Assert.Equal(ExitCodes.Usage, ex.ExitCode);
        Assert.Contains("--keystore", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ONEGAME_ANDROID_STORE_PASSWORD", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DevBuildsAndExplicitDebugKeyUseTheDebugKey()
    {
        Assert.True(ExportVerb.ResolveSigning(null, null, dev: true, debugKey: false, NoEnv, () => "debug.keystore").IsDebug);
        Assert.True(ExportVerb.ResolveSigning(null, null, dev: false, debugKey: true, NoEnv, () => "debug.keystore").IsDebug);
    }

    [Fact]
    public void AKeystoreNeedsAnAliasAndPasswordsFromTheEnvironment()
    {
        var file = Path.GetTempFileName();
        try
        {
            Assert.Equal("key_alias_required", Assert.Throws<CliException>(() => ExportVerb.ResolveSigning(file, null, false, false, NoEnv, () => "d")).Code);
            Assert.Equal("keystore_password_required", Assert.Throws<CliException>(() => ExportVerb.ResolveSigning(file, "up", false, false, NoEnv, () => "d")).Code);
            var env = new Dictionary<string, string> { ["ONEGAME_ANDROID_STORE_PASSWORD"] = "store-pw" };
            var signing = ExportVerb.ResolveSigning(file, "up", false, false, env.GetValueOrDefault, () => "d");
            Assert.False(signing.IsDebug);
            Assert.Equal("store-pw", signing.KeyPassword);
            env["ONEGAME_ANDROID_KEY_PASSWORD"] = "key-pw";
            Assert.Equal("key-pw", ExportVerb.ResolveSigning(file, "up", false, false, env.GetValueOrDefault, () => "d").KeyPassword);
            Assert.Equal("failed: ***", signing.Redact("failed: store-pw"));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void VersionCodeOverrideIsCheckedAgainstPlaysLimits()
    {
        Assert.Equal(10203, ExportVerb.ResolveVersionCode(null, "1.2.3"));
        Assert.Equal(4242, ExportVerb.ResolveVersionCode("4242", "1.2.3"));
        Assert.Throws<CliException>(() => ExportVerb.ResolveVersionCode("0", "1.2.3"));
        Assert.Throws<CliException>(() => ExportVerb.ResolveVersionCode("2100000001", "1.2.3"));
        Assert.Throws<CliException>(() => ExportVerb.ResolveVersionCode("-5", "1.2.3"));
    }

    [Fact]
    public void BillingExportsCarryTheLibraryVersionForPlay()
    {
        var xml = ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified", playBilling: true);
        var ns = System.Xml.Linq.XNamespace.Get("http://schemas.android.com/apk/res/android");
        var meta = System.Xml.Linq.XDocument.Parse(xml).Root!.Element("application")!.Elements("meta-data")
            .Single(m => (string?)m.Attribute(ns + "name") == "com.google.android.play.billingclient.version");
        Assert.Equal(PlayBillingKit.BillingVersion, (string?)meta.Attribute(ns + "value"));
        Assert.DoesNotContain("billingclient.version", ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified"), StringComparison.Ordinal);
    }

    [Fact]
    public void ElfLoadAlignmentIsReadFromTheProgramHeaders()
    {
        // A minimal little-endian ELF64 with one PT_LOAD whose p_align is 0x4000, then 0x1000.
        static byte[] Elf(long align)
        {
            var b = new byte[64 + 56];
            b[0] = 0x7f; b[1] = (byte)'E'; b[2] = (byte)'L'; b[3] = (byte)'F'; b[4] = 2; b[5] = 1;
            BitConverter.TryWriteBytes(b.AsSpan(32), 64L);
            BitConverter.TryWriteBytes(b.AsSpan(54), (ushort)56);
            BitConverter.TryWriteBytes(b.AsSpan(56), (ushort)1);
            BitConverter.TryWriteBytes(b.AsSpan(64), 1u);
            BitConverter.TryWriteBytes(b.AsSpan(64 + 48), align);
            return b;
        }

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, Elf(0x4000));
            Assert.Equal(0x4000, ExportVerb.LoadSegmentAlignment(path));
            File.WriteAllBytes(path, Elf(0x1000));
            Assert.Equal(0x1000, ExportVerb.LoadSegmentAlignment(path));
            File.WriteAllBytes(path, new byte[100]);
            Assert.Null(ExportVerb.LoadSegmentAlignment(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("0.1.0", 100)]
    [InlineData("1.2.3", 10203)]
    [InlineData("2.0.0-preview.1", 20000)]
    [InlineData("0.0.0", 1)]
    [InlineData("1.200.0", 19900)]
    public void VersionCodesGrowWithTheVersion(string version, int code) => Assert.Equal(code, ExportVerb.VersionCode(version));
}
