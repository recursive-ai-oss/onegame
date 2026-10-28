using System.Numerics;
using OneGame.Core;
using OneGame.Particles;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>
/// Immediate-mode 2D drawing for view code: call it every frame to say what to show, and the stage batches it onto the same
/// <see cref="Scene2D"/> and <see cref="Renderer2D"/> a hand-built stage would use. Nothing is retained between frames except the camera.
/// World coordinates are the renderer's: +X right, +Y down, one unit is one pixel at camera zoom 1. Draw order is by <c>z</c> (lower is
/// behind), then call order; text is drawn over everything else, in call order. Steady frames allocate nothing (text strings that the view
/// builds each frame are the view's own allocation).
/// </summary>
/// <remarks>
/// Reach it as the parameter of an <see cref="IView2D"/> entity's <c>Draw</c> method. It is view-side: read state, never write it. Use
/// <see cref="Time"/> (not a clock) to animate so captures and replays repeat exactly. For more control drop down to <see cref="Scene"/>
/// and <see cref="Renderer"/>.
/// </remarks>
/// <example>
/// A sprite that moves with its synced position, animated from a sheet, with a camera that follows it and a text label:
/// <code>
/// private SpriteSheet? _sheet;
///
/// [View]
/// public void Draw(Draw2D d)
/// {
///     if (_sheet is null)
///     {
///         _sheet = SpriteSheet.Grid(d.Texture("art/hero.png"), 32, 32);
///         _sheet.AddClip("walk", firstFrame: 0, frameCount: 4, fps: 8);
///     }
///
///     var at = new Vector2(X.ToSingle(), Y.ToSingle());
///     d.Camera.Zoom = 2;
///     d.Camera.Smoothing = 0.15f;
///     d.Follow(at);                                           // eases the camera toward the hero
///     d.FillRect(new Rect(-200, 40, 400, 8), Color.Rgb(0x335533));   // the ground
///     d.Sprite(_sheet, "walk", at, scale: 1);                // plays the clip from d.Time
///     d.Text("hero", at - new Vector2(0, 24), size: 12, anchor: new Vector2(0.5f, 1));
/// }
/// </code>
/// </example>
public sealed partial class Draw2D
{
    private const int DiskSize = 128;
    private const int SoftSize = 64;

    private struct TextItem
    {
        public string Text;
        public string Family;
        public Vector2 Position;
        public float Size;
        public Color Color;
        public Vector2 Anchor;
        public int Weight;
        public bool Screen;
    }

    private readonly Scene2D _scene = new();
    private readonly List<TextItem> _texts = [];
    private readonly Dictionary<(string Path, TextureFilter Filter), Asset<Texture2D>> _textures = [];
    private Renderer2D? _assetsFor;
    private Texture2D? _white;
    private Texture2D? _disk;
    private Texture2D? _soft;
    private readonly List<ParticleEmitter> _emitters = [];
    private long _lastTick = long.MinValue;
    private float _lastTime;

    internal Draw2D()
    {
        Layer = _scene.DefaultLayer;
    }

    /// <summary>The game's assets, where <see cref="Texture"/> and the path overloads of <see cref="Sprite(string, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)"/> load from. Null when the stage was made without them.</summary>
    public Assets? Assets { get; internal set; }

    /// <summary>The renderer drawing this frame; use it to create textures (<see cref="Renderer2D.CreateTexture"/>). Null before the first frame.</summary>
    public Renderer2D? Renderer { get; private set; }

    /// <summary>The low-level scene this frame is collected in. Use it to drop down a layer: add layers, tilemaps or materials alongside what the calls here draw.</summary>
    public Scene2D Scene => _scene;

    /// <summary>The layer new sprites go on; the scene's default layer at the start of every frame.</summary>
    public Layer2D Layer { get; set; }

    /// <summary>The camera. It remembers where it is between frames; call <see cref="Follow"/> or set its properties from view code.</summary>
    public Camera2DRig Camera { get; } = new();

    /// <summary>The entity being asked to draw, or default for drawing done outside an entity.</summary>
    public EntityId Entity { get; internal set; }

    /// <summary>The simulation tick being shown.</summary>
    public long Tick { get; private set; }

    /// <summary>How far past <see cref="Tick"/> the frame is, from 0 to just under 1, for blending a moving thing between ticks.</summary>
    public Fixed Alpha { get; private set; }

    /// <summary>
    /// View time in seconds: the shown tick and fraction at the simulation's rate. Animations read this, so they are smooth in play,
    /// repeat exactly in captures and replays, and stand still while the game is paused. It is a view value; never store it in synced state.
    /// </summary>
    public float Time { get; private set; }

    /// <summary>View time since the previous drawn frame, in seconds; zero when the same moment is drawn twice or time went backwards.</summary>
    public float DeltaTime { get; private set; }

    /// <summary>Target width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Target height in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>The number of sprites queued so far this frame (shapes count; text does not).</summary>
    public int SpriteCount { get; private set; }

    /// <summary>The number of text lines queued so far this frame.</summary>
    public int TextCount => _texts.Count;

    private void Begin(Renderer2D renderer, in FrameInfo frame)
    {
        if (!ReferenceEquals(_assetsFor, renderer))
        {
            _assetsFor = renderer;
            _white = renderer.CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest);
            _disk = renderer.CreateTexture(DiskSize, DiskSize, MakeDisk(), TextureFilter.Linear);
            _soft = renderer.CreateTexture(SoftSize, SoftSize, MakeSoft(), TextureFilter.Linear);
        }

        Renderer = renderer;
        _scene.Clear();
        _texts.Clear();
        _emitters.Clear();
        Layer = _scene.DefaultLayer;
        SpriteCount = 0;
        Width = frame.Width;
        Height = frame.Height;
        Tick = frame.Tick;
        Alpha = frame.Alpha;
        Time = (Tick + Alpha.ToSingle()) / GameInstance.TickRate;
        DeltaTime = _lastTick == long.MinValue || Time <= _lastTime ? 0 : Time - _lastTime;
        _lastTick = Tick;
        _lastTime = Time;
    }

    /// <summary>
    /// Draws the shapes of entities that cannot reference the host (a shared rules assembly): called with the world being drawn, after the
    /// <see cref="IView2D"/> entities. It may read any state and writes only into the <see cref="Draw2D"/>.
    /// </summary>
    public Action<World, Draw2D>? Describer { get; set; }

    internal void Draw(Renderer2D renderer, in FrameInfo frame, TextQuadBuilder text, FontTextMeasurer measurer, GlyphAtlas glyphs)
    {
        Begin(renderer, frame);
        var entities = frame.World.Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is IView2D view)
            {
                Entity = entities[i].Id;
                view.Draw(this);
            }
        }

        Entity = default;
        Describer?.Invoke(frame.World, this);
        Entity = default;
        End(renderer, text, measurer, glyphs);
    }

    private void End(Renderer2D renderer, TextQuadBuilder text, FontTextMeasurer measurer, GlyphAtlas glyphs)
    {
        var size = new Vector2(Width, Height);
        if (SpriteCount > 0 || _scene.Layers.Count > 1)
        {
            renderer.DrawScene(_scene, Camera.Current(size));
        }

        if (_texts.Count == 0)
        {
            return;
        }

        var view = Camera.Current(size).View(size, Vector2.One);
        for (var i = 0; i < _texts.Count; i++)
        {
            var t = _texts[i];
            var px = t.Screen ? t.Size : t.Size * Camera.Zoom;
            if (!(px >= 1) || t.Text.Length == 0)
            {
                continue;
            }

            var style = new TextStyle(t.Family, px, t.Weight, false, 0, 0, WhiteSpace.NoWrap);
            var at = t.Screen ? t.Position : Vector2.Transform(t.Position, view);
            if (t.Anchor != Vector2.Zero)
            {
                var m = measurer.Measure(t.Text, style, float.PositiveInfinity);
                at -= new Vector2(m.Width * t.Anchor.X, m.Height * t.Anchor.Y);
            }

            renderer.Ui.Text(text, glyphs, t.Text, style, at, t.Color);
        }
    }

    /// <summary>The camera moves toward a point, by its smoothing, using this frame's <see cref="DeltaTime"/>.</summary>
    /// <param name="target">The world point to centre on.</param>
    public void Follow(Vector2 target) => Camera.Follow(target, DeltaTime);

    /// <summary>Where a world point lands on the screen this frame, in pixels.</summary>
    /// <param name="world">A world point.</param>
    /// <returns>Framebuffer pixels, origin top-left.</returns>
    public Vector2 WorldToScreen(Vector2 world) => Camera.WorldToScreen(world, new Vector2(Width, Height));

    /// <summary>The world point under a screen pixel this frame.</summary>
    /// <param name="screen">Framebuffer pixels, origin top-left.</param>
    /// <returns>A world point.</returns>
    public Vector2 ScreenToWorld(Vector2 screen) => Camera.ScreenToWorld(screen, new Vector2(Width, Height));

    // ---- sprites ----

    /// <summary>
    /// A texture loaded from the game's content folders (<c>art/ship.png</c>), cached after the first call, so it is cheap to ask for every frame.
    /// The texture is updated in place when the file changes during development, so sprites and <see cref="SpriteSheet"/>s over it show the new image.
    /// </summary>
    /// <param name="path">Path under a content folder.</param>
    /// <param name="filter">How it is filtered when scaled; <see cref="TextureFilter.Nearest"/> for pixel art.</param>
    /// <returns>The texture.</returns>
    /// <exception cref="AssetException">The file is missing or damaged.</exception>
    /// <exception cref="InvalidOperationException">The stage has no <see cref="Assets"/>.</exception>
    public Texture2D Texture(string path, TextureFilter filter = TextureFilter.Linear)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (_textures.TryGetValue((path, filter), out var known))
        {
            return known.Value;
        }

        var assets = Assets ?? throw new InvalidOperationException("This Draw2D has no Assets to load from.");
        var loaded = assets.LoadTexture(path, filter);
        _textures[(path, filter)] = loaded;
        return loaded.Value;
    }

    /// <summary>Draws a whole image loaded from the game's content folders; see <see cref="Texture"/>. The other parameters are those of <see cref="Sprite(Texture2D, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)"/>.</summary>
    /// <param name="path">Path under a content folder, like <c>art/ship.png</c>.</param>
    /// <param name="position">Where its origin goes, in world units.</param>
    /// <param name="rotation">Radians about the origin; positive turns clockwise on screen.</param>
    /// <param name="scale">World units per texture pixel.</param>
    /// <param name="tint">Colour multiplied into the texture.</param>
    /// <param name="origin">The point of the sprite placed at <paramref name="position"/>, as a fraction of its size; the centre by default.</param>
    /// <param name="flip">Mirroring.</param>
    /// <param name="z">Draw order inside the layer.</param>
    public void Sprite(string path, Vector2 position, float rotation = 0, float scale = 1, Color? tint = null, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None, float z = 0) =>
        Put(Texture(path), default, position, rotation, new Vector2(scale), tint ?? Color.White, origin ?? new Vector2(0.5f), flip, z);

    /// <summary>Draws a whole texture.</summary>
    /// <param name="texture">The image, from <see cref="Renderer2D.CreateTexture"/>.</param>
    /// <param name="position">Where its origin goes, in world units.</param>
    /// <param name="rotation">Radians about the origin; positive turns clockwise on screen.</param>
    /// <param name="scale">World units per texture pixel.</param>
    /// <param name="tint">Colour multiplied into the texture; white (the default) draws it unchanged.</param>
    /// <param name="origin">The point of the sprite placed at <paramref name="position"/>, as a fraction of its size; the centre by default.</param>
    /// <param name="flip">Mirroring.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    public void Sprite(Texture2D texture, Vector2 position, float rotation = 0, float scale = 1, Color? tint = null, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None, float z = 0) =>
        Put(texture, default, position, rotation, new Vector2(scale), tint ?? Color.White, origin ?? new Vector2(0.5f), flip, z);

    /// <summary>Draws part of a texture.</summary>
    /// <param name="texture">The image.</param>
    /// <param name="source">The part to draw, in texture pixels.</param>
    /// <param name="position">Where its origin goes, in world units.</param>
    /// <param name="rotation">Radians about the origin; positive turns clockwise on screen.</param>
    /// <param name="scale">World units per texture pixel.</param>
    /// <param name="tint">Colour multiplied into the texture.</param>
    /// <param name="origin">The point of the sprite placed at <paramref name="position"/>, as a fraction of its size; the centre by default.</param>
    /// <param name="flip">Mirroring.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    public void Sprite(Texture2D texture, PixelRect source, Vector2 position, float rotation = 0, float scale = 1, Color? tint = null, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None, float z = 0) =>
        Put(texture, source, position, rotation, new Vector2(scale), tint ?? Color.White, origin ?? new Vector2(0.5f), flip, z);

    /// <summary>Draws one frame of a sprite sheet.</summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="frame">The frame number.</param>
    /// <param name="position">Where its origin goes, in world units.</param>
    /// <param name="rotation">Radians about the origin; positive turns clockwise on screen.</param>
    /// <param name="scale">World units per texture pixel.</param>
    /// <param name="tint">Colour multiplied into the texture.</param>
    /// <param name="origin">The point of the sprite placed at <paramref name="position"/>, as a fraction of its size; the centre by default.</param>
    /// <param name="flip">Mirroring.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    public void Sprite(SpriteSheet sheet, int frame, Vector2 position, float rotation = 0, float scale = 1, Color? tint = null, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None, float z = 0)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        Put(sheet.Texture, sheet.Frame(frame), position, rotation, new Vector2(scale), tint ?? Color.White, origin ?? new Vector2(0.5f), flip, z);
    }

    /// <summary>Draws a named clip of a sprite sheet at the current view time.</summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="clip">The clip's name.</param>
    /// <param name="position">Where its origin goes, in world units.</param>
    /// <param name="rotation">Radians about the origin; positive turns clockwise on screen.</param>
    /// <param name="scale">World units per texture pixel.</param>
    /// <param name="tint">Colour multiplied into the texture.</param>
    /// <param name="origin">The point of the sprite placed at <paramref name="position"/>, as a fraction of its size; the centre by default.</param>
    /// <param name="flip">Mirroring.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    /// <param name="clipTime">Seconds into the clip; the view's <see cref="Time"/> when null, so every thing plays in step. Pass <c>Time - startedAt</c> to play from a moment.</param>
    /// <exception cref="UnknownClipException">The sheet has no clip of that name.</exception>
    public void Sprite(SpriteSheet sheet, string clip, Vector2 position, float rotation = 0, float scale = 1, Color? tint = null, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None, float z = 0, float? clipTime = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        Sprite(sheet, sheet.Clip(clip), position, rotation, scale, tint, origin, flip, z, clipTime);
    }

    /// <summary>Draws a clip of a sprite sheet at the current view time.</summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="clip">The clip, from <see cref="SpriteSheet.AddClip(string, int, int, float, bool)"/>.</param>
    /// <param name="position">Where its origin goes, in world units.</param>
    /// <param name="rotation">Radians about the origin; positive turns clockwise on screen.</param>
    /// <param name="scale">World units per texture pixel.</param>
    /// <param name="tint">Colour multiplied into the texture.</param>
    /// <param name="origin">The point of the sprite placed at <paramref name="position"/>, as a fraction of its size; the centre by default.</param>
    /// <param name="flip">Mirroring.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    /// <param name="clipTime">Seconds into the clip; the view's <see cref="Time"/> when null.</param>
    public void Sprite(SpriteSheet sheet, SpriteClip clip, Vector2 position, float rotation = 0, float scale = 1, Color? tint = null, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None, float z = 0, float? clipTime = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(clip);
        Put(sheet.Texture, sheet.Frame(clip.FrameAt(clipTime ?? Time)), position, rotation, new Vector2(scale), tint ?? Color.White, origin ?? new Vector2(0.5f), flip, z);
    }

    /// <summary>Queues a fully specified low-level sprite on the current layer.</summary>
    /// <param name="sprite">The sprite.</param>
    public void Sprite(in Sprite sprite)
    {
        _scene.Draw(Layer, sprite);
        SpriteCount++;
    }

    private void Put(Texture2D texture, PixelRect source, Vector2 position, float rotation, Vector2 scale, Color tint, Vector2 origin, SpriteFlip flip, float z)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _scene.Draw(Layer, new Sprite
        {
            Texture = texture,
            Source = source,
            Position = position,
            Pivot = origin,
            Scale = scale,
            Rotation = rotation,
            Tint = tint,
            Flip = flip,
            Z = z,
        });
        SpriteCount++;
    }

    // ---- shapes ----

    /// <summary>Fills a rectangle.</summary>
    /// <param name="bounds">The rectangle in world units.</param>
    /// <param name="color">Fill colour, straight alpha.</param>
    /// <param name="rotation">Radians about the rectangle's centre.</param>
    /// <param name="z">Draw order inside the layer.</param>
    public void FillRect(Rect bounds, Color color, float rotation = 0, float z = 0) =>
        Put(White(), default, new Vector2(bounds.X + (bounds.Width * 0.5f), bounds.Y + (bounds.Height * 0.5f)), rotation, new Vector2(bounds.Width, bounds.Height), color, new Vector2(0.5f), SpriteFlip.None, z);

    /// <summary>Draws a rectangle's outline, inside its bounds.</summary>
    /// <param name="bounds">The rectangle in world units.</param>
    /// <param name="color">Line colour, straight alpha.</param>
    /// <param name="thickness">Line width in world units.</param>
    /// <param name="z">Draw order inside the layer.</param>
    public void StrokeRect(Rect bounds, Color color, float thickness = 1, float z = 0)
    {
        var t = Math.Min(thickness, Math.Min(bounds.Width, bounds.Height) * 0.5f);
        FillRect(new Rect(bounds.X, bounds.Y, bounds.Width, t), color, 0, z);
        FillRect(new Rect(bounds.X, bounds.Bottom - t, bounds.Width, t), color, 0, z);
        FillRect(new Rect(bounds.X, bounds.Y + t, t, bounds.Height - (2 * t)), color, 0, z);
        FillRect(new Rect(bounds.Right - t, bounds.Y + t, t, bounds.Height - (2 * t)), color, 0, z);
    }

    /// <summary>Draws a line with square ends.</summary>
    /// <param name="from">Start in world units.</param>
    /// <param name="to">End in world units.</param>
    /// <param name="color">Line colour, straight alpha.</param>
    /// <param name="thickness">Line width in world units.</param>
    /// <param name="z">Draw order inside the layer.</param>
    public void Line(Vector2 from, Vector2 to, Color color, float thickness = 1, float z = 0)
    {
        var d = to - from;
        Put(White(), default, from, MathF.Atan2(d.Y, d.X), new Vector2(d.Length(), thickness), color, new Vector2(0, 0.5f), SpriteFlip.None, z);
    }

    /// <summary>Fills a circle.</summary>
    /// <param name="center">Centre in world units.</param>
    /// <param name="radius">Radius in world units.</param>
    /// <param name="color">Fill colour, straight alpha.</param>
    /// <param name="z">Draw order inside the layer.</param>
    public void FillCircle(Vector2 center, float radius, Color color, float z = 0)
    {
        _ = White();
        Put(_disk!, default, center, 0, new Vector2(radius / (DiskSize / 2)), color, new Vector2(0.5f), SpriteFlip.None, z);
    }

    /// <summary>Draws a circle's outline as short straight segments, enough of them to look round at the current zoom.</summary>
    /// <param name="center">Centre in world units.</param>
    /// <param name="radius">Radius in world units; the line is centred on it.</param>
    /// <param name="color">Line colour, straight alpha.</param>
    /// <param name="thickness">Line width in world units.</param>
    /// <param name="z">Draw order inside the layer.</param>
    public void StrokeCircle(Vector2 center, float radius, Color color, float thickness = 1, float z = 0)
    {
        var segments = Math.Clamp((int)(radius * MathF.Max(Camera.Zoom, 0.01f) * 0.75f), 16, 128);
        var prev = center + new Vector2(radius, 0);
        for (var i = 1; i <= segments; i++)
        {
            var a = i * MathF.Tau / segments;
            var next = center + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
            // Each piece runs a little past its end so the joints leave no notch.
            var d = next - prev;
            var len = d.Length();
            Put(White(), default, prev - (d * (thickness * 0.25f / MathF.Max(len, 1e-6f))), MathF.Atan2(d.Y, d.X), new Vector2(len + (thickness * 0.5f), thickness), color, new Vector2(0, 0.5f), SpriteFlip.None, z);
            prev = next;
        }
    }

    private Texture2D White() => _white ?? throw new InvalidOperationException("Draw2D can only draw while the stage is rendering a frame.");

    private static byte[] MakeDisk()
    {
        var px = new byte[DiskSize * DiskSize * 4];
        const float r = DiskSize / 2f;
        for (var y = 0; y < DiskSize; y++)
        {
            for (var x = 0; x < DiskSize; x++)
            {
                var dist = MathF.Sqrt(((x + 0.5f - r) * (x + 0.5f - r)) + ((y + 0.5f - r) * (y + 0.5f - r)));
                var i = ((y * DiskSize) + x) * 4;
                px[i] = px[i + 1] = px[i + 2] = 255;
                px[i + 3] = (byte)(Math.Clamp(r - dist, 0f, 1f) * 255f + 0.5f);
            }
        }

        return px;
    }

    // ---- text ----

    /// <summary>Draws one line of text at a world point. The size scales with the camera zoom; text is not rotated, and is drawn over sprites and shapes.</summary>
    /// <param name="text">The string. Newlines are not broken.</param>
    /// <param name="position">Where the anchor of the text goes, in world units.</param>
    /// <param name="size">Font size in world units (pixels at zoom 1).</param>
    /// <param name="color">Text colour, straight alpha; white by default.</param>
    /// <param name="anchor">The point of the text placed at <paramref name="position"/>, as a fraction of its size: (0,0) top-left (default), (0.5,0.5) centred.</param>
    /// <param name="family">A font family the game added in <c>DefineFonts</c>, or null for the first.</param>
    /// <param name="weight">Font weight, 400 regular.</param>
    public void Text(string text, Vector2 position, float size = 16, Color? color = null, Vector2? anchor = null, string? family = null, int weight = 400) =>
        AddText(text, position, size, color, anchor, family, weight, false);

    /// <summary>Draws one line of text at a screen pixel, unaffected by the camera.</summary>
    /// <param name="text">The string. Newlines are not broken.</param>
    /// <param name="position">Where the anchor of the text goes, in framebuffer pixels.</param>
    /// <param name="size">Font size in pixels.</param>
    /// <param name="color">Text colour, straight alpha; white by default.</param>
    /// <param name="anchor">The point of the text placed at <paramref name="position"/>, as a fraction of its size: (0,0) top-left (default).</param>
    /// <param name="family">A font family the game added in <c>DefineFonts</c>, or null for the first.</param>
    /// <param name="weight">Font weight, 400 regular.</param>
    public void ScreenText(string text, Vector2 position, float size = 16, Color? color = null, Vector2? anchor = null, string? family = null, int weight = 400) =>
        AddText(text, position, size, color, anchor, family, weight, true);

    private void AddText(string text, Vector2 position, float size, Color? color, Vector2? anchor, string? family, int weight, bool screen)
    {
        ArgumentNullException.ThrowIfNull(text);
        _texts.Add(new TextItem
        {
            Text = text,
            Family = family ?? string.Empty,
            Position = position,
            Size = size,
            Color = color ?? Color.White,
            Anchor = anchor ?? Vector2.Zero,
            Weight = weight,
            Screen = screen,
        });
    }
}
