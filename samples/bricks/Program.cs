using Bricks;
using OneGame.Host;

/// <summary>The program entry point. It is a named class, not top-level statements, so the Android entry point can call it.</summary>
internal static class Program
{
    internal static int Main(string[] args)
    {
        var services = Launch.CreateServices(args);
        try
        {
            return GameApp.Run(new BricksGame(services), args);
        }
        finally
        {
            // In a web page Main returns at once and the game keeps running on the page's frame loop, so its services stay open.
            if (!OperatingSystem.IsBrowser())
            {
                (services.Sounds as IDisposable)?.Dispose();
            }
        }
    }
}
