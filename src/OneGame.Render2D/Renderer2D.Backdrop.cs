using System.Numerics;
using RecursiveAI.OneGpu;

namespace OneGame.Render2D;

public sealed partial class Renderer2D
{
    // Std deviation of the nine-tap kernel in Quad.hlsl/Ui.hlsl at a tap step of one texel.
    private const float KernelSigma = 1.689f;

    // Largest std deviation per separable pass, in texels of the reduced image; the image is halved until the pass fits.
    private const float MaxPassSigma = 2f;

    internal const float MaxBackdropSigma = 64f;

    /// <summary>Whether the target being drawn can be copied (frame targets and render textures can; a wrapped foreign texture may not).</summary>
    internal bool CanSnapshotCurrent => (_targets[_current].State.Texture.Usage & TextureUsage.CopySource) != 0;

    /// <summary>
    /// Records a Gaussian blur of what the current target holds at this point of the frame, for the pixels of the rectangle (rx, ry, rw, rh) (plus the
    /// reach of the blur). The target is copied whole (a texture copy needs matching sizes), reduced by halving until each separable pass is
    /// small, blurred horizontally and vertically over the region only, and the result is returned as a texture to sample by pixel position.
    /// Returns the temp target holding it; the caller draws it and then releases it with <see cref="ReleaseGroupTarget"/>.
    /// </summary>
    internal int RecordBackdropBlur(float sigma, int rx, int ry, int rw, int rh, out GpuBindGroup sample)
    {
        var source = _targets[_current].State.Texture;
        var w = (int)source.Width;
        var h = (int)source.Height;
        var reach = (int)MathF.Ceiling(3f * sigma);
        var x0 = Math.Max(rx - ((2 * reach) + 2), 0);
        var y0 = Math.Max(ry - ((2 * reach) + 2), 0);
        var x1 = Math.Min(rx + rw + (2 * reach) + 2, w);
        var y1 = Math.Min(ry + rh + (2 * reach) + 2, h);

        var snapshot = AcquireTemp(w, h);
        _ops.Add(new DrawOp { IsCopy = true, Target = _current, CopyTo = snapshot });

        var levels = 0;
        while (levels < 5 && sigma / (1 << levels) > MaxPassSigma)
        {
            levels++;
        }

        var read = snapshot;
        var lw = w;
        var lh = h;
        for (var i = 1; i <= levels; i++)
        {
            lw = Math.Max(1, w >> i);
            lh = Math.Max(1, h >> i);
            var next = AcquireTemp(lw, lh);
            EmitBackdropPass(next, read, Vector2.Zero, Region(x0, y0, x1, y1, i, lw, lh));
            ReleaseTemp(read);
            read = next;
        }

        var step = sigma / (1 << levels) / KernelSigma;
        var box = Region(x0, y0, x1, y1, levels, lw, lh);
        var across = AcquireTemp(lw, lh);
        EmitBackdropPass(across, read, new Vector2(step / lw, 0f), box);
        ReleaseTemp(read);
        var result = AcquireTemp(lw, lh);
        EmitBackdropPass(result, across, new Vector2(0f, step / lh), box);
        ReleaseTemp(across);
        sample = _targets[result].State.Sample!;
        return result;
    }

    // The pixels of a full-size rectangle at a reduced level, rounded outwards and kept inside the image.
    private static (int X, int Y, int W, int H) Region(int x0, int y0, int x1, int y1, int level, int lw, int lh)
    {
        var f = 1 << level;
        var a = Math.Clamp(x0 / f, 0, lw);
        var b = Math.Clamp((x1 + f - 1) / f, 0, lw);
        var c = Math.Clamp(y0 / f, 0, lh);
        var d = Math.Clamp((y1 + f - 1) / f, 0, lh);
        return (a, c, b - a, d - c);
    }

    private void EmitBackdropPass(int dest, int source, Vector2 tapStep, (int X, int Y, int W, int H) r)
    {
        if (r.W <= 0 || r.H <= 0)
        {
            return;
        }

        var white = Colors.Pack(new OneGame.UI.Color(255, 255, 255, 255));
        var item = new UiInstance
        {
            Rect = new Vector4(r.X, r.Y, r.W, r.H),
            M = new Vector4(1f, 0f, 0f, 1f),
            C0 = white,
            C1 = white,
            Grad = new Vector4(tapStep.X, tapStep.Y, 0f, 0f),
            Kind = 7,
        };
        EmitUiOn(dest, item, _targets[source].State.Sample!, r.X, r.Y, r.W, r.H);
    }
}
