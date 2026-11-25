[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# EnvironmentLoad class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class EnvironmentLoad : IDisposable
```

An environment being made over several frames: the conversion runs off the render thread and the filtering passes are added a few per frame, while the game keeps drawing its current map. Start one with [`Renderer3D.CreateEnvironmentAsync()`](Renderer3D.md#m-createenvironmentasync-hdrimage-environmentloadoptions), keep drawing, and when [`EnvironmentLoad.IsReady`](EnvironmentLoad.md#m-isready) is true take the map with [`EnvironmentLoad.Take()`](EnvironmentLoad.md#m-take) and set it as `Environment.Map` (or `NextMap` and fade with `Blend`). The result is the same as [`Renderer3D.CreateEnvironment()`](Renderer3D.md#m-createenvironment-hdrimage-int) followed by [`Renderer3D.PrepareEnvironment()`](Renderer3D.md#m-prepareenvironment-environmentmap). Use it from the render thread only. Progress needs frames to be rendered through the renderer, and the panorama must not change until the load is ready.

## Properties

| Member | Description |
| --- | --- |
| [`Environment`](#m-environment) | The finished map, still owned by this load until [`EnvironmentLoad.Take()`](EnvironmentLoad.md#m-take). |
| [`Error`](#m-error) | The error that stopped the load, when [`EnvironmentLoad.State`](EnvironmentLoad.md#m-state) is [`EnvironmentLoadState.Failed`](EnvironmentLoadState.md#m-failed). |
| [`IsReady`](#m-isready) | True once every pass has been added: [`EnvironmentLoad.Take()`](EnvironmentLoad.md#m-take) will return the map. |
| [`IsRunning`](#m-isrunning) | True while the load still needs frames (it is converting, uploading or filtering). |
| [`Progress`](#m-progress) | How far along the load is, from 0 to 1 (conversion and upload count for a fifth, filtering for the rest, by estimated cost). |
| [`State`](#m-state) | Where the load is. |

### <a id="m-environment"></a>Environment

```csharp
public EnvironmentMap Environment { get; }
```

The finished map, still owned by this load until [`EnvironmentLoad.Take()`](EnvironmentLoad.md#m-take). Throws until [`EnvironmentLoad.IsReady`](EnvironmentLoad.md#m-isready).

**Type:** [`EnvironmentMap`](EnvironmentMap.md)

### <a id="m-error"></a>Error

```csharp
public Exception? Error { get; }
```

The error that stopped the load, when [`EnvironmentLoad.State`](EnvironmentLoad.md#m-state) is [`EnvironmentLoadState.Failed`](EnvironmentLoadState.md#m-failed).

**Type:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)?

### <a id="m-isready"></a>IsReady

```csharp
public bool IsReady { get; }
```

True once every pass has been added: [`EnvironmentLoad.Take()`](EnvironmentLoad.md#m-take) will return the map.

**Type:** `bool`

### <a id="m-isrunning"></a>IsRunning

```csharp
public bool IsRunning { get; }
```

True while the load still needs frames (it is converting, uploading or filtering).

**Type:** `bool`

### <a id="m-progress"></a>Progress

```csharp
public double Progress { get; }
```

How far along the load is, from 0 to 1 (conversion and upload count for a fifth, filtering for the rest, by estimated cost).

**Type:** `double`

### <a id="m-state"></a>State

```csharp
public EnvironmentLoadState State { get; }
```

Where the load is.

**Type:** [`EnvironmentLoadState`](EnvironmentLoadState.md)

## Methods

| Member | Description |
| --- | --- |
| [`Cancel()`](#m-cancel) | Stops a load that is still running and frees what it made; the previous map is untouched and frames in flight are not hurt. |
| [`Dispose()`](#m-dispose) | Cancels a running load, and frees a finished map that was never taken. |
| [`Take()`](#m-take) | Hands the finished map to the caller, who disposes it before the renderer. |

### <a id="m-cancel"></a>Cancel()

```csharp
public void Cancel();
```

Stops a load that is still running and frees what it made; the previous map is untouched and frames in flight are not hurt. Does nothing once ready.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Cancels a running load, and frees a finished map that was never taken. Safe at any time, also after the renderer is disposed.

### <a id="m-take"></a>Take()

```csharp
public EnvironmentMap Take();
```

Hands the finished map to the caller, who disposes it before the renderer. After this, disposing the load leaves the map alone.

**Returns:** [`EnvironmentMap`](EnvironmentMap.md)

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The load is not ready, or the map was taken already.
