using OneGame.Host;
using Solitaire;

/// <summary>The program entry point. It is a named class, not top-level statements, so the Android entry point can call it.</summary>
internal static class Program
{
    internal static int Main(string[] args)
    {
        var services = Launch.CreateServices(args);
        try
        {
            return GameApp.Run(new SolitaireGame(services), Launch.WithRandomSeed(args));
        }
        finally
        {
            // In a web page Main returns at once and the game keeps running on the page's frame loop, so its services stay open.
            if (!OperatingSystem.IsBrowser())
            {
                (services.Sounds as IDisposable)?.Dispose();
                services.Store?.Dispose();
            }
        }
    }
}
