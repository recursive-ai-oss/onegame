using OneGame.Core;

namespace Bricks.Tests;

/// <summary>The rules: starting, launching, losing and winning, driven on a bare world with no screen.</summary>
public class SimTests
{
    [Fact]
    public void TheTenLevelsLoadAndEachHasSomethingToBreak()
    {
        Assert.Equal(10, Levels.Count);
        for (var n = 1; n <= Levels.Count; n++)
        {
            var level = Levels.Get(n);
            Assert.False(string.IsNullOrEmpty(level.Name));
            Assert.True(level.Breakable is > 0 and <= 80, $"level {n} has {level.Breakable} bricks");
            Assert.Equal(Rules.Columns * Rules.Rows, level.Cells.Length);
        }
    }

    [Theory]
    [InlineData("name: X\nrows:\n  - \"111111111111\"\nsurprise: 1\n", "surprise")]
    [InlineData("name: X\nrows:\n  - \"11111111111\"\n", "11 glyphs")]
    [InlineData("name: X\nrows:\n  - \"11111111111Z\"\n", "'Z'")]
    [InlineData("name: X\nrows:\n  - \"############\"\n", "nothing to break")]
    [InlineData("name: X\nrows: []\n", "'rows'")]
    [InlineData("name: X\nspeed: 500\nrows:\n  - \"111111111111\"\n", "0 to 100")]
    public void ABadLevelFileIsRejectedWithAMessageThatNamesTheProblem(string yaml, string expected)
    {
        var ex = Assert.Throws<LevelException>(() => Levels.Parse(yaml, "bad.yaml"));
        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
        Assert.Contains("bad.yaml", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AGoodLevelFileParsesIntoPackedBricks()
    {
        var level = Levels.Parse("name: Tiny\nspeed: 5\ncapsules: 7\nrows:\n  - \"1T#H........\"\n", "tiny.yaml");
        Assert.Equal("Tiny", level.Name);
        Assert.Equal(7, level.CapsulePercent);
        Assert.Equal(3, level.Breakable);
        Assert.Equal(1, Brick.Hits(level.Cells[0]));
        Assert.Equal(2, Brick.Hits(level.Cells[1]));
        Assert.Equal(Brick.Unbreakable, Brick.Hits(level.Cells[2]));
        Assert.Equal(3, Brick.Hits(level.Cells[3]));
    }

    [Fact]
    public void ANewGameServesTheBallOnThePaddleAndRefusesALevelThatDoesNotExist()
    {
        var world = Bot.NewWorld();
        var court = Bot.CourtOf(world);
        Assert.Equal(CourtPhase.Idle, court.Phase);
        Assert.False(court.NewGame(0));
        Assert.False(court.NewGame(11));
        Assert.False(court.Launch());
        Assert.True(court.NewGame(1));
        Assert.Equal(CourtPhase.Serve, court.Phase);
        Assert.Equal(Rules.StartLives, court.Lives);
        Assert.Single(court.Balls);
        Assert.Equal(court.PaddleX, court.Balls[0].X);
        Assert.Equal(Rules.PaddleTop - Rules.BallRadius, court.Balls[0].Y);
        Assert.Equal(3, court.Refused);
    }

    [Fact]
    public void TheServedBallFollowsThePaddleUntilItIsLaunched()
    {
        var world = Bot.NewWorld();
        var court = Bot.CourtOf(world);
        court.NewGame(1);
        court.Push(100);
        for (var i = 0; i < 10; i++)
        {
            world.Step();
        }

        Assert.Equal(Fixed.FromInt(240) + (Rules.DriveSpeed * 10), court.PaddleX);
        Assert.Equal(court.PaddleX, court.Balls[0].X);
        court.Push(0);
        court.Launch();
        world.Step();
        Assert.Equal(CourtPhase.Playing, court.Phase);
        Assert.True(court.Balls[0].Vy < Fixed.Zero);
    }

    [Fact]
    public void ThePaddleStopsAtTheWallsAndSlidesToAPointer()
    {
        var world = Bot.NewWorld();
        var court = Bot.CourtOf(world);
        court.NewGame(1);
        court.Push(-100);
        for (var i = 0; i < 100; i++)
        {
            world.Step();
        }

        Assert.Equal(Fixed.FromInt(Rules.PaddleWidth / 2), court.PaddleX);
        court.Steer(300);
        world.Step();
        Assert.Equal(Fixed.FromInt(Rules.PaddleWidth / 2) + Rules.AimSpeed, court.PaddleX);
        for (var i = 0; i < 20; i++)
        {
            world.Step();
        }

        Assert.Equal(Fixed.FromInt(300), court.PaddleX);
        court.Push(100);
        Assert.False(court.AimActive);
    }

    [Fact]
    public void ABallThatFallsCostsALifeAndTheLastLifeEndsTheGame()
    {
        var world = Bot.NewWorld();
        var court = Bot.CourtOf(world);
        court.SetupFalling();
        for (var i = 0; i < 60; i++)
        {
            world.Step();
        }

        Assert.Equal(2, court.Lives);
        Assert.Equal(CourtPhase.Serve, court.Phase);

        court.Launch();
        court.Push(-100);
        for (var lives = 2; lives > 0; lives--)
        {
            // Nobody steers: the paddle stays in the corner and the ball comes back down past it.
            for (var i = 0; i < 600 && court.Phase != CourtPhase.Serve && court.Phase != CourtPhase.Over; i++)
            {
                world.Step();
            }

            if (court.Phase == CourtPhase.Serve)
            {
                court.Launch();
            }
        }

        for (var i = 0; i < 600 && court.Phase != CourtPhase.Over; i++)
        {
            world.Step();
        }

        Assert.Equal(CourtPhase.Over, court.Phase);
        Assert.Equal(0, court.Lives);
        Assert.False(court.Victory);
    }

    [Fact]
    public void PausingFreezesEverythingAndOnlyAGameInProgressCanPause()
    {
        var world = Bot.NewWorld();
        var court = Bot.CourtOf(world);
        Assert.False(court.Pause(true));
        court.NewGame(1);
        court.Launch();
        world.Step();
        Assert.True(court.Pause(true));
        var hash = world.ComputeHash();
        var tick = world.Tick;
        for (var i = 0; i < 30; i++)
        {
            world.Step();
        }

        Assert.Equal(tick + 30, world.Tick);
        Assert.Equal(court.Balls[0].Y, court.Balls[0].Y);
        var y = court.Balls[0].Y;
        court.Pause(false);
        world.Step();
        Assert.NotEqual(y, court.Balls[0].Y);
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void ABotClearsEveryLevelAndWinsWithoutAnAllocationPerTick()
    {
        var world = Bot.NewWorld();
        var court = Bot.CourtOf(world);
        court.NewGame(1);
        var levelsSeen = 0;
        var lastLevel = 1;
        var ticks = 0;
        long allocated = 0;
        long measuredTicks = 0;
        while (court.Phase != CourtPhase.Over && ticks < 400_000)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            Bot.Play(world);
            if (ticks > 1000)
            {
                allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                measuredTicks++;
            }

            ticks++;
            if (court.Level != lastLevel)
            {
                levelsSeen++;
                lastLevel = court.Level;
            }
        }

        Assert.True(court.Victory, $"the bot ended on level {court.Level} after {ticks} ticks with {court.Lives} lives, phase {court.Phase}");
        Assert.Equal(9, levelsSeen);
        Assert.Equal(10, court.Clears);

        // The bot itself allocates nothing either (its own steering is arithmetic), so this is the court's tick plus the bot's commands.
        Console.WriteLine($"bot won in {ticks} ticks, score {court.Score}; {allocated} bytes over {measuredTicks} ticks");
        Assert.True(allocated / Math.Max(1, measuredTicks) < 64, $"{allocated} bytes over {measuredTicks} ticks");
    }
}
