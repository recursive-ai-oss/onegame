using System.Numerics;
using OneGame.Core;
using OneGame.Render3D;

namespace OneGame.Host;

public sealed partial class SceneWriter
{
    internal DebugDraw3D Debug { get; } = new();

    /// <summary>Whether the view is collecting debug geometry this frame.</summary>
    public bool DebugVisible { get; internal set; }

    /// <summary>Adds a debug line when debug drawing is enabled. Colour is linear straight-alpha; size is in the chosen units.</summary>
    public void DebugLine(Vector3 from, Vector3 to, Vector4 color, float width = 1, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested)
    {
        if (DebugVisible) { Debug.Line(from, to, color, width, units, mode); }
    }

    /// <summary>Adds a debug line at fixed-point positions when debug drawing is enabled.</summary>
    public void DebugLine(FVec3 from, FVec3 to, Vector4 color, float width = 1, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested)
        => DebugLine(ToVector(from), ToVector(to), color, width, units, mode);

    /// <summary>Adds a debug point when debug drawing is enabled. Colour is linear straight-alpha; size is in the chosen units.</summary>
    public void DebugPoint(Vector3 position, Vector4 color, float size = 4, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested)
    {
        if (DebugVisible) { Debug.Point(position, color, size, units, mode); }
    }

    /// <summary>Adds a debug point at fixed-point positions when debug drawing is enabled.</summary>
    public void DebugPoint(FVec3 position, Vector4 color, float size = 4, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested)
        => DebugPoint(ToVector(position), color, size, units, mode);

    /// <summary>Adds a debug arrow when debug drawing is enabled. Colour is linear straight-alpha; size is in the chosen units.</summary>
    public void DebugArrow(Vector3 from, Vector3 to, Vector4 color, float width = 1, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested)
    {
        if (DebugVisible) { Debug.Arrow(from, to, color, width, units, mode); }
    }

    /// <summary>Adds a debug arrow at fixed-point positions when debug drawing is enabled.</summary>
    public void DebugArrow(FVec3 from, FVec3 to, Vector4 color, float width = 1, DebugSize3D units = DebugSize3D.Pixels, DebugDepth3D mode = DebugDepth3D.DepthTested)
        => DebugArrow(ToVector(from), ToVector(to), color, width, units, mode);
}
