using System.Xml.Linq;
using OneGame.Cli.Verbs;
using OneGame.Host;

namespace OneGame.Cli.Tests;

/// <summary>What a game can declare for its Android package: Java activity, manifest additions and the launcher icon.</summary>
public class AndroidPackageExtrasTests : IDisposable
{
    private static readonly XNamespace Ns = "http://schemas.android.com/apk/res/android";
    private readonly string _dir = Directory.CreateTempSubdirectory("onegame-android-extras-").FullName;

    public void Dispose()
    {
        Directory.Delete(_dir, true);
        GC.SuppressFinalize(this);
    }

    private string Write(string name, string text)
    {
        var path = Path.Combine(_dir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private static string Engine(string? activity = null, bool icon = false) =>
        ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: false, "unspecified", activity: activity, extraCode: activity is not null, icon: icon);

    [Fact]
    public void AGameActivityReplacesNativeActivityAndTurnsOnCode()
    {
        var doc = XDocument.Parse(Engine("example.game.GameActivity"));
        var app = doc.Root!.Element("application")!;
        Assert.Equal("true", (string?)app.Attribute(Ns + "hasCode"));
        Assert.Equal("example.game.GameActivity", (string?)app.Element("activity")!.Attribute(Ns + "name"));
        Assert.Equal("false", (string?)XDocument.Parse(Engine()).Root!.Element("application")!.Attribute(Ns + "hasCode"));
    }

    [Fact]
    public void TheIconIsWiredAsIconAndRoundIcon()
    {
        var app = XDocument.Parse(Engine(icon: true)).Root!.Element("application")!;
        Assert.Equal("@mipmap/ic_launcher", (string?)app.Attribute(Ns + "icon"));
        Assert.Equal("@mipmap/ic_launcher_round", (string?)app.Attribute(Ns + "roundIcon"));
    }

    [Fact]
    public void NothingDeclaredLeavesTheManifestAsItWas()
    {
        var engine = Engine();
        Assert.Equal(engine, AndroidManifestMerge.Apply(engine, null, null, null));
    }

    [Fact]
    public void AttributesAndAFragmentAreMergedIntoTheEnginesManifest()
    {
        var fragment = Write("m.xml", """
            <manifest xmlns:android="http://schemas.android.com/apk/res/android">
              <uses-permission android:name="android.permission.VIBRATE"/>
              <uses-permission android:name="android.permission.INTERNET"/>
              <application android:supportsRtl="true">
                <meta-data android:name="a" android:value="b"/>
                <activity android:name="android.app.NativeActivity" android:showWhenLocked="true">
                  <meta-data android:name="inner" android:value="1"/>
                </activity>
                <service android:name="example.Svc"/>
              </application>
            </manifest>
            """);
        var engine = ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: true, "unspecified");
        var merged = XDocument.Parse(AndroidManifestMerge.Apply(engine, fragment, "android:largeHeap=true", "android:resizeableActivity=false"));
        var root = merged.Root!;
        var app = root.Element("application")!;
        Assert.Equal("true", (string?)app.Attribute(Ns + "largeHeap"));
        Assert.Equal("true", (string?)app.Attribute(Ns + "supportsRtl"));
        var main = app.Element("activity")!;
        Assert.Equal("false", (string?)main.Attribute(Ns + "resizeableActivity"));
        Assert.Equal("true", (string?)main.Attribute(Ns + "showWhenLocked"));
        Assert.Contains(main.Elements("meta-data"), m => (string?)m.Attribute(Ns + "name") == "inner");
        Assert.Contains(app.Elements("meta-data"), m => (string?)m.Attribute(Ns + "name") == "a");
        Assert.Single(app.Elements("service"));
        // INTERNET was already there from the dev build: a repeat is not a conflict and is not doubled.
        Assert.Single(root.Elements("uses-permission"), p => (string?)p.Attribute(Ns + "name") == "android.permission.INTERNET");
        Assert.Contains(root.Elements("uses-permission"), p => (string?)p.Attribute(Ns + "name") == "android.permission.VIBRATE");
    }

    [Theory]
    [InlineData("android:debuggable=false", null)]
    [InlineData("android:icon=@mipmap/other", null)]
    [InlineData(null, "android:screenOrientation=landscape")]
    [InlineData(null, "android:name=com.example.Other")]
    public void AnAttributeThatContradictsTheEngineIsAConflict(string? application, string? activity)
    {
        var engine = ExportVerb.AndroidManifest("com.example.game", "G", "1.0.0", "G", dev: true, "unspecified", icon: true);
        var ex = Assert.Throws<CliException>(() => AndroidManifestMerge.Apply(engine, null, application, activity));
        Assert.Equal("manifest_conflict", ex.Code);
        Assert.Contains("already writes", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADifferentElementOfTheSameNameIsAConflictAndAnIdenticalOneIsNot()
    {
        var engine = Engine();
        var same = Write("same.xml", "<manifest xmlns:android='http://schemas.android.com/apk/res/android'><uses-feature android:name='android.hardware.touchscreen' android:required='false'/></manifest>");
        Assert.NotNull(AndroidManifestMerge.Apply(engine, same, null, null));
        var other = Write("other.xml", "<manifest xmlns:android='http://schemas.android.com/apk/res/android'><uses-feature android:name='android.hardware.touchscreen' android:required='true'/></manifest>");
        Assert.Equal("manifest_conflict", Assert.Throws<CliException>(() => AndroidManifestMerge.Apply(engine, other, null, null)).Code);
    }

    [Fact]
    public void MalformedInputIsNamedNotThrownAsAnXmlError()
    {
        var engine = Engine();
        Assert.Equal("bad_manifest_fragment", Assert.Throws<CliException>(() => AndroidManifestMerge.Apply(engine, Write("bad.xml", "<manifest"), null, null)).Code);
        Assert.Equal("bad_manifest_fragment", Assert.Throws<CliException>(() => AndroidManifestMerge.Apply(engine, Write("root.xml", "<application/>"), null, null)).Code);
        Assert.Equal("bad_manifest_attribute", Assert.Throws<CliException>(() => AndroidManifestMerge.Apply(engine, null, "largeHeap=true", null)).Code);
    }

    [Fact]
    public void TheActivityIsTheTopLevelClassThatExtendsNativeActivity()
    {
        var found = AndroidJavaBuild.FindActivities("""
            // class Fake extends NativeActivity
            package example.game;
            import android.app.NativeActivity;
            /* class AlsoFake extends NativeActivity {} */
            public class Real extends NativeActivity { }
            class Helper { }
            """).ToArray();
        Assert.Equal(["example.game.Real"], found);
        Assert.Equal(["Plain"], AndroidJavaBuild.FindActivities("final class Plain extends android.app.NativeActivity {}").ToArray());
    }

    private static AndroidSdk? FindSdk()
    {
        try
        {
            return AndroidSdk.Find(null, needNdk: false);
        }
        catch (CliException)
        {
            return null;
        }
    }

    [Fact]
    public void JavaThatDoesNotCompileFailsTheExportWithTheCompilersWords()
    {
        var sdk = FindSdk();
        Assert.SkipUnless(sdk is not null, "needs the Android SDK (ANDROID_HOME)");
        Write("android/Broken.java", "public class Broken extends android.app.NativeActivity { void f() { int x = \"text\"; } }");
        var ex = Assert.Throws<CliException>(() => AndroidJavaBuild.Build(sdk!, Path.Combine(_dir, "android"), null, Path.Combine(_dir, "work")));
        Assert.Equal("java_compile_failed", ex.Code);
        Assert.Contains("Broken.java", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JavaBecomesADexAndTheActivityIsFound()
    {
        var sdk = FindSdk();
        Assert.SkipUnless(sdk is not null, "needs the Android SDK (ANDROID_HOME)");
        Write("android/example/Main.java", "package example; public class Main extends android.app.NativeActivity { }");
        Directory.CreateDirectory(Path.Combine(_dir, "work"));
        var built = AndroidJavaBuild.Build(sdk!, Path.Combine(_dir, "android"), null, Path.Combine(_dir, "work"));
        Assert.NotNull(built);
        Assert.Equal("example.Main", built.Activity);
        Assert.True(File.ReadAllBytes(built.Dex).AsSpan(0, 3).SequenceEqual("dex"u8));

        var named = Assert.Throws<CliException>(() => AndroidJavaBuild.Build(sdk!, Path.Combine(_dir, "android"), "example.Missing", Path.Combine(_dir, "work2")));
        Assert.Equal("activity_not_found", named.Code);
    }

    [Fact]
    public void TheIconMakesAdaptiveXmlAndPngsAtEveryDensity()
    {
        var svg = Write("icon.svg", "<svg xmlns='http://www.w3.org/2000/svg' width='108' height='108'><rect x='30' y='30' width='48' height='48' fill='#ff0000'/></svg>");
        var res = Path.Combine(_dir, "res");
        AndroidIconBuild.Write(res, svg, "#102030", _dir);
        var sizes = new Dictionary<string, int> { ["mdpi"] = 1, ["hdpi"] = 2, ["xhdpi"] = 3, ["xxhdpi"] = 4, ["xxxhdpi"] = 6 };
        foreach (var (density, scale) in sizes)
        {
            Assert.Equal(48 * scale, Png.Load(Path.Combine(res, "mipmap-" + density, "ic_launcher.png")).Width);
            Assert.Equal(48 * scale, Png.Load(Path.Combine(res, "mipmap-" + density, "ic_launcher_round.png")).Width);
            Assert.Equal(108 * scale, Png.Load(Path.Combine(res, "mipmap-" + density, "ic_launcher_foreground.png")).Width);
        }

        var adaptive = File.ReadAllText(Path.Combine(res, "mipmap-anydpi-v26", "ic_launcher.xml"));
        Assert.Contains("@color/ic_launcher_background", adaptive, StringComparison.Ordinal);
        Assert.Contains("#102030", File.ReadAllText(Path.Combine(res, "values", "ic_launcher_colors.xml")), StringComparison.Ordinal);

        // The round legacy icon is cut to a circle: the corner is transparent, the middle is the background or art.
        var round = Png.Load(Path.Combine(res, "mipmap-xxxhdpi", "ic_launcher_round.png"));
        Assert.Equal(0, round.Rgba[3]);
        var mid = ((round.Height / 2 * round.Width) + (round.Width / 2)) * 4;
        Assert.Equal(255, round.Rgba[mid + 3]);
        Assert.Equal(255, round.Rgba[mid]); // the red square
    }

    [Fact]
    public void AnImageBackgroundBecomesAMipmapLayer()
    {
        var svg = Write("icon.svg", "<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><circle cx='5' cy='5' r='3'/></svg>");
        var back = Write("back.svg", "<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='10' height='10' fill='#00ff00'/></svg>");
        var res = Path.Combine(_dir, "res");
        AndroidIconBuild.Write(res, svg, "back.svg", _dir);
        Assert.True(File.Exists(Path.Combine(res, "mipmap-hdpi", "ic_launcher_background.png")));
        Assert.Contains("@mipmap/ic_launcher_background", File.ReadAllText(Path.Combine(res, "mipmap-anydpi-v26", "ic_launcher.xml")), StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(res, "values")));
        _ = back;
    }

    [Theory]
    [InlineData("missing.png", null)]
    [InlineData("notimage.png", null)]
    [InlineData("wide.svg", null)]
    [InlineData("ok.svg", "#12")]
    [InlineData("ok.svg", "gone.png")]
    public void ABadIconOrBackgroundIsRefusedWithBadIcon(string icon, string? background)
    {
        Write("notimage.png", "this is not a png");
        Write("wide.svg", "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='10'/>");
        Write("ok.svg", "<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'/>");
        var ex = Assert.Throws<CliException>(() => AndroidIconBuild.Write(Path.Combine(_dir, "res"), Path.Combine(_dir, icon), background, _dir));
        Assert.Equal("bad_icon", ex.Code);
    }
}
