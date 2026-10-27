using OneGame.Storage;

namespace OneGame.Sync.Tests.Storage;

public class SettingsTests
{
    private static SettingsFile<SampleSettings> Open(IByteStore store) => new(store, SampleSettings.Schema);

    [Fact]
    public void SettingsRoundTripAcrossARestart()
    {
        var store = new MemoryByteStore();
        var first = Open(store);
        Assert.Equal(SettingsStatus.Missing, first.Load().Status);
        Assert.Equal(7, first.Value.Volume);

        first.Value.Volume = 3;
        first.Value.Fullscreen = true;
        first.Value.Scale = 1.25f;
        first.Value.Name = "Ada ☕";
        first.Value.Quality = SampleQuality.High;
        Assert.True(first.Save());

        var second = Open(store);
        var load = second.Load();
        Assert.True(load.Clean);
        Assert.Equal(2, load.FileVersion);
        Assert.Equal((3, true, 1.25f, "Ada ☕", SampleQuality.High), (second.Value.Volume, second.Value.Fullscreen, second.Value.Scale, second.Value.Name, second.Value.Quality));
        Assert.Equal(first.ToText(), second.ToText());
        Assert.Contains("quality = 2", second.ToText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ADamagedFileFallsBackToDefaultsAndSaysWhy()
    {
        var store = new MemoryByteStore();
        var good = Open(store);
        good.Value.Volume = 2;
        good.Save();
        store.TryRead("sample-settings.bin", int.MaxValue, out var bytes);

        foreach (var (name, damaged, why) in new (string, byte[], string)[]
        {
            ("garbage", [1, 2, 3, 4, 5], "not a settings file"),
            ("empty", [], "empty"),
            ("cut", bytes[..(bytes.Length - 3)], "damaged"),
            ("extra", [.. bytes, 0], "damaged"),
            ("huge", new byte[SettingsFile<SampleSettings>.MaxBytes + 1], "larger"),
        })
        {
            store.Write("sample-settings.bin", damaged);
            var file = Open(store);
            file.Value.Volume = 9; // must be overwritten by the default, not kept
            var load = file.Load();
            Assert.Equal(SettingsStatus.Defaulted, load.Status);
            Assert.Contains(why, load.Reason, StringComparison.Ordinal);
            Assert.False(load.Clean, name);
            Assert.Equal(7, file.Value.Volume);
        }
    }

    [Fact]
    public void AnOutOfRangeOrWrongTypeFieldTakesItsDefaultAndTheRestStay()
    {
        var store = new MemoryByteStore();
        var w = new SyncWriter();
        w.WriteFixed32(0x5453474F);
        w.WriteVarUInt(1);
        w.WriteVarInt(2);
        w.WriteVarUInt(6);
        w.WriteString("volume"); w.WriteByte(0); w.WriteVarInt(99);              // out of range
        w.WriteString("fullscreen"); w.WriteByte(1); w.WriteBool(true);         // fine
        w.WriteString("scale"); w.WriteByte(2); w.WriteFixed32(BitConverter.SingleToUInt32Bits(float.NaN));
        w.WriteString("name"); w.WriteByte(3); w.WriteString("a name that is much too long");
        w.WriteString("quality"); w.WriteByte(0); w.WriteVarInt(77);            // not a member
        w.WriteString("retired"); w.WriteByte(0); w.WriteVarInt(1);             // not in this version
        store.Write("sample-settings.bin", w.Written);

        var file = Open(store);
        var load = file.Load();
        Assert.Equal(SettingsStatus.Loaded, load.Status);
        Assert.False(load.Clean);
        Assert.Equal(5, load.Notes.Count);
        Assert.Contains(load.Notes, n => n.StartsWith("'volume'", StringComparison.Ordinal));
        Assert.Contains(load.Notes, n => n.StartsWith("'retired'", StringComparison.Ordinal) && n.Contains("ignored", StringComparison.Ordinal));
        Assert.Equal((7, true, 1f, "player", SampleQuality.Medium), (file.Value.Volume, file.Value.Fullscreen, file.Value.Scale, file.Value.Name, file.Value.Quality));
    }

    [Fact]
    public void ATypeMismatchOnAKnownKeyIsNotedNotFatal()
    {
        var store = new MemoryByteStore();
        var w = new SyncWriter();
        w.WriteFixed32(0x5453474F);
        w.WriteVarUInt(1);
        w.WriteVarInt(1);
        w.WriteVarUInt(2);
        w.WriteString("volume"); w.WriteByte(3); w.WriteString("loud");
        w.WriteString("fullscreen"); w.WriteByte(1); w.WriteBool(true);
        store.Write("sample-settings.bin", w.Written);
        var file = Open(store);
        var load = file.Load();
        Assert.Single(load.Notes);
        Assert.True(file.Value.Fullscreen);
        Assert.Equal(7, file.Value.Volume);
    }

    [Fact]
    public void ASaveThatCannotBeWrittenReportsFalseAndKeepsTheOldFile()
    {
        var inner = new MemoryByteStore();
        var flaky = new FlakyStore(inner);
        var file = Open(flaky);
        file.Value.Volume = 1;
        Assert.True(file.Save());
        flaky.WritesLeft = 0;
        file.Value.Volume = 5;
        Assert.False(file.Save());
        var again = Open(inner);
        again.Load();
        Assert.Equal(1, again.Value.Volume);
    }

    [Fact]
    public void ResetGoesBackToDefaultsAndTheSchemaRefusesBadDeclarations()
    {
        var file = Open(new MemoryByteStore());
        file.Value.Quality = SampleQuality.Low;
        file.Reset();
        Assert.Equal(SampleQuality.Medium, file.Value.Quality);
        Assert.Throws<ArgumentException>(() => new SettingsSchema<SampleSettings>().Whole("a", s => s.Volume, (s, v) => s.Volume = v).Whole("a", s => s.Volume, (s, v) => s.Volume = v));
        Assert.Throws<ArgumentException>(() => new SettingsSchema<SampleSettings>("bad/name"));
        Assert.Throws<ArgumentException>(() => new SettingsSchema<SampleSettings>().Whole("", s => s.Volume, (s, v) => s.Volume = v));
    }

    [Fact]
    public void TheSameValuesAlwaysWriteTheSameBytes()
    {
        var a = Open(new MemoryByteStore());
        var b = Open(new MemoryByteStore());
        Assert.Equal(a.ToBytes(), b.ToBytes());
    }
}
