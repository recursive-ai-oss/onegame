using System.Runtime.InteropServices;

namespace OneGame.Render3D;

/// <summary>How debug primitive widths and point sizes are measured.</summary>
public enum DebugSize3D
{
    /// <summary>Output pixels, independent of distance.</summary>
    Pixels,
    /// <summary>World units, projected at each endpoint.</summary>
    World,
}

/// <summary>Whether scene surfaces hide debug geometry.</summary>
public enum DebugDepth3D
{
    /// <summary>Test scene depth, without writing depth.</summary>
    DepthTested,
    /// <summary>Draw after all depth-tested geometry, ignoring depth.</summary>
    AlwaysOnTop,
}

/// <summary>Reusable immediate-mode debug primitives. Colours are linear straight-alpha; sizes must be finite and positive.
/// Clear before refilling each frame. Within each depth mode primitives blend in submission order, not distance order.</summary>
public sealed class DebugDraw3D
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Primitive
    {
        public Vector4 FromSize;
        public Vector4 ToUnits;
        public Vector4 Color;
        public Vector4 Kind;
    }

    internal readonly List<Primitive> Tested = [];
    internal readonly List<Primitive> Top = [];

    /// <summary>Number of submitted primitives.</summary>
    public int Count => Tested.Count + Top.Count;

    /// <summary>Removes all primitives, retaining storage.</summary>
    public void Clear() { Tested.Clear(); Top.Clear(); }

    /// <summary>Appends another list, retaining its per-mode submission order.</summary>
    public void Append(DebugDraw3D source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Tested.AddRange(source.Tested);
        Top.AddRange(source.Top);
    }

    /// <summary>Adds a camera-facing thick line. Coincident endpoints produce no pixels.</summary>
    public void Line(Vector3 from, Vector3 to, Vector4 color, float width = 1, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested) => Add(from, to, color, width, units, mode, 0);

    /// <summary>Adds a camera-facing square centred on a world position.</summary>
    public void Point(Vector3 position, Vector4 color, float size = 4, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested) => Add(position, position, color, size, units, mode, 1);

    /// <summary>Adds a line and triangular head at its destination. Head length is six widths, capped at half the projected shaft length.</summary>
    public void Arrow(Vector3 from, Vector3 to, Vector4 color, float width = 1, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested) => Add(from, to, color, width, units, mode, 2);

    private void Add(Vector3 from, Vector3 to, Vector4 color, float size, DebugSize3D units, DebugDepth3D mode, int kind)
    {
        if (!float.IsFinite(size) || size <= 0 || !Finite(from) || !Finite(to) ||
            !float.IsFinite(color.X) || !float.IsFinite(color.Y) || !float.IsFinite(color.Z) || !float.IsFinite(color.W) || color.W < 0 || color.W > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Positions and colours must be finite, size positive, and alpha in 0..1.");
        }

        if (units is not (DebugSize3D.Pixels or DebugSize3D.World)) { throw new ArgumentOutOfRangeException(nameof(units)); }
        if (mode is not (DebugDepth3D.DepthTested or DebugDepth3D.AlwaysOnTop)) { throw new ArgumentOutOfRangeException(nameof(mode)); }
        var list = mode == DebugDepth3D.DepthTested ? Tested : Top;
        list.Add(new Primitive { FromSize = new Vector4(from, size), ToUnits = new Vector4(to, (float)units), Color = new Vector4(new Vector3(color.X, color.Y, color.Z) * color.W, color.W), Kind = new Vector4(kind, 0, 0, 0) });
    }

    private static bool Finite(Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
}
