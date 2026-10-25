using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OneGame.Gpu;

namespace OneGame.Render3D;

// These mirror the constant and storage buffer layouts in Shaders/Frame.hlsli and Shaders/Pbr.hlsl. Every member is a matrix or a
// four-component vector, so the HLSL packing rules cannot move anything.

[StructLayout(LayoutKind.Sequential)]
internal struct UInt4
{
    public uint X;
    public uint Y;
    public uint Z;
    public uint W;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FrameConstants
{
    public Matrix4x4 View;
    public Matrix4x4 Proj;
    public Matrix4x4 ViewProj;
    public Matrix4x4 InvProj;
    public Matrix4x4 InvViewProj;
    public Vector4 CameraPos;
    public Vector4 Grid;
    public Vector4 Slice;
    public Vector4 EnvA;
    public Vector4 ViewportInfo;
    public UInt4 Counts;
    public Vector4 Sky;
    public Vector4 ShadowInfo;
    public Vector4 AoInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LightRecord
{
    public Vector4 PosRange;
    public Vector4 DirType;
    public Vector4 Color;
    public Vector4 Spot;
}

/// <summary>One object's constants, padded to the dynamic offset alignment.</summary>
[StructLayout(LayoutKind.Sequential, Size = (int)GpuDevice.DynamicOffsetAlignment)]
internal struct ObjectConstants
{
    public Matrix4x4 Model;
    public Matrix4x4 NormalMat;
    public UInt4 Info;
}

[InlineArray(10)]
internal struct TransformRows
{
    private Vector4 _first;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MaterialConstants
{
    public Vector4 BaseColor;
    public Vector4 Emissive;
    public Vector4 P0;
    public Vector4 P1;
    public UInt4 Flags;
    public TransformRows Xf;
    public Vector4 StyleA;
    public Vector4 StyleB;
    public Vector4 OutlineColor;
}

[StructLayout(LayoutKind.Sequential, Size = (int)GpuDevice.DynamicOffsetAlignment)]
internal struct PostConstants
{
    public Vector4 P;
}

[InlineArray(24)]
internal struct ShadowColumns
{
    private Vector4 _first;
}

[InlineArray(6)]
internal struct ShadowRects
{
    private Vector4 _first;
}

/// <summary>One shadow-casting light as the pixel shader reads it (Pbr.hlsl, <c>ShadowData</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ShadowRecord
{
    public ShadowColumns Col;
    public ShadowRects Rect;
    public Vector4 Splits;
    public Vector4 Texels;
    public Vector4 P0;
    public Vector4 P1;
}

/// <summary>Frame constants padded to the uniform offset alignment, so the shadow tiles' constants sit side by side in one buffer.</summary>
[StructLayout(LayoutKind.Sequential, Size = Renderer3D.FrameStride)]
internal struct FrameSlot
{
    public FrameConstants Frame;
}

/// <summary>Per-instance vertex data: the instance matrix as three rows of a 3x4 (x' = dot((p, 1), Rx)) and a colour.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct InstanceGpu
{
    public Vector4 Rx;
    public Vector4 Ry;
    public Vector4 Rz;
    public Vector4 Color;
}

/// <summary>Ambient occlusion pass constants (Ssao.hlsl).</summary>
[StructLayout(LayoutKind.Sequential, Size = (int)GpuDevice.DynamicOffsetAlignment)]
internal struct SsaoConstants
{
    public Vector4 A;
    public Vector4 B;
    public Vector4 C;
}
