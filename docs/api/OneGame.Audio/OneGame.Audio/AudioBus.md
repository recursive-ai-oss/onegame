[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioBus struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioBus`](AudioBus.md)&gt;

```csharp
public readonly struct AudioBus : IEquatable<AudioBus>
```

A mixing bus. The service creates three named ones; further indices up to [`AudioServiceOptions.BusCount`](AudioServiceOptions.md#m-buscount) are free for games.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioBus(int)`](#m-ctor-int) | A mixing bus. |

### <a id="m-ctor-int"></a>AudioBus(int)

```csharp
public AudioBus(int Index);
```

A mixing bus. The service creates three named ones; further indices up to [`AudioServiceOptions.BusCount`](AudioServiceOptions.md#m-buscount) are free for games.

**Parameters**

- `Index` (`int`): The bus index.

## Properties

| Member | Description |
| --- | --- |
| [`Index`](#m-index) | The bus index. |
| [`Music`](#m-music) | Background music. |
| [`Sfx`](#m-sfx) | Sound effects. |
| [`Ui`](#m-ui) | User interface sounds. |

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

The bus index.

**Type:** `int`

### <a id="m-music"></a>Music

```csharp
public static AudioBus Music { get; }
```

Background music.

**Type:** [`AudioBus`](AudioBus.md)

### <a id="m-sfx"></a>Sfx

```csharp
public static AudioBus Sfx { get; }
```

Sound effects.

**Type:** [`AudioBus`](AudioBus.md)

### <a id="m-ui"></a>Ui

```csharp
public static AudioBus Ui { get; }
```

User interface sounds.

**Type:** [`AudioBus`](AudioBus.md)
