using System.Numerics;
using OneGame.Gpu;
using OneGame.Atlas;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Render2D;

internal enum AtlasKind
{
    Glyphs,
    Art,
}

/// <summary>A colour ramp for a filled shape. Positions are in the same coordinates as the shape's bounds, before any transform.</summary>
/// <param name="From">The colour at the start (or the centre), sRGB straight alpha.</param>
/// <param name="To">The colour at the end (or the rim).</param>
/// <param name="Start">The start point of a linear ramp, or the centre of a radial one.</param>
/// <param name="End">The end point of a linear ramp (unused for radial).</param>
/// <param name="Radius">Greater than zero makes the ramp radial with this radius around <paramref name="Start"/>.</param>
public readonly record struct Gradient(Color From, Color To, Vector2 Start, Vector2 End, float Radius)
{
    /// <summary>A ramp along the line from <paramref name="start"/> to <paramref name="end"/>.</summary>
    /// <param name="from">Colour at the start.</param>
    /// <param name="to">Colour at the end.</param>
    /// <param name="start">Start point.</param>
    /// <param name="end">End point.</param>
    /// <returns>The gradient.</returns>
    public static Gradient Linear(Color from, Color to, Vector2 start, Vector2 end) => new(from, to, start, end, 0);

    /// <summary>A ramp from the centre outwards.</summary>
    /// <param name="inner">Colour at the centre.</param>
    /// <param name="outer">Colour at <paramref name="radius"/> and beyond.</param>
    /// <param name="center">The centre.</param>
    /// <param name="radius">Distance at which the ramp ends.</param>
    /// <returns>The gradient.</returns>
    public static Gradient Radial(Color inner, Color outer, Vector2 center, float radius) => new(inner, outer, center, center, radius);
}

/// <summary>
/// Draws UI shapes, images and text into the current frame, in coordinates of framebuffer pixels (origin top-left, y down). It keeps stacks of clips,
/// transforms and opacity groups that nest and must balance by the end of the frame. <see cref="Draw(DisplayList, TextQuadBuilder, GlyphAtlas, ArtAtlas)"/> replays a whole UI display list;
/// the other methods draw single primitives for HUDs and tools. Obtain it from <see cref="Renderer2D.Ui"/>.
/// </summary>
public sealed class UiCanvas : IDisposable
{
    private struct ClipState
    {
        public int X0, Y0, X1, Y1;
        public uint Id;
    }

    private struct GroupState
    {
        public bool IsGroup;
        public int Parent;
        public int Temp;
        public float Opacity;
        public ClipState Scissor;
    }

    private sealed class PageTexture
    {
        public required Texture2D Texture;
        public int Version = -1;
        public int Width;
        public int Height;
    }

    private readonly Renderer2D _r;
    private readonly List<ClipState> _clips = [];
    private readonly List<Matrix3x2> _transforms = [];
    private readonly List<GroupState> _groups = [];
    private readonly List<GlyphQuad> _glyphs = [];
    private readonly List<PageTexture?>[] _pages = [[], []];
    private readonly Dictionary<int, PageTexture> _standalone = [];
    private readonly List<int> _released = [];
    private Matrix3x2 _m = Matrix3x2.Identity;
    private int _skip;
    private int _width;
    private int _height;

    internal UiCanvas(Renderer2D renderer) => _r = renderer;

    /// <summary>Images the display list named that the atlas could not find, drawn as magenta boxes; counts since the renderer was made.</summary>
    public int MissingArt { get; private set; }

    internal void Reset(int width, int height)
    {
        _width = width;
        _height = height;
        _m = Matrix3x2.Identity;
        _transforms.Clear();
        _groups.Clear();
        _clips.Clear();
        _clips.Add(new ClipState { X0 = 0, Y0 = 0, X1 = width, Y1 = height });
        _skip = 0;
    }

    internal void Finish()
    {
        if (_clips.Count != 1 || _transforms.Count != 0 || _groups.Count != 0)
        {
            throw new InvalidOperationException("the UI stacks are unbalanced at the end of the frame: every PushClip, PushTransform and PushOpacity needs its Pop");
        }
    }

    // ---- stacks ----

    /// <summary>Intersects the clip with a rounded rectangle until <see cref="PopClip"/>. Axis-aligned square clips cut at whole pixels; rounded or turned clips are anti-aliased.</summary>
    /// <param name="bounds">The rectangle, in the coordinates in effect now.</param>
    /// <param name="radii">Corner radii, or default for square corners.</param>
    public void PushClip(Rect bounds, CornerRadii radii = default)
    {
        var top = _clips[^1];
        if (_skip > 0)
        {
            _clips.Add(top);
            return;
        }

        var p0 = Vector2.Transform(new Vector2(bounds.X, bounds.Y), _m);
        var p1 = Vector2.Transform(new Vector2(bounds.X + bounds.Width, bounds.Y), _m);
        var p2 = Vector2.Transform(new Vector2(bounds.X, bounds.Y + bounds.Height), _m);
        var p3 = Vector2.Transform(new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height), _m);
        var next = new ClipState
        {
            X0 = Math.Max(top.X0, (int)MathF.Round(MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)))),
            Y0 = Math.Max(top.Y0, (int)MathF.Round(MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)))),
            X1 = Math.Min(top.X1, (int)MathF.Round(MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)))),
            Y1 = Math.Min(top.Y1, (int)MathF.Round(MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)))),
            Id = top.Id,
        };
        var axisAligned = MathF.Abs(_m.M12) < 1e-4f && MathF.Abs(_m.M21) < 1e-4f;
        if (!axisAligned || !radii.IsZero)
        {
            if (Matrix3x2.Invert(_m, out var inv))
            {
                next.Id = _r.AddClip(
                    new Vector4(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                    new Vector4(radii.TopLeft, radii.TopRight, radii.BottomRight, radii.BottomLeft),
                    inv,
                    top.Id);
            }
            else
            {
                next.X1 = next.X0;
            }
        }

        _clips.Add(next);
    }

    /// <summary>Ends the clip of the matching <see cref="PushClip"/>.</summary>
    public void PopClip()
    {
        if (_clips.Count <= 1)
        {
            throw new InvalidOperationException("PopClip without a matching PushClip");
        }

        _clips.RemoveAt(_clips.Count - 1);
    }

    /// <summary>
    /// Applies <paramref name="matrix"/> to everything drawn until <see cref="PopTransform"/>. The matrix maps coordinates in effect now to the
    /// screen the way the display list's transforms do (it already contains its pivot), and nested transforms compose.
    /// </summary>
    /// <param name="matrix">The transform, for row vectors.</param>
    public void PushTransform(Matrix3x2 matrix)
    {
        _transforms.Add(_m);
        if (_skip == 0)
        {
            _m = matrix * _m;
        }
    }

    /// <summary>Ends the transform of the matching <see cref="PushTransform"/>.</summary>
    public void PopTransform()
    {
        if (_transforms.Count == 0)
        {
            throw new InvalidOperationException("PopTransform without a matching PushTransform");
        }

        _m = _transforms[^1];
        _transforms.RemoveAt(_transforms.Count - 1);
    }

    /// <summary>Draws everything until <see cref="PopOpacity"/> as a group and blends the group with <paramref name="opacity"/>, so overlapping shapes inside do not show through each other.</summary>
    /// <param name="opacity">0 (nothing) to 1 (no effect).</param>
    public void PushOpacity(float opacity)
    {
        if (_skip > 0 || opacity <= 0f)
        {
            _groups.Add(default);
            _skip++;
            return;
        }

        if (opacity >= 0.999f)
        {
            _groups.Add(default);
            return;
        }

        var temp = _r.AcquireGroupTarget();
        _groups.Add(new GroupState { IsGroup = true, Parent = _r.CurrentTarget, Temp = temp, Opacity = opacity, Scissor = _clips[^1] });
        _r.CurrentTarget = temp;
    }

    /// <summary>Ends the group of the matching <see cref="PushOpacity"/> and blends it onto what was drawn before.</summary>
    public void PopOpacity()
    {
        if (_groups.Count == 0)
        {
            throw new InvalidOperationException("PopOpacity without a matching PushOpacity");
        }

        var g = _groups[^1];
        _groups.RemoveAt(_groups.Count - 1);
        if (_skip > 0)
        {
            _skip--;
            return;
        }

        if (g.IsGroup)
        {
            _r.EmitComposite(g.Temp, g.Parent, g.Opacity, g.Scissor.X0, g.Scissor.Y0, g.Scissor.X1 - g.Scissor.X0, g.Scissor.Y1 - g.Scissor.Y0);
            _r.ReleaseGroupTarget(g.Temp);
            _r.CurrentTarget = g.Parent;
        }
    }

    // ---- primitives ----

    /// <summary>Fills a rounded rectangle with a colour.</summary>
    /// <param name="bounds">The rectangle.</param>
    /// <param name="color">sRGB, straight alpha.</param>
    /// <param name="radii">Corner radii.</param>
    public void Fill(Rect bounds, Color color, CornerRadii radii = default) =>
        Emit(0, bounds, radii, default, color, color, 0, default, default, _r.GroupOf(_r.WhiteTexture), true);

    /// <summary>Fills a rounded rectangle with a gradient.</summary>
    /// <param name="bounds">The rectangle.</param>
    /// <param name="gradient">The colour ramp.</param>
    /// <param name="radii">Corner radii.</param>
    public void Fill(Rect bounds, in Gradient gradient, CornerRadii radii = default)
    {
        var radial = gradient.Radius > 0;
        var grad = radial
            ? new Vector4(gradient.Start.X, gradient.Start.Y, gradient.Radius, 0)
            : new Vector4(gradient.Start.X, gradient.Start.Y, gradient.End.X, gradient.End.Y);
        Emit(0, bounds, radii, default, gradient.From, gradient.To, radial ? 2u : 1u, grad, default, _r.GroupOf(_r.WhiteTexture), true);
    }

    /// <summary>Strokes the inside edge of a rounded rectangle, with a thickness per side.</summary>
    /// <param name="bounds">The outer rectangle.</param>
    /// <param name="color">sRGB, straight alpha.</param>
    /// <param name="widths">Thickness of the left, top, right and bottom edges.</param>
    /// <param name="radii">Corner radii of the outer edge.</param>
    public void Border(Rect bounds, Color color, Edges widths, CornerRadii radii = default) =>
        Emit(1, bounds, radii, new Vector4(widths.Left, widths.Top, widths.Right, widths.Bottom), color, color, 0, default, default, _r.GroupOf(_r.WhiteTexture), true);

    /// <summary>Draws a soft shadow of a rounded box. Nothing is drawn under the box itself.</summary>
    /// <param name="box">The element's box.</param>
    /// <param name="color">Shadow colour.</param>
    /// <param name="radii">The box's corner radii.</param>
    /// <param name="offset">How far the shadow is moved.</param>
    /// <param name="blur">Blur radius in pixels (twice the standard deviation of the blur).</param>
    /// <param name="spread">How much the shadow's shape grows beyond the box.</param>
    public void Shadow(Rect box, Color color, CornerRadii radii, Vector2 offset, float blur, float spread)
    {
        if (_skip > 0)
        {
            return;
        }

        var scale = Scale();
        var sigma = MathF.Max(blur * 0.5f, 0.5f / scale);
        var shape = new Rect(box.X + offset.X - spread, box.Y + offset.Y - spread, box.Width + (2 * spread), box.Height + (2 * spread));
        var grown = new CornerRadii(
            radii.TopLeft > 0 ? radii.TopLeft + spread : 0,
            radii.TopRight > 0 ? radii.TopRight + spread : 0,
            radii.BottomRight > 0 ? radii.BottomRight + spread : 0,
            radii.BottomLeft > 0 ? radii.BottomLeft + spread : 0);
        var pad = (3 * sigma) + (1.5f / scale);
        EmitRaw(2, shape, grown, new Vector4(box.X, box.Y, box.Width, box.Height), color, color, 0, new Vector4(sigma, 0, 0, 0), default, _r.GroupOf(_r.WhiteTexture), pad);
    }

    /// <summary>Draws a texture (or part of it) into a rectangle.</summary>
    /// <param name="bounds">Destination.</param>
    /// <param name="texture">The image.</param>
    /// <param name="tint">Colour multiplied in; white for none.</param>
    /// <param name="radii">Corner radii that round the image.</param>
    public void Image(Rect bounds, Texture2D texture, Color tint, CornerRadii radii = default) =>
        Image(bounds, texture, new PixelRect(0, 0, texture.Width, texture.Height), tint, radii);

    /// <summary>Draws part of a texture into a rectangle.</summary>
    /// <param name="bounds">Destination.</param>
    /// <param name="texture">The image.</param>
    /// <param name="source">The part to draw, in pixels.</param>
    /// <param name="tint">Colour multiplied in; white for none.</param>
    /// <param name="radii">Corner radii that round the image.</param>
    public void Image(Rect bounds, Texture2D texture, PixelRect source, Color tint, CornerRadii radii = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        var uv = new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height, source.Width / (float)texture.Width, source.Height / (float)texture.Height);
        Emit(3, bounds, radii, default, tint, tint, 0, default, uv, _r.GroupOf(texture), true);
    }

    // ---- display list ----

    /// <summary>
    /// Draws a display list: every command in order, with its clips, transforms and opacity groups. Text comes from <paramref name="text"/> and
    /// <paramref name="glyphs"/>, images from <paramref name="art"/>. The atlases' pages are uploaded here, so call it after the frame's lookups are done.
    /// </summary>
    /// <param name="list">The list from <c>UiTree.Frame()</c>.</param>
    /// <param name="text">Turns text runs into glyph quads; built over the same measurer the UI laid out with.</param>
    /// <param name="glyphs">The glyph atlas <paramref name="text"/> draws from.</param>
    /// <param name="art">The SVG and image atlas.</param>
    public void Draw(DisplayList list, TextQuadBuilder text, GlyphAtlas glyphs, ArtAtlas art)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(glyphs);
        ArgumentNullException.ThrowIfNull(art);
        glyphs.BeginFrame(_r.FrameNumber);
        art.BeginFrame(_r.FrameNumber);
        var commands = list.Commands;
        for (var i = 0; i < commands.Length; i++)
        {
            ref readonly var c = ref commands[i];
            switch (c.Kind)
            {
                case DrawKind.Rect:
                    Fill(c.Bounds, c.Color, c.Radii);
                    break;
                case DrawKind.Border:
                    Border(c.Bounds, c.Color, c.Widths, c.Radii);
                    break;
                case DrawKind.Shadow:
                    Shadow(c.Bounds, c.Color, c.Radii, c.Offset, c.Value, c.Spread);
                    break;
                case DrawKind.Image:
                    DrawArt(c, art, list.Scale);
                    break;
                case DrawKind.Text:
                    DrawText(list, c, text, glyphs);
                    break;
                case DrawKind.PushClip:
                    PushClip(c.Bounds, c.Radii);
                    break;
                case DrawKind.PopClip:
                    PopClip();
                    break;
                case DrawKind.PushTransform:
                    PushTransform(c.Transform);
                    break;
                case DrawKind.PopTransform:
                    PopTransform();
                    break;
                case DrawKind.PushOpacity:
                    PushOpacity(c.Value);
                    break;
                case DrawKind.PopOpacity:
                    PopOpacity();
                    break;
                default:
                    break;
            }
        }

        SyncPages(glyphs.Pages, AtlasKind.Glyphs);
        SyncPages(art.Pages, AtlasKind.Art);
        DropReleased(art);
    }

    private void DrawArt(in DrawCommand c, ArtAtlas art, float scale)
    {
        if (_skip > 0)
        {
            return;
        }

        var tiled = c.TileSize != default;
        ArtRegion region;
        AtlasStatus status;
        if (c.Resource is null)
        {
            region = default;
            status = AtlasStatus.UnknownResource;
        }
        else if (tiled)
        {
            // A vector tile is rasterized at the tile's own whole-pixel size, so the pattern is crisp at every UI scale.
            status = art.TryGet(c.Resource, (int)Math.Clamp(MathF.Round(c.TileSize.X), 1, int.MaxValue), (int)Math.Clamp(MathF.Round(c.TileSize.Y), 1, int.MaxValue), out region);
            if (status is AtlasStatus.TooLarge or AtlasStatus.Full)
            {
                status = art.TryGetScaled(c.Resource, scale, out region);
            }
        }
        else if (c.Slice.IsZero)
        {
            status = art.TryGetFitted(c.Resource, c.Bounds.Width, c.Bounds.Height, scale, out region);
        }
        else
        {
            status = art.TryGetScaled(c.Resource, scale, out region);
        }

        if (status != AtlasStatus.Ok)
        {
            MissingArt++;
            Fill(c.Bounds, new Color(255, 0, 255, 255), c.Radii);
            return;
        }

        var group = _r.GroupOf(ArtTexture(art, region).Texture);
        if (tiled)
        {
            // Kind 5 wraps inside the region in the shader, so the whole pattern is one instance however many tiles it has.
            var grad = new Vector4((c.Repeat & TileAxes.X) != 0 ? 1 : 0, (c.Repeat & TileAxes.Y) != 0 ? 1 : 0, region.Width, region.Height);
            var span = new Vector4(c.TileOrigin.X, c.TileOrigin.Y, c.TileSize.X, c.TileSize.Y);
            Emit(5, c.Bounds, c.Radii, span, c.Color, c.Color, 0, grad, new Vector4(region.U0, region.V0, region.U1 - region.U0, region.V1 - region.V0), group, true);
            return;
        }

        if (c.Slice.IsZero)
        {
            Emit(3, c.Bounds, c.Radii, default, c.Color, c.Color, 0, default, new Vector4(region.U0, region.V0, region.U1 - region.U0, region.V1 - region.V0), group, true);
            return;
        }

        // Nine-slice: corners keep their size, edges stretch along one axis, the middle stretches both. The destination insets are the command's
        // (screen pixels); the source insets are the art's own (in raster pixels), or the same numbers when the art has none. A box too small
        // for its corners shrinks them together.
        var b = c.Bounds;
        var l = c.Slice.Left;
        var t = c.Slice.Top;
        var r = c.Slice.Right;
        var bt = c.Slice.Bottom;
        var fx = l + r > b.Width && l + r > 0 ? b.Width / (l + r) : 1f;
        var fy = t + bt > b.Height && t + bt > 0 ? b.Height / (t + bt) : 1f;
        var src = region.Slice.IsZero ? new NineSlice(l, t, r, bt) : region.Slice;
        Span<float> dx = [b.X, b.X + (l * fx), b.X + b.Width - (r * fx), b.X + b.Width];
        Span<float> dy = [b.Y, b.Y + (t * fy), b.Y + b.Height - (bt * fy), b.Y + b.Height];
        var du = (region.U1 - region.U0) / region.Width;
        var dv = (region.V1 - region.V0) / region.Height;
        Span<float> su = [region.U0, region.U0 + (src.Left * du), region.U1 - (src.Right * du), region.U1];
        Span<float> sv = [region.V0, region.V0 + (src.Top * dv), region.V1 - (src.Bottom * dv), region.V1];
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var w = dx[col + 1] - dx[col];
                var h = dy[row + 1] - dy[row];
                if (w <= 0 || h <= 0)
                {
                    continue;
                }

                var uv = new Vector4(su[col], sv[row], su[col + 1] - su[col], sv[row + 1] - sv[row]);
                Emit(3, new Rect(dx[col], dy[row], w, h), default, default, c.Color, c.Color, 0, default, uv, group, true);
            }
        }
    }

    private void DrawText(DisplayList list, in DrawCommand c, TextQuadBuilder text, GlyphAtlas glyphs)
    {
        if (_skip > 0)
        {
            return;
        }

        _glyphs.Clear();
        _ = text.Build(list, c.TextRun, _glyphs);
        // Glyph pages are not sRGB art: the coverage is multiplied into the run's colour.
        foreach (var g in _glyphs)
        {
            var page = Page(glyphs.Pages, AtlasKind.Glyphs, g.Page);
            var uv = new Vector4(g.U0, g.V0, g.U1 - g.U0, g.V1 - g.V0);
            EmitRaw(4, new Rect(g.X0, g.Y0, g.X1 - g.X0, g.Y1 - g.Y0), default, default, g.Color, g.Color, 0, default, uv, _r.GroupOf(page.Texture), 0);
        }
    }

    // The texture a region lives in: its shared page, or the texture of its own that a large raster gets (created and filled on first use).
    private PageTexture ArtTexture(ArtAtlas art, in ArtRegion region)
    {
        if (region.Standalone == 0)
        {
            return Page(art.Pages, AtlasKind.Art, region.Page);
        }

        if (!art.TryGetStandalone(region.Standalone, out var data))
        {
            throw new InvalidOperationException("A standalone art texture was released while the frame still draws it.");
        }

        if (!_standalone.TryGetValue(region.Standalone, out var tex))
        {
            tex = new PageTexture { Texture = _r.CreateRaw(data.Width, data.Height, GpuFormat.Rgba8UnormSrgb, TextureFilter.Linear), Width = data.Width, Height = data.Height, Version = 0 };
            _standalone[region.Standalone] = tex;
        }

        if (data.NeedsUpload)
        {
            _r.Device.WriteTexture(tex.Texture.Texture, 0, 0, data.Pixels);
            data.MarkUploaded();
        }

        return tex;
    }

    private void DropReleased(ArtAtlas art)
    {
        art.TakeReleased(_released);
        foreach (var handle in _released)
        {
            if (_standalone.Remove(handle, out var tex))
            {
                tex.Texture.Dispose();
            }
        }

        _released.Clear();
    }

    private PageTexture Page(AtlasPages pages, AtlasKind kind, int index)
    {
        var list = _pages[(int)kind];
        while (list.Count <= index)
        {
            list.Add(null);
        }

        var page = list[index];
        if (page is null)
        {
            var format = kind == AtlasKind.Glyphs ? GpuFormat.R8Unorm : GpuFormat.Rgba8UnormSrgb;
            var info = pages[index];
            page = new PageTexture { Texture = _r.CreateRaw(info.Width, info.Height, format, TextureFilter.Linear), Width = info.Width, Height = info.Height };
            list[index] = page;
        }

        return page;
    }

    /// <summary>Uploads every page that changed since it was last taken. The GPU layer writes whole levels, so a changed page goes up whole.</summary>
    private void SyncPages(AtlasPages pages, AtlasKind kind)
    {
        for (var i = 0; i < pages.PageCount; i++)
        {
            var p = pages[i];
            var tex = Page(pages, kind, i);
            if (p.TakeDirty().IsEmpty && tex.Version >= 0)
            {
                continue;
            }

            _r.Device.WriteTexture(tex.Texture.Texture, 0, 0, p.Pixels);
            tex.Version = p.Version;
        }
    }

    // ---- emission ----

    private float Scale() => MathF.Max(MathF.Sqrt(MathF.Abs((_m.M11 * _m.M22) - (_m.M12 * _m.M21))), 1e-4f);

    private void Emit(uint kind, Rect bounds, CornerRadii radii, Vector4 widths, Color c0, Color c1, uint gradKind, Vector4 grad, Vector4 uv, GpuBindGroup texture, bool pad) =>
        EmitRaw(kind, bounds, radii, widths, c0, c1, gradKind, grad, uv, texture, pad ? 1.5f / Scale() : 0f);

    private void EmitRaw(uint kind, Rect bounds, CornerRadii radii, Vector4 widths, Color c0, Color c1, uint gradKind, Vector4 grad, Vector4 uv, GpuBindGroup texture, float pad)
    {
        if (_skip > 0)
        {
            return;
        }

        var clip = _clips[^1];
        if (clip.X1 <= clip.X0 || clip.Y1 <= clip.Y0)
        {
            return;
        }

        // Skip shapes wholly outside the clip box.
        var p0 = Vector2.Transform(new Vector2(bounds.X - pad, bounds.Y - pad), _m);
        var p1 = Vector2.Transform(new Vector2(bounds.X + bounds.Width + pad, bounds.Y - pad), _m);
        var p2 = Vector2.Transform(new Vector2(bounds.X - pad, bounds.Y + bounds.Height + pad), _m);
        var p3 = Vector2.Transform(new Vector2(bounds.X + bounds.Width + pad, bounds.Y + bounds.Height + pad), _m);
        if (MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)) < clip.X0 || MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)) > clip.X1
            || MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)) < clip.Y0 || MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)) > clip.Y1)
        {
            return;
        }

        var item = new UiInstance
        {
            Rect = new Vector4(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            M = new Vector4(_m.M11, _m.M12, _m.M21, _m.M22),
            T = new Vector2(_m.M31, _m.M32),
            Radii = new Vector4(radii.TopLeft, radii.TopRight, radii.BottomRight, radii.BottomLeft),
            Widths = widths,
            Uv = uv,
            C0 = Colors.Pack(c0),
            C1 = Colors.Pack(c1),
            Grad = grad,
            Kind = kind,
            GradKind = gradKind,
            ClipId = clip.Id,
            Pad = pad,
        };
        _r.EmitUi(item, texture, clip.X0, clip.Y0, clip.X1 - clip.X0, clip.Y1 - clip.Y0);
    }

    /// <summary>Releases the atlas page textures.</summary>
    public void Dispose()
    {
        foreach (var t in _standalone.Values)
        {
            t.Texture.Dispose();
        }

        _standalone.Clear();
        foreach (var list in _pages)
        {
            foreach (var p in list)
            {
                p?.Texture.Dispose();
            }

            list.Clear();
        }
    }
}
