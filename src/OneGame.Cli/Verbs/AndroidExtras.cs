using System.Text.RegularExpressions;
using System.Xml.Linq;
using OneGame.Host;
using OneImage;
using OneImage.Svg;

namespace OneGame.Cli.Verbs;

/// <summary>The game's own Java: sources compiled against the platform's android.jar into one extra dex.</summary>
internal sealed record AndroidJavaCode(string Dex, string? Activity, int Files);

/// <summary>
/// Compiles the Java sources a game keeps in its <c>android</c> folder. This is the same tool path the Play Billing wrapper uses
/// (<c>javac</c> against the platform jar, then the SDK's own dex compiler); Kotlin is not supported because the SDK has no Kotlin compiler.
/// </summary>
internal static partial class AndroidJavaBuild
{
    /// <summary>The class name a game's activity must extend.</summary>
    public const string NativeActivity = "android.app.NativeActivity";

    /// <summary>
    /// Builds <c>classes.dex</c> from the folder's <c>*.java</c> files into <paramref name="work"/>; null when there are none and no activity is named.
    /// The activity is <paramref name="named"/> or the one top-level class that extends <c>NativeActivity</c>.
    /// </summary>
    public static AndroidJavaCode? Build(AndroidSdk sdk, string folder, string? named, string work)
    {
        var sources = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.java", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray() : [];
        if (sources.Length == 0)
        {
            return named is null ? null : throw new CliException(ExitCodes.Usage, "activity_not_found", $"AndroidActivity is {named}, but there are no .java files under {folder}.");
        }

        string? activity = named;
        if (activity is null)
        {
            var found = new List<string>();
            foreach (var source in sources)
            {
                found.AddRange(FindActivities(File.ReadAllText(source)));
            }

            if (found.Count > 1)
            {
                throw new CliException(ExitCodes.Usage, "activity_ambiguous", $"{string.Join(" and ", found)} all extend NativeActivity. Name the one the app starts with the AndroidActivity property.");
            }

            activity = found.FirstOrDefault();
        }

        var classes = Directory.CreateDirectory(Path.Combine(work, "game-classes")).FullName;
        var java = Tool("javac");
        var compile = Proc.Run(java, ["--release", "8", "-Xlint:-options", "-encoding", "UTF-8", "-cp", sdk.AndroidJar, "-d", classes, .. sources], work);
        if (compile.ExitCode != 0)
        {
            var lines = (compile.Output + "\n" + compile.Error).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).Take(24);
            throw new CliException(ExitCodes.BuildFailed, "java_compile_failed", $"javac failed on the game's Java under {folder}: {string.Join(" | ", lines)}");
        }

        if (activity is not null && !File.Exists(Path.Combine(classes, activity.Replace('.', Path.DirectorySeparatorChar) + ".class")))
        {
            throw new CliException(ExitCodes.Usage, "activity_not_found", $"AndroidActivity is {activity}, but no such class was compiled from {folder}.");
        }

        var output = Directory.CreateDirectory(Path.Combine(work, "game-dex")).FullName;
        var d8 = Proc.Run(Tool("java"), ["-cp", Path.Combine(sdk.BuildTools, "lib", "d8.jar"), "com.android.tools.r8.D8", "--release", "--min-api", "26", "--lib", sdk.AndroidJar, "--output", output,
            .. Directory.GetFiles(classes, "*.class", SearchOption.AllDirectories)], work);
        var dex = Path.Combine(output, "classes.dex");
        if (d8.ExitCode != 0 || !File.Exists(dex))
        {
            throw new CliException(ExitCodes.BuildFailed, "java_dex_failed", $"d8 failed on the game's Java: {string.Join(" | ", (d8.Output + "\n" + d8.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Take(8))}");
        }

        return new AndroidJavaCode(dex, activity, sources.Length);
    }

    /// <summary>The fully qualified names of the top-level classes in one source file that extend <c>NativeActivity</c>.</summary>
    internal static IEnumerable<string> FindActivities(string source)
    {
        var text = CommentPattern().Replace(source, " ");
        var package = PackagePattern().Match(text);
        var prefix = package.Success ? package.Groups[1].Value + "." : "";
        foreach (Match m in ActivityPattern().Matches(text))
        {
            yield return prefix + m.Groups[1].Value;
        }
    }

    [GeneratedRegex(@"/\*.*?\*/|//[^\n]*", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"^\s*package\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex PackagePattern();

    [GeneratedRegex(@"\bclass\s+(\w+)\s+extends\s+(?:android\s*\.\s*app\s*\.\s*)?NativeActivity\b")]
    private static partial Regex ActivityPattern();

    private static string Tool(string name)
    {
        var home = Environment.GetEnvironmentVariable("JAVA_HOME");
        return !string.IsNullOrEmpty(home) && File.Exists(Path.Combine(home, "bin", name)) ? Path.Combine(home, "bin", name) : name;
    }
}

/// <summary>Adds the game's manifest attributes and fragment to the manifest the engine writes, and refuses anything that contradicts it.</summary>
internal static class AndroidManifestMerge
{
    private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

    /// <summary>
    /// <paramref name="fragmentPath"/> is a manifest-shaped file (a <c>manifest</c> root with <c>uses-permission</c>, <c>uses-feature</c>, <c>queries</c> and <c>application</c> children).
    /// Its <c>application</c> element adds attributes and children to the engine's; an <c>activity</c> there with the main activity's name adds to the main activity,
    /// and any other activity, service or receiver is added. An attribute the engine already wrote with another value, or an element of the same kind and name that differs, is a conflict.
    /// </summary>
    public static string Apply(string manifest, string? fragmentPath, string? applicationAttributes, string? activityAttributes)
    {
        if (fragmentPath is null && applicationAttributes is null && activityAttributes is null)
        {
            return manifest;
        }

        var doc = XDocument.Parse(manifest);
        var root = doc.Root!;
        var app = root.Element("application")!;
        var main = app.Elements("activity").First();
        var mainName = (string?)main.Attribute(Android + "name") ?? "";

        MergeAttributes(app, ParseAttributes(applicationAttributes, "AndroidApplicationAttributes"), "AndroidApplicationAttributes", "application");
        MergeAttributes(main, ParseAttributes(activityAttributes, "AndroidActivityAttributes"), "AndroidActivityAttributes", "the main activity");

        if (fragmentPath is not null)
        {
            XDocument fragment;
            try
            {
                fragment = XDocument.Load(fragmentPath);
            }
            catch (Exception ex) when (ex is System.Xml.XmlException or IOException)
            {
                throw new CliException(ExitCodes.Usage, "bad_manifest_fragment", $"{fragmentPath} is not a readable manifest fragment: {ex.Message}");
            }

            var file = Path.GetFileName(fragmentPath);
            if (fragment.Root is not { Name.LocalName: "manifest" } froot)
            {
                throw new CliException(ExitCodes.Usage, "bad_manifest_fragment", $"{file} must have a <manifest> root element.");
            }

            MergeAttributes(root, froot.Attributes().Where(a => !a.IsNamespaceDeclaration), file, "manifest");
            foreach (var child in froot.Elements())
            {
                if (child.Name.LocalName == "application")
                {
                    MergeAttributes(app, child.Attributes().Where(a => !a.IsNamespaceDeclaration), file, "application");
                    foreach (var element in child.Elements())
                    {
                        if (element.Name.LocalName == "activity" && (string?)element.Attribute(Android + "name") == mainName)
                        {
                            MergeAttributes(main, element.Attributes().Where(a => !a.IsNamespaceDeclaration && a.Name != Android + "name"), file, "the main activity");
                            foreach (var inner in element.Elements())
                            {
                                AddChild(main, inner, file);
                            }
                        }
                        else
                        {
                            AddChild(app, element, file);
                        }
                    }
                }
                else
                {
                    AddChild(root, child, file);
                }
            }
        }

        return "<?xml version='1.0' encoding='utf-8'?>\n" + doc.ToString() + "\n";
    }

    // "android:name=value;android:other=value". A value with a semicolon needs the fragment file instead.
    internal static IEnumerable<XAttribute> ParseAttributes(string? text, string property)
    {
        var list = new List<XAttribute>();
        foreach (var part in (text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0 || !part.StartsWith("android:", StringComparison.Ordinal) || eq == "android:".Length)
            {
                throw new CliException(ExitCodes.Usage, "bad_manifest_attribute", $"{property}: '{part}' is not android:name=value. Separate attributes with semicolons.");
            }

            list.Add(new XAttribute(Android + part["android:".Length..eq].Trim(), part[(eq + 1)..].Trim()));
        }

        return list;
    }

    private static void MergeAttributes(XElement target, IEnumerable<XAttribute> attributes, string source, string where)
    {
        foreach (var attribute in attributes)
        {
            var existing = target.Attribute(attribute.Name);
            if (existing is not null && existing.Value != attribute.Value)
            {
                var name = attribute.Name.Namespace == Android ? "android:" + attribute.Name.LocalName : attribute.Name.LocalName;
                throw new CliException(ExitCodes.Usage, "manifest_conflict",
                    $"{source} sets {name}='{attribute.Value}' on {where}, but the export already writes {name}='{existing.Value}'. Remove it from {source}; the engine's value is set by an export option or project property where one exists.");
            }

            target.SetAttributeValue(attribute.Name, attribute.Value);
        }
    }

    private static void AddChild(XElement parent, XElement child, string source)
    {
        var name = (string?)child.Attribute(Android + "name");
        if (name is not null)
        {
            var same = parent.Elements(child.Name).FirstOrDefault(e => (string?)e.Attribute(Android + "name") == name);
            if (same is not null)
            {
                if (XNode.DeepEquals(same, child))
                {
                    return;
                }

                throw new CliException(ExitCodes.Usage, "manifest_conflict",
                    $"{source} declares <{child.Name.LocalName} android:name='{name}'> differently from the one the export already writes. Remove it from {source}.");
            }
        }

        parent.Add(new XElement(child));
    }
}

/// <summary>
/// Launcher icon resources from one foreground image and a background: an adaptive icon for Android 8 and later and density PNGs for the legacy
/// and round entries. Every image is read and scaled through OneImage; an SVG is rendered directly at each size.
/// </summary>
internal static class AndroidIconBuild
{
    // The adaptive icon's layers are 108 dp; launchers show the middle 72 dp, so the legacy icon is cut from there.
    // Android developer docs, "Adaptive icons".
    private static readonly (string Name, int Scale)[] Densities = [("mdpi", 1), ("hdpi", 2), ("xhdpi", 3), ("xxhdpi", 4), ("xxxhdpi", 6)];
    private const int LayerDp = 108;
    private const int VisibleDp = 72;
    private const int LegacyDp = 48;
    private const int MasterSize = LayerDp * 6;

    /// <summary>The resource names the manifest points at.</summary>
    public const string IconResource = "@mipmap/ic_launcher";

    /// <summary>The round launcher icon's resource.</summary>
    public const string RoundIconResource = "@mipmap/ic_launcher_round";

    /// <summary>Writes the <c>res</c> folder and returns warnings (a small source image, for example).</summary>
    public static List<ExportWarning> Write(string res, string iconPath, string? background, string projectDirectory)
    {
        var warnings = new List<ExportWarning>();
        var foreground = Load(iconPath, "AndroidIcon", MasterSize, warnings);
        Rgba32[] backgroundPixels;
        string? colour = null;
        if (string.IsNullOrWhiteSpace(background))
        {
            colour = "#FFFFFF";
        }
        else if (background.StartsWith('#'))
        {
            colour = ParseColour(background);
        }

        if (colour is not null)
        {
            var (r, g, b) = Rgb(colour);
            backgroundPixels = new Rgba32[MasterSize * MasterSize];
            Array.Fill(backgroundPixels, new Rgba32(r, g, b, 255));
        }
        else
        {
            var path = Path.GetFullPath(background!, projectDirectory);
            backgroundPixels = Load(path, "AndroidIconBackground", MasterSize, warnings);
            for (var i = 0; i < backgroundPixels.Length; i++)
            {
                backgroundPixels[i] = new Rgba32(backgroundPixels[i].R, backgroundPixels[i].G, backgroundPixels[i].B, 255);
            }
        }

        // The legacy icon is the composite's middle 72 dp.
        var composite = new Rgba32[MasterSize * MasterSize];
        for (var i = 0; i < composite.Length; i++)
        {
            composite[i] = Over(foreground[i], backgroundPixels[i]);
        }

        var cut = MasterSize * VisibleDp / LayerDp;
        var offset = (MasterSize - cut) / 2;
        var middle = new Rgba32[cut * cut];
        for (var y = 0; y < cut; y++)
        {
            Array.Copy(composite, ((y + offset) * MasterSize) + offset, middle, y * cut, cut);
        }

        foreach (var (name, scale) in Densities)
        {
            var folder = Directory.CreateDirectory(Path.Combine(res, "mipmap-" + name)).FullName;
            WritePng(Path.Combine(folder, "ic_launcher_foreground.png"), foreground, MasterSize, LayerDp * scale);
            if (colour is null)
            {
                WritePng(Path.Combine(folder, "ic_launcher_background.png"), backgroundPixels, MasterSize, LayerDp * scale);
            }

            var legacy = LegacyDp * scale;
            var square = Resize(middle, cut, legacy);
            WritePng(Path.Combine(folder, "ic_launcher.png"), square, legacy, legacy);
            var round = (Rgba32[])square.Clone();
            Circle(round, legacy);
            WritePng(Path.Combine(folder, "ic_launcher_round.png"), round, legacy, legacy);
        }

        var adaptive = Directory.CreateDirectory(Path.Combine(res, "mipmap-anydpi-v26")).FullName;
        var layer = colour is null ? "@mipmap/ic_launcher_background" : "@color/ic_launcher_background";
        foreach (var file in new[] { "ic_launcher.xml", "ic_launcher_round.xml" })
        {
            File.WriteAllText(Path.Combine(adaptive, file),
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n  <background android:drawable=\"{layer}\"/>\n  <foreground android:drawable=\"@mipmap/ic_launcher_foreground\"/>\n</adaptive-icon>\n");
        }

        if (colour is not null)
        {
            var values = Directory.CreateDirectory(Path.Combine(res, "values")).FullName;
            File.WriteAllText(Path.Combine(values, "ic_launcher_colors.xml"), $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n  <color name=\"ic_launcher_background\">{colour}</color>\n</resources>\n");
        }

        return warnings;
    }

    private static string ParseColour(string text)
    {
        var hex = text[1..];
        if (hex.Length == 3 && hex.All(Uri.IsHexDigit))
        {
            return "#" + string.Concat(hex.Select(c => new string(c, 2))).ToUpperInvariant();
        }

        if (hex.Length == 6 && hex.All(Uri.IsHexDigit))
        {
            return "#" + hex.ToUpperInvariant();
        }

        throw new CliException(ExitCodes.Usage, "bad_icon", $"AndroidIconBackground '{text}' is not a colour (#RGB or #RRGGBB) or an image file.");
    }

    private static (byte R, byte G, byte B) Rgb(string colour) =>
        (Convert.ToByte(colour.Substring(1, 2), 16), Convert.ToByte(colour.Substring(3, 2), 16), Convert.ToByte(colour.Substring(5, 2), 16));

    // Reads a square PNG, JPEG, WebP or SVG as straight-alpha pixels at size x size.
    private static Rgba32[] Load(string path, string property, int size, List<ExportWarning> warnings)
    {
        if (!File.Exists(path))
        {
            throw new CliException(ExitCodes.Usage, "bad_icon", $"{property} names {path}, which does not exist.");
        }

        var svg = path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        var options = new OneImage.LoadOptions
        {
            Configuration = svg ? new ImageConfiguration().Add(SvgFormat.Instance) : ImageConfiguration.Default,
            Limits = new ImageLimits { MaxDimension = 16384, MaxPixelsPerFrame = 8192L * 8192, MaxFrames = 1, MaxTotalPixels = 8192L * 8192 },
        };
        try
        {
            if (new FileInfo(path).Length > 32 * 1024 * 1024)
            {
                throw new CliException(ExitCodes.Usage, "bad_icon", $"{property} {Path.GetFileName(path)} is larger than 32 MiB.");
            }

            var image = Image.Load(File.ReadAllBytes(path), options);
            if (image.Width != image.Height)
            {
                throw new CliException(ExitCodes.Usage, "bad_icon", $"{property} {Path.GetFileName(path)} is {image.Width}x{image.Height}; an icon layer must be square.");
            }

            if (!svg && image.Width < size)
            {
                warnings.Add(new("icon_low_resolution", $"{property} {Path.GetFileName(path)} is {image.Width} px; {size} px gives the sharpest result on the densest screens."));
            }

            if (image.Width != size)
            {
                image = image.Resize(size, size, Fit.Stretch);
            }

            return image.ToArray<Rgba32>();
        }
        catch (Exception ex) when (ex is not CliException and not OutOfMemoryException)
        {
            throw new CliException(ExitCodes.Usage, "bad_icon", $"{property} {Path.GetFileName(path)} is not a readable image: {ex.Message}");
        }
    }

    private static Rgba32 Over(Rgba32 top, Rgba32 bottom)
    {
        var a = top.A;
        int Mix(int t, int b) => ((t * a) + (b * (255 - a)) + 127) / 255;
        return new Rgba32((byte)Mix(top.R, bottom.R), (byte)Mix(top.G, bottom.G), (byte)Mix(top.B, bottom.B), (byte)Math.Max(a, bottom.A));
    }

    private static Rgba32[] Resize(Rgba32[] pixels, int from, int to) =>
        from == to ? pixels : Image.FromPixels<Rgba32>(pixels, from, from).Resize(to, to, Fit.Stretch).ToArray<Rgba32>();

    private static void Circle(Rgba32[] pixels, int size)
    {
        var centre = size / 2.0;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = Math.Sqrt(((x + 0.5 - centre) * (x + 0.5 - centre)) + ((y + 0.5 - centre) * (y + 0.5 - centre)));
                var cover = Math.Clamp(centre - d + 0.5, 0, 1);
                var p = pixels[(y * size) + x];
                pixels[(y * size) + x] = new Rgba32(p.R, p.G, p.B, (byte)Math.Round(p.A * cover));
            }
        }
    }

    private static void WritePng(string path, Rgba32[] pixels, int from, int to)
    {
        var scaled = Resize(pixels, from, to);
        var image = Image.FromPixels<Rgba32>(scaled, to, to);
        using var stream = File.Create(path);
        image.Save(stream, PngFormat.Instance);
    }
}
