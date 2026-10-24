using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace Solitaire;

/// <summary>The game: its entity, its starting deal and its screens.</summary>
public sealed class SolitaireGame : GameDefinition
{
    /// <inheritdoc />
    public override string Name => "Solitaire";

    /// <inheritdoc />
    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Solitaire.EntityTypes.Register(registry);

    /// <inheritdoc />
    public override void Setup(World world) => world.Add(new Table()).NewGame(1);

    /// <inheritdoc />
    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Label(() => "Score " + ((Table)game.World.Entities[0]).Score));
}

/// <summary>Command-line helpers for the program entry point.</summary>
public static class Launch
{
    /// <summary>Adds a seed from the clock to a windowed run that was not given one, so every game deals differently. Headless runs keep the fixed default so they repeat.</summary>
    public static string[] WithRandomSeed(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (Array.IndexOf(args, "--seed") >= 0 || Array.IndexOf(args, "--headless") >= 0)
        {
            return args;
        }

        return [.. args, "--seed", ((ulong)Environment.TickCount64 ^ (ulong)Environment.ProcessId).ToString(System.Globalization.CultureInfo.InvariantCulture)];
    }
}
