namespace OneGame.Generators.Tests;

public class VisibilityRuleTests
{
    private const string Owner = "[Synced, OwnerKey] public partial uint Who { get; set; }\n";
    private const string Team = "[Synced, TeamKey] public partial uint Side { get; set; }\n";

    public static TheoryData<string, string, string> Violations() => new()
    {
        // OG0017: owner and team keys
        { "two owner keys", Harness.Entity(Owner + "[Synced, OwnerKey] public partial uint Other { get; set; }"), "OG0017" },
        { "two team keys", Harness.Entity(Team + "[Synced, TeamKey] public partial uint Other { get; set; }"), "OG0017" },
        { "owner key not synced", Harness.Entity("[OwnerKey] public uint Who { get; set; }"), "OG0017" },
        { "owner key is a string", Harness.Entity("[Synced, OwnerKey] public partial string Who { get; set; }"), "OG0017" },
        { "team key is a long", Harness.Entity("[Synced, TeamKey] public partial long Side { get; set; }"), "OG0017" },
        { "owner key is a list", Harness.Entity("[Synced, OwnerKey] public SyncList<int> Who { get; } = new();"), "OG0017" },

        // OG0018: visibility that has nothing to match against
        { "owner member without owner key", Harness.Entity("[Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }"), "OG0018" },
        { "team member without team key", Harness.Entity(Owner + "[Synced(Visibility = Visibility.Team)] public partial int Stamina { get; set; }"), "OG0018" },
        { "owner entity without owner key", Harness.Usings + "namespace T; [Entity(Visibility = Visibility.Owner)] public partial class E : Entity { }", "OG0018" },
        { "team entity without team key", Harness.Usings + "namespace T; [Entity(Visibility = Visibility.Team)] public partial class E : Entity { }", "OG0018" },
        { "owner only command without owner key", Harness.Entity("[Command(OwnerOnly = true)] public void Rename(int a) { }"), "OG0018" },
        { "hot data owner visibility", Harness.Entity("", "[HotData(Visibility = Visibility.Owner)] public struct H { public int X { get; set; } }"), "OG0018" },

        // OG0019: a view reading something no replica ever holds
        { "view reads a hidden member", Harness.Entity("[Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }\n[View] public int Shown => Secret;"), "OG0019" },
        { "view class reads a hidden member", Harness.Entity("[Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }", "[View] public class Hud { public int Read(E e) => e.Secret; }"), "OG0019" },
        { "view reads a member of a hidden entity", Harness.Usings + "namespace T; [Entity(Visibility = Visibility.Hidden)] public partial class H : Entity { [Synced] public partial int Mood { get; set; } }\n[View] public class Hud { public int Read(H h) => h.Mood; }", "OG0019" },
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public async Task MisuseIsACompileError(string name, string source, string id)
    {
        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Any(d => d.Id == id), $"{name}: expected {id}, got:\n{Harness.Describe(diagnostics)}");
    }

    public static TheoryData<string, string> Fine() => new()
    {
        { "all four visibilities with keys", Harness.Entity(Owner + Team
            + "[Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }\n"
            + "[Synced(Visibility = Visibility.Team)] public SyncList<int> Plans { get; } = new();\n"
            + "[Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }") },
        { "int keys", Harness.Entity("[Synced, OwnerKey] public partial int Who { get; set; }\n[Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }") },
        { "view reads an owner member", Harness.Entity(Owner + "[Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }\n[View] public int Shown => Gold;") },
        { "sim reads a hidden member", Harness.Entity("[Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }\nprotected override void OnTick() { Secret++; N = Secret; }") },
        { "owner only command with key", Harness.Entity(Owner + "[Command(OwnerOnly = true)] public void Rename(int a) { N = a; }") },
        { "hidden hot data and hidden entity", Harness.Usings + "namespace T; [Entity(Visibility = Visibility.Hidden)] public partial class H : Entity { [Synced] public partial int Mood { get; set; } }\n[HotData(Visibility = Visibility.Hidden)] public struct R { public int X { get; set; } }" },
        { "command reply", Harness.Entity("[Command] public CommandReply Spend(int a) => a > N ? CommandReply.Reject(\"no\") : CommandReply.Ok;\n[Command] public CommandReply Maybe(int a) => a > N;") },
    };

    [Theory]
    [MemberData(nameof(Fine))]
    public async Task ValidDeclarationsCompileCleanly(string name, string source)
    {
        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Count == 0, $"{name}:\n{Harness.Describe(diagnostics)}");
    }

    [Fact]
    public async Task TheReadOnlyViewLeavesOutHiddenMembers()
    {
        var members = "[Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }\n[Synced] public partial int Shown { get; set; }";
        Assert.Empty(await Harness.Diagnose(Harness.Entity(members, "public static class Use { public static int Read(E e) => e.AsReadOnly.Shown; }")));
        var diagnostics = await Harness.Diagnose(Harness.Entity(members, "public static class Use { public static int Read(E e) => e.AsReadOnly.Secret; }"));
        Assert.Contains(diagnostics, d => d.Id == "CS1061");
    }
}
