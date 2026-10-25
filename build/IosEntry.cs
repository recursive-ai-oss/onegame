// Compiled into a game built for iOS (build/iOS.targets). UIKit owns the process's main thread, so the program's entry point hands over to
// the app host, which runs the game's own Main on a thread of its own once the window exists.
internal static class OneGameIosEntry
{
    internal static int Main(string[] args) =>
        OneGame.Platform.IosAppHost.Run(args, gameArgs =>
        {
#if ONEGAME_DEV
            OneGame.Live.Dev.Enable();
#endif
            return global::Program.Main(gameArgs);
        });
}
