using System.Text.Json;
using OneGame.Audio;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Control.Tests;

public class AudioOpTests
{
    private sealed class QuietGame : GameDefinition
    {
        public override string Name => "QuietGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }

    // One second of a 1 kHz mono sine at -20 dBFS peak, as 16-bit PCM WAV.
    private static byte[] Tone()
    {
        const int Rate = 48_000;
        var b = new byte[44 + (Rate * 2)];
        "RIFF"u8.CopyTo(b);
        BitConverter.TryWriteBytes(b.AsSpan(4), b.Length - 8);
        "WAVEfmt "u8.CopyTo(b.AsSpan(8));
        BitConverter.TryWriteBytes(b.AsSpan(16), 16);
        BitConverter.TryWriteBytes(b.AsSpan(20), (short)1);
        BitConverter.TryWriteBytes(b.AsSpan(22), (short)1);
        BitConverter.TryWriteBytes(b.AsSpan(24), Rate);
        BitConverter.TryWriteBytes(b.AsSpan(28), Rate * 2);
        BitConverter.TryWriteBytes(b.AsSpan(32), (short)2);
        BitConverter.TryWriteBytes(b.AsSpan(34), (short)16);
        "data"u8.CopyTo(b.AsSpan(36));
        BitConverter.TryWriteBytes(b.AsSpan(40), Rate * 2);
        for (var i = 0; i < Rate; i++)
        {
            BitConverter.TryWriteBytes(b.AsSpan(44 + (i * 2)), (short)Math.Round(Math.Sin(2 * Math.PI * 1000 * i / Rate) * 0.1 * 32767));
        }

        return b;
    }

    [Fact]
    public void TheAudioOpsReadTheMixAndRecordIt()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-audio-op-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var game = GameInstance.Create(new QuietGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
            var surface = new ControlSurface(game, dir);
            Assert.Equal("no_audio", Assert.Throws<ControlException>(() => surface.Call("audio")).Code);

            using var audio = AudioService.Create(new AudioServiceOptions { Capture = true, Device = new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 } });
            audio.SetBusFilter(AudioBus.Music, AudioFilterKind.LowPass, 5000f);
            audio.Play(audio.LoadSound(new MemoryStream(Tone())), new PlayOptions { Loop = true, Bus = AudioBus.Music });

            // The newest service is found without wiring anything.
            using (var doc = JsonDocument.Parse(surface.Call("audio_capture", """{"seconds":5,"path":"mix/out.wav","reset":true}""")))
            {
                var r = doc.RootElement;
                Assert.Equal(240_000, r.GetProperty("frames").GetInt32());
                Assert.Equal(64, r.GetProperty("sha256").GetString()!.Length);
                Assert.Equal(-23.0, r.GetProperty("loudness").GetProperty("integratedLufs").GetDouble(), 0.2);
                var music = r.GetProperty("busLoudness")[0];
                Assert.Equal("music", music.GetProperty("name").GetString());
                Assert.InRange(music.GetProperty("loudness").GetProperty("integratedLufs").GetDouble(), -25.5, -22.0);
                Assert.True(File.Exists(Path.Combine(dir, "mix", "out.wav")));
            }

            using (var doc = JsonDocument.Parse(surface.Call("audio")))
            {
                var r = doc.RootElement;
                Assert.Equal("music", r.GetProperty("buses")[0].GetProperty("name").GetString());
                Assert.Equal(5000, r.GetProperty("buses")[0].GetProperty("lowPass").GetProperty("cutoffHz").GetDouble());
                Assert.Equal(-23.0, r.GetProperty("loudness").GetProperty("integratedLufs").GetDouble(), 0.2);
                Assert.Equal(1, r.GetProperty("activeVoices").GetInt32());
            }

            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"reset":true}""")))
            {
                // Silence is left out of the JSON rather than written as a number.
                Assert.False(doc.RootElement.GetProperty("loudness").TryGetProperty("integratedLufs", out _));
            }

            Assert.Equal("path_outside", Assert.Throws<ControlException>(() => surface.Call("audio_capture", """{"path":"../x.wav"}""")).Code);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
