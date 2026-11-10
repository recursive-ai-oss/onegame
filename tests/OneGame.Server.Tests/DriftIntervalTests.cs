namespace OneGame.Server.Tests;

public class DriftIntervalTests
{
    [Fact]
    public void TheConfigurationKeyIsReadAndDefaultsToThirty()
    {
        Assert.Equal(30, ServerConfig.Parse("name: a").DriftCheckInterval);
        Assert.Equal(7, ServerConfig.Parse("driftCheckInterval: 7").DriftCheckInterval);
    }

    [Theory]
    [InlineData("driftCheckInterval: 0")]
    [InlineData("driftCheckInterval: 10001")]
    public void OutOfRangeValuesAreErrorsThatNameTheLine(string yaml)
    {
        var ex = Assert.Throws<ServerConfigException>(() => ServerConfig.Parse(yaml, "server.yaml"));
        Assert.StartsWith("server.yaml:1:", ex.Message, StringComparison.Ordinal);
        Assert.Contains("driftCheckInterval must be between 1 and 10000", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePropertyBeatsTheFileAndIsCheckedAndFrozenAtStart()
    {
        var host = new ServerHost(new ServerConfig { DriftCheckInterval = 12 });
        Assert.Equal(12, host.DriftCheckInterval);
        host.DriftCheckInterval = 5;
        Assert.Equal(5, host.DriftCheckInterval);
        Assert.Throws<ArgumentOutOfRangeException>(() => host.DriftCheckInterval = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => host.DriftCheckInterval = 10_001);
        host.DriftCheckInterval = 0; // back to the file's value
        Assert.Equal(12, host.DriftCheckInterval);
        host.Start();
        Assert.Equal(12, host.Node.DriftCheckInterval);
        Assert.Throws<InvalidOperationException>(() => host.DriftCheckInterval = 3);
        host.Stop();
    }

    [Fact]
    public void ACodeBuiltConfigurationOutOfRangeRefusesToStart()
    {
        var host = new ServerHost(new ServerConfig { DriftCheckInterval = 0 });
        Assert.Throws<ServerConfigException>(() => host.Start());
    }

    private const byte FrameCode = 17; // the frame message type of the sync protocol

    private sealed class Tap(IMessageChannel inner, List<long> hashedTicks) : IMessageChannel
    {
        public void Send(ReadOnlySpan<byte> message) => inner.Send(message);

        public bool TryReceive(out ReadOnlySpan<byte> message)
        {
            if (!inner.TryReceive(out message))
            {
                return false;
            }

            if (message[0] == FrameCode)
            {
                var r = new SyncReader(message[1..]);
                r.ReadVarUInt();
                var tick = r.ReadVarInt();
                if ((r.ReadByte() & 2) != 0)
                {
                    hashedTicks.Add(tick);
                }
            }

            return true;
        }
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(7, 7)]
    public void FramesCarryAHashAtTheConfiguredInterval(int setInCode, int expected)
    {
        var host = new ServerHost(new ServerConfig { DriftCheckInterval = 30 }) { DriftCheckInterval = setInCode };
        host.Start();
        var hashed = new List<long>();
        var client = new Node("client");
        var (a, b) = Loopback.CreatePair();
        var link = client.ConnectTo(host.Node, Trust.Reader(new ScopeId(1)), new Tap(a, hashed), b, "client");
        var replica = client.Subscribe(link, new ScopeId(1), host.CreateReplicaWorld(new ScopeId(1)));
        for (var i = 0; i < 100; i++)
        {
            host.StepOnce();
            host.Node.Pump();
            client.Pump();
        }

        Assert.NotEmpty(hashed);
        Assert.All(hashed, t => Assert.Equal(0, t % expected));
        Assert.Equal(100 / expected, hashed.Count);
        Assert.Empty(client.Refusals);
        Assert.Equal(ReplicaState.Live, replica.State);
        host.Stop();
    }

    [Fact]
    public void ServerInfoReportsTheIntervalInForce()
    {
        var host = new ServerHost(new ServerConfig()) { DriftCheckInterval = 9 };
        host.Start();
        var control = new OneGame.Server.Live.ServerControl(host);
        Assert.Contains("\"driftCheckInterval\":9", control.Call("server_info"), StringComparison.Ordinal);
        host.Stop();
    }
}
