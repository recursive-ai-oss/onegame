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

/// <summary>One-off helpers used to choose the known seed and to write the scenario that plays it.</summary>
public class Discovery
{
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
