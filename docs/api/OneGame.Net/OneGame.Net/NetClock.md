[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# NetClock class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public abstract class NetClock
```

The time source transports use for timeouts, retransmission and the conditioner. Tests swap in [`ManualNetClock`](ManualNetClock.md) to run seconds of network time instantly.

## Constructors

| Member | Description |
| --- | --- |
| [`NetClock()`](#m-ctor) |  |

### <a id="m-ctor"></a>NetClock()

```csharp
protected NetClock();
```

## Properties

| Member | Description |
| --- | --- |
| [`NowMs`](#m-nowms) | Milliseconds since an arbitrary origin; never goes backwards. |
| [`System`](#m-system) | The real clock. |

### <a id="m-nowms"></a>NowMs

```csharp
public abstract long NowMs { get; }
```

Milliseconds since an arbitrary origin; never goes backwards.

**Type:** `long`

### <a id="m-system"></a>System

```csharp
public static NetClock System { get; }
```

The real clock.

**Type:** [`NetClock`](NetClock.md)
