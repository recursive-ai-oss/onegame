[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ReplayOptions class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class ReplayOptions
```

Options for [`Replayer.Replay()`](Replayer.md#m-replay-recording-world-replayoptions).

## Constructors

| Member | Description |
| --- | --- |
| [`ReplayOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>ReplayOptions()

```csharp
public ReplayOptions();
```

## Fields

| Member | Description |
| --- | --- |
| [`DefaultMaxTicks`](#m-defaultmaxticks) | The default for [`ReplayOptions.MaxTicks`](ReplayOptions.md#m-maxticks). |

### <a id="m-defaultmaxticks"></a>DefaultMaxTicks

```csharp
public const long DefaultMaxTicks = 2000000;
```

The default for [`ReplayOptions.MaxTicks`](ReplayOptions.md#m-maxticks).

**Type:** `long`

## Properties

| Member | Description |
| --- | --- |
| [`MaxTicks`](#m-maxticks) | The most ticks a replay may run, counted from the recording's start tick to its end tick. |
| [`OnTick`](#m-ontick) | Called after every replayed tick, for captures and assertions. |

### <a id="m-maxticks"></a>MaxTicks

```csharp
public long MaxTicks { get; init; }
```

The most ticks a replay may run, counted from the recording's start tick to its end tick. A recording that asks for more is refused before anything runs, so a hostile file cannot demand an endless simulation. The default is 2 000 000 ticks (about 9 hours at 60 ticks per second); raise it for longer soaks.

**Type:** `long`

### <a id="m-ontick"></a>OnTick

```csharp
public Action<World>? OnTick { get; init; }
```

Called after every replayed tick, for captures and assertions.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;?
