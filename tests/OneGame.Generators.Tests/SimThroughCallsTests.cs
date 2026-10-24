using Microsoft.CodeAnalysis;

namespace OneGame.Generators.Tests;

/// <summary>The sim rules hold through helpers: calls are followed, the diagnostic names the chain, outside APIs are allow-listed.</summary>
public class SimThroughCallsTests
{
    // One violating static method per rule, called Util.Leaf.
    private static readonly (string Rule, string Leaf)[] Leaves =
    [
        ("OG0002", "public static int Leaf() { double d = 1; return (int)(d * 1.5); }"),
        ("OG0003", "public static int Leaf() { var d = new Dictionary<int,int>(); int s = 0; foreach (var kv in d) { s += kv.Value; } return s; }"),
        ("OG0004", "public static int Leaf() => (int)DateTime.Now.Ticks;"),
        ("OG0005", "public static int Leaf() { Task.Run(() => { }); return 0; }"),
        ("OG0006", "public static int Leaf() => File.ReadAllText(\"x\").Length;"),
        ("OG0007", "public static int Leaf() => new Random().Next();"),
    ];

    public static TheoryData<string, string, string, string> Shapes()
    {
        var data = new TheoryData<string, string, string, string>();
        foreach (var (rule, leaf) in Leaves)
        {
            var util = $"static class Util {{ {leaf} }}";
            data.Add("one level", rule, Harness.Entity("protected override void OnTick() { N = Util.Leaf(); }", util), "E.OnTick \u2192 Util.Leaf");
            data.Add("three levels", rule, Harness.Entity("protected override void OnTick() { N = Mid.A(); }",
                util + " static class Mid { public static int A() => B(); static int B() => C(); static int C() => Util.Leaf(); }"),
                "E.OnTick \u2192 Mid.A \u2192 Mid.B \u2192 Mid.C \u2192 Util.Leaf");
            data.Add("extension", rule, Harness.Entity("protected override void OnTick() { N = N.Go(); }",
                util + " static class Ext { public static int Go(this int x) => Util.Leaf() + x; }"), "E.OnTick \u2192 Ext.Go \u2192 Util.Leaf");
            data.Add("lambda to helper", rule, Harness.Entity("protected override void OnTick() { N = Run.It(() => Util.Leaf()); }",
                util + " static class Run { public static int It(Func<int> f) => f(); }"), "E.OnTick \u2192 Util.Leaf");
            data.Add("lambda inside helper", rule, Harness.Entity("protected override void OnTick() { N = Run.It(); }",
                util + " static class Run { public static int It() { Func<int> f = () => Util.Leaf(); return f(); } }"), "E.OnTick \u2192 Run.It \u2192 Util.Leaf");
            data.Add("generic helper", rule, Harness.Entity("protected override void OnTick() { N = G.Id(N); }",
                util + " static class G { public static T Id<T>(T x) { Util.Leaf(); return x; } }"), "E.OnTick \u2192 G.Id \u2192 Util.Leaf");
            data.Add("recursive pair", rule, Harness.Entity("protected override void OnTick() { N = R.A(3); }",
                util + " static class R { public static int A(int n) => n > 0 ? B(n - 1) : Util.Leaf(); static int B(int n) => A(n); }"), "E.OnTick \u2192 R.A \u2192 Util.Leaf");
            data.Add("property getter", rule, Harness.Entity("protected override void OnTick() { N = P.Value; }",
                util + " static class P { public static int Value => Util.Leaf(); }"), "E.OnTick \u2192 P.Value \u2192 Util.Leaf");
            data.Add("constructor", rule, Harness.Entity("protected override void OnTick() { N = new Box().V; }",
                util + " class Box { public int V; public Box() { V = Util.Leaf(); } }"), "E.OnTick \u2192 new Box \u2192 Util.Leaf");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task HelperViolationIsReportedAtTheCallWithItsChain(string shape, string rule, string source, string chain)
    {
        var found = await Harness.DiagnoseAll(source);
        var hit = found.Where(d => d.Id == rule && d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(hit.Count > 0, shape + ": expected " + rule + " but got:\n" + Harness.Describe(found));
        Assert.Contains(hit, d => d.GetMessage().Contains(chain, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecursionWithoutAViolationIsCleanAndEnds()
    {
        var source = Harness.Entity("protected override void OnTick() { N = R.A(3); }",
            "static class R { public static int A(int n) => n > 0 ? B(n - 1) : 0; public static int B(int n) => A(n) + 1; }");
        Assert.Empty(await Harness.DiagnoseAll(source));
    }

    [Fact]
    public async Task ViolationStaysOneDiagnosticPerCallSite()
    {
        var source = Harness.Entity("protected override void OnTick() { N = Util.Leaf() + Util.Leaf(); }", "static class Util { public static int Leaf() => (int)DateTime.Now.Ticks; }");
        Assert.Equal(2, (await Harness.DiagnoseAll(source)).Count(d => d.Id == "OG0004"));
    }

    [Fact]
    public async Task AllowedFrameworkAndEngineApisAreClean()
    {
        var source = Harness.Entity(@"
  private readonly List<int> _items = new();
  private readonly SortedDictionary<int,int> _sorted = new();
  private readonly Dictionary<int,int> _lookup = new();
  protected override void OnTick()
  {
    N = Math.Max(Math.Abs(N), 3) + (int)BitOperations.PopCount((uint)N);
    _items.Add(N); _items.Sort(); Array.Sort(new[] { 3, 1, 2 });
    foreach (var kv in _sorted) { N += kv.Value; }
    _lookup[1] = 2; N += _lookup.TryGetValue(1, out var v) ? v : 0; N += _lookup.Count;
    var sb = new System.Text.StringBuilder(); sb.Append('x').Append(""y"").Append(N.ToString(CultureInfo.InvariantCulture));
    N += string.Compare(""a"", ""b"", StringComparison.Ordinal) + sb.Length + ""abc"".IndexOf('b') + (""a"".Equals(""b"") ? 1 : 0);
    N += _items.Select(x => x + 1).Sum() + _items.Count;
    Span<int> span = stackalloc int[4]; span.Fill(2); N += span.Length;
    var f = Fixed.FromInt(2) * Fixed.FromRatio(1, 3); N += Fixed.Floor(f).ToInt();
    N += World!.Rng[""a""].NextBool() ? 1 : 0;
  }
", "");
        source = source.Replace("using OneGame.Core;", "using OneGame.Core; using System.Numerics; using System.Globalization;");
        var found = await Harness.DiagnoseAll(source);
        Assert.True(found.Count == 0, Harness.Describe(found));
    }

    private const string UriCall = "protected override void OnTick() { N = new Uri(\"http://x\").Host.Length; }";

    [Fact]
    public async Task UnknownOutsideApiIsAWarning()
    {
        var found = await Harness.DiagnoseAll(Harness.Entity(UriCall));
        Assert.Contains(found, d => d.Id == "OG0015" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task UnknownOutsideApiInAHelperNamesTheChain()
    {
        var found = await Harness.DiagnoseAll(Harness.Entity("protected override void OnTick() { N = H.Len(); }",
            "static class H { public static int Len() => new Uri(\"http://x\").Host.Length; }"));
        Assert.Contains(found, d => d.Id == "OG0015" && d.GetMessage().Contains("E.OnTick \u2192 H.Len \u2192 ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("attribute")]
    [InlineData("editorconfig")]
    [InlineData("sim-safe helper")]
    public async Task OptInSilencesTheWarning(string how)
    {
        var source = Harness.Entity(UriCall);
        string? trusted = null;
        switch (how)
        {
            case "attribute":
                source = source.Replace("namespace T;", "[assembly: SimTrust(\"System.Uri\")]\nnamespace T;", StringComparison.Ordinal);
                break;
            case "editorconfig":
                trusted = "Other.Lib; System.Uri";
                break;
            default:
                source = Harness.Entity("protected override void OnTick() { N = H.Len(); }",
                    "static class H { [SimSafe] public static int Len() => new Uri(\"http://x\").Host.Length; }");
                break;
        }

        Assert.Empty(await Harness.DiagnoseAll(source, trusted));
    }

    public static TheoryData<string, bool> Culture() => new()
    {
        { "N.ToString()", true },
        { "int.Parse(\"1\")", true },
        { "int.TryParse(\"1\", out var _)", true },
        { "\"a\".ToUpper().Length", true },
        { "string.Format(\"{0}\", N).Length", true },
        { "$\"{N}\".Length", true },
        { "(\"a\" + N).Length", true },
        { "\"abc\".StartsWith(\"a\") ? 1 : 0", true },
        { "string.Compare(\"a\", \"b\")", true },
        { "\"abc\".IndexOf(\"b\", StringComparison.CurrentCulture)", true },
        { "CultureInfo.CurrentCulture.Name.Length", true },
        { "Convert.ToInt32(\"5\")", true },
        { "new System.Text.StringBuilder().Append(N).Length", true },
        { "N.ToString(CultureInfo.InvariantCulture).Length", false },
        { "int.Parse(\"1\", CultureInfo.InvariantCulture)", false },
        { "\"a\".ToUpperInvariant().Length", false },
        { "\"abc\".StartsWith(\"a\", StringComparison.Ordinal) ? 1 : 0", false },
        { "\"abc\".IndexOf(\"b\", StringComparison.InvariantCulture)", false },
        { "string.Create(CultureInfo.InvariantCulture, $\"{N}\").Length", false },
        { "FormattableString.Invariant($\"{N}\").Length", false },
        { "N.ToString(\"x2\", CultureInfo.InvariantCulture).Length", false },
        { "string.CompareOrdinal(\"a\", \"b\")", false },
        { "$\"{\"a\"}{true}\".Length", false },
        { "Convert.ToInt32(\"5\", CultureInfo.InvariantCulture)", false },
    };

    [Theory]
    [MemberData(nameof(Culture))]
    public async Task CultureSensitiveTextIsADiagnosticAndInvariantIsNot(string expression, bool fires)
    {
        var source = Harness.Entity($"protected override void OnTick() {{ N = (int)({expression}); }}")
            .Replace("using OneGame.Core;", "using OneGame.Core; using System.Globalization;");
        var found = await Harness.DiagnoseAll(source);
        var culture = found.Where(d => d.Id == "OG0016").ToList();
        Assert.True(fires == culture.Count > 0, expression + "\n" + Harness.Describe(found));
        if (!fires)
        {
            Assert.Empty(found);
        }
    }

    [Fact]
    public async Task CultureSensitiveHelperIsFollowed()
    {
        var source = Harness.Entity("protected override void OnTick() { N = H.Text(N).Length; }", "static class H { public static string Text(int n) => n.ToString(); }");
        var found = await Harness.DiagnoseAll(source);
        Assert.Contains(found, d => d.Id == "OG0016" && d.GetMessage().Contains("E.OnTick \u2192 H.Text \u2192 ", StringComparison.Ordinal));
    }
}
