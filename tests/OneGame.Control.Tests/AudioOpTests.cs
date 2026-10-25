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

            // The device section is there for every output; a capture sink reports itself and nothing the device never did.
            using (var doc = JsonDocument.Parse(surface.Call("audio")))
            {
                var device = doc.RootElement.GetProperty("device");
                Assert.Equal("capture", device.GetProperty("backend").GetString());
                Assert.Equal(0, device.GetProperty("underruns").GetInt64());
                Assert.False(device.GetProperty("suspended").GetBoolean());
            }

            // suspend=true stops the device as the app lifecycle does, and the answer says so; this is not a web page or an iPhone, so unlock and interrupt say that.
            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"suspend":true}""")))
            {
                var device = doc.RootElement.GetProperty("device");
                Assert.True(device.GetProperty("suspended").GetBoolean());
                Assert.Equal("suspend:true", device.GetProperty("request").GetString());
            }

            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"unlock":true}""")))
            {
                Assert.Equal("unlock:not_a_web_page", doc.RootElement.GetProperty("device").GetProperty("request").GetString());
            }

            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"interrupt":"begin"}""")))
            {
                Assert.Equal("interrupt:not_ios", doc.RootElement.GetProperty("device").GetProperty("request").GetString());
            }

            Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("audio", """{"interrupt":"sideways"}""")).Code);
            surface.Call("audio", """{"suspend":false}""");

            // The service has the default three meters, all taken: asking for a fourth is "no slot", never an error.
            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"enableMeter":3}""")))
            {
                Assert.Equal("no_free_slot", doc.RootElement.GetProperty("meterRequest").GetString());
                Assert.Equal(0, doc.RootElement.GetProperty("freeBusMeters").GetInt32());
                Assert.True(doc.RootElement.GetProperty("buses")[0].GetProperty("metered").GetBoolean());
                Assert.False(doc.RootElement.GetProperty("buses")[3].GetProperty("metered").GetBoolean());
            }

            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"disableMeter":2}""")))
            {
                Assert.Equal("disabled", doc.RootElement.GetProperty("meterRequest").GetString());
                Assert.Equal(1, doc.RootElement.GetProperty("freeBusMeters").GetInt32());
            }

            using (var doc = JsonDocument.Parse(surface.Call("audio", """{"enableMeter":3}""")))
            {
                Assert.Equal("metered", doc.RootElement.GetProperty("meterRequest").GetString());
                Assert.True(doc.RootElement.GetProperty("buses")[3].GetProperty("metered").GetBoolean());
            }

            Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("audio", """{"enableMeter":99}""")).Code);

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
