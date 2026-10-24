using System.Runtime.CompilerServices;

// Compiled into a game's Debug build by build/Game.targets. It turns on the development tools before the game's own code runs.
internal static class OneGameDevBoot
{
    [ModuleInitializer]
    internal static void Enable() => OneGame.Live.Dev.Enable();
}
