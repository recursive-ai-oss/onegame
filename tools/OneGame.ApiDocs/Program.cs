using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OneGame.ApiDocs;

static class Program
{
    static int Main(string[] args)
    {
        string root = FindRoot();
        string src = Path.Combine(root, "src");
        string artifacts = Environment.GetEnvironmentVariable("ONEGAME_ARTIFACTS") is { Length: > 0 } a ? a : Path.Combine(root, "artifacts");
        string bin = Path.Combine(artifacts, "bin");
        string config = "release";
        string out_ = Path.Combine(root, "docs", "api");
        bool check = false;
        string? report = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out": out_ = Path.GetFullPath(args[++i]); break;
                case "--bin": bin = Path.GetFullPath(args[++i]); break;
                case "--src": src = Path.GetFullPath(args[++i]); break;
                case "--config": config = args[++i].ToLowerInvariant(); break;
                case "--report": report = args[++i]; break;
                case "--check": check = true; break;
                default:
                    Console.Error.WriteLine("usage: onegame-apidocs [--out docs/api] [--bin <artifacts>/bin] [--src src] [--config release] [--check] [--report file]");
                    return 2;
            }
        }

        var assemblies = new List<AssemblyInfo>();
        var noAssembly = new List<string>();
        foreach (string dir in Directory.GetDirectories(src).OrderBy(d => d, StringComparer.Ordinal))
        {
            string name = Path.GetFileName(dir);
            string? csproj = Directory.GetFiles(dir, "*.csproj").FirstOrDefault();
            if (csproj is null) continue;
            string text = File.ReadAllText(csproj);
            if (Regex.IsMatch(text, @"<IsPackable>\s*false\s*</IsPackable>")) continue;
            string asmName = Regex.Match(text, @"<AssemblyName>([^<]+)</AssemblyName>") is { Success: true } m ? m.Groups[1].Value : name;
            string description = Regex.Match(text, @"<Description>([^<]*)</Description>") is { Success: true } d ? System.Net.WebUtility.HtmlDecode(d.Groups[1].Value) : "";
            string package = Regex.Match(text, @"<PackageId>([^<]+)</PackageId>") is { Success: true } p ? p.Groups[1].Value : "RecursiveAI.OneGame" + name["OneGame".Length..];
            if (text.Contains("<PackageType>MSBuildSdk</PackageType>"))
            {
                noAssembly.Add(name);
                continue;
            }
            string dll = Path.Combine(bin, name, config, asmName + ".dll");
            string xml = Path.ChangeExtension(dll, ".xml");
            if (!File.Exists(dll) || !File.Exists(xml))
            {
                Console.Error.WriteLine($"missing build output for {name}: {dll} (build the solution in Release first)");
                return 2;
            }
            var info = new AssemblyInfo { Name = name, Package = package, Description = description };
            foreach (XElement mem in XDocument.Load(xml).Descendants("member"))
                if ((string?)mem.Attribute("name") is { } id) info.Docs[id] = mem;
            AssemblyReader.Read(dll, info);
            assemblies.Add(info);
        }

        // Paths and anchors first: links between pages need both before any page is written.
        var typeByKey = new Dictionary<string, TypeInfo>(StringComparer.Ordinal);
        var allXml = new Dictionary<string, XElement>(StringComparer.Ordinal);
        var seenPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (AssemblyInfo asm in assemblies)
        {
            foreach (var kv in asm.Docs) allXml[kv.Key] = kv.Value;
            foreach (TypeInfo t in Pages.All(asm.Types))
            {
                typeByKey[t.Key] = t;
                string file = string.Join('.', t.Names.Select(FileSafe));
                t.Path = asm.Name + "/" + (t.Namespace.Length == 0 ? "_global" : t.Namespace) + "/" + file + ".md";
                if (seenPaths.TryGetValue(t.Path, out string? other))
                {
                    Console.Error.WriteLine($"two types share the page {t.Path}: {other} and {t.Key}");
                    return 2;
                }
                seenPaths[t.Path] = t.Key;
                AssignAnchors(t);
            }
        }

        var links = new Links(typeByKey);
        var docs = new DocLookup(allXml, typeByKey);
        var pages = new Pages(assemblies, typeByKey, links, docs, File.Exists(Path.Combine(out_, "sdk.md")), noAssembly);
        SortedDictionary<string, string> files = pages.Build();

        int rc = 0;
        if (check) rc = Compare(out_, files);
        else Write(out_, files);

        var sb = new StringBuilder();
        sb.Append("packages: ").Append(assemblies.Count).Append(", pages: ").Append(files.Count).Append('\n');
        foreach (AssemblyInfo asm in assemblies)
        {
            TypeInfo[] types = [.. Pages.All(asm.Types)];
            sb.Append($"  {asm.Name}: {types.Select(t => t.Namespace).Distinct().Count()} namespaces, {types.Length} types, {types.Sum(t => t.Members.Count)} members\n");
        }
        sb.Append($"unresolved crefs: {links.Unresolved.Count}\n");
        foreach (string u in links.Unresolved.Distinct().OrderBy(x => x, StringComparer.Ordinal)) sb.Append("  ").Append(u).Append('\n');
        sb.Append($"crefs to sibling libraries (plain names): {links.ToSibling}\n");
        sb.Append($"crefs matched by name only: {links.Rescued}\n");
        sb.Append($"inheritdoc without a source: {docs.UnresolvedInheritDoc}\n");
        foreach (string u in docs.UnresolvedInheritDocIds.OrderBy(x => x, StringComparer.Ordinal)) sb.Append("  ").Append(u).Append('\n');
        if (report is not null) File.WriteAllText(report, sb.ToString());
        Console.Error.WriteLine($"{assemblies.Count} packages, {files.Count} pages, unresolved crefs: {links.Unresolved.Count}" + (report is null ? " (use --report <file> for the list)" : ""));
        return rc;
    }

    static string FileSafe(string metadataName) => metadataName.Replace('`', '-');

    static void AssignAnchors(TypeInfo t)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var ordered = t.Kind == TypeKind.Enum ? t.Members : [.. t.Members.OrderBy(m => m.Kind).ThenBy(m => m.SortKey, StringComparer.Ordinal)];
        foreach (MemberInfo m in ordered)
        {
            string types = string.Join(' ', m.Params.Select(p => TypeText.Plain(p.Type)));
            string baseName = m.Kind == MemberKind.Property && m.Params.Count > 0 ? "item" : m.Kind == MemberKind.Constructor ? "ctor" : m.Name;
            string raw = baseName + " " + string.Join(' ', m.TypeParams) + " " + types;
            string a = "m-" + Regex.Replace(raw.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            string candidate = a;
            for (int n = 2; !used.Add(candidate); n++) candidate = a + "-" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            m.Anchor = candidate;
        }
    }

    static void Write(string outDir, SortedDictionary<string, string> files)
    {
        Directory.CreateDirectory(outDir);
        foreach (string stale in StaleFiles(outDir, files).ToList()) File.Delete(stale);
        foreach (var kv in files)
        {
            string full = Path.Combine(outDir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, new UTF8Encoding(false).GetBytes(kv.Value));
        }
        // Folders left empty by deleted pages.
        foreach (string d in Directory.GetDirectories(outDir, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length))
            if (Directory.GetFileSystemEntries(d).Length == 0) Directory.Delete(d);
    }

    // Everything under the output folder that this tool owns but did not write this time. The two hand-written files stay.
    static IEnumerable<string> StaleFiles(string outDir, SortedDictionary<string, string> files)
    {
        if (!Directory.Exists(outDir)) yield break;
        foreach (string f in Directory.GetFiles(outDir, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(outDir, f).Replace('\\', '/');
            if (rel is "README.md" or "sdk.md" || files.ContainsKey(rel)) continue;
            yield return f;
        }
    }

    static int Compare(string outDir, SortedDictionary<string, string> files)
    {
        int diffs = 0;
        foreach (var kv in files)
        {
            string full = Path.Combine(outDir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
            byte[] want = new UTF8Encoding(false).GetBytes(kv.Value);
            if (!File.Exists(full)) { Console.Error.WriteLine("missing: docs/api/" + kv.Key); diffs++; }
            else if (!File.ReadAllBytes(full).AsSpan().SequenceEqual(want)) { Console.Error.WriteLine("differs: docs/api/" + kv.Key); diffs++; }
        }
        foreach (string s in StaleFiles(outDir, files)) { Console.Error.WriteLine("stale: " + Path.GetRelativePath(outDir, s)); diffs++; }
        if (diffs > 0) Console.Error.WriteLine($"{diffs} file(s) out of date: run `dotnet run --project tools/OneGame.ApiDocs -c Release -- --out docs/api` after a Release build and commit the result.");
        return diffs == 0 ? 0 : 1;
    }

    static string FindRoot()
    {
        for (string? d = Directory.GetCurrentDirectory(); d is not null; d = Path.GetDirectoryName(d))
            if (File.Exists(Path.Combine(d, "OneGame.slnx"))) return d;
        return Directory.GetCurrentDirectory();
    }
}
