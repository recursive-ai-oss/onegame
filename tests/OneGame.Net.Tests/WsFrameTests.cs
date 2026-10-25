namespace OneGame.Net.Tests;

/// <summary>RFC 6455 section 5 rules, checked on the frame parser and writer with exact bytes.</summary>
public class WsFrameTests
{
    private static WsEvent Feed(WsFrameReader r, byte[] bytes, out int used) => r.Feed(bytes, out used);

    [Fact]
    public void MaskedFrameMatchesTheRfcExample()
    {
        // RFC 6455 section 5.7: a single-frame masked text message "Hello".
        var buf = new byte[32];
        var n = WsFrames.Write(buf, WsFrames.OpText, true, "Hello"u8, [0x37, 0xfa, 0x21, 0x3d]);
        Assert.Equal([0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58], buf[..n]);
        n = WsFrames.Write(buf, WsFrames.OpText, true, "Hello"u8, default);
        Assert.Equal([0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f], buf[..n]);
    }

    [Fact]
    public void LengthEncodingsMatchTheRfcExamples()
    {
        // Section 5.7: 256 bytes use the 16-bit form, 65536 bytes the 64-bit form.
        var buf = new byte[70000];
        var n = WsFrames.Write(buf, WsFrames.OpBinary, true, new byte[256], default);
        Assert.Equal([0x82, 0x7E, 0x01, 0x00], buf[..4]);
        Assert.Equal(260, n);
        n = WsFrames.Write(buf, WsFrames.OpBinary, true, new byte[65536], default);
        Assert.Equal([0x82, 0x7F, 0, 0, 0, 0, 0, 1, 0, 0], buf[..10]);
        Assert.Equal(65546, n);
    }

    [Fact]
    public void FragmentedMessageWithAPingBetweenFragmentsIsReassembled()
    {
        // Section 5.7: "Hel" + "lo" as a fragmented unmasked message (binary here), a ping may sit between.
        var r = new WsFrameReader(requireMask: false, 1000);
        Assert.Equal(WsEvent.NeedMore, Feed(r, [0x02, 0x03, 0x48, 0x65, 0x6c], out _));
        var ping = new byte[] { 0x89, 0x02, 0xAA, 0xBB };
        Assert.Equal(WsEvent.Ping, Feed(r, ping, out _));
        Assert.Equal([0xAA, 0xBB], r.ControlPayload.ToArray());
        Assert.Equal(WsEvent.Message, Feed(r, [0x80, 0x02, 0x6c, 0x6f], out _));
        var buf = r.TakeMessage(out var len);
        Assert.Equal("Hello", System.Text.Encoding.ASCII.GetString(buf, 0, len));
        NetBuffers.Return(buf);
    }

    [Fact]
    public void BytesMayArriveOneAtATime()
    {
        var frame = new byte[300];
        var n = WsFrames.Write(frame, WsFrames.OpBinary, true, Rig.Pattern(200, 1), [9, 8, 7, 6]);
        var r = new WsFrameReader(requireMask: true, 1000);
        WsEvent e = WsEvent.NeedMore;
        for (var i = 0; i < n; i++)
        {
            e = r.Feed(frame.AsSpan(i, 1), out var used);
            Assert.Equal(1, used);
        }

        Assert.Equal(WsEvent.Message, e);
        var buf = r.TakeMessage(out var len);
        Assert.True(buf.AsSpan(0, len).SequenceEqual(Rig.Pattern(200, 1)));
        NetBuffers.Return(buf);
    }

    [Theory]
    [InlineData("rsv1", new byte[] { 0xC2, 0x00 }, true, 1002)]
    [InlineData("rsv2", new byte[] { 0xA2, 0x00 }, true, 1002)]
    [InlineData("rsv3", new byte[] { 0x92, 0x00 }, true, 1002)]
    [InlineData("reserved data opcode", new byte[] { 0x83, 0x80, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("reserved control opcode", new byte[] { 0x8B, 0x80, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("client frame unmasked", new byte[] { 0x82, 0x01, 0x00 }, true, 1002)]
    [InlineData("server frame masked", new byte[] { 0x82, 0x81, 0, 0, 0, 0, 0 }, false, 1002)]
    [InlineData("fragmented ping", new byte[] { 0x09, 0x80, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("ping over 125 bytes", new byte[] { 0x89, 0xFE, 0x00, 0x7E, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("length 125 sent in 16 bits", new byte[] { 0x82, 0xFE, 0x00, 0x7D, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("length 65535 sent in 64 bits", new byte[] { 0x82, 0xFF, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("64-bit length with top bit", new byte[] { 0x82, 0xFF, 0x80, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("continuation with nothing to continue", new byte[] { 0x80, 0x80, 0, 0, 0, 0 }, true, 1002)]
    [InlineData("text frame", new byte[] { 0x81, 0x80, 0, 0, 0, 0 }, true, 1003)]
    [InlineData("close with one byte", new byte[] { 0x88, 0x81, 0, 0, 0, 0, 0x03 }, true, 1002)]
    [InlineData("close code 1005 on the wire", new byte[] { 0x88, 0x82, 0, 0, 0, 0, 0x03, 0xED }, true, 1002)]
    [InlineData("close code 999", new byte[] { 0x88, 0x82, 0, 0, 0, 0, 0x03, 0xE7 }, true, 1002)]
    [InlineData("close reason not UTF-8", new byte[] { 0x88, 0x84, 0, 0, 0, 0, 0x03, 0xE8, 0xFF, 0xFE }, true, 1007)]
    [InlineData("oversize message", new byte[] { 0x82, 0xFE, 0xFF, 0xFF, 0, 0, 0, 0 }, true, 1009)]
    public void BrokenStreamsAreRejectedWithTheRightCode(string why, byte[] bytes, bool requireMask, int code)
    {
        var r = new WsFrameReader(requireMask, 1000);
        Assert.Equal(WsEvent.Error, r.Feed(bytes, out _));
        Assert.Equal(code, r.ErrorCode);
        Assert.False(string.IsNullOrEmpty(r.ErrorDetail), why);
        Assert.Equal(WsEvent.Error, r.Feed([0x82, 0x00], out var used)); // sticky: nothing more is read
        Assert.Equal(0, used);
    }

    [Fact]
    public void ANewMessageInsideAFragmentedOneIsRejected()
    {
        var r = new WsFrameReader(requireMask: false, 1000);
        Assert.Equal(WsEvent.NeedMore, r.Feed([0x02, 0x01, 0x00], out _));
        Assert.Equal(WsEvent.Error, r.Feed([0x02, 0x01, 0x00], out _));
        Assert.Equal(1002, r.ErrorCode);
    }

    [Fact]
    public void FragmentsTogetherMayNotExceedTheLimit()
    {
        var r = new WsFrameReader(requireMask: false, 10);
        Assert.Equal(WsEvent.NeedMore, r.Feed([0x02, 0x06, 1, 2, 3, 4, 5, 6], out _));
        Assert.Equal(WsEvent.Error, r.Feed([0x80, 0x05], out _));
        Assert.Equal(1009, r.ErrorCode);
    }

    [Fact]
    public void ValidCloseFramesAreReported()
    {
        var r = new WsFrameReader(requireMask: false, 1000);
        Assert.Equal(WsEvent.Close, r.Feed([0x88, 0x05, 0x03, 0xE8, (byte)'b', (byte)'y', (byte)'e'], out _));
        Assert.Equal(1000, r.CloseCode);
        Assert.Equal(WsEvent.Close, r.Feed([0x88, 0x00], out _));
        Assert.Equal(1005, r.CloseCode);
        Assert.Equal(WsEvent.Close, r.Feed([0x88, 0x02, 0x0F, 0xA0], out _));
        Assert.Equal(4000, r.CloseCode);
    }

    [Fact]
    public void WrittenFramesParseBack()
    {
        foreach (var size in new[] { 0, 1, 125, 126, 127, 65535, 65536, 100_000 })
        {
            foreach (var masked in new[] { false, true })
            {
                var payload = Rig.Pattern(size, size);
                var frame = new byte[size + WsFrames.MaxHeaderBytes];
                var n = WsFrames.Write(frame, WsFrames.OpBinary, true, payload, masked ? [0xDE, 0xAD, 0xBE, 0xEF] : default);
                var r = new WsFrameReader(masked, 1 << 20);
                Assert.Equal(WsEvent.Message, r.Feed(frame.AsSpan(0, n), out var used));
                Assert.Equal(n, used);
                var buf = r.TakeMessage(out var len);
                Assert.True(buf.AsSpan(0, len).SequenceEqual(payload), $"size {size} masked {masked}");
                NetBuffers.Return(buf);
            }
        }
    }
}
