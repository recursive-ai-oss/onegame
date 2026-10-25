using OneGame.Core;

[assembly: SimTrust("OneGame.Sync")]

namespace Lanterns;

/// <summary>The numbers that shape the world. Positions are whole units, 16 to a tile; the simulation uses no floating point.</summary>
public static class Rules
{
    /// <summary>Scope of the first island.</summary>
    public const uint IslandA = 1;

    /// <summary>Scope of the second island.</summary>
    public const uint IslandB = 2;

    /// <summary>Scope of the master server: accounts, the leaderboard and the swap ledger.</summary>
    public const uint Master = 3;

    /// <summary>The identity of server-side code. Commands owned by it cannot be sent by a player.</summary>
    public const uint ServerPlayer = uint.MaxValue - 2;

    /// <summary>Island width in units.</summary>
    public const int Width = 384;

    /// <summary>Island height in units.</summary>
    public const int Height = 192;

    /// <summary>Top of the bridge opening on the island's side edge.</summary>
    public const int BridgeTop = 80;

    /// <summary>Bottom of the bridge opening.</summary>
    public const int BridgeBottom = 112;

    /// <summary>Units a player moves per tick.</summary>
    public const int Speed = 2;

    /// <summary>A lantern is picked up when a player is within this many units on both axes.</summary>
    public const int PickupReach = 12;

    /// <summary>Different lantern colours: red, blue and gold.</summary>
    public const int Colors = 3;

    /// <summary>The island keeps this many lanterns on the ground.</summary>
    public const int GroundTarget = 10;

    /// <summary>A new lantern appears this often (in ticks) while the ground has fewer than <see cref="GroundTarget"/>.</summary>
    public const int SpawnEvery = 20;

    /// <summary>Escrow kind of a colour (kind 0 is not used).</summary>
    public static uint Kind(int color) => (uint)color + 1;

    /// <summary>The scope on the far side of the bridge.</summary>
    public static uint Across(uint island) => island == IslandA ? IslandB : IslandA;

    /// <summary>True when a height lines up with the bridge.</summary>
    public static bool OnBridge(int y) => y >= BridgeTop && y <= BridgeBottom;

    /// <summary>Colour names for views and scenarios.</summary>
    public static string ColorName(int color) => color switch { 0 => "Red", 1 => "Blue", _ => "Gold" };
}
