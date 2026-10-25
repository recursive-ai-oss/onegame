using OneGame.Core;

namespace Bricks.Tests;

/// <summary>The ball's motion against walls, the paddle and bricks: the edge cases that make a brick-breaker feel right or wrong.</summary>
public class PhysicsTests
{
    private static readonly Fixed Speed = Fixed.FromRatio(9, 2);

    private static SyncList<ushort> Grid(params (int Col, int Row, ushort Brick)[] bricks)
    {
        var list = new SyncList<ushort>();
        var cells = new ushort[Rules.Columns * Rules.Rows];
        foreach (var (col, row, brick) in bricks)
        {
            cells[(row * Rules.Columns) + col] = brick;
        }

        list.ReplaceWith(cells);
        return list;
    }

    private static Ball At(double x, double y, double vx, double vy) =>
        new() { X = Fixed.FromDouble(x), Y = Fixed.FromDouble(y), Vx = Fixed.FromDouble(vx), Vy = Fixed.FromDouble(vy) };

    private static Contact Step(ref Ball ball, SyncList<ushort> bricks, out int cell, double paddleX = 240, double half = 40)
    {
        var left = Fixed.One;
        return Physics.Advance(ref ball, ref left, bricks, Fixed.FromDouble(paddleX), Fixed.FromDouble(half), Speed, out cell);
    }

    // Runs whole ticks, applying damage like the court does, until something other than a brick or wall happens or the ticks run out.
    private static (int Ticks, int BricksHit) Fly(ref Ball ball, SyncList<ushort> bricks, int ticks, double paddleX = 240)
    {
        var hit = 0;
        for (var t = 0; t < ticks; t++)
        {
            var left = Fixed.One;
            for (var n = 0; n < 8 && left > Fixed.Zero; n++)
            {
                var c = Physics.Advance(ref ball, ref left, bricks, Fixed.FromDouble(paddleX), 40, Speed, out var cell);
                if (c == Contact.Brick)
                {
                    hit++;
                    bricks[cell] = 0;
                }
                else if (c is Contact.Floor or Contact.Paddle)
                {
                    return (t, hit);
                }
                else if (c == Contact.None)
                {
                    break;
                }
            }
        }

        return (ticks, hit);
    }

    [Fact]
    public void ABallInTheOpenMovesByItsVelocity()
    {
        var ball = At(100, 300, 3, -2);
        var c = Step(ref ball, Grid(), out var cell);
        Assert.Equal(Contact.None, c);
        Assert.Equal(-1, cell);
        Assert.Equal(Fixed.FromDouble(103), ball.X);
        Assert.Equal(Fixed.FromDouble(298), ball.Y);
    }

    [Fact]
    public void TheLeftWallBouncesTheBallAtTheContactPointAndKeepsTheRestOfTheTick()
    {
        var ball = At(8, 300, -4, -1);
        var left = Fixed.One;
        var c = Physics.Advance(ref ball, ref left, Grid(), 240, 40, Speed, out _);
        Assert.Equal(Contact.Wall, c);
        Assert.Equal(Fixed.FromInt(Rules.BallRadius), ball.X);
        Assert.True(ball.Vx > Fixed.Zero);
        Assert.True(left > Fixed.Zero && left < Fixed.One);

        // The rest of the tick carries on to the right.
        var before = ball.X;
        Assert.Equal(Contact.None, Physics.Advance(ref ball, ref left, Grid(), 240, 40, Speed, out _));
        Assert.True(ball.X > before);
    }

    [Fact]
    public void ABrickIsHitFromBelowAndTheBallGoesDown()
    {
        var bricks = Grid((5, 3, Brick.Pack(1, 1, 1)));
        var ball = At(220, 145, 0, -4);
        var c = Step(ref ball, bricks, out var cell);
        Assert.Equal(Contact.Brick, c);
        Assert.Equal((3 * Rules.Columns) + 5, cell);
        Assert.True(ball.Vy > Fixed.Zero);
        Assert.Equal(Fixed.Zero, ball.Vx);

        // The ball stopped one radius under the brick's bottom edge.
        Assert.Equal(Fixed.FromInt(Rules.GridTop + (4 * Rules.BrickHeight) + Rules.BallRadius), ball.Y);
    }

    [Fact]
    public void ABrickIsHitOnItsSideAndOnlyTheHorizontalPartFlips()
    {
        var bricks = Grid((5, 3, Brick.Pack(1, 1, 1)));
        var ball = At(192, 128, 4, -1);
        var c = Step(ref ball, bricks, out _);
        Assert.Equal(Contact.Brick, c);
        Assert.True(ball.Vx < Fixed.Zero);
        Assert.True(ball.Vy < Fixed.Zero);
        Assert.Equal(Fixed.FromInt((5 * Rules.BrickWidth) - Rules.BallRadius), ball.X);
    }

    [Fact]
    public void ACornerHitFlipsBothDirections()
    {
        // Aimed exactly at the brick's lower-left corner (grown by the radius): both faces are reached at the same instant.
        var bricks = Grid((5, 3, Brick.Pack(1, 1, 1)));
        var cornerX = (5 * Rules.BrickWidth) - Rules.BallRadius;
        var cornerY = Rules.GridTop + (4 * Rules.BrickHeight) + Rules.BallRadius;
        var ball = At(cornerX - 4, cornerY + 4, 4, -4);
        var c = Step(ref ball, bricks, out _);
        Assert.Equal(Contact.Brick, c);
        Assert.True(ball.Vx < Fixed.Zero, "horizontal flipped");
        Assert.True(ball.Vy > Fixed.Zero, "vertical flipped");
    }

    [Fact]
    public void AGrazingPathPastACornerMisses()
    {
        var bricks = Grid((5, 3, Brick.Pack(1, 1, 1)));
        var cornerX = (5 * Rules.BrickWidth) - Rules.BallRadius;
        var ball = At(cornerX - 10, 200, 0, -4);
        for (var t = 0; t < 10; t++)
        {
            Assert.Equal(Contact.None, Step(ref ball, bricks, out _));
        }

        Assert.Equal(Fixed.FromInt(cornerX - 10), ball.X);
    }

    [Fact]
    public void AFastBallNeverTunnelsThroughAThinWall()
    {
        // A one-row wall across the court and a ball many times faster than the brick is tall: the sweep still finds the first brick.
        var row = new List<(int, int, ushort)>();
        for (var col = 0; col < Rules.Columns; col++)
        {
            row.Add((col, 5, Brick.Pack(1, 1, 1)));
        }

        for (var speed = 4; speed <= 90; speed += 7)
        {
            for (var offset = 0; offset < 40; offset += 7)
            {
                var bricks = Grid([.. row]);
                var ball = At(20 + offset + (speed % 5 * 80), 174 + (speed * 0.5), speed * 0.1, -speed);
                var (_, hits) = Fly(ref ball, bricks, 1);
                Assert.True(hits == 1, $"speed {speed} offset {offset}: ball y {ball.Y} passed the wall");
            }
        }
    }

    [Fact]
    public void ABallOnLongDiagonalsNeverEndsInsideABrickOrOutsideTheCourt()
    {
        // A thousand ticks in a sealed room of bricks with a hard floor: the ball stays in bounds and out of every brick.
        var bricks = Grid((2, 4, Brick.Pack(Brick.Unbreakable, 15, 8)), (7, 6, Brick.Pack(Brick.Unbreakable, 15, 8)), (4, 9, Brick.Pack(Brick.Unbreakable, 15, 8)));
        var ball = At(100, 400, 3.1, -3.3);
        var speed = Speed;
        for (var t = 0; t < 1000; t++)
        {
            var left = Fixed.One;
            for (var n = 0; n < 8 && left > Fixed.Zero; n++)
            {
                var c = Physics.Advance(ref ball, ref left, bricks, 240, 4000, speed, out _);
                if (c == Contact.None)
                {
                    break;
                }
            }

            Assert.InRange(ball.X.ToDouble(), Rules.BallRadius, Rules.Width - Rules.BallRadius);
            Assert.True(ball.Y.ToDouble() >= Rules.BallRadius);
            foreach (var (col, row) in new[] { (2, 4), (7, 6), (4, 9) })
            {
                var inside = ball.X.ToDouble() > (col * 40) - 6 && ball.X.ToDouble() < ((col + 1) * 40) + 6
                    && ball.Y.ToDouble() > 72 + (row * 16) - 6 && ball.Y.ToDouble() < 72 + ((row + 1) * 16) + 6;
                Assert.False(inside, $"tick {t}: ball at {ball.X}, {ball.Y} is inside brick {col},{row}");
            }
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.5)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ThePaddleSteersTheBallByWhereItIsHit(double offset)
    {
        // The ball lands at `offset` of the paddle's half-width (plus the radius): the centre goes straight up, the edges at 60 degrees.
        var paddle = 240.0;
        var reach = 40.0 + Rules.BallRadius;
        var ball = At(paddle + (offset * reach), (double)Rules.PaddleTop.ToDouble() - Rules.BallRadius - 2, 0, 4.5);
        var c = Step(ref ball, Grid(), out _, paddle);
        Assert.Equal(Contact.Paddle, c);

        var vx = ball.Vx.ToDouble();
        var vy = ball.Vy.ToDouble();
        Assert.True(vy < 0, "leaves upward");
        Assert.Equal(4.5, Math.Sqrt((vx * vx) + (vy * vy)), 3);
        var angle = Math.Atan2(vx, -vy) * 180 / Math.PI;
        Assert.Equal(offset * 60, angle, 0.5);
    }

    [Fact]
    public void ABallBesideThePaddleFallsThrough()
    {
        var ball = At(100, Rules.PaddleTop.ToDouble() - Rules.BallRadius - 2, 0, 4.5);
        Assert.Equal(Contact.None, Step(ref ball, Grid(), out _, 300));
        var far = At(100, Rules.Height + Rules.BallRadius - 1, 0, 4.5);
        Assert.Equal(Contact.Floor, Step(ref far, Grid(), out _, 300));
    }

    [Fact]
    public void ANearlyFlatBallIsLiftedSoItNeverRunsSideways()
    {
        var ball = At(200, 300, 4.499, -0.001);
        Physics.KeepRising(ref ball, Speed);
        Assert.True(Fixed.Abs(ball.Vy) >= Speed * Fixed.FromRatio(1, 5) - Fixed.Epsilon);
        var vx = ball.Vx.ToDouble();
        var vy = ball.Vy.ToDouble();
        Assert.Equal(4.5, Math.Sqrt((vx * vx) + (vy * vy)), 2);
    }
}
