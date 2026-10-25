namespace OneGame.Cluster.Tests;

/// <summary>Where the built fixture programs are, and how to describe clusters of them.</summary>
internal static class Fixture
{
    private static string Program(string name)
    {
        // <artifacts>/bin/<this test app>/<configuration>/ next to <artifacts>/bin/<program>/<configuration>/
        var config = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Name;
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", name, config, name + (OperatingSystem.IsWindows() ? ".exe" : "")));
        return File.Exists(path) ? path : throw new FileNotFoundException($"{path} is not built; build the test project (it builds the fixture programs).");
    }

    public static string Server => Program("ClusterServer");

    public static string Game => Program("ClusterGame");

    public static string Scenario(string name) => Path.Combine(AppContext.BaseDirectory, "scenarios", name);

    /// <summary>A scratch folder under the test run's temporary folder.</summary>
    public static string NewDir(string name) => Path.Combine(Path.GetTempPath(), "og-cluster-" + name + "-" + Guid.NewGuid().ToString("N")[..6]);

    public static NodeSpec ServerNode(string name, string listen = "udp", int tickRate = 0, bool manual = false) => new()
    {
        Name = name,
        Role = NodeRole.Server,
        Exe = Server,
        Listen = listen,
        TickRate = tickRate,
        Manual = manual,
        Scopes = [new ScopeSpec(1, 7)],
    };

    public static NodeSpec ClientNode(string name, string server, string transport = "udp", string? net = null) => new()
    {
        Name = name,
        Role = NodeRole.Client,
        Exe = Game,
        Connect = server,
        Transport = transport,
        Net = net,
    };
}
