using System;
using OneGame.Host;
using OneGame.Purchases;
using StoreGame;

/// <summary>The entry point is a named class so the Android entry point can call it.</summary>
internal static class Program
{
    internal static int Main(string[] args)
    {
        // A phone has no way to set environment variables for the app, so the fake store can also be chosen with an argument.
        var flag = Array.IndexOf(args, "--fake-store");
        if (flag >= 0)
        {
            Environment.SetEnvironmentVariable(PurchaseBackends.BackendVariable, "fake");
            args = [.. args[..flag], .. args[(flag + 1)..]];
        }

        return GameApp.Run(new ShopGame(), args);
    }
}
