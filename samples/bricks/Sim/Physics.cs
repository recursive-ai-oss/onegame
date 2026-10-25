using OneGame.Core;

namespace Bricks;

/// <summary>What a ball ran into while it moved.</summary>
public enum Contact : byte
{
    /// <summary>Nothing: the ball moved the whole way.</summary>
    None,

    /// <summary>The side or top wall.</summary>
    Wall,

    /// <summary>The paddle's top face.</summary>
    Paddle,

    /// <summary>A brick; its cell index is reported alongside.</summary>
    Brick,

    /// <summary>The ball fell past the paddle and out of the court.</summary>
    Floor,
}

/// <summary>
/// The ball's motion: a swept test against the walls, the paddle and the brick grid, so a fast ball can never skip over a brick or a wall.
/// The ball is a circle; each obstacle is grown by the radius, which turns the ball into a point and the obstacle into a rectangle, and the
/// ball's path in one tick becomes a line segment. The first rectangle the segment enters is the one that is hit, and the face it enters
/// through gives the normal. Entering through two faces at the same instant is a corner hit and flips both velocity components. The
/// grown corners are square rather than round, which makes corner hits a hair generous and keeps the maths to additions, multiplications
/// and divisions of fixed-point numbers.
/// </summary>
[Sim]
public static class Physics
{
    /// <summary>The smallest share of the speed that stays vertical, so a ball can never settle into a flat, endless sideways run.</summary>
    private static readonly Fixed MinVertical = Fixed.FromRatio(1, 5);

    /// <summary>
    /// Moves <paramref name="ball"/> until it hits something or <paramref name="time"/> (the part of the tick left, from 0 to 1) runs out.
    /// A hit leaves the ball at the contact point with its velocity already bounced, and <paramref name="time"/> reduced by the part of
    /// the path used. Call again with the same <paramref name="time"/> to carry on. The caller applies damage to the brick.
    /// </summary>
    /// <param name="ball">The ball, changed in place.</param>
    /// <param name="time">The part of the tick still to move, changed in place.</param>
    /// <param name="bricks">The brick grid (<see cref="Rules.Columns"/> by <see cref="Rules.Rows"/> packed cells).</param>
    /// <param name="paddleX">Paddle centre x.</param>
    /// <param name="paddleHalf">Half the paddle's width.</param>
    /// <param name="speed">The speed a bounce off the paddle gives the ball.</param>
    /// <param name="brick">The cell index when the result is <see cref="Contact.Brick"/>, otherwise -1.</param>
    public static Contact Advance(ref Ball ball, ref Fixed time, SyncList<ushort> bricks, Fixed paddleX, Fixed paddleHalf, Fixed speed, out int brick)
    {
        brick = -1;
        if (time <= Fixed.Zero)
        {
            return Contact.None;
        }

        Fixed r = Rules.BallRadius;
        var x = ball.X;
        var y = ball.Y;
        var dx = ball.Vx * time;
        var dy = ball.Vy * time;
        if (dx == Fixed.Zero && dy == Fixed.Zero)
        {
            time = Fixed.Zero;
            return Contact.None;
        }

        // The earliest hit so far, as a fraction of the path (0 to 1). "One + epsilon" means none yet.
        var hit = new Pending { Time = Fixed.One + Fixed.Epsilon };

        // Walls: the ball's centre may not get closer than one radius to the left, right or top wall.
        if (dx < Fixed.Zero)
        {
            hit.Offer((r - x) / dx, Contact.Wall, flipX: true, flipY: false, atX: r, atY: null);
        }
        else if (dx > Fixed.Zero)
        {
            hit.Offer((Rules.Width - r - x) / dx, Contact.Wall, flipX: true, flipY: false, atX: Rules.Width - r, atY: null);
        }

        if (dy < Fixed.Zero)
        {
            hit.Offer((r - y) / dy, Contact.Wall, flipX: false, flipY: true, atX: null, atY: r);
        }
        else if (dy > Fixed.Zero)
        {
            // Floor: well below the paddle line, so the ball is gone only when it has fallen clear out of the court.
            hit.Offer((Rules.Height + r - y) / dy, Contact.Floor, flipX: false, flipY: false, atX: null, atY: null);

            // Paddle: only its top face counts, and only for a ball coming down onto it from above.
            var plane = Rules.PaddleTop - r;
            if (y <= plane)
            {
                var fp = (plane - y) / dy;
                var across = x + (dx * fp);
                if (Fixed.Abs(across - paddleX) <= paddleHalf + r)
                {
                    hit.Offer(fp, Contact.Paddle, flipX: false, flipY: false, atX: across, atY: plane);
                }
            }
        }

        // Bricks: look only at the cells the path (grown by the radius, and by one unit so a ball that just touches a cell edge still sees it) can touch.
        var cell = -1;
        var minX = Fixed.Min(x, x + dx) - r - 1;
        var maxX = Fixed.Max(x, x + dx) + r + 1;
        var minY = Fixed.Min(y, y + dy) - r - 1;
        var maxY = Fixed.Max(y, y + dy) + r + 1;
        var col0 = Math.Clamp((minX / Rules.BrickWidth).FloorToInt(), 0, Rules.Columns - 1);
        var col1 = Math.Clamp((maxX / Rules.BrickWidth).FloorToInt(), 0, Rules.Columns - 1);
        var row0 = Math.Clamp(((minY - Rules.GridTop) / Rules.BrickHeight).FloorToInt(), 0, Rules.Rows - 1);
        var row1 = Math.Clamp(((maxY - Rules.GridTop) / Rules.BrickHeight).FloorToInt(), 0, Rules.Rows - 1);
        for (var row = row0; row <= row1; row++)
        {
            for (var col = col0; col <= col1; col++)
            {
                var index = (row * Rules.Columns) + col;
                if (Brick.Hits(bricks[index]) == 0)
                {
                    continue;
                }

                Fixed left = (col * Rules.BrickWidth) - r;
                Fixed right = ((col + 1) * Rules.BrickWidth) + r;
                Fixed top = (Rules.GridTop + (row * Rules.BrickHeight)) - r;
                Fixed bottom = (Rules.GridTop + ((row + 1) * Rules.BrickHeight)) + r;
                if (!Slab(x, dx, left, right, out var enterX, out var exitX) || !Slab(y, dy, top, bottom, out var enterY, out var exitY))
                {
                    continue;
                }

                var enter = Fixed.Max(enterX, enterY);
                var exit = Fixed.Min(exitX, exitY);

                // A path that starts inside the grown rectangle or only leaves it (enter below zero) is a ball moving away from a face it
                // just bounced off, not a new hit.
                if (enter > exit)
                {
                    continue;
                }

                var throughX = enterX >= enterY;
                var throughY = enterY >= enterX;
                if (hit.Offer(enter, Contact.Brick, flipX: throughX, flipY: throughY, atX: throughX ? (dx > Fixed.Zero ? left : right) : null, atY: throughY ? (dy > Fixed.Zero ? top : bottom) : null))
                {
                    cell = index;
                }
            }
        }

        var kind = hit.Kind;
        if (kind == Contact.None)
        {
            ball.X = x + dx;
            ball.Y = y + dy;
            time = Fixed.Zero;
            return Contact.None;
        }

        // Move to the contact point (snapping the coordinate that touches the face, so rounding never leaves the ball inside it).
        ball.X = hit.AtX ?? x + (dx * hit.Time);
        ball.Y = hit.AtY ?? y + (dy * hit.Time);
        time -= time * hit.Time;
        if (kind == Contact.Floor)
        {
            return kind;
        }

        if (kind == Contact.Paddle)
        {
            // The bounce angle follows where the ball met the paddle: -1 at its left edge, +1 at its right edge.
            var offset = Fixed.Clamp((ball.X - paddleX) / (paddleHalf + r), -1, 1);
            var angle = offset * Rules.MaxBounceAngle;
            ball.Vx = speed * Fixed.Sin(angle);
            ball.Vy = -(speed * Fixed.Cos(angle));
            return kind;
        }

        if (hit.FlipX)
        {
            ball.Vx = -ball.Vx;
        }

        if (hit.FlipY)
        {
            ball.Vy = -ball.Vy;
        }

        KeepRising(ref ball, speed);
        brick = kind == Contact.Brick ? cell : -1;
        return kind;
    }

    /// <summary>
    /// Keeps the ball from travelling almost flat: when less than a fifth of its speed is vertical, the vertical part is raised to that and
    /// the horizontal part shrinks so the speed stays the same.
    /// </summary>
    public static void KeepRising(ref Ball ball, Fixed speed)
    {
        var floor = speed * MinVertical;
        if (Fixed.Abs(ball.Vy) >= floor)
        {
            return;
        }

        var vy = ball.Vy < Fixed.Zero || ball.Vy == Fixed.Zero ? -floor : floor;
        var vx = Fixed.Sqrt((speed * speed) - (floor * floor));
        ball.Vy = vy;
        ball.Vx = ball.Vx < Fixed.Zero ? -vx : vx;
    }

    // The earliest contact found so far on a ball's path.
    private struct Pending
    {
        public Fixed Time;
        public Contact Kind;
        public bool FlipX;
        public bool FlipY;
        public Fixed? AtX;
        public Fixed? AtY;

        // Keeps a contact if it happens within the path (0 to 1) and strictly before the best so far. A tie goes to whatever was offered
        // first, and the other contact is found on the next call, with the rest of the time.
        public bool Offer(Fixed fraction, Contact kind, bool flipX, bool flipY, Fixed? atX, Fixed? atY)
        {
            if (fraction < Fixed.Zero || fraction > Fixed.One || fraction >= Time)
            {
                return false;
            }

            Time = fraction;
            Kind = kind;
            FlipX = flipX;
            FlipY = flipY;
            AtX = atX;
            AtY = atY;
            return true;
        }
    }

    // The span of the path (as fractions of it) during which one coordinate is inside [low, high]. False when it never is.
    private static bool Slab(Fixed start, Fixed delta, Fixed low, Fixed high, out Fixed enter, out Fixed exit)
    {
        if (delta == Fixed.Zero)
        {
            enter = Fixed.MinValue;
            exit = Fixed.MaxValue;
            return start > low && start < high;
        }

        var a = (low - start) / delta;
        var b = (high - start) / delta;
        enter = Fixed.Min(a, b);
        exit = Fixed.Max(a, b);
        return true;
    }
}
