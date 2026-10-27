namespace Bricks;

/// <summary>The things the game makes a sound about.</summary>
public enum SoundEvent
{
    /// <summary>A brick broke.</summary>
    Brick,

    /// <summary>A brick was hit and is still standing.</summary>
    Tough,

    /// <summary>A ball bounced off a brick nothing breaks.</summary>
    Clink,

    /// <summary>A ball bounced off a wall.</summary>
    Wall,

    /// <summary>A ball bounced off the paddle.</summary>
    Paddle,

    /// <summary>A power-up was caught.</summary>
    Catch,

    /// <summary>A ball fell out of the court.</summary>
    Lost,

    /// <summary>A ball was launched.</summary>
    Launch,

    /// <summary>A level was cleared.</summary>
    Clear,

    /// <summary>The game ended.</summary>
    Over,

    /// <summary>A menu button was used.</summary>
    Click,
}

/// <summary>Where the game sends its sounds. An audio-backed implementation plays them; the default is silent.</summary>
public interface ISoundSink
{
    /// <summary>Plays a sound at a volume from 0 to 1.</summary>
    void Play(SoundEvent sound, float volume);
}

/// <summary>Makes no sound.</summary>
public sealed class SilentSounds : ISoundSink
{
    /// <inheritdoc />
    public void Play(SoundEvent sound, float volume)
    {
    }
}
