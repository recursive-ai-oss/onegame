using System.Net.Sockets;
using OneGame.Control;
using OneGame.Live;
using OneGame.Server.Live;
using OneGame.Server.Plugins;

namespace OneGame.Server.Tests;

public class ControlTests : IDisposable
{
    private readonly List<string> _folders = [];

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var f in _folders)
        {
            try
            {
                Directory.Delete(f, true);
            }
            catch (IOException)
            {
            }
        }
    }

    private string Folder(int variant = 1, Func<string, string>? edit = null)
    {
        var dir = PluginBuilds.NewFolder();
        _folders.Add(dir);
        return PluginBuilds.Build(dir, variant, edit);
    }

    private static string Q(string s) => System.Text.Json.JsonSerializer.Serialize(s);

    [Fact]
    public void PluginOperationsListLoadReloadAndUnloadAsJson()
    {
        var dir = Folder(1);
        using var t = new TestServer(new ServerConfig(), loader: new PluginLoader());
        var control = new ServerControl(t.Host);

        var loaded = control.Call("plugin_load", $$$"""{"name":"counter","path":{{{Q(dir)}}},"settings":{"step":3}}""");
        Assert.Contains("\"state\":\"Loaded\"", loaded, StringComparison.Ordinal);
        Assert.Contains("\"entityTypes\":[\"Counter\"]", loaded, StringComparison.Ordinal);

        control.Call("step", """{"ticks":4}""");
        Assert.Contains("\"Value\":12", control.Call("state_query", """{"entity":1}"""), StringComparison.Ordinal);
        Assert.Contains("\"status\":\"Queued\"", control.Call("command_send", """{"entity":1,"command":"Add","args":["10"]}"""), StringComparison.Ordinal);
        control.Call("step");
        Assert.Contains("\"Value\":25", control.Call("state_query", """{"entity":1}"""), StringComparison.Ordinal);

        PluginBuilds.Build(dir, 2);
        var reload = control.Call("plugin_reload", """{"name":"counter"}""");
        Assert.Contains("\"collected\":true", reload, StringComparison.Ordinal);
        control.Call("step");

        // Variant 2 adds twice the configured step of 3.
        Assert.Contains("\"Value\":31", control.Call("state_query", """{"entity":1}"""), StringComparison.Ordinal);
        Assert.Contains("\"reloads\":1", control.Call("plugin_list"), StringComparison.Ordinal);

        var unloaded = control.Call("plugin_unload", """{"name":"counter"}""");
        Assert.Contains("\"entitiesRemoved\":1", unloaded, StringComparison.Ordinal);
        Assert.Equal("[]", control.Call("plugin_list"));
    }

    [Fact]
    public void FailuresCarryAStableCodeAndAMessageThatSaysWhatToDo()
    {
        var bad = Folder(1, s => s.Replace("[assembly: ServerPlugin(typeof(Sample.CounterPlugin))]", "[assembly: ServerPlugin(typeof(Sample.CounterPlugin), 7)]", StringComparison.Ordinal));
        using var t = new TestServer(new ServerConfig(), loader: new PluginLoader());
        var control = new ServerControl(t.Host);

        var mismatch = Assert.Throws<ControlException>(() => control.Call("plugin_load", $$"""{"name":"counter","path":{{Q(bad)}}}"""));
        Assert.Equal("api_mismatch", mismatch.Code);
        Assert.Contains("Rebuild the plugin", mismatch.Message, StringComparison.Ordinal);

        Assert.Equal("not_found", Assert.Throws<ControlException>(() => control.Call("plugin_unload", """{"name":"x"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => control.Call("plugin_load", """{"name":"x"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => control.Call("step", """{"ticks":0}""")).Code);
        Assert.Equal("unknown_op", Assert.Throws<ControlException>(() => control.Call("nope")).Code);
        Assert.Equal("not_found", Assert.Throws<ControlException>(() => control.Call("command_send", """{"entity":99,"command":"Add"}""")).Code);
    }

    [Fact]
    public void ALoadFailureAtStartIsListedInServerInfoAndTheServerStillRuns()
    {
        var config = new ServerConfig { Plugins = [new PluginConfig("counter", "/does/not/exist", [], [])] };
        var host = new ServerHost(config, loader: new PluginLoader());
        host.Start();
        host.Step(2);
        var info = new ServerControl(host).Call("server_info");
        Assert.Contains("\"loadFailures\":[{\"name\":\"counter\",\"code\":\"not_found\"", info, StringComparison.Ordinal);
        Assert.Contains("\"tick\":2", info, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLiveSocketSpeaksMcpForAServer()
    {
        var dir = Folder();
        var host = new ServerHost(new ServerConfig(), loader: new PluginLoader());
        host.Start();
        var sock = Path.Combine(Path.GetTempPath(), "og-srv-" + Guid.NewGuid().ToString("N")[..8] + ".sock");
        using var live = new LiveServer(new ServerControl(host));
        live.Start(sock);

        string? list = null, call = null, load = null;
        var client = new Thread(() =>
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(sock));
            using var stream = new NetworkStream(socket);
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            writer.WriteLine("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""");
            list = reader.ReadLine();
            writer.WriteLine("""{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"plugin_load","arguments":{"name":"counter","path":""" + Q(dir) + "}}}");
            load = reader.ReadLine();
            writer.WriteLine("""{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"server_info","arguments":{}}}""");
            call = reader.ReadLine();
        });
        client.Start();
        var deadline = Environment.TickCount64 + 20_000;
        while (client.IsAlive && Environment.TickCount64 < deadline)
        {
            live.Pump();
            live.WaitForWork(5);
        }

        Assert.NotNull(list);
        Assert.Contains("plugin_reload", list, StringComparison.Ordinal);
        Assert.Contains("hot-reload", list, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Loaded", load, StringComparison.Ordinal);
        Assert.Contains("counter", call, StringComparison.Ordinal);
        Assert.Single(host.Plugins());
    }
}
