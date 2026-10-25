using Lanterns;
using OneGame.Host;

/// <summary>The program entry point. It is a named class, not top-level statements, so the Android entry point can call it.</summary>
internal static class Program
{
    // Single player by default; with --connect <address> --scope N --also-scopes M,3 the same game is a client of the island servers.
    internal static int Main(string[] args) => GameApp.Run(new LanternsGame(), args);
}
