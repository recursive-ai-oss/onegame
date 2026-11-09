[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FrameTiming struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FrameTiming`](FrameTiming.md)&gt;

```csharp
public readonly struct FrameTiming : IEquatable<FrameTiming>
```

How long one frame took on the CPU.

## Constructors

| Member | Description |
| --- | --- |
| [`FrameTiming(long, double, double, double)`](#m-ctor-long-double-double-double) | How long one frame took on the CPU. |

### <a id="m-ctor-long-double-double-double"></a>FrameTiming(long, double, double, double)

```csharp
public FrameTiming(long Frame, double CpuMs, double SimMs, double DrawMs);
```

How long one frame took on the CPU.

**Parameters**

- `Frame` (`long`): The frame number ([`GameInstance.Frame`](GameInstance.md#m-frame)).
- `CpuMs` (`double`): The whole frame: input, simulation, view update and drawing, in milliseconds.
- `SimMs` (`double`): The part spent stepping the simulation (or taking in the server's ticks), in milliseconds.
- `DrawMs` (`double`): The part spent building and submitting the picture, in milliseconds; 0 in a headless run.

## Properties

| Member | Description |
| --- | --- |
| [`CpuMs`](#m-cpums) | The whole frame: input, simulation, view update and drawing, in milliseconds. |
| [`DrawMs`](#m-drawms) | The part spent building and submitting the picture, in milliseconds; 0 in a headless run. |
| [`Frame`](#m-frame) | The frame number ([`GameInstance.Frame`](GameInstance.md#m-frame)). |
| [`SimMs`](#m-simms) | The part spent stepping the simulation (or taking in the server's ticks), in milliseconds. |

### <a id="m-cpums"></a>CpuMs

```csharp
public double CpuMs { get; init; }
```

The whole frame: input, simulation, view update and drawing, in milliseconds.

**Type:** `double`

### <a id="m-drawms"></a>DrawMs

```csharp
public double DrawMs { get; init; }
```

The part spent building and submitting the picture, in milliseconds; 0 in a headless run.

**Type:** `double`

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

The frame number ([`GameInstance.Frame`](GameInstance.md#m-frame)).

**Type:** `long`

### <a id="m-simms"></a>SimMs

```csharp
public double SimMs { get; init; }
```

The part spent stepping the simulation (or taking in the server's ticks), in milliseconds.

**Type:** `double`
