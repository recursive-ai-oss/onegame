using System.Numerics;
using OneGame.Gpu;
using OneGame.UI;

namespace OneGame.Render2D;

public sealed partial class Renderer2D
{
    internal int FrameNumber => _frame;

    internal int FrameWidth => _width;

    internal int FrameHeight => _height;

    internal int CurrentTarget
    {
        get => _current;
        set => _current = value;
    }

    internal Texture2D WhiteTexture => _white;

    internal int AcquireGroupTarget() => AcquireTemp();

    internal void ReleaseGroupTarget(int target) => ReleaseTemp(target);

    internal GpuBindGroup GroupOf(Texture2D texture) => TextureGroup(texture);

    /// <summary>Blends an offscreen group onto its parent target with the group's opacity, inside a scissor rectangle.</summary>
    internal void EmitComposite(int source, int destination, float opacity, int sx, int sy, int sw, int sh)
    {
        var first = AppendQuad(FullScreen(opacity));
        AddQuadOp(_spritePipeline, _targets[source].State.Sample!, null, first, 1, destination, sx, sy, sw, sh);
    }

    internal void EmitUi(in UiInstance item, GpuBindGroup texture, int sx, int sy, int sw, int sh)
    {
        if (_uiCount == _uiItems.Length)
        {
            Array.Resize(ref _uiItems, _uiCount * 2);
        }

        var index = _uiCount;
        _uiItems[_uiCount++] = item;
        if (_ops.Count > 0)
        {
            ref var last = ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_ops)[^1];
            if (last.IsUi && last.Target == _current && last.Texture == texture && last.Sx == sx && last.Sy == sy && last.Sw == sw && last.Sh == sh
                && last.First + last.Count == index)
            {
                last.Count++;
                return;
            }
        }

        _ops.Add(new DrawOp { Target = _current, Pipeline = _uiPipeline, Texture = texture, IsUi = true, First = index, Count = 1, Sx = sx, Sy = sy, Sw = sw, Sh = sh });
    }

    /// <summary>Adds a rounded clip to the table. Returns 0 when the table is full (the clip then applies only through its scissor box).</summary>
    internal uint AddClip(in Vector4 rect, in Vector4 radii, in Matrix3x2 inverse, uint parent)
    {
        if (_clipCount == MaxClips)
        {
            return parent;
        }

        var b = _clipCount * 4;
        _clipData[b] = rect;
        _clipData[b + 1] = radii;
        _clipData[b + 2] = new Vector4(inverse.M11, inverse.M12, inverse.M21, inverse.M22);
        _clipData[b + 3] = new Vector4(inverse.M31, inverse.M32, parent, 0);
        return (uint)++_clipCount;
    }
}
