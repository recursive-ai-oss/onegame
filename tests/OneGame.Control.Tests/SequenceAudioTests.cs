using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using OneGame.Audio;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Control.Tests;

[Collection("AudioService.Active")]
public class SequenceAudioTests
{
    private sealed class QuietGame : GameDefinition
    {
        public override string Name => "SequenceAudio";
        public override void RegisterEntities(EntityRegistry registry) { }
        public override void Setup(World world) { }
        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("clip")));
    }

    [Fact]
    public void SequenceMixIsAlignedRepeatableAndMuxedWhenAvailable()
    {
        var root = Path.Combine(Path.GetTempPath(), "sequence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var first = Capture(root, "first", withAudio: true);
            var second = Capture(root, "second", withAudio: true);
            Assert.Equal(first, second);
            Assert.Equal(44 + (3200 * 2 * 4), first.Length);
            Assert.Equal(48_000, BinaryPrimitives.ReadInt32LittleEndian(first.AsSpan(24)));
            Assert.Equal(3200 * 2 * 4, BinaryPrimitives.ReadInt32LittleEndian(first.AsSpan(40)));
            Assert.All(Samples(first, 0, 1600), sample => Assert.Equal(0f, sample));
            Assert.Contains(Samples(first, 1600, 800), sample => Math.Abs(sample) > 0.01f);
            Assert.Contains(Samples(first, 2400, 800), sample => Math.Abs(sample) > 0.01f);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SilentSequenceStillWritesExactWavAndFractionalFrameBoundariesDoNotDrift()
    {
        var root = Path.Combine(Path.GetTempPath(), "sequence-silent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var game = GameInstance.Create(new QuietGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
            var surface = new ControlSurface(game, root);
            using var reply = JsonDocument.Parse(surface.Call("capture_sequence", """{"dir":"silent","count":7,"fps":29,"size":"64x48"}"""));
            var wav = File.ReadAllBytes(reply.RootElement.GetProperty("wav").GetString()!);
            var frames = 7 * 48_000 / 29;
            Assert.Equal(44 + (frames * 2 * 4), wav.Length);
            Assert.All(Samples(wav, 0, frames), sample => Assert.Equal(0f, sample));
            Assert.Equal(6, game.World.Tick);
            Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("capture_sequence", """{"dir":"bad","count":1,"fps":0}""")).Code);
            Assert.Equal("too_large", Assert.Throws<ControlException>(() => surface.Call("capture_sequence", """{"dir":"bad","count":3600,"fps":1}""")).Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] Capture(string root, string dir, bool withAudio)
    {
        using var game = GameInstance.Create(new QuietGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        using var audio = AudioService.Create(new AudioServiceOptions { Capture = true });
        if (withAudio)
        {
            audio.Play(audio.LoadSound(new MemoryStream(Tone())), new PlayOptions { StartAtFrame = 1600 });
        }

        var surface = new ControlSurface(game, root) { Audio = audio };
        using var reply = JsonDocument.Parse(surface.Call("capture_sequence", $$"""{"dir":"{{dir}}","count":4,"fps":60,"size":"64x48"}"""));
        var result = reply.RootElement;
        Assert.Equal(4, Directory.GetFiles(Path.Combine(root, dir), "frame-*.png").Length);
        Assert.Equal(3, game.World.Tick);
        Assert.Equal(0, audio.Capture!.FramesCaptured);
        if (result.TryGetProperty("mp4", out var video))
        {
            var mp4 = video.GetString()!;
            Assert.True(File.Exists(mp4));
            Assert.False(result.TryGetProperty("mp4SkipReason", out _));
            AssertAudioStreamIfFfprobeExists(mp4);
        }
        else
        {
            Assert.Contains("ffmpeg", result.GetProperty("mp4SkipReason").GetString()!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("not found on PATH", result.GetProperty("mp4SkipReason").GetString()!);
        }

        return File.ReadAllBytes(result.GetProperty("wav").GetString()!);
    }

    private static void AssertAudioStreamIfFfprobeExists(string path)
    {
        if (!(Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).Any(p => File.Exists(Path.Combine(p, OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe"))))
        {
            return;
        }

        var start = new ProcessStartInfo("ffprobe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-v", "error", "-select_streams", "a", "-show_entries", "stream=codec_type", "-of", "csv=p=0", path })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        Assert.True(process.WaitForExit(10_000));
        Assert.Equal(0, process.ExitCode);
        Assert.Equal("audio", output.Trim());
    }

    private static float[] Samples(byte[] wav, int startFrame, int count)
    {
        var result = new float[count * 2];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = BinaryPrimitives.ReadSingleLittleEndian(wav.AsSpan(44 + ((startFrame * 2 + i) * 4)));
        }

        return result;
    }

    private static byte[] Tone()
    {
        const int Rate = 48_000;
        var bytes = new byte[44 + (Rate * 2)];
        "RIFF"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), bytes.Length - 8);
        "WAVEfmt "u8.CopyTo(bytes.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), Rate);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), Rate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(34), 16);
        "data"u8.CopyTo(bytes.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(40), Rate * 2);
        for (var i = 0; i < Rate; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(44 + (i * 2)), (short)(Math.Sin(i * Math.PI / 24) * 3000));
        }

        return bytes;
    }
}
