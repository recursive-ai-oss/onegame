namespace OneGame.Generators.Tests;

public class AnalyzerRuleTests
{
    public static TheoryData<string, string, string> Violations() => new()
    {
        // OG0001: float in synced state
        { "float property", Harness.Entity("[Synced] public partial float Speed { get; set; }"), "OG0001" },
        { "double property", Harness.Entity("[Synced] public partial double Speed { get; set; }"), "OG0001" },
        { "decimal property", Harness.Entity("[Synced] public partial decimal Cash { get; set; }"), "OG0001" },
        { "float list", Harness.Entity("[Synced] public SyncList<float> Weights { get; } = new();"), "OG0001" },
        { "float command argument", Harness.Entity("[Command] public void Push(float force) { }"), "OG0001" },
        { "float in synced struct", Harness.Entity("", "[SyncedStruct] public partial struct P { public float X { get; set; } }"), "OG0001" },
        { "float in hot data", Harness.Entity("", "[HotData] public struct H { public double X { get; set; } }"), "OG0001" },

        // OG0002: float arithmetic in sim code
        { "float literal math", Harness.Entity("protected override void OnTick() { var x = 1.5f * N; }"), "OG0002" },
        { "Math.Sqrt", Harness.Entity("protected override void OnTick() { N = (int)Math.Sqrt(N); }"), "OG0002" },
        { "double local", Harness.Entity("protected override void OnTick() { double d = N; N = (int)d; }"), "OG0002" },
        { "float field", Harness.Entity("private float _speed;"), "OG0002" },
        { "float parameter", Harness.Entity("private int Scale(int a, float k) => 0;"), "OG0002" },
        { "Fixed.ToSingle", Harness.Entity("protected override void OnTick() { var f = Fixed.One.ToSingle(); }"), "OG0002" },

        // OG0003: unordered iteration
        { "foreach Dictionary", Harness.Entity("private Dictionary<int,int> _d = new(); protected override void OnTick() { foreach (var kv in _d) { N += kv.Value; } }"), "OG0003" },
        { "foreach HashSet", Harness.Entity("private HashSet<int> _h = new(); protected override void OnTick() { foreach (var v in _h) { N += v; } }"), "OG0003" },
        { "foreach dict.Keys", Harness.Entity("private Dictionary<int,int> _d = new(); protected override void OnTick() { foreach (var k in _d.Keys) { N += k; } }"), "OG0003" },
        { "foreach IDictionary", Harness.Entity("private IDictionary<int,int> _d = new SortedDictionary<int,int>(); protected override void OnTick() { foreach (var k in _d) { N += k.Key; } }"), "OG0003" },
        { "LINQ over dictionary", Harness.Entity("private Dictionary<int,int> _d = new(); protected override void OnTick() { N = _d.Select(kv => kv.Value).First(); }"), "OG0003" },
        { "LINQ over HashSet", Harness.Entity("private HashSet<int> _h = new(); protected override void OnTick() { N = _h.ToList()[0]; }"), "OG0003" },

        // OG0004: clock
        { "DateTime.Now", Harness.Entity("protected override void OnTick() { var t = DateTime.Now; }"), "OG0004" },
        { "DateTime.UtcNow", Harness.Entity("protected override void OnTick() { var t = DateTime.UtcNow; }"), "OG0004" },
        { "DateTimeOffset.UtcNow", Harness.Entity("protected override void OnTick() { var t = DateTimeOffset.UtcNow; }"), "OG0004" },
        { "Stopwatch", Harness.Entity("protected override void OnTick() { var s = Stopwatch.StartNew(); }"), "OG0004" },
        { "Environment.TickCount", Harness.Entity("protected override void OnTick() { N = Environment.TickCount; }"), "OG0004" },
        { "TimeProvider", Harness.Entity("protected override void OnTick() { var t = TimeProvider.System.GetUtcNow(); }"), "OG0004" },

        // OG0005: threads and tasks
        { "Task.Run", Harness.Entity("protected override void OnTick() { Task.Run(() => { }); }"), "OG0005" },
        { "new Thread", Harness.Entity("protected override void OnTick() { new Thread(() => { }).Start(); }"), "OG0005" },
        { "Parallel.For", Harness.Entity("protected override void OnTick() { Parallel.For(0, 3, i => { }); }"), "OG0005" },
        { "ThreadPool", Harness.Entity("protected override void OnTick() { ThreadPool.QueueUserWorkItem(_ => { }); }"), "OG0005" },
        { "lock", Harness.Entity("private readonly object _gate = new(); protected override void OnTick() { lock (_gate) { N++; } }"), "OG0005" },
        { "async method", Harness.Entity("private async Task Later() { await Task.Delay(1); }"), "OG0005" },
        { "AsParallel", Harness.Entity("protected override void OnTick() { N = L.AsParallel().Sum(); }"), "OG0005" },

        // OG0006: I/O
        { "File.ReadAllText", Harness.Entity("protected override void OnTick() { var s = File.ReadAllText(\"x\"); }"), "OG0006" },
        { "Console", Harness.Entity("protected override void OnTick() { Console.WriteLine(N); }"), "OG0006" },
        { "Environment variable", Harness.Entity("protected override void OnTick() { var s = Environment.GetEnvironmentVariable(\"X\"); }"), "OG0006" },
        { "HttpClient", Harness.Entity("protected override void OnTick() { var c = new HttpClient(); }"), "OG0006" },
        { "FileStream", Harness.Entity("protected override void OnTick() { using var f = new FileStream(\"x\", FileMode.Open); }"), "OG0006" },

        // OG0007: not a named stream
        { "new Random", Harness.Entity("protected override void OnTick() { N = new Random(1).Next(); }"), "OG0007" },
        { "Random.Shared", Harness.Entity("protected override void OnTick() { N = Random.Shared.Next(); }"), "OG0007" },
        { "Guid.NewGuid", Harness.Entity("protected override void OnTick() { var g = Guid.NewGuid(); }"), "OG0007" },
        { "RandomNumberGenerator", Harness.Entity("protected override void OnTick() { N = RandomNumberGenerator.GetInt32(5); }"), "OG0007" },

        // OG0008: view code changing state
        { "view assigns synced property", Harness.Entity("[View] public void Show() { N = 5; }"), "OG0008" },
        { "view increments synced property", Harness.Entity("[View] public void Show() { N++; }"), "OG0008" },
        { "view compound assignment", Harness.Entity("[View] public int Show() { N += 2; return N; }"), "OG0008" },
        { "view mutates synced list", Harness.Entity("[View] public void Show() { L.Add(1); }"), "OG0008" },
        { "view sets list element", Harness.Entity("[View] public void Show() { L[0] = 1; }"), "OG0008" },
        { "view calls command handler directly", Harness.Entity("[View] public void Show() { Bump(); }"), "OG0008" },
        { "view calls helper that writes", Harness.Entity("private void Helper() { N = 1; } [View] public void Show() { Helper(); }"), "OG0008" },
        { "view calls helper two levels deep", Harness.Entity("private void A() { B(); } private void B() { L.Clear(); } [View] public void Show() { A(); }"), "OG0008" },
        { "view property setter writes", Harness.Entity("private int Mirror { get => 0; set { N = value; } } [View] public void Show() { Mirror = 3; }"), "OG0008" },
        { "view draws from a random stream", Harness.Entity("[View] public int Show(World w) => w.Rng[\"x\"].NextInt(3);"), "OG0008" },
        { "view class writes entity state", Harness.Entity("", "[View] public class Hud { public void Poke(E e) { e.N = 1; } }"), "OG0008" },
        { "view steps the world", Harness.Entity("", "[View] public class Hud { public void Poke(World w) { w.Step(); } }"), "OG0008" },

        // OG0009: no codec
        { "object property", Harness.Entity("[Synced] public partial object Thing { get; set; }"), "OG0009" },
        { "List property", Harness.Entity("[Synced] public partial List<int> Items { get; set; }"), "OG0009" },
        { "nullable int property", Harness.Entity("[Synced] public partial int? Maybe { get; set; }"), "OG0009" },
        { "array property", Harness.Entity("[Synced] public partial int[] Values { get; set; }"), "OG0009" },
        { "unsupported list element", Harness.Entity("[Synced] public SyncList<object> Things { get; } = new();"), "OG0009" },
        { "struct without codec", Harness.Entity("[Synced] public partial Plain P { get; set; }", "public struct Plain { public int A; }"), "OG0009" },
        { "synced struct member without codec", Harness.Entity("", "[SyncedStruct] public partial struct P { public object O { get; set; } }"), "OG0009" },
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public async Task RuleFires(string name, string source, string id)
    {
        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Any(d => d.Id == id), $"{name}: expected {id}, got:\n{Harness.Describe(diagnostics)}");
        // Nothing else should be broken: no compile errors from the generated code, and only the rule under test.
        Assert.True(diagnostics.All(d => d.Id == id), $"{name}: unexpected extra diagnostics:\n{Harness.Describe(diagnostics.Where(d => d.Id != id))}");
    }

    public static TheoryData<string, string> Clean() => new()
    {
        { "fixed-point sim", Harness.Entity("protected override void OnTick() { N += (Fixed.One * 3).ToInt(); }") },
        { "foreach list and array", Harness.Entity("private List<int> _l = new(); protected override void OnTick() { foreach (var v in _l) { N += v; } foreach (var v in L) { N += v; } }") },
        { "sorted collections", Harness.Entity("private SortedDictionary<int,int> _d = new(); private SortedSet<int> _s = new(); protected override void OnTick() { foreach (var kv in _d) { N += kv.Value; } foreach (var v in _s) { N += v; } }") },
        { "dictionary lookups", Harness.Entity("private Dictionary<int,int> _d = new(); protected override void OnTick() { if (_d.TryGetValue(1, out var v)) { N = v; } _d[2] = N; }") },
        { "named stream", Harness.Entity("protected override void OnTick() { N = World!.Rng[\"loot\"].NextInt(10); }") },
        { "world tick as time", Harness.Entity("protected override void OnTick() { N = (int)World!.Tick; }") },
        { "view reads and sends commands", Harness.Entity("[View] public string Show() => $\"{N}/{L.Count}\"; [View] public void Click() { Send.Bump(); }") },
        { "view uses floats and the clock", Harness.Entity("[View] public float Alpha() => (float)(DateTime.Now.Ticks % 100) / 100f * N; [View] public double Phase() => Math.Sin(Stopwatch.GetTimestamp());") },
        { "view class reads", Harness.Entity("", "[View] public class Hud { public float Fill(E e) => e.N / 10f; public void Click(E e) { e.Send.Bump(); } }") },
        { "view calls pure helper", Harness.Entity("private int Twice(int a) => a * 2; [View] public int Show() => Twice(N);") },
        { "view recursion without writes", Harness.Entity("private int Down(int a) => a <= 0 ? 0 : Down(a - 1); [View] public int Show() => Down(N);") },
        { "sim system with fixed-point", Harness.Entity("", "public sealed class Sys : ISimSystem { public void Tick(World w) { var e = w.Entities.Count; } }") },
        { "plain class is not checked", Harness.Entity("", "public class Tool { public float Now() => (float)DateTime.Now.Ticks; }") },
        { "full-featured entity", Harness.Entity("[Command] public bool Take(int n, string who) { if (n > N) { return false; } N -= n; L.Add(n); return true; } [Migrate(1)] static void Old(ref SyncReader r, E e) { e.N = r.ReadInt32(); }", "") },
    };

    [Theory]
    [MemberData(nameof(Clean))]
    public async Task CleanCodeHasNoDiagnostics(string name, string source)
    {
        // Schema 1 entity with a migration needs schema > 1; the last case adjusts it.
        if (name == "full-featured entity")
        {
            source = source.Replace("[Entity]", "[Entity(Schema = 2)]");
        }

        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Count == 0, $"{name}:\n{Harness.Describe(diagnostics)}");
    }

    [Fact]
    public async Task SimSystemsAreCheckedLikeEntities()
    {
        var diagnostics = await Harness.Diagnose(Harness.Usings
            + "namespace T; public sealed class Sys : ISimSystem { public void Tick(World w) { var t = DateTime.Now; float f = 1f; } }");
        Assert.Contains(diagnostics, d => d.Id == "OG0004");
        Assert.Contains(diagnostics, d => d.Id == "OG0002");
    }

    [Fact]
    public async Task SimAttributeMarksAnyClassAsSimCode()
    {
        var diagnostics = await Harness.Diagnose(Harness.Usings + "namespace T; [Sim] public static class Rules { public static int Roll() => new Random().Next(); }");
        Assert.Contains(diagnostics, d => d.Id == "OG0007");
    }

    [Fact]
    public async Task ViewAttributeOnAMemberOverridesTheSimDefault()
    {
        // The same expression is an error in OnTick and fine in a [View] member.
        var bad = await Harness.Diagnose(Harness.Entity("protected override void OnTick() { var f = Math.Sin(1.0); }"));
        var good = await Harness.Diagnose(Harness.Entity("[View] public double Wave() => Math.Sin(1.0);"));
        Assert.Contains(bad, d => d.Id == "OG0002");
        Assert.Empty(good);
    }

    [Fact]
    public async Task MessagesNameTheFix()
    {
        var diagnostics = await Harness.Diagnose(Harness.Entity("[View] public void Show() { N = 5; }"));
        var message = diagnostics.Single(d => d.Id == "OG0008").GetMessage();
        Assert.Contains("Send", message);
        diagnostics = await Harness.Diagnose(Harness.Entity("private Dictionary<int,int> _d = new(); protected override void OnTick() { foreach (var kv in _d) { } }"));
        Assert.Contains("SortedDictionary", diagnostics.Single(d => d.Id == "OG0003").GetMessage());
        diagnostics = await Harness.Diagnose(Harness.Entity("protected override void OnTick() { var r = new Random(); }"));
        Assert.Contains("Rng", diagnostics.Single(d => d.Id == "OG0007").GetMessage());
    }
}
