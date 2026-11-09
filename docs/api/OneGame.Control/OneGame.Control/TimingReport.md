[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# TimingReport record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TimingReport`](TimingReport.md)&gt;

```csharp
public sealed record TimingReport : IEquatable<TimingReport>
```

Frame timing over the last [`FrameTimings.Capacity`](../../OneGame.Host/OneGame.Host/FrameTimings.md#m-capacity) frames. Timing runs while the debug panel is visible (`overlay` with `visible=true`); it costs nothing otherwise.

## Constructors

| Member | Description |
| --- | --- |
| [`TimingReport(int, double, double, double, double, double, double, double, string, double?, GpuPassReport[], double[])`](#m-ctor-int-double-double-double-double-double-double-double-string-double-gpupassreport-double) | Frame timing over the last [`FrameTimings.Capacity`](../../OneGame.Host/OneGame.Host/FrameTimings.md#m-capacity) frames. |

### <a id="m-ctor-int-double-double-double-double-double-double-double-string-double-gpupassreport-double"></a>TimingReport(int, double, double, double, double, double, double, double, string, double?, GpuPassReport\[\], double\[\])

```csharp
public TimingReport(int Samples, double Cpu, double CpuAvg, double CpuMax, double Sim, double SimAvg, double SimMax, double Draw, string Gpu, double? GpuMs, GpuPassReport[] GpuPasses, double[] CpuHistory);
```

Frame timing over the last [`FrameTimings.Capacity`](../../OneGame.Host/OneGame.Host/FrameTimings.md#m-capacity) frames. Timing runs while the debug panel is visible (`overlay` with `visible=true`); it costs nothing otherwise.

**Parameters**

- `Samples` (`int`): Frames in the history.
- `Cpu` (`double`): Newest frame, whole CPU time in milliseconds.
- `CpuAvg` (`double`): Average CPU frame time over the history.
- `CpuMax` (`double`): Longest CPU frame time over the history.
- `Sim` (`double`): Newest frame's time stepping the simulation, in milliseconds.
- `SimAvg` (`double`): Average simulation time over the history.
- `SimMax` (`double`): Longest simulation time over the history.
- `Draw` (`double`): Newest frame's time drawing and submitting, in milliseconds (0 when headless).
- `Gpu` (`string`): `ready` (pass times follow), `unsupported` (the backend has no timestamps: Metal, WebGPU) or `none` (no device or no frame drawn yet).
- `GpuMs` (`double?`): Sum of the pass times in milliseconds; null unless `Gpu` is `ready`.
- `GpuPasses` ([`GpuPassReport`](GpuPassReport.md)\[\]): GPU time per pass of the newest finished frame.
- `CpuHistory` (`double[]`): CPU frame times in milliseconds, oldest first.

## Properties

| Member | Description |
| --- | --- |
| [`Cpu`](#m-cpu) | Newest frame, whole CPU time in milliseconds. |
| [`CpuAvg`](#m-cpuavg) | Average CPU frame time over the history. |
| [`CpuHistory`](#m-cpuhistory) | CPU frame times in milliseconds, oldest first. |
| [`CpuMax`](#m-cpumax) | Longest CPU frame time over the history. |
| [`Draw`](#m-draw) | Newest frame's time drawing and submitting, in milliseconds (0 when headless). |
| [`Gpu`](#m-gpu) | `ready` (pass times follow), `unsupported` (the backend has no timestamps: Metal, WebGPU) or `none` (no device or no frame drawn yet). |
| [`GpuMs`](#m-gpums) | Sum of the pass times in milliseconds; null unless `Gpu` is `ready`. |
| [`GpuPasses`](#m-gpupasses) | GPU time per pass of the newest finished frame. |
| [`Samples`](#m-samples) | Frames in the history. |
| [`Sim`](#m-sim) | Newest frame's time stepping the simulation, in milliseconds. |
| [`SimAvg`](#m-simavg) | Average simulation time over the history. |
| [`SimMax`](#m-simmax) | Longest simulation time over the history. |

### <a id="m-cpu"></a>Cpu

```csharp
public double Cpu { get; init; }
```

Newest frame, whole CPU time in milliseconds.

**Type:** `double`

### <a id="m-cpuavg"></a>CpuAvg

```csharp
public double CpuAvg { get; init; }
```

Average CPU frame time over the history.

**Type:** `double`

### <a id="m-cpuhistory"></a>CpuHistory

```csharp
public double[] CpuHistory { get; init; }
```

CPU frame times in milliseconds, oldest first.

**Type:** `double[]`

### <a id="m-cpumax"></a>CpuMax

```csharp
public double CpuMax { get; init; }
```

Longest CPU frame time over the history.

**Type:** `double`

### <a id="m-draw"></a>Draw

```csharp
public double Draw { get; init; }
```

Newest frame's time drawing and submitting, in milliseconds (0 when headless).

**Type:** `double`

### <a id="m-gpu"></a>Gpu

```csharp
public string Gpu { get; init; }
```

`ready` (pass times follow), `unsupported` (the backend has no timestamps: Metal, WebGPU) or `none` (no device or no frame drawn yet).

**Type:** `string`

### <a id="m-gpums"></a>GpuMs

```csharp
public double? GpuMs { get; init; }
```

Sum of the pass times in milliseconds; null unless `Gpu` is `ready`.

**Type:** `double?`

### <a id="m-gpupasses"></a>GpuPasses

```csharp
public GpuPassReport[] GpuPasses { get; init; }
```

GPU time per pass of the newest finished frame.

**Type:** [`GpuPassReport`](GpuPassReport.md)\[\]

### <a id="m-samples"></a>Samples

```csharp
public int Samples { get; init; }
```

Frames in the history.

**Type:** `int`

### <a id="m-sim"></a>Sim

```csharp
public double Sim { get; init; }
```

Newest frame's time stepping the simulation, in milliseconds.

**Type:** `double`

### <a id="m-simavg"></a>SimAvg

```csharp
public double SimAvg { get; init; }
```

Average simulation time over the history.

**Type:** `double`

### <a id="m-simmax"></a>SimMax

```csharp
public double SimMax { get; init; }
```

Longest simulation time over the history.

**Type:** `double`
