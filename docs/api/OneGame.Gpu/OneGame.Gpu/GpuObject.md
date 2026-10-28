[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuObject class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public abstract class GpuObject : IDisposable
```

Base class of every GPU object. Disposing releases the object once the GPU has finished with it.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuObject()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuObject()

```csharp
protected GpuObject();
```

## Properties

| Member | Description |
| --- | --- |
| [`Label`](#m-label) | Name shown in debugging tools, if one was given. |

### <a id="m-label"></a>Label

```csharp
public string? Label { get; protected init; }
```

Name shown in debugging tools, if one was given.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Releases the object. |

### <a id="m-dispose"></a>Dispose()

```csharp
public abstract void Dispose();
```

Releases the object. The GPU may still be using it; the backend defers the actual release until it is done.
