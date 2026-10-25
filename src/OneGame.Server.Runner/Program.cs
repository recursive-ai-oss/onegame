using OneGame.Server.Live;
using OneGame.Server.Plugins;

namespace OneGame.Server.Runner;

internal static class Program
{
    // The plugin host: no game types of its own, plugins bring them. See ServerApp for the command line.
    private static int Main(string[] args) => ServerApp.Run(null, args, new PluginLoader());
}
