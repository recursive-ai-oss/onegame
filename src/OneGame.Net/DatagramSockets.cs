using System.Net;
using System.Net.Sockets;

namespace OneGame.Net;

/// <summary>
/// The datagram socket the UDP transport talks to. The real one is <see cref="SystemDatagramSocket"/>; tests use
/// <see cref="MemoryDatagramNetwork"/> sockets, and <see cref="ConditionedDatagramSocket"/> wraps either.
/// </summary>
public interface IDatagramSocket : IDisposable
{
    /// <summary>The address this socket receives on.</summary>
    SocketAddress LocalAddress { get; }

    /// <summary>Sends one datagram. A datagram the network cannot take is dropped, as UDP allows.</summary>
    void Send(ReadOnlySpan<byte> datagram, SocketAddress destination);

    /// <summary>Takes the next waiting datagram into <paramref name="buffer"/> and writes the sender into <paramref name="from"/>; returns its length, or -1 if none is waiting.</summary>
    int Receive(Span<byte> buffer, SocketAddress from);
}

/// <summary>A non-blocking operating-system UDP socket.</summary>
public sealed class SystemDatagramSocket : IDatagramSocket
{
    private readonly Socket _socket;

    /// <summary>Binds a socket.</summary>
    /// <param name="bind">The local address; port 0 picks a free port.</param>
    public SystemDatagramSocket(IPEndPoint bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        _socket = new Socket(bind.AddressFamily, SocketType.Dgram, ProtocolType.Udp) { Blocking = false };
        try
        {
            _socket.ReceiveBufferSize = 4 << 20;
            _socket.SendBufferSize = 4 << 20;
            _socket.Bind(bind);
            LocalAddress = ((IPEndPoint)_socket.LocalEndPoint!).Serialize();
        }
        catch
        {
            _socket.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public SocketAddress LocalAddress { get; }

    /// <inheritdoc />
    public void Send(ReadOnlySpan<byte> datagram, SocketAddress destination)
    {
        try
        {
            _ = _socket.SendTo(datagram, SocketFlags.None, destination);
        }
        catch (SocketException)
        {
            // A full send buffer or an unreachable host is packet loss to the protocol above, which repairs it.
        }
        catch (ObjectDisposedException)
        {
            // Closed while a connection was still winding down.
        }
    }

    /// <inheritdoc />
    public int Receive(Span<byte> buffer, SocketAddress from)
    {
        while (true)
        {
            try
            {
                if (!_socket.Poll(0, SelectMode.SelectRead))
                {
                    return -1;
                }

                return _socket.ReceiveFrom(buffer, SocketFlags.None, from);
            }
            catch (SocketException)
            {
                // Windows reports an ICMP port-unreachable for an earlier send as an error on the next receive; nothing was read.
                if (!_socket.Poll(0, SelectMode.SelectRead))
                {
                    return -1;
                }
            }
            catch (ObjectDisposedException)
            {
                return -1;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => _socket.Dispose();
}

/// <summary>An in-process datagram network: sockets that deliver to each other by address, with no operating system involved. Tests use it to inject datagrams from any source address.</summary>
public sealed class MemoryDatagramNetwork
{
    private const int MaxQueued = 8192;
    private readonly Dictionary<SocketAddress, MemorySocket> _sockets = [];
    private int _nextPort = 20000;

    /// <summary>Datagrams that found no socket to receive them.</summary>
    public long Undeliverable { get; private set; }

    /// <summary>Creates a socket at a fresh loopback address.</summary>
    public IDatagramSocket CreateSocket() => CreateSocket(_nextPort++);

    /// <summary>Creates a socket on a given loopback port.</summary>
    /// <param name="port">The port; must be unused on this network.</param>
    public IDatagramSocket CreateSocket(int port)
    {
        var address = new IPEndPoint(IPAddress.Loopback, port).Serialize();
        var socket = new MemorySocket(this, address);
        _sockets.Add(address, socket);
        return socket;
    }

    /// <summary>Delivers a datagram as if <paramref name="from"/> had sent it, whether or not such a socket exists. This is how tests play spoofed sources.</summary>
    /// <param name="datagram">The bytes.</param>
    /// <param name="from">The claimed sender.</param>
    /// <param name="destination">The receiving socket's address.</param>
    public void Inject(ReadOnlySpan<byte> datagram, SocketAddress from, SocketAddress destination)
    {
        if (!_sockets.TryGetValue(destination, out var target))
        {
            Undeliverable++;
            return;
        }

        target.Enqueue(datagram, from);
    }

    private sealed class MemorySocket : IDatagramSocket
    {
        private readonly MemoryDatagramNetwork _net;
        private readonly Queue<(byte[] Data, int Length, SocketAddress From)> _inbox = new();

        public MemorySocket(MemoryDatagramNetwork net, SocketAddress address)
        {
            _net = net;
            LocalAddress = address;
        }

        public SocketAddress LocalAddress { get; }

        public void Send(ReadOnlySpan<byte> datagram, SocketAddress destination) => _net.Inject(datagram, LocalAddress, destination);

        public void Enqueue(ReadOnlySpan<byte> datagram, SocketAddress from)
        {
            if (_inbox.Count >= MaxQueued)
            {
                return;
            }

            var copy = NetBuffers.Rent(Math.Max(1, datagram.Length));
            datagram.CopyTo(copy);
            _inbox.Enqueue((copy, datagram.Length, from));
        }

        public int Receive(Span<byte> buffer, SocketAddress from)
        {
            if (!_inbox.TryDequeue(out var d))
            {
                return -1;
            }

            var n = Math.Min(buffer.Length, d.Length);
            d.Data.AsSpan(0, n).CopyTo(buffer);
            NetBuffers.Return(d.Data);
            d.From.Buffer.Span.CopyTo(from.Buffer.Span);
            return n;
        }

        public void Dispose()
        {
            _ = _net._sockets.Remove(LocalAddress);
            while (_inbox.TryDequeue(out var d))
            {
                NetBuffers.Return(d.Data);
            }
        }
    }
}
