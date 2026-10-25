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
}

[StructLayout(LayoutKind.Sequential, Size = (int)GpuDevice.DynamicOffsetAlignment)]
internal struct PostConstants
{
    public Vector4 P;
}
