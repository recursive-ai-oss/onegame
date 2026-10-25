using OneGame.Host;
using StoreGame;

/// <summary>The entry point is a named class so the Android entry point can call it.</summary>
internal static class Program
{
    internal static int Main(string[] args) => GameApp.Run(new ShopGame(), args);
}
