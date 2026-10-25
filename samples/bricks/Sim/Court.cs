using OneGame.Core;

namespace Bricks;

/// <summary>
/// The whole game: bricks, balls, falling capsules, the paddle, score and lives. It is one entity, so a save is one snapshot and a replay
/// is the log of the commands sent to it. Everything here is simulation code: the views read it and send commands, and never change it.
/// Physics is in <see cref="Physics"/>; this class decides what a contact means.
/// </summary>
[Entity]
public partial class Court : Entity
{
    /// <summary>The brick grid, row by row (see <see cref="Brick"/> for the packing). Empty cells are zero.</summary>
    [Synced] public SyncList<ushort> Bricks { get; } = new();

    /// <summary>The balls in play. While serving there is one, resting on the paddle.</summary>
    [Synced] public SyncList<Ball> Balls { get; } = new();

    /// <summary>The power-ups on their way down.</summary>
    [Synced] public SyncList<Capsule> Capsules { get; } = new();

    /// <summary>What the game is doing.</summary>
    [Synced] public partial CourtPhase Phase { get; private set; }

    /// <summary>The score.</summary>
    [Synced] public partial int Score { get; private set; }

    /// <summary>Lives left, counting the ball in play.</summary>
    [Synced] public partial int Lives { get; private set; }

    /// <summary>The level being played, from 1.</summary>
    [Synced] public partial int Level { get; private set; }

    /// <summary>Bricks still to break on this level.</summary>
    [Synced] public partial int BricksLeft { get; private set; }

    /// <summary>Bricks hit since a ball last touched the paddle. It raises the points a brick is worth.</summary>
    [Synced] public partial int Streak { get; private set; }

    /// <summary>The paddle's centre x.</summary>
    [Synced] public partial Fixed PaddleX { get; private set; }

    /// <summary>Where the pointer or finger wants the paddle, when <see cref="AimActive"/>.</summary>
    [Synced] public partial Fixed Aim { get; private set; }

    /// <summary>True while the paddle follows <see cref="Aim"/>; a key or stick takes it back to <see cref="Drive"/>.</summary>
    [Synced] public partial bool AimActive { get; private set; }

    /// <summary>The push on a key or stick, -100 (full left) to 100 (full right).</summary>
    [Synced] public partial int Drive { get; private set; }

    /// <summary>Ticks left of the wide-paddle power-up.</summary>
    [Synced] public partial int WideLeft { get; private set; }

    /// <summary>Ticks until the next level is served, while the level-cleared banner shows.</summary>
    [Synced] public partial int ClearLeft { get; private set; }

    /// <summary>True while the pause overlay is up: nothing moves.</summary>
    [Synced] public partial bool Paused { get; private set; }

    /// <summary>True when the game ended because the last level was cleared.</summary>
    [Synced] public partial bool Victory { get; private set; }

    /// <summary>Bricks damaged or broken, ever (a counter for views to hear changes by).</summary>
    [Synced] public partial int Hits { get; private set; }

    /// <summary>Bricks broken, ever.</summary>
    [Synced] public partial int Breaks { get; private set; }

    /// <summary>Bounces off the unbreakable bricks, ever.</summary>
    [Synced] public partial int Clinks { get; private set; }

    /// <summary>Bounces off a wall, ever.</summary>
    [Synced] public partial int WallBounces { get; private set; }

    /// <summary>Bounces off the paddle, ever.</summary>
    [Synced] public partial int PaddleBounces { get; private set; }

    /// <summary>Power-ups caught, ever.</summary>
    [Synced] public partial int Catches { get; private set; }

    /// <summary>Balls lost, ever (a ball falling out of a multi-ball counts too).</summary>
    [Synced] public partial int Drops { get; private set; }

    /// <summary>Levels cleared, ever.</summary>
    [Synced] public partial int Clears { get; private set; }

    /// <summary>Commands refused since the world was made.</summary>
    [Synced] public partial int Refused { get; private set; }

    /// <summary>Half the paddle's width right now.</summary>
    public Fixed PaddleHalf => (WideLeft > 0 ? Rules.PaddleWideWidth : Rules.PaddleWidth) / 2;

    /// <summary>The speed of the balls on this level.</summary>
    public Fixed BallSpeed => Rules.BallSpeed(Level) + Levels.Get(Level).Speed;

    /// <summary>True when a game is being played: the title screen is not up and the game is not over.</summary>
    public bool InPlay => Phase is CourtPhase.Serve or CourtPhase.Playing or CourtPhase.Cleared;

    // ---- commands ----------------------------------------------------------------------------------------------------

    /// <summary>Starts a new game on the given level (1 for a normal game), with the paddle centred and the ball ready to launch.</summary>
    /// <param name="level">The level to start on, 1 to the number of levels. Other values are refused.</param>
    [Command]
    public bool NewGame(int level)
    {
        if (level < 1 || level > Levels.Count)
        {
            Refused++;
            return false;
        }

        Score = 0;
        Lives = Rules.StartLives;
        Victory = false;
        Paused = false;
        Drive = 0;
        AimActive = false;
        LoadLevel(level);
        return true;
    }

    /// <summary>Launches the ball resting on the paddle, up and a little off vertical (the angle comes from the <c>serve</c> random stream).</summary>
    [Command]
    public bool Launch()
    {
        if (Phase != CourtPhase.Serve || Paused || Balls.Count == 0)
        {
            Refused++;
            return false;
        }

        var spread = Fixed.Pi / 10;
        var angle = World!.Rng["serve"].NextFixed(-spread, spread);
        var speed = BallSpeed;
        var ball = Balls[0];
        ball.Vx = speed * Fixed.Sin(angle);
        ball.Vy = -(speed * Fixed.Cos(angle));
        Balls[0] = ball;
        Phase = CourtPhase.Playing;
        return true;
    }

    /// <summary>Points the paddle at an x position (a pointer or a finger); it slides there at its top speed.</summary>
    [Command]
    public void Steer(Fixed x)
    {
        Aim = x;
        AimActive = true;
    }

    /// <summary>Drives the paddle with a key or a stick: -100 to 100 is the share of full speed, left to right; 0 stops it.</summary>
    [Command]
    public void Push(int amount)
    {
        Drive = Math.Clamp(amount, -100, 100);
        if (Drive != 0)
        {
            AimActive = false;
        }
    }

    /// <summary>Pauses or resumes. Only a game in progress can be paused.</summary>
    [Command]
    public bool Pause(bool on)
    {
        if (on && !InPlay)
        {
            Refused++;
            return false;
        }

        Paused = on;
        return true;
    }

    /// <summary>Leaves the game and returns to the title.</summary>
    [Command]
    public void Quit()
    {
        Phase = CourtPhase.Idle;
        Paused = false;
        Balls.Clear();
        Capsules.Clear();
    }

    // ---- the tick ----------------------------------------------------------------------------------------------------

    /// <summary>Moves the paddle, then the balls and capsules, and decides what the contacts and the end of the level mean.</summary>
    protected override void OnTick()
    {
        if (Paused || Phase is CourtPhase.Idle or CourtPhase.Over)
        {
            return;
        }

        MovePaddle();
        if (WideLeft > 0)
        {
            WideLeft--;
        }

        switch (Phase)
        {
            case CourtPhase.Serve:
                if (Balls.Count > 0)
                {
                    var ball = Balls[0];
                    ball.X = PaddleX;
                    Balls[0] = ball;
                }

                break;
            case CourtPhase.Playing:
                MoveBalls();
                MoveCapsules();
                if (Phase == CourtPhase.Playing && BricksLeft == 0)
                {
                    ClearLevel();
                }

                break;
            case CourtPhase.Cleared:
                if (--ClearLeft <= 0)
                {
                    Advance();
                }

                break;
        }
    }

    private void MovePaddle()
    {
        var half = PaddleHalf;
        var x = PaddleX;
        if (AimActive)
        {
            var step = Fixed.Clamp(Aim - x, -Rules.AimSpeed, Rules.AimSpeed);
            x += step;
        }
        else if (Drive != 0)
        {
            x += Rules.DriveSpeed * Drive / 100;
        }

        PaddleX = Fixed.Clamp(x, half, Rules.Width - half);
    }

    private void MoveBalls()
    {
        var speed = BallSpeed;
        var half = PaddleHalf;
        for (var i = Balls.Count - 1; i >= 0; i--)
        {
            var ball = Balls[i];
            var left = Fixed.One;
            var lost = false;

            // A ball can bounce a few times in one tick (a notch in the wall of bricks); the loop is bounded so that a ball wedged
            // between two surfaces costs time and never hangs the tick.
            for (var bounces = 0; bounces < 8 && left > Fixed.Zero; bounces++)
            {
                var contact = Physics.Advance(ref ball, ref left, Bricks, PaddleX, half, speed, out var cell);
                if (contact == Contact.None)
                {
                    break;
                }

                if (contact == Contact.Floor)
                {
                    lost = true;
                    break;
                }

                switch (contact)
                {
                    case Contact.Wall:
                        WallBounces++;
                        break;
                    case Contact.Paddle:
                        PaddleBounces++;
                        Streak = 0;
                        break;
                    case Contact.Brick:
                        HitBrick(cell);
                        break;
                }
            }

            if (lost)
            {
                Balls.RemoveAt(i);
                Drops++;
            }
            else
            {
                Balls[i] = ball;
            }
        }

        if (Balls.Count == 0)
        {
            LoseLife();
        }
    }

    private void HitBrick(int cell)
    {
        var value = Bricks[cell];
        var hits = Brick.Hits(value);
        if (hits >= Brick.Unbreakable)
        {
            Clinks++;
            return;
        }

        Streak++;
        Hits++;
        var multiplier = 1 + Math.Min(Streak / 4, 3);
        hits--;
        if (hits > 0)
        {
            Bricks[cell] = Brick.Pack(hits, Brick.Max(value), Brick.Tint(value));
            Score += 5 * multiplier;
            return;
        }

        Bricks[cell] = 0;
        BricksLeft--;
        Breaks++;
        Score += 10 * Brick.Max(value) * multiplier;
        if (Capsules.Count < Rules.MaxCapsules && World!.Rng["drops"].NextInt(100) < Levels.Get(Level).CapsulePercent)
        {
            Capsules.Add(new Capsule
            {
                X = ((cell % Rules.Columns) * Rules.BrickWidth) + (Rules.BrickWidth / 2),
                Y = Rules.GridTop + ((cell / Rules.Columns) * Rules.BrickHeight) + (Rules.BrickHeight / 2),
                Kind = World!.Rng["drops"].NextBool() ? CapsuleKind.Wide : CapsuleKind.Multi,
            });
        }
    }

    private void MoveCapsules()
    {
        var half = PaddleHalf;
        for (var i = Capsules.Count - 1; i >= 0; i--)
        {
            var capsule = Capsules[i];
            capsule.Y += Rules.CapsuleSpeed;
            var caught = capsule.Y >= Rules.PaddleTop && capsule.Y <= Rules.PaddleY + (Rules.PaddleHeight / 2)
                && Fixed.Abs(capsule.X - PaddleX) <= half + 12;
            if (caught)
            {
                Capsules.RemoveAt(i);
                Catches++;
                Score += 50;
                if (capsule.Kind == CapsuleKind.Wide)
                {
                    WideLeft = Rules.WideTicks;
                }
                else
                {
                    SplitBalls();
                }
            }
            else if (capsule.Y > Rules.Height + 20)
            {
                Capsules.RemoveAt(i);
            }
            else
            {
                Capsules[i] = capsule;
            }
        }
    }

    // Every ball becomes three: itself and two more turned 20 degrees each way, while there is room.
    private void SplitBalls()
    {
        var turn = Fixed.Pi / 9;
        var sin = Fixed.Sin(turn);
        var cos = Fixed.Cos(turn);
        var count = Balls.Count;
        for (var i = 0; i < count && Balls.Count + 2 <= Rules.MaxBalls; i++)
        {
            var b = Balls[i];
            Balls.Add(new Ball { X = b.X, Y = b.Y, Vx = (b.Vx * cos) - (b.Vy * sin), Vy = (b.Vx * sin) + (b.Vy * cos) });
            Balls.Add(new Ball { X = b.X, Y = b.Y, Vx = (b.Vx * cos) + (b.Vy * sin), Vy = (b.Vy * cos) - (b.Vx * sin) });
        }
    }

    private void LoseLife()
    {
        Lives--;
        Capsules.Clear();
        WideLeft = 0;
        Streak = 0;
        if (Lives <= 0)
        {
            Lives = 0;
            Phase = CourtPhase.Over;
            return;
        }

        Serve();
    }

    private void ClearLevel()
    {
        Phase = CourtPhase.Cleared;
        ClearLeft = Rules.ClearTicks;
        Clears++;
        Score += 100 * Level;
        Balls.Clear();
        Capsules.Clear();
    }

    private void Advance()
    {
        if (Level >= Levels.Count)
        {
            Victory = true;
            Phase = CourtPhase.Over;
            return;
        }

        LoadLevel(Level + 1);
    }

    private void LoadLevel(int level)
    {
        var data = Levels.Get(level);
        Level = level;
        Bricks.ReplaceWith(data.Cells);
        BricksLeft = data.Breakable;
        Capsules.Clear();
        WideLeft = 0;
        Streak = 0;
        PaddleX = Rules.Width / 2;
        Serve();
    }

    private void Serve()
    {
        Balls.Clear();
        Balls.Add(new Ball { X = PaddleX, Y = Rules.PaddleTop - Rules.BallRadius });
        Phase = CourtPhase.Serve;
    }

    // ---- states for scenarios and tests ------------------------------------------------------------------------------

    /// <summary>A new game on the given level, with the ball ready to launch.</summary>
    public Court Started(int level)
    {
        NewGame(level);
        return this;
    }

    /// <summary>A game on level 1 with one brick left, with the ball already flying straight up at it, so the level clears about a second and a half later.</summary>
    public Court SetupLastBrick()
    {
        NewGame(1);
        var cells = new ushort[Rules.Columns * Rules.Rows];
        cells[(2 * Rules.Columns) + 5] = Brick.Pack(1, 1, 3);
        cells[(2 * Rules.Columns) + 6] = Brick.Pack(Brick.Unbreakable, Brick.Unbreakable, 8);
        Bricks.ReplaceWith(cells);
        BricksLeft = 1;
        PaddleX = 5 * Rules.BrickWidth + (Rules.BrickWidth / 2);
        Balls.Clear();
        Balls.Add(new Ball { X = PaddleX, Y = Rules.PaddleTop - Rules.BallRadius - 40, Vx = 0, Vy = -BallSpeed });
        Phase = CourtPhase.Playing;
        return this;
    }

    /// <summary>
    /// A game with a two-hit brick above the paddle, a brick nothing breaks above that, and one far-off brick that keeps the level from ending.
    /// The ball flies straight up the middle of the paddle, so it bounces back up the same line: it dents the two-hit brick, breaks it on the
    /// second visit and then rings off the plate.
    /// </summary>
    public Court SetupTypes()
    {
        NewGame(1);
        var cells = new ushort[Rules.Columns * Rules.Rows];
        cells[(3 * Rules.Columns) + 3] = Brick.Pack(2, 2, 7);
        cells[(2 * Rules.Columns) + 3] = Brick.Pack(Brick.Unbreakable, Brick.Unbreakable, 8);
        cells[10] = Brick.Pack(1, 1, 5);
        Bricks.ReplaceWith(cells);
        BricksLeft = 2;
        PaddleX = 3 * Rules.BrickWidth + (Rules.BrickWidth / 2);
        Balls.Clear();
        Balls.Add(new Ball { X = PaddleX, Y = Rules.PaddleTop - Rules.BallRadius - 40, Vx = 0, Vy = -BallSpeed });
        Phase = CourtPhase.Playing;
        return this;
    }

    /// <summary>A game with a single ball about to fall past the paddle, so the next ticks cost a life.</summary>
    public Court SetupFalling()
    {
        NewGame(1);
        PaddleX = 60;
        Balls.Clear();
        Balls.Add(new Ball { X = 400, Y = Rules.PaddleY - 40, Vx = 0, Vy = BallSpeed });
        Phase = CourtPhase.Playing;
        return this;
    }

    /// <summary>A game with a wide-paddle capsule and a multi-ball capsule falling onto the paddle, and a ball in the air.</summary>
    public Court SetupCapsules()
    {
        NewGame(1);
        Balls.Clear();
        Balls.Add(new Ball { X = 100, Y = 400, Vx = BallSpeed / 2, Vy = -BallSpeed / 2 });
        Capsules.Add(new Capsule { X = PaddleX, Y = Rules.PaddleY - 30, Kind = CapsuleKind.Wide });
        Capsules.Add(new Capsule { X = PaddleX + 20, Y = Rules.PaddleY - 60, Kind = CapsuleKind.Multi });
        Phase = CourtPhase.Playing;
        return this;
    }

    /// <summary>The last ball of the last life, on level 3 with a score worth a place in the table, about to fall past the paddle.</summary>
    public Court SetupLastLife()
    {
        NewGame(3);
        Lives = 1;
        Score = 1230;
        PaddleX = 60;
        Balls.Clear();
        Balls.Add(new Ball { X = 400, Y = Rules.PaddleY - 40, Vx = 0, Vy = BallSpeed });
        Phase = CourtPhase.Playing;
        return this;
    }
}
