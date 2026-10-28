[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# LimiterOptions struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LimiterOptions`](LimiterOptions.md)&gt;

```csharp
public readonly struct LimiterOptions : IEquatable<LimiterOptions>
```

The master limiter's settings.

## Constructors

| Member | Description |
| --- | --- |
| [`LimiterOptions()`](#m-ctor) | Creates a -1 dBFS ceiling with a 100 ms release. |

### <a id="m-ctor"></a>LimiterOptions()

```csharp
public LimiterOptions();
```

Creates a -1 dBFS ceiling with a 100 ms release.

## Properties

| Member | Description |
| --- | --- |
| [`CeilingDb`](#m-ceilingdb) | The highest sample magnitude allowed, in dBFS (-60 to 0). |
| [`ReleaseMilliseconds`](#m-releasemilliseconds) | Time constant of the gain recovering after a peak, in milliseconds (1 to 10000). |

### <a id="m-ceilingdb"></a>CeilingDb

```csharp
public float CeilingDb { get; init; }
```

The highest sample magnitude allowed, in dBFS (-60 to 0). Default -1.

**Type:** `float`

### <a id="m-releasemilliseconds"></a>ReleaseMilliseconds

```csharp
public float ReleaseMilliseconds { get; init; }
```

Time constant of the gain recovering after a peak, in milliseconds (1 to 10000). Default 100.

**Type:** `float`
