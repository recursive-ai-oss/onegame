using System.Diagnostics;
using System.Net;

namespace OneGame.Net.Tests;

/// <summary>
/// Checks the WebSocket transport against a third-party implementation (the Python <c>websockets</c> package), used only as a
/// black-box peer. Set ONEGAME_WS_PYTHON to a Python interpreter that has the package installed to run these.
/// </summary>
public class WebSocketInteropTests
{
    private static readonly int[] Sizes = [0, 1, 125, 126, 127, 65535, 65536, 300000, 1000000];

    private static string? Python => Environment.GetEnvironmentVariable("ONEGAME_WS_PYTHON");

    private static string Script => Path.Combine(AppContext.BaseDirectory, "interop", "peer.py");

    private static Process Start(params string[] args)
    {
        var info = new ProcessStartInfo(Python!) { RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(Script);
        foreach (var a in args)
        {
            info.ArgumentList.Add(a);
        }

        return Process.Start(info)!;
    }

    [Fact]
    public async Task AThirdPartyClientTalksToOurServer()
    {
        Assert.SkipUnless(Python is not null, "ONEGAME_WS_PYTHON is not set");
        using var server = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.LocalEndPoint).Port;
        using var peer = Start("client", port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var stdout = peer.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = peer.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        INetConnection? conn = null;
        DisconnectInfo? closed = null;
        var echoed = 0;
        var deadline = Environment.TickCount64 + 60_000;
        while (closed is null && Environment.TickCount64 < deadline)
        {
            server.Pump();
            if (conn is null && server.TryAccept(out conn))
            {
                conn.Disconnected += (_, i) => closed = i;
            }

            while (conn is not null && conn.TryReceive(out var m))
            {
                Assert.True(m.SequenceEqual(Rig.Pattern(Sizes[echoed], echoed)), $"message {echoed} corrupted");
                conn.Send(m);
                echoed++;
            }

            Thread.Sleep(1);
        }

        Assert.True(peer.WaitForExit(30_000));
        Assert.True(peer.ExitCode == 0, await stderr);
        Assert.Contains("OK", await stdout, StringComparison.Ordinal);
        Assert.Equal(Sizes.Length, echoed);
        Assert.Equal(new DisconnectInfo(DisconnectReason.Normal, true, 1000), closed);
    }

    [Fact]
    public void OurClientTalksToAThirdPartyServer()
    {
        Assert.SkipUnless(Python is not null, "ONEGAME_WS_PYTHON is not set");
        var portFile = Path.Combine(Path.GetTempPath(), $"onegame-ws-{Environment.ProcessId}-{Environment.TickCount64}.port");
        using var peer = Start("server", portFile);
        try
        {
            var deadline = Environment.TickCount64 + 30_000;
            while (!File.Exists(portFile) && Environment.TickCount64 < deadline)
            {
                Thread.Sleep(20);
            }

            Assert.True(File.Exists(portFile), "the peer did not start");
            Thread.Sleep(50);
            var port = int.Parse(File.ReadAllText(portFile), System.Globalization.CultureInfo.InvariantCulture);
            var client = WebSocketTransport.Connect(new IPEndPoint(IPAddress.Loopback, port));
            for (var i = 0; i < Sizes.Length; i++)
            {
                client.Send(Rig.Pattern(Sizes[i], i));
            }

            var got = 0;
            deadline = Environment.TickCount64 + 60_000;
            while (got < Sizes.Length && Environment.TickCount64 < deadline)
            {
                client.Pump();
                while (client.TryReceive(out var m))
                {
                    Assert.True(m.SequenceEqual(Rig.Pattern(Sizes[got], got)), $"echo {got} corrupted");
                    got++;
                }

                Thread.Sleep(1);
            }

            Assert.Equal(Sizes.Length, got);
            client.Disconnect(DisconnectReason.Normal, flush: true);
            for (var i = 0; i < 500 && client.State != ConnectionState.Closed; i++)
            {
                client.Pump();
                Thread.Sleep(2);
            }

            Assert.Equal(ConnectionState.Closed, client.State);
            Assert.Equal(new DisconnectInfo(DisconnectReason.Normal, false, 1000), client.CloseInfo);
        }
        finally
        {
            peer.Kill();
            File.Delete(portFile);
        }
    }
}
