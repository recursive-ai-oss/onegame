[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# BusLoudnessStatus record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BusLoudnessStatus`](BusLoudnessStatus.md)&gt;

```csharp
public sealed record BusLoudnessStatus : IEquatable<BusLoudnessStatus>
```

One metered bus in [`AudioCaptureResult`](AudioCaptureResult.md).

## Constructors

| Member | Description |
| --- | --- |
| [`BusLoudnessStatus(int, string?, LoudnessStatus)`](#m-ctor-int-string-loudnessstatus) | One metered bus in [`AudioCaptureResult`](AudioCaptureResult.md). |

### <a id="m-ctor-int-string-loudnessstatus"></a>BusLoudnessStatus(int, string?, LoudnessStatus)

```csharp
public BusLoudnessStatus(int Index, string? Name, LoudnessStatus Loudness);
```

One metered bus in [`AudioCaptureResult`](AudioCaptureResult.md).

**Parameters**

- `Index` (`int`): The bus index.
- `Name` (`string?`): `music`, `sfx`, `ui` or null.
- `Loudness` ([`LoudnessStatus`](../../OneGame.Audio/OneGame.Audio/LoudnessStatus.md)): Its loudness since the last reset, measured before ducking and the master stage.

## Properties

| Member | Description |
| --- | --- |
| [`Index`](#m-index) | The bus index. |
| [`Loudness`](#m-loudness) | Its loudness since the last reset, measured before ducking and the master stage. |
| [`Name`](#m-name) | `music`, `sfx`, `ui` or null. |

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

The bus index.

**Type:** `int`

### <a id="m-loudness"></a>Loudness

```csharp
public LoudnessStatus Loudness { get; init; }
```

Its loudness since the last reset, measured before ducking and the master stage.

**Type:** [`LoudnessStatus`](../../OneGame.Audio/OneGame.Audio/LoudnessStatus.md)

### <a id="m-name"></a>Name

```csharp
public string? Name { get; init; }
```

`music`, `sfx`, `ui` or null.

**Type:** `string?`
