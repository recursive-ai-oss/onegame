[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuFormat enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum GpuFormat : int
```

Pixel formats of textures and render targets. Chosen to exist on Vulkan, Metal and WebGPU alike.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-undefined"></a>`Undefined` | `0` | No format. |
| <a id="m-r8unorm"></a>`R8Unorm` | `1` | 8-bit single channel, normalized. |
| <a id="m-rg8unorm"></a>`Rg8Unorm` | `2` | 8-bit two channels, normalized. |
| <a id="m-rgba8unorm"></a>`Rgba8Unorm` | `3` | 8-bit RGBA, normalized, linear. |
| <a id="m-rgba8unormsrgb"></a>`Rgba8UnormSrgb` | `4` | 8-bit RGBA with sRGB encoding on color channels. |
| <a id="m-bgra8unorm"></a>`Bgra8Unorm` | `5` | 8-bit BGRA, normalized, linear. |
| <a id="m-bgra8unormsrgb"></a>`Bgra8UnormSrgb` | `6` | 8-bit BGRA with sRGB encoding on color channels. |
| <a id="m-r16float"></a>`R16Float` | `7` | 16-bit float single channel. |
| <a id="m-rgba16float"></a>`Rgba16Float` | `8` | 16-bit float RGBA. |
| <a id="m-r32float"></a>`R32Float` | `9` | 32-bit float single channel. |
| <a id="m-rg32float"></a>`Rg32Float` | `10` | 32-bit float two channels. |
| <a id="m-rgba32float"></a>`Rgba32Float` | `11` | 32-bit float RGBA. |
| <a id="m-r32uint"></a>`R32Uint` | `12` | 32-bit unsigned integer single channel. |
| <a id="m-depth32float"></a>`Depth32Float` | `13` | 32-bit float depth. |
| <a id="m-depth32floatstencil8"></a>`Depth32FloatStencil8` | `14` | 32-bit float depth with 8-bit stencil. |
| <a id="m-rg16float"></a>`Rg16Float` | `15` | 16-bit float, two channels. |
| <a id="m-rg11b10float"></a>`Rg11B10Float` | `16` | Packed 11/11/10-bit unsigned float RGB: a compact HDR color format. Not usable as a storage texture. |
| <a id="m-rgb10a2unorm"></a>`Rgb10A2Unorm` | `17` | Packed 10/10/10-bit RGB with 2-bit alpha, normalized. |
| <a id="m-depth16unorm"></a>`Depth16Unorm` | `18` | 16-bit normalized depth. |
