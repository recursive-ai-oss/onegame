using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>What a render stage is given for one frame.</summary>
/// <param name="Width">Target width in pixels.</param>
/// <param name="Height">Target height in pixels.</param>
/// <param name="Scale">UI scale the frame was laid out at.</param>
/// <param name="Tick">The simulation tick the frame shows.</param>
/// <param name="List">What the UI wants drawn.</param>
/// <param name="World">The simulation state, for stages that draw the world.</param>
/// <param name="Alpha">How far past <paramref name="Tick"/> the frame is, from 0 to just under 1, for stages that draw between ticks.</param>
public readonly record struct FrameInfo(int Width, int Height, float Scale, long Tick, DisplayList List, World World, Fixed Alpha = default);

/// <summary>
/// Draws one frame. The host owns the window, the swapchain and the offscreen targets and hands the stage a texture; a renderer
/// replaces the default by overriding <see cref="GameDefinition.CreateRenderStage"/>.
/// </summary>
public interface IRenderStage
{
    /// <summary>A short name shown in harness output.</summary>
    string Name { get; }

    /// <summary>Records and submits the draw for one frame into <paramref name="target"/> (a swapchain image or an offscreen texture).</summary>
    void Render(GpuDevice device, GpuTexture target, in FrameInfo frame);

    /// <summary>
    /// Produces the whole frame on the CPU as RGBA8, row-major, top row first. Used for captures on machines with no usable GPU.
    /// </summary>
    void RenderToPixels(Span<byte> rgba, in FrameInfo frame);

    /// <summary>Runs on the RGBA8 pixels read back from a captured GPU frame, after <see cref="Render"/>. Stages that draw everything on the GPU leave it empty.</summary>
    void PaintOver(Span<byte> rgba, in FrameInfo frame)
    {
    }

    /// <summary>The host is about to destroy a target (a capture texture, or all swapchain images on resize). Stages that cache per-target state drop it. <paramref name="only"/> null means every target.</summary>
    void ReleaseTargets(GpuTexture? only)
    {
    }

    /// <summary>True when the stage draws into sRGB targets (the host then makes its swapchain and capture textures sRGB).</summary>
    bool RequiresSrgb => false;

    /// <summary>The render graph the stage draws with, once it has drawn a frame; null for stages that do not use one.</summary>
    RenderGraph? Graph => null;
}

/// <summary>
/// The stand-in stage: the GPU clears the target to a flat color, and for captures the UI's rectangles, borders and text lines are
/// painted over the read-back pixels by a small deterministic CPU rasterizer. It exists so frames can be captured and compared
/// before the 2D renderer lands; a window shows only the clear color.
/// </summary>
public sealed class StubStage : IRenderStage
{
    /// <summary>The clear color (RGBA).</summary>
    public static readonly (byte R, byte G, byte B, byte A) Clear = (18, 20, 26, 255);

    /// <inheritdoc />
    public string Name => "stub";

    /// <inheritdoc />
    public void Render(GpuDevice device, GpuTexture target, in FrameInfo frame)
    {
        ArgumentNullException.ThrowIfNull(device);
        var enc = device.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, Clear.R / 255f, Clear.G / 255f, Clear.B / 255f, Clear.A / 255f));
        pass.EndPass();
        device.Submit(enc);
    }

    /// <inheritdoc />
    public void RenderToPixels(Span<byte> rgba, in FrameInfo frame)
    {
        for (var i = 0; i + 3 < rgba.Length; i += 4)
        {
            rgba[i] = Clear.R;
            rgba[i + 1] = Clear.G;
            rgba[i + 2] = Clear.B;
            rgba[i + 3] = Clear.A;
        }

        PaintOver(rgba, frame);
    }

    /// <inheritdoc />
    public void PaintOver(Span<byte> rgba, in FrameInfo frame) => DisplayListRaster.Paint(rgba, frame.Width, frame.Height, frame.List);
}

/// <summary>
/// A small CPU rasterizer for display lists: flat rectangles, borders and one bar per text line, with clips and group opacity.
/// Corner radii, images, shadows and transforms are ignored. Integer math only, so the output is identical on every machine.
/// </summary>
public static class DisplayListRaster
{
    /// <summary>Paints <paramref name="list"/> over <paramref name="rgba"/> (width × height, RGBA8).</summary>
    public static void Paint(Span<byte> rgba, int width, int height, DisplayList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        var clips = new Stack<(int X0, int Y0, int X1, int Y1)>();
        var opacity = new Stack<int>();
        var clip = (X0: 0, Y0: 0, X1: width, Y1: height);
        var alpha = 256;
        var runs = list.TextRuns;
        var lines = list.Lines;
        foreach (var c in list.Commands)
        {
            switch (c.Kind)
            {
                case DrawKind.Rect:
                    Fill(rgba, width, clip, c.Bounds.X, c.Bounds.Y, c.Bounds.Right, c.Bounds.Bottom, c.Color, alpha);
                    break;
                case DrawKind.Border:
                    var b = c.Bounds;
                    var w = c.Widths;
                    Fill(rgba, width, clip, b.X, b.Y, b.Right, b.Y + w.Top, c.Color, alpha);
                    Fill(rgba, width, clip, b.X, b.Bottom - w.Bottom, b.Right, b.Bottom, c.Color, alpha);
                    Fill(rgba, width, clip, b.X, b.Y + w.Top, b.X + w.Left, b.Bottom - w.Bottom, c.Color, alpha);
                    Fill(rgba, width, clip, b.Right - w.Right, b.Y + w.Top, b.Right, b.Bottom - w.Bottom, c.Color, alpha);
                    break;
                case DrawKind.Text:
                    // A text line shows as a thin bar in the middle of its line box, tinted by the text color.
                    var run = runs[c.TextRun];
                    for (var i = 0; i < run.LineCount; i++)
                    {
                        var l = lines[run.FirstLine + i];
                        var barHeight = MathF.Max(1, MathF.Round(l.Height * 0.3f));
                        var top = l.Y + ((l.Height - barHeight) / 2);
                        Fill(rgba, width, clip, l.X, top, l.X + l.Width, top + barHeight, c.Color, alpha);
                    }

                    break;
                case DrawKind.PushClip:
                    clips.Push(clip);
                    clip = Intersect(clip, c.Bounds, width, height);
                    break;
                case DrawKind.PopClip:
                    clip = clips.Count > 0 ? clips.Pop() : clip;
                    break;
                case DrawKind.PushOpacity:
                    opacity.Push(alpha);
                    alpha = (alpha * (int)MathF.Round(Math.Clamp(c.Value, 0f, 1f) * 256f)) >> 8;
                    break;
                case DrawKind.PopOpacity:
                    alpha = opacity.Count > 0 ? opacity.Pop() : alpha;
                    break;
                default:
                    break;
            }
        }
    }

    private static (int X0, int Y0, int X1, int Y1) Intersect((int X0, int Y0, int X1, int Y1) clip, Rect r, int width, int height)
    {
        var x0 = Math.Max(clip.X0, Round(r.X));
        var y0 = Math.Max(clip.Y0, Round(r.Y));
        var x1 = Math.Min(clip.X1, Round(r.Right));
        var y1 = Math.Min(clip.Y1, Round(r.Bottom));
        return (Math.Clamp(x0, 0, width), Math.Clamp(y0, 0, height), Math.Clamp(Math.Max(x0, x1), 0, width), Math.Clamp(Math.Max(y0, y1), 0, height));
    }

    private static int Round(float v) => (int)MathF.Floor(v + 0.5f);

    private static void Fill(Span<byte> rgba, int width, (int X0, int Y0, int X1, int Y1) clip, float fx0, float fy0, float fx1, float fy1, Color color, int groupAlpha)
    {
        var x0 = Math.Max(clip.X0, Round(fx0));
        var y0 = Math.Max(clip.Y0, Round(fy0));
        var x1 = Math.Min(clip.X1, Round(fx1));
        var y1 = Math.Min(clip.Y1, Round(fy1));
        var a = (color.A * groupAlpha) >> 8;
        if (a <= 0 || x1 <= x0 || y1 <= y0)
        {
            return;
        }

        for (var y = y0; y < y1; y++)
        {
            var o = ((y * width) + x0) * 4;
            for (var x = x0; x < x1; x++, o += 4)
            {
                if (a >= 255)
                {
                    rgba[o] = color.R;
                    rgba[o + 1] = color.G;
                    rgba[o + 2] = color.B;
                    rgba[o + 3] = 255;
                }
                else
                {
                    var ia = 255 - a;
                    rgba[o] = (byte)(((color.R * a) + (rgba[o] * ia) + 127) / 255);
                    rgba[o + 1] = (byte)(((color.G * a) + (rgba[o + 1] * ia) + 127) / 255);
                    rgba[o + 2] = (byte)(((color.B * a) + (rgba[o + 2] * ia) + 127) / 255);
                    rgba[o + 3] = (byte)Math.Min(255, rgba[o + 3] + (a * (255 - rgba[o + 3]) / 255));
                }
            }
        }
    }
}

/// <summary>Named capture sizes.</summary>
public static class Resolution
{
    /// <summary>Parses a size such as <c>720p</c>, <c>1080p</c>, <c>4k</c>, <c>phone</c> or <c>1280x720</c>.</summary>
    public static bool TryParse(string? text, out int width, out int height)
    {
        width = height = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        switch (text.Trim().ToLowerInvariant())
        {
            case "360p": (width, height) = (640, 360); return true;
            case "480p": (width, height) = (854, 480); return true;
            case "720p" or "hd": (width, height) = (1280, 720); return true;
            case "1080p" or "fhd": (width, height) = (1920, 1080); return true;
            case "1440p" or "qhd": (width, height) = (2560, 1440); return true;
            case "4k" or "2160p": (width, height) = (3840, 2160); return true;
            case "deck": (width, height) = (1280, 800); return true;
            case "phone": (width, height) = (390, 844); return true;
            case "tablet": (width, height) = (820, 1180); return true;
        }

        var x = text.IndexOf('x', StringComparison.OrdinalIgnoreCase);
        return x > 0
            && int.TryParse(text.AsSpan(0, x), NumberStyles.None, CultureInfo.InvariantCulture, out width)
            && int.TryParse(text.AsSpan(x + 1), NumberStyles.None, CultureInfo.InvariantCulture, out height)
            && width is > 0 and <= 16384 && height is > 0 and <= 16384;
    }

    /// <summary>Parses a size or throws.</summary>
    /// <exception cref="ArgumentException">The text is not a size.</exception>
    public static (int Width, int Height) Parse(string? text)
    {
        if (!TryParse(text, out var w, out var h))
        {
            throw new ArgumentException($"'{text}' is not a size. Use WIDTHxHEIGHT (1280x720) or a name: 360p 480p 720p 1080p 1440p 4k deck phone tablet.");
        }

        return (w, h);
    }
}
