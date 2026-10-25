namespace Solitaire.Tests;

/// <summary>Deals the tests share.</summary>
internal static class Fixtures
{
    /// <summary>The seed whose draw-1 deal the scenarios win. Found by <see cref="Discovery"/>.</summary>
    public const ulong WinnableSeed = 1;

    private static readonly Lazy<List<SolverMove>> Line = new(() =>
        Solver.Solve(Rig.TableOf(Rig.NewWorld(WinnableSeed)), 2_000_000) ?? throw new InvalidOperationException("The known seed has no solution within the budget."));

    /// <summary>The seed and a line that wins it.</summary>
    public static (ulong Seed, List<SolverMove> Line) Winnable() => (WinnableSeed, Line.Value);
}

/// <summary>One-off helpers used to choose the known seed and to write the scenarios that play it.</summary>
public class Discovery
{
    /// <summary>
    /// Writes <c>01-solve-known-seed.yaml</c> and <c>04-save-continue.yaml</c> into the folder named by <c>SOLITAIRE_WRITE_SCENARIOS</c>: the solver's
    /// line for the known seed as <c>command</c> steps, with the score, move count and state hash a real run of that line produces.
    /// </summary>
    [Fact]
    public void WriteScenarios()
    {
        if (Environment.GetEnvironmentVariable("SOLITAIRE_WRITE_SCENARIOS") is not { Length: > 0 } dir)
        {
            return;
        }

        var (seed, line) = Fixtures.Winnable();
        using var cluster = OneGame.Testing.TestCluster.Start(new SolitaireGame(), new OneGame.Testing.TestOptions { Width = 1280, Height = 720 });
        foreach (var m in line)
        {
            Play(cluster, m);
        }

        var score = cluster.Client.Get("Table", "Score");
        var moves = cluster.Client.Get("Table", "Moves");
        var hash = cluster.Client.Hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
        var finish = $$"""
            expect:
              - state: { entity: Table, property: Phase, equals: 1 }
              - state: { entity: Table, property: Score, equals: {{score}} }
              - state: { entity: Table, property: Moves, equals: {{moves}} }
              - hash: {{hash}}
              - ui: { id: win, visible: true }
              - ui: { id: win-score, text: "Score {{score}}" }
            """;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "01-solve-known-seed.yaml"), $$"""
            name: solve-known-seed
            description: Seed {{seed}} (draw 1) is won by a scripted list of {{line.Count}} commands, one per tick, ending in the win dialog.
            steps:
              - screen: table
            {{Steps(line)}}
            {{finish}}
            """ + "\n");
        var split = line.Count / 2;
        File.WriteAllText(Path.Combine(dir, "04-save-continue.yaml"), $$"""
            name: save-continue
            description: Play half of the winning line, throw the game away, continue from the saved game and finish. The end state matches the straight run exactly.
            steps:
              - screen: table
            {{Steps(line.Take(split))}}
              - fixture: fresh
              - screen: main
              - expect:
                  ui: { id: continue, enabled: true }
              - click: continue
              - expect:
                  state: { entity: Table, property: Moves, equals: {{line.Take(split).Count()}} }
            {{Steps(line.Skip(split))}}
            {{finish}}
            """ + "\n");
    }

    private static void Play(OneGame.Testing.TestCluster cluster, SolverMove m)
    {
        if (m.IsDraw)
        {
            cluster.Client.Send("Table", "Draw");
        }
        else
        {
            cluster.Client.Send("Table", "Move", m.From.ToString(System.Globalization.CultureInfo.InvariantCulture), m.To.ToString(System.Globalization.CultureInfo.InvariantCulture), m.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        cluster.Client.Step(1);
    }

    private static string Steps(IEnumerable<SolverMove> moves) => string.Join("\n", moves.Select(m => m.IsDraw
        ? "  - command: { entity: Table, name: Draw }\n  - step: 1"
        : $"  - command: {{ entity: Table, name: Move, args: [{m.From}, {m.To}, {m.Count}] }}\n  - step: 1"));

    [Fact]
    public void ListSolvableSeeds()
    {
        if (Environment.GetEnvironmentVariable("SOLITAIRE_DISCOVER") is not { Length: > 0 })
        {
            return;
        }

        var report = new List<string>();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var line = Solver.Solve(Rig.TableOf(Rig.NewWorld(seed)), 300_000);
            report.Add($"{seed}: {(line is null ? "none" : line.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))}");
        }

        File.WriteAllLines(Environment.GetEnvironmentVariable("SOLITAIRE_DISCOVER")!, report);
    }
}

/// <summary>Prints a deal for scenario authors.</summary>
public class DealPrinter
{
    [Fact]
    public void PrintSecondDeal()
    {
        if (Environment.GetEnvironmentVariable("SOLITAIRE_PRINT") is not { Length: > 0 } path)
        {
            return;
        }

        var world = Rig.NewWorld(1);
        var t = Rig.TableOf(world);
        t.NewGame(1);
        var lines = new List<string>();
        for (var p = 0; p < Piles.Count; p++)
        {
            lines.Add(Piles.Name(p) + ": " + string.Join(" ", t.Pile(p).Select(c => Cards.Name(c) + (Cards.IsFaceUp(c) ? "+" : ""))));
        }

        var line = Solver.Solve(t, 300_000);
        lines.Add(line is null ? "no line" : string.Join(", ", line.Take(14)));
        File.WriteAllLines(path, lines);
    }
}
