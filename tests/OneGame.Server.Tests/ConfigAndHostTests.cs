namespace OneGame.Server.Tests;

public class ConfigTests
{
    [Fact]
    public void AFullConfigurationReadsIntoTypedValues()
    {
        var c = ServerConfig.Parse("""
            name: arena
            tickRate: 30
            scopes:
              - { id: 1, seed: 42 }
              - { id: 2 }
            plugins:
              - name: counter
                path: /opt/plugins/counter
                scopes: [2]
                settings: { step: 3, mode: fast }
            """);
        Assert.Equal("arena", c.Name);
        Assert.Equal(30, c.TickRate);
        Assert.Equal(2, c.Scopes.Count);
        Assert.Equal(42UL, c.Scopes[0].Seed);
        Assert.Equal(new PluginConfig("counter", "/opt/plugins/counter", [2], [new("step", "3"), new("mode", "fast")]).Name, c.Plugins[0].Name);
        Assert.Equal([2u], c.Plugins[0].Scopes);
        Assert.Equal("3", c.Plugins[0].Settings[0].Value);
    }

    [Theory]
    [InlineData("nme: x", "unknown key 'nme'")]
    [InlineData("scopes:\n  - { id: 1, sed: 3 }", "unknown key 'sed'")]
    [InlineData("scopes:\n  - { id: 1 }\n  - { id: 1 }", "declared twice")]
    [InlineData("plugins:\n  - { name: a, path: p, scopes: [9] }", "not declared")]
    [InlineData("plugins:\n  - { name: a, path: p, colour: red }", "unknown key 'colour'")]
    [InlineData("tickRate: 0", "between 1 and 1000")]
    [InlineData("scopes:\n  - { seed: 3 }", "'id' is required")]
    public void MistakesAreErrorsThatNameTheLine(string yaml, string expected)
    {
        var ex = Assert.Throws<ServerConfigException>(() => ServerConfig.Parse(yaml, "server.yaml"));
        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
        Assert.StartsWith("server.yaml:", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RelativePluginPathsResolveAgainstTheFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "og-cfg-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "server.yaml");
            File.WriteAllText(file, "plugins:\n  - { name: a, path: plugins/a }\n");
            var c = ServerConfig.Load(file);
            Assert.Equal(Path.Combine(dir, "plugins", "a"), c.Plugins[0].Path);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

public class HostTests
{
    [Fact]
    public void TheLifecycleRunsStartTickDrainStopAndRejectsMisuse()
    {
        var host = new ServerHost(new ServerConfig { Scopes = [new ScopeConfig(1, 1), new ScopeConfig(2, 2)] });
        Assert.Equal(ServerState.Created, host.State);
        Assert.Throws<InvalidOperationException>(() => host.StepOnce());
        host.Start();
        Assert.Equal(ServerState.Running, host.State);
        Assert.Throws<InvalidOperationException>(() => host.Start());
        host.Step(3);
        Assert.All(host.ScopeInfos(), s => Assert.Equal(3, s.Tick));

        // The fixed-step driver turns elapsed time into whole ticks.
        var due = host.Advance(TimeSpan.FromMilliseconds(50));
        Assert.InRange(due, 2, 3);
        host.Stop();
        Assert.Equal(ServerState.Stopped, host.State);
        Assert.Throws<InvalidOperationException>(() => host.StepOnce());
    }

    [Fact]
    public void RunStopsWhenAskedAndServesTheBetweenHook()
    {
        var host = new ServerHost(new ServerConfig { TickRate = 1000 });
        host.Start();
        var calls = 0;
        host.Run(between: () =>
        {
            if (++calls == 5)
            {
                host.RequestStop();
            }

            Thread.Sleep(1);
        });
        Assert.Equal(5, calls);
        Assert.True(host.Tick > 0);
    }

    [Fact]
    public void TheLogKeepsRecentLinesAsText()
    {
        var sink = new StringWriter();
        var host = new ServerHost(new ServerConfig(), log: new ServerLog(sink));
        host.Start();
        Assert.Contains("started", sink.ToString(), StringComparison.Ordinal);
        Assert.Contains(host.Log.Tail(5), l => l.Message.Contains("started", StringComparison.Ordinal));
    }
}
