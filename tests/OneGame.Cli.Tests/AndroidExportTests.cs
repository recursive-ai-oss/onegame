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

    [Theory]
    [InlineData("0.1.0", 100)]
    [InlineData("1.2.3", 10203)]
    [InlineData("2.0.0-preview.1", 20000)]
    [InlineData("0.0.0", 1)]
    [InlineData("1.200.0", 19900)]
    public void VersionCodesGrowWithTheVersion(string version, int code) => Assert.Equal(code, ExportVerb.VersionCode(version));
}
