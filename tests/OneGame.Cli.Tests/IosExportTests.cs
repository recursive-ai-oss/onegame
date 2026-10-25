using System.Xml.Linq;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

public class IosExportTests
{
    // A property list is a flat run of key and value elements; this reads one into a dictionary of the values' elements.
    private static Dictionary<string, XElement> Plist(string xml)
    {
        var dict = XDocument.Parse(xml).Root!.Element("dict")!;
        var result = new Dictionary<string, XElement>(StringComparer.Ordinal);
        var elements = dict.Elements().ToArray();
        for (var i = 0; i + 1 < elements.Length; i += 2)
        {
            Assert.Equal("key", elements[i].Name.LocalName);
            result[elements[i].Value] = elements[i + 1];
        }

        return result;
    }

    [Fact]
    public void InfoPlistDescribesAFullScreenSceneApp()
    {
        var p = Plist(ExportVerb.IosInfoPlist("com.example.game", "My <Game>", "1.2.3-beta.1", "MyGame", "landscape", simulator: true, dev: false, "iphonesimulator", "27.1"));
        Assert.Equal("com.example.game", p["CFBundleIdentifier"].Value);
        Assert.Equal("MyGame", p["CFBundleExecutable"].Value);
        Assert.Equal("My <Game>", p["CFBundleDisplayName"].Value);
        Assert.Equal("1.2.3", p["CFBundleShortVersionString"].Value);
        Assert.Equal("iPhoneSimulator", p["CFBundleSupportedPlatforms"].Value);

        // An empty launch screen is what makes iOS run the app at the screen's full size.
        Assert.True(p["UILaunchScreen"].IsEmpty);
        Assert.Equal("true", p["UIStatusBarHidden"].Name.LocalName);

        // The scene lifecycle: the delegate class named here is the one the platform layer registers.
        var scene = p["UIApplicationSceneManifest"];
        Assert.Contains("OneGameSceneDelegate", scene.ToString(), StringComparison.Ordinal);
        Assert.Contains("UIWindowSceneSessionRoleApplication", scene.ToString(), StringComparison.Ordinal);

        Assert.Equal(["UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight"], p["UISupportedInterfaceOrientations"].Elements().Select(e => e.Value));
        Assert.False(p.ContainsKey("NSLocalNetworkUsageDescription"));
    }

    [Theory]
    [InlineData("portrait", 1)]
    [InlineData("landscape", 2)]
    [InlineData("all", 3)]
    public void OrientationChoosesTheListedOrientations(string orientation, int phoneCount)
    {
        var p = Plist(ExportVerb.IosInfoPlist("a.b", "G", "1.0.0", "G", orientation, simulator: false, dev: false, "iphoneos", "27.1"));
        Assert.Equal(phoneCount, p["UISupportedInterfaceOrientations"].Elements().Count());
        Assert.Equal("iPhoneOS", p["CFBundleSupportedPlatforms"].Value);
    }

    [Fact]
    public void DevBuildsMayBeReachedOverTheLocalNetwork()
    {
        var p = Plist(ExportVerb.IosInfoPlist("a.b", "G", "1.0.0", "G", "all", simulator: true, dev: true, "iphonesimulator", "27.1"));
        Assert.True(p.ContainsKey("NSLocalNetworkUsageDescription"));
        Assert.Contains("NSAllowsLocalNetworking", p["NSAppTransportSecurity"].ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("2.0.0-preview.1", "2.0.0")]
    [InlineData("1.2.3.4", "1.2.3")]
    [InlineData("x", "1.0.0")]
    public void BundleVersionsAreUpToThreeIntegers(string version, string expected) => Assert.Equal(expected, ExportVerb.BundleVersion(version));

    [Fact]
    public void EntitlementsLeaveTheTeamForTheOwnersSigningStep()
    {
        var xml = ExportVerb.IosEntitlements("com.example.game", dev: true);
        Assert.Contains("TEAMID.com.example.game", xml, StringComparison.Ordinal);
        Assert.Contains("<key>get-task-allow</key><true/>", xml, StringComparison.Ordinal);
        Assert.Contains("<key>get-task-allow</key><false/>", ExportVerb.IosEntitlements("a.b", dev: false), StringComparison.Ordinal);
    }
}
