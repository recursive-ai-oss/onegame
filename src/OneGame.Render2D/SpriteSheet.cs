namespace OneGame.Render2D;

/// <summary>A clip name that a <see cref="SpriteSheet"/> does not have.</summary>
public sealed class UnknownClipException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="name">The name that was asked for.</param>
    /// <param name="known">The clips the sheet has.</param>
    public UnknownClipException(string name, IEnumerable<string> known)
        : base($"The sprite sheet has no clip named '{name}'. Add it with sheet.AddClip(\"{name}\", firstFrame, frameCount, fps). Known clips: {string.Join(", ", known)}.")
    {
        Name = name;
    }

    /// <summary>The name that was asked for.</summary>
    public string Name { get; }
}

/// <summary>A named run of frames of a <see cref="SpriteSheet"/> played at a rate. Pure data: it picks a frame for a time and never holds state.</summary>
public sealed class SpriteClip
{
    private readonly int[] _frames;

    internal SpriteClip(string name, int[] frames, float fps, bool loop)
    {
        Name = name;
        _frames = frames;
        Fps = fps;
        Loop = loop;
    }

    /// <summary>The clip's name.</summary>
    public string Name { get; }

    /// <summary>Frames per second.</summary>
    public float Fps { get; }

    /// <summary>True to start over after the last frame; false to hold it.</summary>
    public bool Loop { get; }

    /// <summary>How many frames the clip shows before it repeats or ends.</summary>
    public int Length => _frames.Length;

    /// <summary>How long one pass takes, in seconds.</summary>
    public float Duration => _frames.Length / Fps;

    /// <summary>True when a non-looping clip has played through at <paramref name="seconds"/>.</summary>
    /// <param name="seconds">Time since the clip started.</param>
    /// <returns>Whether it is finished.</returns>
    public bool IsDone(float seconds) => !Loop && seconds >= Duration;

    /// <summary>Which sheet frame the clip shows <paramref name="seconds"/> after it started. Negative times show the first frame.</summary>
    /// <param name="seconds">Time since the clip started, from the view's clock.</param>
    /// <returns>An index into the sheet's frames.</returns>
    public int FrameAt(float seconds)
    {
        if (!(seconds > 0) || !float.IsFinite(seconds))
        {
            return _frames[0];
        }

        var step = (long)(seconds * Fps);
        var i = Loop ? (int)(step % _frames.Length) : (int)Math.Min(step, _frames.Length - 1);
        return _frames[i];
    }
}

/// <summary>
/// The frames of one texture: a grid of equal cells or explicit rectangles, plus named <see cref="SpriteClip"/>s. A sheet is view data (it
/// holds a GPU texture and no game state); build it once and draw from it every frame.
/// </summary>
public sealed class SpriteSheet
{
    private readonly PixelRect[] _frames;
    private readonly Dictionary<string, SpriteClip> _clips = new(StringComparer.Ordinal);

    private SpriteSheet(Texture2D texture, PixelRect[] frames)
    {
        Texture = texture;
        _frames = frames;
    }

    /// <summary>The image all the frames are cut from.</summary>
    public Texture2D Texture { get; }

    /// <summary>How many frames the sheet has.</summary>
    public int FrameCount => _frames.Length;

    /// <summary>The names of the clips added so far.</summary>
    public IEnumerable<string> ClipNames => _clips.Keys;

    /// <summary>Cuts the texture into a grid of equal cells, numbered left to right then top to bottom.</summary>
    /// <param name="texture">The image.</param>
    /// <param name="frameWidth">Cell width in pixels.</param>
    /// <param name="frameHeight">Cell height in pixels.</param>
    /// <param name="margin">Pixels around the whole grid.</param>
    /// <param name="spacing">Pixels between cells.</param>
    /// <returns>The sheet.</returns>
    /// <exception cref="ArgumentException">Not even one cell fits in the texture.</exception>
    public static SpriteSheet Grid(Texture2D texture, int frameWidth, int frameHeight, int margin = 0, int spacing = 0)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameHeight, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(margin);
        ArgumentOutOfRangeException.ThrowIfNegative(spacing);
        var cols = (texture.Width - (2 * margin) + spacing) / (frameWidth + spacing);
        var rows = (texture.Height - (2 * margin) + spacing) / (frameHeight + spacing);
        if (cols < 1 || rows < 1)
        {
            throw new ArgumentException($"A {frameWidth}x{frameHeight} cell does not fit in the {texture.Width}x{texture.Height} texture.", nameof(frameWidth));
        }

        var frames = new PixelRect[cols * rows];
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                frames[(r * cols) + c] = new PixelRect(margin + (c * (frameWidth + spacing)), margin + (r * (frameHeight + spacing)), frameWidth, frameHeight);
            }
        }

        return new SpriteSheet(texture, frames);
    }

    /// <summary>Uses explicit rectangles as the frames, numbered in the order given.</summary>
    /// <param name="texture">The image.</param>
    /// <param name="frames">Pixel rectangles inside the texture.</param>
    /// <returns>The sheet.</returns>
    /// <exception cref="ArgumentException">There are no frames, or one lies outside the texture.</exception>
    public static SpriteSheet Frames(Texture2D texture, ReadOnlySpan<PixelRect> frames)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (frames.IsEmpty)
        {
            throw new ArgumentException("A sprite sheet needs at least one frame.", nameof(frames));
        }

        foreach (var f in frames)
        {
            if (f.Width < 1 || f.Height < 1 || f.X < 0 || f.Y < 0 || f.X + f.Width > texture.Width || f.Y + f.Height > texture.Height)
            {
                throw new ArgumentException($"Frame {f} lies outside the {texture.Width}x{texture.Height} texture.", nameof(frames));
            }
        }

        return new SpriteSheet(texture, frames.ToArray());
    }

    /// <summary>The pixel rectangle of a frame.</summary>
    /// <param name="index">The frame number.</param>
    /// <returns>The rectangle.</returns>
    public PixelRect Frame(int index) => (uint)index < (uint)_frames.Length
        ? _frames[index]
        : throw new ArgumentOutOfRangeException(nameof(index), index, $"The sheet has {_frames.Length} frames.");

    /// <summary>Adds a clip of consecutive frames.</summary>
    /// <param name="name">The clip's name.</param>
    /// <param name="firstFrame">The first frame.</param>
    /// <param name="frameCount">How many frames.</param>
    /// <param name="fps">Frames per second; above zero.</param>
    /// <param name="loop">Start over after the last frame, or hold it.</param>
    /// <returns>The clip, which is also kept under its name.</returns>
    public SpriteClip AddClip(string name, int firstFrame, int frameCount, float fps, bool loop = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(frameCount, 1);
        var frames = new int[frameCount];
        for (var i = 0; i < frameCount; i++)
        {
            frames[i] = firstFrame + i;
        }

        return AddClip(name, frames, fps, loop);
    }

    /// <summary>Adds a clip of the given frames in order (they may repeat or skip).</summary>
    /// <param name="name">The clip's name.</param>
    /// <param name="frames">Frame numbers.</param>
    /// <param name="fps">Frames per second; above zero.</param>
    /// <param name="loop">Start over after the last frame, or hold it.</param>
    /// <returns>The clip, which is also kept under its name.</returns>
    public SpriteClip AddClip(string name, ReadOnlySpan<int> frames, float fps, bool loop = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (frames.IsEmpty)
        {
            throw new ArgumentException("A clip needs at least one frame.", nameof(frames));
        }

        if (!(fps > 0) || !float.IsFinite(fps))
        {
            throw new ArgumentOutOfRangeException(nameof(fps), fps, "fps must be above zero.");
        }

        foreach (var f in frames)
        {
            _ = Frame(f);
        }

        var clip = new SpriteClip(name, frames.ToArray(), fps, loop);
        _clips[name] = clip;
        return clip;
    }

    /// <summary>Finds a clip by name.</summary>
    /// <param name="name">The clip's name.</param>
    /// <returns>The clip.</returns>
    /// <exception cref="UnknownClipException">The sheet has no such clip.</exception>
    public SpriteClip Clip(string name) => _clips.TryGetValue(name, out var c) ? c : throw new UnknownClipException(name, _clips.Keys);
}
