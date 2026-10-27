using System.Linq;
using OneGame.Core;

namespace PagesGame;

/// <summary>A score. The only synced state; every page in this game is view-only.</summary>
[Entity]
public partial class Meter : Entity
{
    [Synced] public partial int Score { get; private set; }

    /// <summary>Adds points.</summary>
    [Command]
    public void Add(int points) => Score += points;

    /// <summary>Finds the meter of a world.</summary>
    public static Meter Of(World world) => (Meter)world.Entities.First(e => e is Meter);
}
