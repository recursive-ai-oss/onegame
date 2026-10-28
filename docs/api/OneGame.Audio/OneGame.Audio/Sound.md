[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# Sound class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public sealed class Sound
```

A decoded clip held in memory, ready to play any number of times at once.

## Properties

| Member | Description |
| --- | --- |
| [`DurationSeconds`](#m-durationseconds) | The clip length in seconds, or a negative number when the core can't tell. |
| [`Name`](#m-name) | The name given at load time, for diagnostics. |

### <a id="m-durationseconds"></a>DurationSeconds

```csharp
public double DurationSeconds { get; }
```

The clip length in seconds, or a negative number when the core can't tell.

**Type:** `double`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The name given at load time, for diagnostics.

**Type:** `string`
