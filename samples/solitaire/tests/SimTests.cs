using OneGame.Core;

namespace Solitaire.Tests;

/// <summary>Builds worlds with the game's entity set, without a window or a UI.</summary>
internal static class Rig
{
    public static World NewWorld(ulong seed = 1, int drawMode = 1)
    {
        var registry = new EntityRegistry();
        new SolitaireGame().RegisterEntities(registry);
        var world = new World(seed, registry);
        world.Add(new Table()).NewGame(drawMode);
        return world;
    }

    public static Table TableOf(World world) => (Table)world.Entities[0];

    /// <summary>Plays a solver line on a table, directly through the command handlers.</summary>
    public static void Play(Table table, IEnumerable<SolverMove> moves)
    {
        foreach (var m in moves)
        {
            var ok = m.IsDraw ? table.Draw() : table.Move(m.From, m.To, m.Count);
            Assert.True(ok, $"the game refused {m}");
        }
    }
}

public class SimTests
{
    [Fact]
    public void DealHasSevenColumnsAndATwentyFourCardStock()
    {
        var t = Rig.TableOf(Rig.NewWorld(5));
        for (var c = 0; c < 7; c++)
        {
            var column = t.Pile(Piles.Tableau + c);
            Assert.Equal(c + 1, column.Count);
            Assert.True(Cards.IsFaceUp(column[^1]));
            Assert.All(column.Take(c), card => Assert.False(Cards.IsFaceUp(card)));
        }

        Assert.Equal(24, t.Stock.Count);
        var all = Enumerable.Range(0, 13).SelectMany(p => t.Pile(p)).Select(Cards.Id).Order().ToArray();
        Assert.Equal(Enumerable.Range(0, 52), all);
    }

    [Fact]
    public void SameSeedDealsTheSameAndDifferentSeedsDiffer()
    {
        Assert.Equal(Rig.NewWorld(9).ComputeHash(), Rig.NewWorld(9).ComputeHash());
        Assert.NotEqual(Rig.NewWorld(9).ComputeHash(), Rig.NewWorld(10).ComputeHash());
    }

    [Fact]
    public void IllegalMovesAreRefusedAndCounted()
    {
        var t = Rig.TableOf(Rig.NewWorld(5));
        var before = t.Moves;
        Assert.False(t.Move(Piles.Waste, Piles.Tableau, 1)); // empty waste
        Assert.False(t.Move(Piles.Tableau, Piles.Tableau, 1)); // same pile
        Assert.False(t.Move(Piles.Tableau + 6, Piles.Tableau, 7)); // face-down cards in the run
        Assert.False(t.Move(Piles.Stock, Piles.Waste, 1)); // the stock only draws
        Assert.False(t.Move(Piles.Tableau, 99, 1));
        Assert.False(t.NewGame(2));
        Assert.Equal(6, t.Refused);
        Assert.Equal(before, t.Moves);
    }

    [Fact]
    public void DrawThreeTurnsThreeCardsAndRecyclesTheWaste()
    {
        var t = Rig.TableOf(Rig.NewWorld(5, 3));
        Assert.True(t.Draw());
        Assert.Equal(3, t.Waste.Count);
        Assert.Equal(21, t.Stock.Count);
        for (var i = 0; i < 7; i++)
        {
            Assert.True(t.Draw());
        }

        Assert.Empty(t.Stock);
        Assert.Equal(24, t.Waste.Count);
        Assert.True(t.Draw()); // turns the waste back over
        Assert.Equal(24, t.Stock.Count);
        Assert.Equal(1, t.Recycles);
        Assert.Equal(0, t.Score); // the penalty cannot push the score below zero
    }

    [Fact]
    public void UndoRestoresTheExactStateAfterEveryMoveOfAWinningLine()
    {
        var (seed, line) = Fixtures.Winnable();
        var world = Rig.NewWorld(seed);
        var t = Rig.TableOf(world);
        var hashes = new List<ulong> { world.ComputeHash() };
        foreach (var m in line.Take(80))
        {
            Assert.True(m.IsDraw ? t.Draw() : t.Move(m.From, m.To, m.Count));
            hashes.Add(world.ComputeHash());
        }

        for (var i = hashes.Count - 2; i >= 0; i--)
        {
            Assert.True(t.Undo());
            Assert.Equal(hashes[i], world.ComputeHash());
        }

        Assert.False(t.Undo());
    }

    [Fact]
    public void TheKnownDealIsWonByItsLine()
    {
        var (seed, line) = Fixtures.Winnable();
        var world = Rig.NewWorld(seed);
        var t = Rig.TableOf(world);
        Rig.Play(t, line);
        Assert.Equal(GamePhase.Won, t.Phase);
        Assert.True(t.IsWon);
        Assert.True(t.Score > 0);
        Assert.False(t.Draw());
        Assert.False(t.Undo()); // a finished game cannot be taken back
    }

    [Fact]
    public void ScoringFollowsTheUsualRules()
    {
        var t = Rig.TableOf(Rig.NewWorld(1));
        t.SetupNearlyWon();
        Assert.Equal(640, t.Score);
        Assert.True(t.AutoMove(Piles.Tableau)); // king of spades to its foundation: +10, and the game is won
        Assert.Equal(GamePhase.Won, t.Phase);
        Assert.Equal(640 + 10 + (700000 / 95), t.Score); // the time bonus applies after thirty seconds
    }

    [Fact]
    public void TimerRunsOnlyAfterTheFirstMoveAndNotWhilePaused()
    {
        var world = Rig.NewWorld(5);
        var t = Rig.TableOf(world);
        world.Step();
        world.Step();
        Assert.Equal(0, t.Elapsed);
        Assert.True(t.Draw());
        world.Step();
        world.Step();
        Assert.Equal(2, t.Elapsed);
        t.Pause(true);
        world.Step();
        Assert.Equal(2, t.Elapsed);
        Assert.False(t.Draw()); // moves are refused while paused
        t.Pause(false);
        world.Step();
        Assert.Equal(3, t.Elapsed);
    }

    [Fact]
    public void AutoFinishPlaysTheRestOfAFinishedPosition()
    {
        var t = Rig.TableOf(Rig.NewWorld(1));
        t.SetupNearlyWon();
        Assert.True(t.CanAutoFinish);
        Assert.True(t.AutoFinish());
        Assert.Equal(GamePhase.Won, t.Phase);
        Assert.False(t.AutoFinish());
    }

    [Fact]
    public void SnapshotRestoreKeepsTheGame()
    {
        var (seed, line) = Fixtures.Winnable();
        var world = Rig.NewWorld(seed);
        Rig.Play(Rig.TableOf(world), line.Take(30));
        var writer = new SyncWriter();
        world.WriteSnapshot(writer);
        var copy = Rig.NewWorld(seed + 1);
        Assert.Equal(SyncError.None, copy.ReadSnapshot(writer.Written));
        Assert.Equal(world.ComputeHash(), copy.ComputeHash());
        Rig.Play(Rig.TableOf(copy), line.Skip(30));
        Assert.True(Rig.TableOf(copy).IsWon);
    }
}
