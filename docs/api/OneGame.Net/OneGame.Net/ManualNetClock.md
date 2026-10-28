[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# ManualNetClock class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Inherits:** [`NetClock`](NetClock.md)

```csharp
public sealed class ManualNetClock : NetClock
```

A clock the caller advances by hand.

## Constructors

| Member | Description |
| --- | --- |
| [`ManualNetClock()`](#m-ctor) |  |

### <a id="m-ctor"></a>ManualNetClock()

```csharp
public ManualNetClock();
```

## Properties

| Member | Description |
| --- | --- |
| [`NowMs`](#m-nowms) | Milliseconds since an arbitrary origin; never goes backwards. |

### <a id="m-nowms"></a>NowMs

```csharp
public override long NowMs { get; }
```

Milliseconds since an arbitrary origin; never goes backwards.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Advance(long)`](#m-advance-long) | Moves time forward. |

### <a id="m-advance-long"></a>Advance(long)

```csharp
public void Advance(long ms);
```

Moves time forward.

**Parameters**

- `ms` (`long`): Milliseconds to add; must not be negative.
