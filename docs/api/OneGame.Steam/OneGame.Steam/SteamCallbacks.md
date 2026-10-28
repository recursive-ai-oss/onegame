[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamCallbacks class

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public sealed class SteamCallbacks
```

Delivers Steam's callbacks and async call results to typed handlers. It drives the library's manual-dispatch loop, so everything runs on whichever thread calls [`SteamCallbacks.Pump()`](SteamCallbacks.md#m-pump), and nothing runs between calls.

## Properties

| Member | Description |
| --- | --- |
| [`PendingCalls`](#m-pendingcalls) | Number of async calls still waiting for their result. |

### <a id="m-pendingcalls"></a>PendingCalls

```csharp
public int PendingCalls { get; }
```

Number of async calls still waiting for their result.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Pump()`](#m-pump) | Runs the library's dispatch loop and invokes handlers for everything that arrived since the last call. |
| [`Subscribe<T>(Action<T>)`](#m-subscribe-t-action-t) | Calls `handler` from [`SteamCallbacks.Pump()`](SteamCallbacks.md#m-pump) every time Steam broadcasts a `T`. |
| [`Track<T>(ulong, Action<T, bool>)`](#m-track-t-ulong-action-t-bool) | Waits for the result of an async call. |

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Runs the library's dispatch loop and invokes handlers for everything that arrived since the last call.

### <a id="m-subscribe-t-action-t"></a>Subscribe&lt;T&gt;(Action&lt;T&gt;)

```csharp
public void Subscribe<T>(Action<T> handler) where T : struct, ISteamCallbackData<T>;
```

Calls `handler` from [`SteamCallbacks.Pump()`](SteamCallbacks.md#m-pump) every time Steam broadcasts a `T`.

**Parameters**

- `handler` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<T>`)

### <a id="m-track-t-ulong-action-t-bool"></a>Track&lt;T&gt;(ulong, Action&lt;T, bool&gt;)

```csharp
public void Track<T>(ulong call, Action<T, bool> handler) where T : struct, ISteamCallbackData<T>;
```

Waits for the result of an async call. `handler` runs once, from [`SteamCallbacks.Pump()`](SteamCallbacks.md#m-pump), with the result and whether the call failed at the I/O level (in which case the value is default).

**Parameters**

- `call` (`ulong`): The handle the async function returned. Zero means the call was refused, and the handler runs at once as failed.
- `handler` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, bool>`): Receives the result.
