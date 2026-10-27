using OneGame.Storage;

namespace OneGame.Sync.Tests.Storage;

/// <summary>The graphics quality choices of the sample settings.</summary>
public enum SampleQuality
{
    /// <summary>Fast.</summary>
    Low = 0,

    /// <summary>Balanced.</summary>
    Medium = 1,

    /// <summary>Pretty.</summary>
    High = 2,
}

/// <summary>A settings class like a game's: every kind of field the schema offers.</summary>
public sealed class SampleSettings
{
    /// <summary>Volume, 0 to 10.</summary>
    public int Volume { get; set; } = 7;

    /// <summary>Whether the window is fullscreen.</summary>
    public bool Fullscreen { get; set; }

    /// <summary>UI scale.</summary>
    public float Scale { get; set; } = 1f;

    /// <summary>The name last typed.</summary>
    public string Name { get; set; } = "player";

    /// <summary>Quality.</summary>
    public SampleQuality Quality { get; set; } = SampleQuality.Medium;

    /// <summary>The schema: file <c>sample-settings.bin</c>, version 2.</summary>
    public static SettingsSchema<SampleSettings> Schema { get; } = new SettingsSchema<SampleSettings>("sample-settings.bin", version: 2)
        .Whole("volume", s => s.Volume, (s, v) => s.Volume = v, 0, 10)
        .Flag("fullscreen", s => s.Fullscreen, (s, v) => s.Fullscreen = v)
        .Number("scale", s => s.Scale, (s, v) => s.Scale = v, 0.5f, 4f)
        .Text("name", s => s.Name, (s, v) => s.Name = v, 16)
        .Choice("quality", s => s.Quality, (s, v) => s.Quality = v);
}
