using OneGame.Core;

namespace Bricks;

/// <summary>
/// The size of the court and the numbers the rules use. Lengths are court units (the court is 480 wide and 640 tall, +x right and +y down,
/// like the screen), speeds are units per tick, and the simulation runs 60 ticks a second. All of it is fixed-point: the court is part of
/// synced state, and floating point would drift between machines.
/// </summary>
public static class Rules
{
    /// <summary>Court width.</summary>
    public const int Width = 480;

    /// <summary>Court height. A ball that passes this (and its radius) is lost.</summary>
    public const int Height = 640;

    /// <summary>Brick columns.</summary>
    public const int Columns = 12;

    /// <summary>Brick rows a level can use.</summary>
    public const int Rows = 14;

    /// <summary>Brick width. Twelve of them fill the court.</summary>
    public const int BrickWidth = 40;

    /// <summary>Brick height.</summary>
    public const int BrickHeight = 16;

    /// <summary>Top edge of the first brick row.</summary>
    public const int GridTop = 72;

    /// <summary>Centre line of the paddle.</summary>
    public const int PaddleY = 600;

    /// <summary>Paddle thickness.</summary>
    public const int PaddleHeight = 14;

    /// <summary>Paddle width without a power-up.</summary>
    public const int PaddleWidth = 80;

    /// <summary>Paddle width while the wide power-up lasts.</summary>
    public const int PaddleWideWidth = 128;

    /// <summary>Ball radius.</summary>
    public const int BallRadius = 6;

    /// <summary>The most balls in play at once.</summary>
    public const int MaxBalls = 6;

    /// <summary>The most power-up capsules falling at once.</summary>
    public const int MaxCapsules = 8;

    /// <summary>Lives at the start of a game.</summary>
    public const int StartLives = 3;

    /// <summary>Ticks the wide paddle lasts: ten seconds.</summary>
    public const int WideTicks = 600;

    /// <summary>Ticks the level-cleared banner stays before the next level is served.</summary>
    public const int ClearTicks = 90;

    /// <summary>Top of the paddle, where balls bounce.</summary>
    public static Fixed PaddleTop => PaddleY - (PaddleHeight / 2);

    /// <summary>The farthest the paddle moves in a tick when driven by a key or a stick at full push.</summary>
    public static Fixed DriveSpeed => 9;

    /// <summary>The farthest the paddle moves in a tick towards a pointer or finger.</summary>
    public static Fixed AimSpeed => 26;

    /// <summary>How fast a capsule falls.</summary>
    public static Fixed CapsuleSpeed => 3;

    /// <summary>
    /// The angle from straight up that a ball leaves the very edge of the paddle at: 60 degrees. The centre sends it straight up, and the
    /// angle grows in proportion to the distance from the centre, so the player steers the ball with the point of contact.
    /// </summary>
    public static Fixed MaxBounceAngle => Fixed.Pi / 3;

    /// <summary>The ball's speed on level <paramref name="level"/> (1-based), before the level's own extra speed.</summary>
    public static Fixed BallSpeed(int level) => Fixed.FromRatio(9, 2) + (Fixed.FromRatio(1, 5) * (level - 1));
}

/// <summary>What the game is doing.</summary>
public enum CourtPhase : byte
{
    /// <summary>The title screen: nothing is being played.</summary>
    Idle,

    /// <summary>The ball rests on the paddle until the player launches it.</summary>
    Serve,

    /// <summary>Balls are in play.</summary>
    Playing,

    /// <summary>The last brick fell; the banner shows, then the next level is served.</summary>
    Cleared,

    /// <summary>Out of lives, or every level cleared (see <see cref="Court.Victory"/>).</summary>
    Over,
}

/// <summary>The two power-ups.</summary>
public enum CapsuleKind : byte
{
    /// <summary>The paddle is wider for ten seconds.</summary>
    Wide,

    /// <summary>Every ball splits in three.</summary>
    Multi,
}

/// <summary>One ball: centre and velocity in court units (velocity per tick).</summary>
[SyncedStruct]
public partial struct Ball
{
    /// <summary>Centre x.</summary>
    public Fixed X { get; set; }

    /// <summary>Centre y.</summary>
    public Fixed Y { get; set; }

    /// <summary>Velocity x.</summary>
    public Fixed Vx { get; set; }

    /// <summary>Velocity y.</summary>
    public Fixed Vy { get; set; }
}

/// <summary>A power-up falling towards the paddle.</summary>
[SyncedStruct]
public partial struct Capsule
{
    /// <summary>Centre x.</summary>
    public Fixed X { get; set; }

    /// <summary>Centre y.</summary>
    public Fixed Y { get; set; }

    /// <summary>What it does when caught.</summary>
    public CapsuleKind Kind { get; set; }
}

/// <summary>Helpers for the packed brick value stored in <see cref="Court.Bricks"/>.</summary>
public static class Brick
{
    /// <summary>The hit count that marks a brick nothing can break.</summary>
    public const int Unbreakable = 15;

    /// <summary>Packs a brick: hits left (0 = empty, 15 = unbreakable), hits it started with and a colour index.</summary>
    public static ushort Pack(int hits, int max, int tint) => (ushort)(hits | (max << 4) | (tint << 8));

    /// <summary>Hits left; zero for an empty cell.</summary>
    public static int Hits(ushort cell) => cell & 15;

    /// <summary>Hits the brick started with.</summary>
    public static int Max(ushort cell) => (cell >> 4) & 15;

    /// <summary>The colour index (1 to 6 for plain bricks, 7 for tough ones).</summary>
    public static int Tint(ushort cell) => (cell >> 8) & 15;

    /// <summary>True when the cell holds a brick the player has to break.</summary>
    public static bool IsBreakable(ushort cell) => Hits(cell) is > 0 and < Unbreakable;
}
