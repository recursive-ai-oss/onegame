using OneGame.Core;

internal static class CryptoSmoke
{
    /// <summary>Runs a full Noise XX handshake between two in-process peers and sends sealed packets both ways. Returns a problem or null.</summary>
    public static string? Run()
    {
        var serverKey = new byte[32];
        var clientKey = new byte[32];
        Entropy.Fill(serverKey);
        Entropy.Fill(clientKey);

        var client = new NoiseHandshake(NoisePattern.XX, true, [1, 2], clientKey, []);
        var server = new NoiseHandshake(NoisePattern.XX, false, [1, 2], serverKey, []);
        var buffer = new byte[512];
        var plain = new byte[512];
        for (var i = 0; i < 3; i++)
        {
            var writer = i % 2 == 0 ? client : server;
            var reader = i % 2 == 0 ? server : client;
            var n = writer.WriteMessage([(byte)i], buffer);
            if (!reader.ReadMessage(buffer.AsSpan(0, n), plain, out var length) || length != 1 || plain[0] != i)
            {
                return "handshake message " + i + " was not read";
            }
        }

        if (!client.IsComplete || !server.IsComplete || !client.HandshakeHash.SequenceEqual(server.HandshakeHash))
        {
            return "handshake did not finish with equal hashes";
        }

        var clientPublic = new byte[32];
        X25519.PublicKey(clientKey, clientPublic);
        if (!server.RemoteStatic.SequenceEqual(clientPublic))
        {
            return "the server did not learn the client's static key";
        }

        client.Split(out var clientSend, out var clientReceive);
        server.Split(out var serverSend, out var serverReceive);
        var sealedPacket = new byte[1200 + 16];
        var opened = new byte[1200];
        for (var i = 0; i < 4; i++)
        {
            clientSend.Encrypt([7], new byte[1200], sealedPacket);
            if (!serverReceive.Decrypt([7], sealedPacket, opened))
            {
                return "server could not open a client packet";
            }

            serverSend.Encrypt([8], opened.AsSpan(0, 100), sealedPacket.AsSpan(0, 116));
            if (!clientReceive.Decrypt([8], sealedPacket.AsSpan(0, 116), opened.AsSpan(0, 100)))
            {
                return "client could not open a server packet";
            }
        }

        var key = new byte[32];
        Hkdf.Noise(key, [1], plain.AsSpan(0, 32), buffer.AsSpan(0, 32));
        return null;
    }
}
