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
            (services.Sounds as IDisposable)?.Dispose();
            services.Store?.Dispose();
        }
    }
}
