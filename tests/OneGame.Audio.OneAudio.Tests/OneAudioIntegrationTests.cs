using System.Buffers.Binary;
using OneAudio;

namespace OneGame.Audio.OneAudio.Tests;

public class OneAudioIntegrationTests
{
    private const int Rate = 48_000;

    // A mono 16-bit PCM WAV with a constant sample value.
    private static byte[] Wav(short value, int frames, int rate = Rate)
    {
        var b = new byte[44 + (frames * 2)];
        "RIFF"u8.CopyTo(b);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4), 36 + (frames * 2));
        "WAVEfmt "u8.CopyTo(b.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(24), rate);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(28), rate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(34), 16);
        "data"u8.CopyTo(b.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(40), frames * 2);
        for (var i = 0; i < frames; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(44 + (i * 2)), value);
        }

        return b;
    }

    private static AudioService Service(OutputLimiterMode limiter = OutputLimiterMode.Clamp) => AudioService.Create(new AudioServiceOptions
    {
        Capture = true,
        Limiter = limiter,
        CoreFactory = new OneAudioCore.Factory(new AudioCodecRegistry().AddWav()),
        Device = new AudioDeviceOptions { SampleRate = Rate, Channels = 2 },
    });

    [Fact]
    public void WavPlaysThroughTheRealMixerIntoTheCapture()
    {
        using var s = Service();
        var clip = s.LoadSound(new MemoryStream(Wav(16384, 480)), "half"); // 0.5 full scale
        Assert.Equal(0.01, clip.DurationSeconds, 6);
        s.Play(clip, new PlayOptions { Volume = 0.5f, StartAtFrame = 100 });
        s.Advance(1000);

        var x = s.Capture!.Samples;
        Assert.Equal(0f, x[99 * 2]);
        Assert.True(x[100 * 2] > 0.1f, $"first audible frame must be exactly 100, got {x[100 * 2]}");
        Assert.Equal(x[100 * 2], x[(100 * 2) + 1], 6); // centred: same level both sides
        Assert.Equal(0f, x[580 * 2]);
        s.Update();
        Assert.True(s.Capture.Peak is > 0.1f and <= 1f);
    }

    [Fact]
    public void BusVolumeAndMasterReachTheRealMixer()
    {
        using var s = Service();
        var clip = s.LoadSound(new MemoryStream(Wav(16384, 4800)));
        s.Play(clip);
        s.Advance(100);
        var loud = s.Capture!.Samples[100];
        s.SetBusVolume(AudioBus.Sfx, 0.5f);
        s.Advance(100);
        // Bus gain applies at the next block; compare after it has taken effect.
        Assert.Equal(loud * 0.5f, s.Capture.Samples[(199 * 2)], 4);
    }

    [Fact]
    public void StreamedMusicLoopsAndCrossfades()
    {
        using var s = Service();
        s.PlayMusic(new MemoryStream(Wav(8192, 4800)));
        s.Advance(9600);
        Assert.True(s.Capture!.Samples[9000 * 2] > 0.1f, "looping music is still audible after two lengths");
        s.PlayMusic(new MemoryStream(Wav(-8192, 48_000)), new MusicOptions { CrossfadeSeconds = 0.05, Loop = false });
        s.Advance(4800);
        Assert.True(s.Capture.Samples[(9600 + 4700) * 2] < -0.1f, "after the fade only the second track is left");
        Assert.Equal(0, ((OneAudioCore)s.Core).Mixer.UnderrunEvents);
    }

    [Fact]
    public void EndedVoiceRaisesAnEvent()
    {
        using var s = Service();
        var seen = new List<AudioEvent>();
        s.EventRaised += seen.Add;
        var v = s.Play(s.LoadSound(new MemoryStream(Wav(1000, 100))));
        s.Advance(2000);
        s.Update();
        Assert.Contains(seen, e => e.Kind == AudioEventKind.VoiceEnded && e.Voice == v);
    }
}
