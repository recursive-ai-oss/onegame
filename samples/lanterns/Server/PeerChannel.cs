using System.Diagnostics;
using System.Net;
using OneGame.Net;
using OneGame.Sync;

namespace Lanterns.Server;

/// <summary>
/// A connection to another server that opens itself again when it drops, so servers can start in any order and a restarted one is found
/// again. While it is not connected, sends are dropped: the sync layer asks again (it resubscribes, and it repeats unanswered hand-over and
/// swap steps), so nothing is queued on a dead link.
/// </summary>
internal sealed class PeerChannel : IMessageChannel
{
    private const int RetryMilliseconds = 250;

    private readonly IPEndPoint _endpoint;
    private readonly UdpOptions _options;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private UdpConnection? _connection;
    private long _nextAttempt;
    private bool _wasConnected;

    public PeerChannel(IPEndPoint endpoint, NetSecurity security)
    {
        _endpoint = endpoint;
        _options = new UdpOptions { Security = security };
    }

    /// <summary>True while the handshake is complete and the connection is open.</summary>
    public bool IsConnected => _connection is { State: ConnectionState.Connected };

    /// <summary>Raised from <see cref="Pump"/> each time a connection (re)opens. The owner resubscribes its replicas.</summary>
    public event Action? Opened;

    public void Pump()
    {
        if (_connection is null || _connection.State == ConnectionState.Closed)
        {
            _connection = null;
            _wasConnected = false;
            if (_clock.ElapsedMilliseconds >= _nextAttempt)
            {
                _nextAttempt = _clock.ElapsedMilliseconds + RetryMilliseconds;
                _connection = UdpTransport.Connect(_endpoint, _options, NetClock.System);
            }

            return;
        }

        _connection.Pump();
        if (IsConnected && !_wasConnected)
        {
            _wasConnected = true;
            Opened?.Invoke();
        }
    }

    public void Send(ReadOnlySpan<byte> message)
    {
        if (IsConnected)
        {
            _connection!.Send(message);
        }
    }

    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (IsConnected)
        {
            return _connection!.TryReceive(out message);
        }

        message = default;
        return false;
    }

    /// <summary>Closes the connection for good (the server is stopping).</summary>
    public void Close()
    {
        _connection?.Disconnect(DisconnectReason.Shutdown);
        _connection = null;
    }
}
