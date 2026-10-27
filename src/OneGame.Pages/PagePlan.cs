using System.Numerics;
using OneGame.Gpu;

namespace OneGame.Pages;

internal enum StepKind : byte
{
    /// <summary>Instanced primitives drawn into <see cref="Step.Dst"/>.</summary>
    Draw,

    /// <summary>A layer laid over <see cref="Step.Dst"/> with an opacity.</summary>
    Layer,

    /// <summary>A layer added to <see cref="Step.Dst"/> (plus-lighter, glow).</summary>
    LayerAdd,

    /// <summary>A layer blended with a non-normal mode over <see cref="Step.Src2"/>; the result is a new layer.</summary>
    Blend,

    /// <summary>A mask layer folded into the coverage so far (<see cref="Step.Src2"/>); the result is a new layer.</summary>
    MaskCombine,

    /// <summary>Content multiplied by a coverage and laid over <see cref="Step.Dst"/>.</summary>
    MaskApply,

    /// <summary>Up to four colour filter functions; the result is a new layer.</summary>
    Color,

    /// <summary>One direction of a Gaussian blur; the result is a new layer.</summary>
    Blur,

    /// <summary>The offset, tinted shadow of a drop-shadow filter; the result is a new layer.</summary>
    DropTint,

    /// <summary>A layer over a second one; the result is a new layer.</summary>
    Over,

    /// <summary>A filtered copy of the parent inside a rounded rectangle; the result is a new layer.</summary>
    Backdrop,

    /// <summary>A layer under a projective transform, laid over <see cref="Step.Dst"/>.</summary>
    Project,

    /// <summary>A layer from a linear sRGB texture back into an encoded layer.</summary>
    Encode,
}

/// <summary>One pass of a plan. Layers are numbered slots; the graph decides the textures behind them.</summary>
internal struct Step
{
    public StepKind Kind;
    public int Dst;
    public int Src;
    public int Src2;
    public GpuTexture? External;
    public bool Clear;
    public int BatchFirst;
    public int BatchCount;
    public int Instance;
    public int Sx;
    public int Sy;
    public int Sw;
    public int Sh;
    public string Name;
}

/// <summary>A run of instances sharing a bind group and a scissor rectangle.</summary>
internal struct Batch
{
    public GpuBindGroup Group;
    public int First;
    public int Count;
    public int Sx;
    public int Sy;
    public int Sw;
    public int Sh;
}

/// <summary>Everything one call of <see cref="PageRenderer.AddPasses"/> draws: instances, clips, passes. Reused between frames.</summary>
internal sealed class PagePlan
{
    public PageInstance[] Instances = new PageInstance[1024];
    public int InstanceCount;
    public Step[] Steps = new Step[64];
    public int StepCount;
    public Batch[] Batches = new Batch[256];
    public int BatchCount;
    public Vector4[] Clips = new Vector4[5 * 64];
    public int ClipCount;
    public int SlotCount;
    public int[] SlotBounds = new int[4 * 32];
    public bool[] SlotWritten = new bool[32];
    public int Width;
    public int Height;
    public int Root;

    public GpuBuffer? InstanceBuffer;
    public ulong InstanceCapacity;
    public GpuBuffer? ClipBuffer;
    public ulong ClipCapacity;

    /// <summary>Counts of what the walk skipped because a limit was reached.</summary>
    public int Dropped;

    public void Reset(int width, int height)
    {
        InstanceCount = 0;
        StepCount = 0;
        BatchCount = 0;
        ClipCount = 0;
        SlotCount = 0;
        Dropped = 0;
        Width = width;
        Height = height;
    }

    public int NewSlot()
    {
        if (SlotCount == SlotWritten.Length)
        {
            Array.Resize(ref SlotWritten, SlotCount * 2);
            Array.Resize(ref SlotBounds, SlotCount * 8);
        }

        SlotWritten[SlotCount] = false;
        var b = SlotCount * 4;
        SlotBounds[b] = int.MaxValue;
        SlotBounds[b + 1] = int.MaxValue;
        SlotBounds[b + 2] = int.MinValue;
        SlotBounds[b + 3] = int.MinValue;
        return SlotCount++;
    }

    public ref PageInstance AddInstance()
    {
        if (InstanceCount == Instances.Length)
        {
            Array.Resize(ref Instances, InstanceCount * 2);
        }

        ref var i = ref Instances[InstanceCount++];
        i = default;
        return ref i;
    }

    public ref Step AddStep()
    {
        if (StepCount == Steps.Length)
        {
            Array.Resize(ref Steps, StepCount * 2);
        }

        ref var s = ref Steps[StepCount++];
        s = default;
        return ref s;
    }

    public ref Batch AddBatch()
    {
        if (BatchCount == Batches.Length)
        {
            Array.Resize(ref Batches, BatchCount * 2);
        }

        ref var b = ref Batches[BatchCount++];
        b = default;
        return ref b;
    }

    public void GrowBounds(int slot, int x0, int y0, int x1, int y1)
    {
        var b = slot * 4;
        SlotBounds[b] = Math.Min(SlotBounds[b], x0);
        SlotBounds[b + 1] = Math.Min(SlotBounds[b + 1], y0);
        SlotBounds[b + 2] = Math.Max(SlotBounds[b + 2], x1);
        SlotBounds[b + 3] = Math.Max(SlotBounds[b + 3], y1);
    }

    /// <summary>The device-pixel bounds of what was drawn into a slot, clipped to the target; empty (width 0) when nothing was.</summary>
    public (int X0, int Y0, int X1, int Y1) BoundsOf(int slot)
    {
        var b = slot * 4;
        var x0 = Math.Max(0, SlotBounds[b]);
        var y0 = Math.Max(0, SlotBounds[b + 1]);
        var x1 = Math.Min(Width, SlotBounds[b + 2]);
        var y1 = Math.Min(Height, SlotBounds[b + 3]);
        return x1 <= x0 || y1 <= y0 ? (0, 0, 0, 0) : (x0, y0, x1, y1);
    }
}
