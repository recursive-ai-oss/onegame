using System;
using OneGame.Host;
using PhysicsPourExample;

// A class with Main (not top-level statements) so the same file also builds as an Android library.
internal static class Program
{
    internal static int Main(string[] args)
    {
        // `--particles N` sets how much liquid the jug holds (default 1000); the jug's size stays the same, so more particles means finer ones.
        var particles = PourGame.DefaultParticles;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var value = a.StartsWith("--particles=", StringComparison.Ordinal) ? a[12..] : a == "--particles" && i + 1 < args.Length ? args[i + 1] : null;
            if (value is not null && int.TryParse(value, out var n))
            {
                particles = Math.Clamp(n, 100, 16000);
            }
        }

        return GameApp.Run(new PourGame(particles), args);
    }
}
