[API reference](../../index.md) › [OneGame.Live](../index.md) › OneGame.Live

# ParentWatch class

**Namespace:** `OneGame.Live`  
**Package:** [`RecursiveAI.OneGame.Live`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class ParentWatch : IDisposable
```

Watches the process that started this one. Harnesses that launch several game processes pass `--parent <pid>`, so a test that crashes or is killed does not leave servers and clients running: when the parent is gone the watcher asks the process to stop, and exits it outright if that has not worked a few seconds later.

## Constructors

| Member | Description |
| --- | --- |
| [`ParentWatch(int, Action)`](#m-ctor-int-action) | Starts watching. |

### <a id="m-ctor-int-action"></a>ParentWatch(int, Action)

```csharp
public ParentWatch(int pid, Action stop);
```

Starts watching.

**Parameters**

- `pid` (`int`): The process to watch.
- `stop` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)): Called once, on a background thread, when the process is gone: it should make the main loop finish.

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).
